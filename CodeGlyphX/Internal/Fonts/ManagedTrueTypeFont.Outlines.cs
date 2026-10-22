// Adapted from OfficeIMO.Core, Copyright (c) 2022 Evotec.
// Licensed under the MIT license; see Licenses/OfficeIMO-MIT.txt.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace CodeGlyphX.Internal.Fonts;

internal sealed partial class ManagedTrueTypeFont {
    private List<List<ManagedPoint>> ReadGlyphContours(
        ushort glyph,
        FontTransform transform,
        int depth,
        ManagedTrueTypeVariations.WorkBudget? variationWorkBudget,
        int maximumPointCount,
        ref int expandedPointCount,
        CancellationToken cancellationToken,
        GlyphReadBudget readBudget,
        List<ManagedPoint>? attachmentPoints) {
        cancellationToken.ThrowIfCancellationRequested();
        readBudget.Consume(1);
        var contours = new List<List<ManagedPoint>>();
        if (glyph >= _numGlyphs || depth > 32) throw new InvalidDataException("Invalid or excessively nested composite glyph.");
        var glyphStart = GlyphOffset(glyph);
        var glyphEnd = GlyphOffset((ushort)(glyph + 1));
        if (glyphStart == glyphEnd) return contours;
        var offset = _glyf + glyphStart;
        var glyphLimit = _glyf + glyphEnd;
        GlyphRange(offset, 10, glyphLimit);
        var contourCount = ReadInt16(_data, offset);
        if (contourCount < 0) {
            if (contourCount != -1) throw new InvalidDataException("Invalid composite glyph header.");
            ReadCompositeGlyphContours(
                glyph,
                offset,
                glyphLimit,
                transform,
                depth,
                contours,
                variationWorkBudget,
                maximumPointCount,
                ref expandedPointCount,
                cancellationToken,
                readBudget,
                attachmentPoints);
            return contours;
        }

        if (contourCount <= 0) return contours;

        GlyphRange(offset + 10, contourCount * 2 + 2, glyphLimit);
        var endPts = new ushort[contourCount];
        for (var i = 0; i < contourCount; i++) {
            endPts[i] = ReadUInt16(_data, offset + 10 + i * 2);
            if (i > 0 && endPts[i] <= endPts[i - 1]) throw new InvalidDataException("Invalid glyph contour endpoints.");
        }
        var rawPointCount = endPts[contourCount - 1] + 1;
        readBudget.Consume(rawPointCount);
        if (rawPointCount > maximumPointCount - expandedPointCount) {
            throw new InvalidOperationException("Font outline expansion exceeded the configured point budget.");
        }
        var instructionLengthOffset = offset + 10 + contourCount * 2;
        var instructionLength = ReadUInt16(_data, instructionLengthOffset);
        GlyphRange(instructionLengthOffset + 2, instructionLength, glyphLimit);
        var p = instructionLengthOffset + 2 + instructionLength;
        var flags = new byte[rawPointCount];
        for (var i = 0; i < rawPointCount; i++) {
            GlyphRange(p, 1, glyphLimit);
            var flag = _data[p++];
            flags[i] = flag;
            if ((flag & 8) == 0) continue;
            GlyphRange(p, 1, glyphLimit);
            var repeat = _data[p++];
            if (repeat > rawPointCount - i - 1) throw new InvalidDataException("Invalid glyph flag repetition.");
            for (var r = 0; r < repeat; r++) flags[++i] = flag;
        }

        var xs = new short[rawPointCount];
        DecodeCoordinates(_data, flags, xs, ref p, glyphLimit, true);
        var ys = new short[rawPointCount];
        DecodeCoordinates(_data, flags, ys, ref p, glyphLimit, false);
        double[] variedXs;
        double[] variedYs;
        if (_variations != null) {
            variedXs = new double[rawPointCount];
            variedYs = new double[rawPointCount];
            for (int index = 0; index < rawPointCount; index++) {
                variedXs[index] = xs[index];
                variedYs[index] = ys[index];
            }
            _variations.ApplySimpleGlyph(
                glyph,
                variedXs,
                variedYs,
                endPts,
                variationWorkBudget ?? _variations.CreateWorkBudget(),
                cancellationToken);
        } else {
            variedXs = new double[rawPointCount];
            variedYs = new double[rawPointCount];
            for (int index = 0; index < rawPointCount; index++) {
                variedXs[index] = xs[index];
                variedYs[index] = ys[index];
            }
        }

        if (attachmentPoints != null) {
            for (var index = 0; index < rawPointCount; index++) {
                attachmentPoints.Add(transform.Apply(variedXs[index], variedYs[index]));
            }
        }

        var start = 0;
        for (var c = 0; c < contourCount; c++) {
            var end = endPts[c];
            var points = new List<GlyphPoint>();
            for (var i = start; i <= end; i++) {
                var point = transform.Apply(variedXs[i], variedYs[i]);
                points.Add(new GlyphPoint(point.X, point.Y, (flags[i] & 1) != 0));
            }

            AddFlattenedContour(points, contours, ref expandedPointCount, maximumPointCount);
            start = end + 1;
        }

        return contours;
    }

    private void ReadCompositeGlyphContours(
        ushort glyph,
        int glyphOffset,
        int glyphLimit,
        FontTransform transform,
        int depth,
        List<List<ManagedPoint>> contours,
        ManagedTrueTypeVariations.WorkBudget? variationWorkBudget,
        int maximumPointCount,
        ref int expandedPointCount,
        CancellationToken cancellationToken,
        GlyphReadBudget readBudget,
        List<ManagedPoint>? attachmentPoints) {
        const ushort argWords = 1;
        const ushort argsAreXy = 2;
        const ushort haveScale = 8;
        const ushort moreComponents = 32;
        const ushort haveXyScale = 64;
        const ushort haveTwoByTwo = 128;

        var p = glyphOffset + 10;
        ushort flags;
        var components = new List<GlyphComponent>();
        do {
            cancellationToken.ThrowIfCancellationRequested();
            readBudget.Consume(1);
            GlyphRange(p, 4, glyphLimit);
            flags = ReadUInt16(_data, p);
            if (ManagedOpenTypeCompositeGlyph.HasConflictingTransformFlags(flags) ||
                ManagedOpenTypeCompositeGlyph.HasConflictingOffsetFlags(flags)) throw new InvalidDataException("Conflicting composite glyph flags.");
            var componentGlyph = ReadUInt16(_data, p + 2);
            p += 4;
            int arg1;
            int arg2;
            if ((flags & argWords) != 0) {
                GlyphRange(p, 4, glyphLimit);
                if ((flags & argsAreXy) != 0) {
                    arg1 = ReadInt16(_data, p);
                    arg2 = ReadInt16(_data, p + 2);
                } else {
                    arg1 = ReadUInt16(_data, p);
                    arg2 = ReadUInt16(_data, p + 2);
                }
                p += 4;
            } else {
                GlyphRange(p, 2, glyphLimit);
                if ((flags & argsAreXy) != 0) {
                    arg1 = (sbyte)_data[p];
                    arg2 = (sbyte)_data[p + 1];
                } else {
                    arg1 = _data[p];
                    arg2 = _data[p + 1];
                }
                p += 2;
            }

            var dx = (flags & argsAreXy) != 0 ? arg1 : 0;
            var dy = (flags & argsAreXy) != 0 ? arg2 : 0;
            int parentPointIndex = (flags & argsAreXy) == 0 ? arg1 : -1;
            int componentPointIndex = (flags & argsAreXy) == 0 ? arg2 : -1;
            var a = 1.0;
            var b = 0.0;
            var c = 0.0;
            var d = 1.0;
            if ((flags & haveScale) != 0) {
                GlyphRange(p, 2, glyphLimit);
                a = d = ReadF2Dot14(_data, p);
                p += 2;
            } else if ((flags & haveXyScale) != 0) {
                GlyphRange(p, 4, glyphLimit);
                a = ReadF2Dot14(_data, p);
                d = ReadF2Dot14(_data, p + 2);
                p += 4;
            } else if ((flags & haveTwoByTwo) != 0) {
                GlyphRange(p, 8, glyphLimit);
                a = ReadF2Dot14(_data, p);
                b = ReadF2Dot14(_data, p + 2);
                c = ReadF2Dot14(_data, p + 4);
                d = ReadF2Dot14(_data, p + 6);
                p += 8;
            }

            components.Add(new GlyphComponent(
                componentGlyph,
                a,
                b,
                c,
                d,
                dx,
                dy,
                flags,
                (flags & argsAreXy) != 0,
                parentPointIndex,
                componentPointIndex));
        } while ((flags & moreComponents) != 0);
        if ((flags & 0x100) != 0) {
            GlyphRange(p, 2, glyphLimit);
            GlyphRange(p + 2, ReadUInt16(_data, p), glyphLimit);
        }

        if (_variations != null && components.Count > 0) {
            var xs = new double[components.Count];
            var ys = new double[components.Count];
            for (int index = 0; index < components.Count; index++) {
                xs[index] = components[index].Dx;
                ys[index] = components[index].Dy;
            }
            _variations.ApplyCompositeGlyph(
                glyph,
                xs,
                ys,
                variationWorkBudget ?? _variations.CreateWorkBudget(),
                cancellationToken);
            for (int index = 0; index < components.Count; index++) {
                components[index] = components[index].WithOffset(xs[index], ys[index]);
            }
        }

        var compositeContours = new List<List<ManagedPoint>>();
        var compositePoints = new List<ManagedPoint>();
        foreach (GlyphComponent component in components) {
            var componentPoints = new List<ManagedPoint>();
            List<List<ManagedPoint>> componentContours = ReadGlyphContours(
                component.Glyph,
                transform.Compose(component.A, component.B, component.C, component.D, 0, 0),
                depth + 1,
                variationWorkBudget,
                maximumPointCount,
                ref expandedPointCount,
                cancellationToken,
                readBudget,
                componentPoints);

            ManagedPoint translation;
            if (component.UsesXyOffsets) {
                ManagedPoint offset = ManagedOpenTypeCompositeGlyph.ResolveXyOffset(
                    component.Flags,
                    component.A,
                    component.B,
                    component.C,
                    component.D,
                    component.Dx,
                    component.Dy);
                translation = transform.ApplyVector(offset.X, offset.Y);
            } else {
                if (!ManagedOpenTypeCompositeGlyph.TryResolvePointAttachment(
                    compositePoints,
                    component.ParentPointIndex,
                    componentPoints,
                    component.ComponentPointIndex,
                    out translation)) {
                    throw new InvalidDataException("Invalid composite glyph point attachment.");
                }

                ManagedPoint variationTranslation = transform.ApplyVector(component.Dx, component.Dy);
                translation = ManagedOpenTypeCompositeGlyph.ApplyComponentVariation(translation, variationTranslation);
            }

            TranslateContours(componentContours, translation);
            TranslatePoints(componentPoints, translation);
            compositeContours.AddRange(componentContours);
            compositePoints.AddRange(componentPoints);
        }

        contours.AddRange(compositeContours);
        attachmentPoints?.AddRange(compositePoints);
    }

    private static void TranslateContours(List<List<ManagedPoint>> contours, ManagedPoint translation) {
        foreach (List<ManagedPoint> contour in contours) TranslatePoints(contour, translation);
    }

    private static void TranslatePoints(List<ManagedPoint> points, ManagedPoint translation) {
        for (int index = 0; index < points.Count; index++) {
            ManagedPoint point = points[index];
            points[index] = new ManagedPoint(point.X + translation.X, point.Y + translation.Y);
        }
    }

    private int GlyphOffset(ushort glyph) {
        if (_indexToLocFormat == 0) return ReadUInt16(_data, _loca + glyph * 2) * 2;
        return CheckedOffset(_data, ReadUInt32(_data, _loca + glyph * 4));
    }

    private static void DecodeCoordinates(byte[] data, byte[] flags, short[] values, ref int p, int glyphLimit, bool xAxis) {
        var shortFlag = xAxis ? 2 : 4;
        var sameOrPositiveFlag = xAxis ? 16 : 32;
        var current = 0;
        for (var i = 0; i < flags.Length; i++) {
            var flag = flags[i];
            int delta;
            if ((flag & shortFlag) != 0) {
                GlyphRange(p, 1, glyphLimit);
                delta = data[p++];
                if ((flag & sameOrPositiveFlag) == 0) delta = -delta;
            } else if ((flag & sameOrPositiveFlag) != 0) {
                delta = 0;
            } else {
                GlyphRange(p, 2, glyphLimit);
                delta = ReadInt16(data, p);
                p += 2;
            }

            current += delta;
            if (current < short.MinValue || current > short.MaxValue) throw new InvalidDataException("A glyph coordinate exceeds the TrueType range.");
            values[i] = (short)current;
        }
    }

    private static void GlyphRange(int offset, int length, int glyphLimit) {
        if (offset < 0 || length < 0 || offset > glyphLimit - length) throw new InvalidDataException("Truncated glyph outline.");
    }

    private sealed class GlyphReadBudget {
        private int _remaining = 1048576;
        internal void Consume(int count) {
            if (count > _remaining) throw new InvalidDataException("Font outline decoding exceeded its work budget.");
            _remaining -= count;
        }
    }

    private static void AddFlattenedContour(
        List<GlyphPoint> source,
        List<List<ManagedPoint>> contours,
        ref int expandedPointCount,
        int maximumPointCount) {
        if (source.Count == 0) return;
        var result = new List<ManagedPoint>();
        var last = source[source.Count - 1];
        var first = source[0];
        var current = first.OnCurve ? first : last.OnCurve ? last : Mid(last, first);
        AddBoundedOutlinePoint(result, current.Point, ref expandedPointCount, maximumPointCount);
        var index = first.OnCurve ? 1 : 0;

        while (index < source.Count) {
            var point = source[index % source.Count];
            if (point.OnCurve) {
                AddBoundedOutlinePoint(result, point.Point, ref expandedPointCount, maximumPointCount);
                current = point;
                index++;
                continue;
            }

            var next = source[(index + 1) % source.Count];
            var end = next.OnCurve ? next : Mid(point, next);
            FlattenQuadratic(current, point, end, result, ref expandedPointCount, maximumPointCount);
            current = end;
            index += next.OnCurve ? 2 : 1;
        }

        if (result.Count >= 3) contours.Add(result);
    }

    private static void FlattenQuadratic(
        GlyphPoint start,
        GlyphPoint control,
        GlyphPoint end,
        List<ManagedPoint> output,
        ref int expandedPointCount,
        int maximumPointCount) {
        // Simple and composite glyph control points already carry the output transform.
        // Flatten in those coordinates so small text and enlarged outlines share a pixel tolerance.
        var steps = ManagedCurveFlattening.QuadraticSegments(new ManagedPoint(start.X, start.Y),
            new ManagedPoint(control.X, control.Y), new ManagedPoint(end.X, end.Y), 1D);
        for (var i = 1; i <= steps; i++) {
            var t = i / (double)steps;
            var mt = 1 - t;
            AddBoundedOutlinePoint(
                output,
                new ManagedPoint(mt * mt * start.X + 2 * mt * t * control.X + t * t * end.X, mt * mt * start.Y + 2 * mt * t * control.Y + t * t * end.Y),
                ref expandedPointCount,
                maximumPointCount);
        }
    }

    private static void AddBoundedOutlinePoint(
        ICollection<ManagedPoint> target,
        ManagedPoint point,
        ref int expandedPointCount,
        int maximumPointCount) {
        if (expandedPointCount >= maximumPointCount) {
            throw new InvalidOperationException("Font outline expansion exceeded the configured point budget.");
        }
        expandedPointCount++;
        target.Add(point);
    }

    private static GlyphPoint Mid(GlyphPoint left, GlyphPoint right) => new((left.X + right.X) / 2.0, (left.Y + right.Y) / 2.0, true);

    private readonly struct FontTransform {
        public FontTransform(double xx, double xy, double yx, double yy, double dx, double dy) {
            Xx = xx;
            Xy = xy;
            Yx = yx;
            Yy = yy;
            Dx = dx;
            Dy = dy;
        }

        private double Xx { get; }
        private double Xy { get; }
        private double Yx { get; }
        private double Yy { get; }
        private double Dx { get; }
        private double Dy { get; }

        public ManagedPoint Apply(double x, double y) => new(Dx + Xx * x + Xy * y, Dy + Yx * x + Yy * y);

        public ManagedPoint ApplyVector(double x, double y) => new(Xx * x + Xy * y, Yx * x + Yy * y);

        public FontTransform Compose(double xx, double xy, double yx, double yy, double dx, double dy) {
            return new FontTransform(
                Xx * xx + Xy * yx,
                Xx * xy + Xy * yy,
                Yx * xx + Yy * yx,
                Yx * xy + Yy * yy,
                Dx + Xx * dx + Xy * dy,
                Dy + Yx * dx + Yy * dy);
        }
    }

    private readonly struct GlyphPoint {
        public GlyphPoint(double x, double y, bool onCurve) {
            X = x;
            Y = y;
            OnCurve = onCurve;
            Point = new ManagedPoint(x, y);
        }

        public double X { get; }
        public double Y { get; }
        public bool OnCurve { get; }
        public ManagedPoint Point { get; }
    }

    private readonly struct GlyphComponent {
        internal GlyphComponent(
            ushort glyph,
            double a,
            double b,
            double c,
            double d,
            double dx,
            double dy,
            ushort flags,
            bool usesXyOffsets,
            int parentPointIndex,
            int componentPointIndex) {
            Glyph = glyph;
            A = a;
            B = b;
            C = c;
            D = d;
            Dx = dx;
            Dy = dy;
            Flags = flags;
            UsesXyOffsets = usesXyOffsets;
            ParentPointIndex = parentPointIndex;
            ComponentPointIndex = componentPointIndex;
        }

        internal ushort Glyph { get; }
        internal double A { get; }
        internal double B { get; }
        internal double C { get; }
        internal double D { get; }
        internal double Dx { get; }
        internal double Dy { get; }
        internal ushort Flags { get; }
        internal bool UsesXyOffsets { get; }
        internal int ParentPointIndex { get; }
        internal int ComponentPointIndex { get; }
        internal GlyphComponent WithOffset(double dx, double dy) =>
            new(Glyph, A, B, C, D, dx, dy, Flags, UsesXyOffsets, ParentPointIndex, ComponentPointIndex);
    }
}
