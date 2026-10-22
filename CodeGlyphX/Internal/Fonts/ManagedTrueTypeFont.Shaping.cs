// Adapted from OfficeIMO.Core, Copyright (c) 2022 Evotec.
// Licensed under the MIT license; see Licenses/OfficeIMO-MIT.txt.

using System;
using System.Collections.Generic;
using System.Threading;

namespace CodeGlyphX.Internal.Fonts;

internal sealed partial class ManagedTrueTypeFont {
    private byte[]? _fontDataForShaping;

    internal byte[] FontDataForShaping {
        get {
            byte[]? existing = _fontDataForShaping;
            if (existing != null) return existing;
            var snapshot = (byte[])_data.Clone();
            return System.Threading.Interlocked.CompareExchange(
                ref _fontDataForShaping,
                snapshot,
                null) ?? snapshot;
        }
    }

    internal int UnitsPerEm => _unitsPerEm;

    int IManagedFontProgram.UnitsPerEm => UnitsPerEm;

    internal bool TryGetGlyphMetrics(int scalar, out int glyphId, out int advanceWidth) {
        ushort mapped = MapGlyph(scalar);
        glyphId = mapped;
        advanceWidth = mapped == 0 ? 0 : AdvanceWidth(mapped);
        return mapped != 0;
    }

    internal int GetKerningAdjustment(
        int leftGlyphId,
        int rightGlyphId,
        int leftScalar,
        int rightScalar) {
        if ((uint)leftGlyphId > ushort.MaxValue || (uint)rightGlyphId > ushort.MaxValue) return 0;
        return Kerning((ushort)leftGlyphId, (ushort)rightGlyphId, leftScalar, rightScalar);
    }

    internal ManagedOpenTypeGlyphPositioning[] PositionGlyphRun(
        IReadOnlyList<int> glyphs,
        IReadOnlyList<int> scalars,
        System.Threading.CancellationToken cancellationToken = default) => _kerning.PositionRun(glyphs, scalars, cancellationToken);

    byte[] IManagedFontProgram.GetFontDataForShaping() => (byte[])FontDataForShaping.Clone();

    bool IManagedFontProgram.HasGlyphs(string text) => HasGlyphs(text);

    IReadOnlyList<double> IManagedFontProgram.MeasureTextElements(
        IReadOnlyList<string> elements,
        double fontSize) => MeasureTextElements(elements, fontSize);

    double IManagedFontProgram.LineSpacingRatio => LineSpacingRatio;

    bool IManagedFontProgram.TryGetGlyphMetrics(
        int scalar,
        out int glyphId,
        out int advanceWidth) => TryGetGlyphMetrics(scalar, out glyphId, out advanceWidth);

    /// <inheritdoc />
    public bool IsOpenTypeCff => false;

    /// <inheritdoc />
    public bool ProvidesComplexTextLayout => false;

    double IManagedFontProgram.MeasureShapedText(
        string text,
        ManagedTextShapingResult result,
        double fontSize) {
        ManagedTrueTypeVariations.WorkBudget? workBudget = _variations?.CreateWorkBudget();
        return CreateShapedTextRun(text, result, workBudget, CancellationToken.None).Measure(fontSize);
    }

    List<List<ManagedPoint>> IManagedFontProgram.GetShapedTextContours(
        string text,
        ManagedTextShapingResult result,
        double x,
        double y,
        double fontSize) {
        ManagedTrueTypeVariations.WorkBudget? workBudget = _variations?.CreateWorkBudget();
        return CreateShapedTextRun(text, result, workBudget, CancellationToken.None).GetContours(
            x,
            y,
            fontSize,
            variationWorkBudget: workBudget);
    }

    List<List<ManagedPoint>> IManagedBoundedFontProgram.GetShapedTextContoursBounded(
        string text,
        ManagedTextShapingResult result,
        double x,
        double y,
        double fontSize,
        int maximumPointCount,
        CancellationToken cancellationToken) {
        ManagedTrueTypeVariations.WorkBudget? workBudget = _variations?.CreateWorkBudget();
        return CreateShapedTextRun(text, result, workBudget, cancellationToken).GetContours(
            x,
            y,
            fontSize,
            maximumPointCount,
            cancellationToken,
            workBudget);
    }

    internal ShapedTextRun CreateShapedTextRun(
        string text,
        ManagedTextShapingResult result,
        ManagedTrueTypeVariations.WorkBudget? variationWorkBudget = null,
        CancellationToken cancellationToken = default) {
        if (text == null) throw new ArgumentNullException(nameof(text));
        if (result == null) throw new ArgumentNullException(nameof(result));
        if (text.Length > 0 && result.Glyphs.Count == 0) {
            throw new ArgumentException(
                "Drawing text shaping provider returned no glyphs for non-empty text.",
                nameof(result));
        }
        variationWorkBudget ??= _variations?.CreateWorkBudget();

        var glyphs = new PositionedGlyph[result.Glyphs.Count];
        var indexes = new int[result.Glyphs.Count];
        long totalAdvance = 0;
        for (int index = 0; index < result.Glyphs.Count; index++) {
            ManagedShapedGlyph glyph = result.Glyphs[index];
            if (glyph.GlyphId <= 0 || glyph.GlyphId >= _numGlyphs) {
                throw new ArgumentException(
                    "Drawing text shaping provider returned glyph id " +
                    glyph.GlyphId.ToString(System.Globalization.CultureInfo.InvariantCulture) +
                    ", which is outside the selected font glyph range.",
                    nameof(result));
            }
            if (glyph.TextIndex < 0 ||
                glyph.TextIndex > text.Length ||
                glyph.UnicodeText.Length > text.Length - glyph.TextIndex ||
                !string.Equals(
                    text.Substring(glyph.TextIndex, glyph.UnicodeText.Length),
                    glyph.UnicodeText,
                    StringComparison.Ordinal)) {
                throw new ArgumentException(
                    "Drawing text shaping provider returned a Unicode mapping outside the source text.",
                    nameof(result));
            }

            indexes[index] = glyph.TextIndex;
            glyphs[index] = new PositionedGlyph(
                (ushort)glyph.GlyphId,
                checked(
                    (glyph.AdvanceWidth ?? AdvanceWidth(
                        (ushort)glyph.GlyphId,
                        variationWorkBudget,
                        cancellationToken)) +
                    result.GetAdvanceAdjustment(index)),
                glyph.AdvanceHeight ?? 0,
                glyph.OffsetX,
                glyph.OffsetY);
            totalAdvance += glyphs[index].AdvanceWidth;
        }
        bool[]? boundaries = TrackingBoundaries(text, indexes, totalAdvance < 0);
        if (boundaries != null) {
            for (int index = 0; index < glyphs.Length; index++) glyphs[index].TrackingBoundary = boundaries[index];
        }

        return new ShapedTextRun(this, glyphs, result.Direction);
    }

    internal sealed class ShapedTextRun {
        private readonly ManagedTrueTypeFont _font;
        internal readonly PositionedGlyph[] _glyphs;
        private readonly long _advanceWidth;
        private readonly long _advanceHeight;
        private readonly int _trackingGlyphCount;

        internal ShapedTextRun(ManagedTrueTypeFont font, PositionedGlyph[] glyphs, ManagedTextDirection direction) {
            _font = font;
            _glyphs = glyphs;
            Direction = direction;
            long width = 0L;
            long height = 0L;
            for (int index = 0; index < glyphs.Length; index++) {
                width = checked(width + glyphs[index].AdvanceWidth);
                height = checked(height + glyphs[index].AdvanceHeight);
                if (glyphs[index].TrackingBoundary) _trackingGlyphCount++;
            }
            _advanceWidth = width;
            _advanceHeight = height;
        }

        internal ManagedTextDirection Direction { get; }

        internal double Measure(double fontSize) => Direction == ManagedTextDirection.TopToBottom
            ? Math.Abs(_advanceHeight * _font.ScaleFor(fontSize))
            : Math.Abs(_advanceWidth * _font.ScaleFor(fontSize) + SignedTracking(fontSize) * _trackingGlyphCount);

        private double SignedTracking(double fontSize) =>
            (_advanceWidth < 0L ? -1D : 1D) * _font.HorizontalTracking(fontSize);

        internal List<List<ManagedPoint>> GetContours(
            double x,
            double y,
            double fontSize,
            int maximumPointCount = int.MaxValue,
            CancellationToken cancellationToken = default,
            ManagedTrueTypeVariations.WorkBudget? variationWorkBudget = null) {
            if (maximumPointCount <= 0) throw new ArgumentOutOfRangeException(nameof(maximumPointCount));
            var contours = new List<List<ManagedPoint>>();
            var readBudget = new GlyphReadBudget();
            double scale = _font.ScaleFor(fontSize);
            if (Direction == ManagedTextDirection.TopToBottom) {
                double cursorY = y;
                int verticalPointCount = 0;
                variationWorkBudget ??= _font._variations?.CreateWorkBudget();
                for (int index = 0; index < _glyphs.Length; index++) {
                    cancellationToken.ThrowIfCancellationRequested();
                    PositionedGlyph glyph = _glyphs[index];
                    double glyphX = x + (glyph.OffsetX * scale);
                    double glyphBaseline = cursorY - (glyph.OffsetY * scale);
                    List<List<ManagedPoint>> glyphContours = _font.ReadGlyphContours(
                        glyph.GlyphId,
                        new FontTransform(scale, 0D, 0D, -scale, glyphX, glyphBaseline),
                        0,
                        variationWorkBudget,
                        maximumPointCount,
                        ref verticalPointCount,
                        cancellationToken,
                        readBudget,
                        attachmentPoints: null);
                    contours.AddRange(glyphContours);
                    cursorY -= glyph.AdvanceHeight * scale;
                }
                return contours;
            }
            bool negativeDirection = _advanceWidth < 0L;
            double tracking = SignedTracking(fontSize);
            double cursor = negativeDirection ? x - (_advanceWidth * scale + tracking * _trackingGlyphCount) : x;
            double baseline = y + (_font._ascender * scale);
            int pointCount = 0;
            variationWorkBudget ??= _font._variations?.CreateWorkBudget();
            for (int index = 0; index < _glyphs.Length; index++) {
                cancellationToken.ThrowIfCancellationRequested();
                PositionedGlyph glyph = _glyphs[index];
                if (negativeDirection) {
                    cursor += glyph.AdvanceWidth * scale + (glyph.TrackingBoundary ? tracking : 0D);
                }
                double glyphX = cursor + (glyph.OffsetX * scale);
                double glyphBaseline = baseline - (glyph.OffsetY * scale);
                List<List<ManagedPoint>> glyphContours = _font.ReadGlyphContours(
                    glyph.GlyphId,
                    new FontTransform(scale, 0D, 0D, -scale, glyphX, glyphBaseline),
                    0,
                    variationWorkBudget,
                    maximumPointCount,
                    ref pointCount,
                    cancellationToken,
                    readBudget,
                    attachmentPoints: null);
                contours.AddRange(glyphContours);
                if (!negativeDirection) {
                    cursor += glyph.AdvanceWidth * scale + (glyph.TrackingBoundary ? tracking : 0D);
                }
            }
            return contours;
        }
    }

    internal struct PositionedGlyph {
        internal PositionedGlyph(ushort glyphId, int advanceWidth, int advanceHeight, int offsetX, int offsetY) {
            TrackingBoundary = true;
            GlyphId = glyphId;
            AdvanceWidth = advanceWidth;
            AdvanceHeight = advanceHeight;
            OffsetX = offsetX;
            OffsetY = offsetY;
        }

        internal bool TrackingBoundary { get; set; }
        internal ushort GlyphId { get; }
        internal int AdvanceWidth { get; }
        internal int AdvanceHeight { get; }
        internal int OffsetX { get; }
        internal int OffsetY { get; }
    }
}
