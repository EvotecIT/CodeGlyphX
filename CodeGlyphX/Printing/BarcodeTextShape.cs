using System;
using System.Collections.Generic;

namespace CodeGlyphX.Printing;

/// <summary>An owned text outline and its advance, line box, baseline and actual ink extent.</summary>
public sealed class BarcodeTextShape {
    /// <summary>Gets the exact source text.</summary>
    public string Text { get; }
    /// <summary>Gets the horizontal advance, including spaces.</summary>
    public double AdvanceWidth { get; }
    /// <summary>Gets the font's line-box height.</summary>
    public double LineHeight { get; }
    /// <summary>Gets the baseline distance from the line-box top.</summary>
    public double BaselineOffset { get; }
    /// <summary>Gets the actual contour bounds, including overhang outside the line box.</summary>
    public BarcodeBounds InkBounds { get; }
    /// <summary>Gets the measured line box.</summary>
    public BarcodeBounds LogicalBounds => new(0, 0, AdvanceWidth, LineHeight);
    /// <summary>Gets closed fill contours, with holes distinguished by their winding.</summary>
    public IReadOnlyList<IReadOnlyList<BarcodePoint>> Contours { get; }
    /// <summary>Gets unsupported Unicode scalars reported by the provider.</summary>
    public IReadOnlyList<int> MissingGlyphs { get; }
    /// <summary>Gets whether the requested script exceeded the managed shaping support and used a fallback.</summary>
    public bool HasIncompleteShaping { get; }

    /// <summary>Snapshots provider output. Contours and points must be finite and bounded in number.</summary>
    public BarcodeTextShape(string text, double advanceWidth, double lineHeight, double baselineOffset,
        IEnumerable<IEnumerable<BarcodePoint>> contours, IEnumerable<int>? missingGlyphs = null, bool hasIncompleteShaping = false) {
        Text = text ?? throw new ArgumentNullException(nameof(text));
        if (!BarcodeBounds.Finite(advanceWidth) || advanceWidth < 0) throw new ArgumentOutOfRangeException(nameof(advanceWidth));
        if (!BarcodeBounds.Finite(lineHeight) || lineHeight <= 0) throw new ArgumentOutOfRangeException(nameof(lineHeight));
        if (!BarcodeBounds.Finite(baselineOffset)) throw new ArgumentOutOfRangeException(nameof(baselineOffset));
        if (text.Length > 32768) throw new ArgumentException("A barcode text run exceeds the supported length.", nameof(text));
        if (contours is null) throw new ArgumentNullException(nameof(contours));
        AdvanceWidth = advanceWidth; LineHeight = lineHeight; BaselineOffset = baselineOffset;
        HasIncompleteShaping = hasIncompleteShaping;
        var owned = new List<IReadOnlyList<BarcodePoint>>();
        var count = 0; var contourCount = 0;
        var left = double.PositiveInfinity; var top = double.PositiveInfinity;
        var right = double.NegativeInfinity; var bottom = double.NegativeInfinity;
        foreach (var contour in contours) {
            if (contour is null) throw new ArgumentException("A text contour cannot be null.", nameof(contours));
            if (++contourCount > 65536) throw new ArgumentException("The text outline has too many contours.", nameof(contours));
            var points = new List<BarcodePoint>();
            foreach (var point in contour) {
                if (++count > 1048576) throw new ArgumentException("The text outline has too many points.", nameof(contours));
                points.Add(point);
            }
            if (points.Count < 3) continue;
            foreach (var point in points) {
                left = Math.Min(left, point.X); top = Math.Min(top, point.Y);
                right = Math.Max(right, point.X); bottom = Math.Max(bottom, point.Y);
            }
            owned.Add(points.AsReadOnly());
        }
        Contours = owned.AsReadOnly();
        InkBounds = owned.Count == 0 ? default : new BarcodeBounds(left, top, right - left, bottom - top);
        var missing = new List<int>();
        var seen = new HashSet<int>();
        var missingCount = 0;
        if (missingGlyphs is not null) foreach (var scalar in missingGlyphs) {
            if (scalar < 0 || scalar > 0x10ffff || scalar is >= 0xd800 and <= 0xdfff) throw new ArgumentOutOfRangeException(nameof(missingGlyphs));
            if (++missingCount > 32768) throw new ArgumentException("Too many missing glyphs.", nameof(missingGlyphs));
            if (seen.Add(scalar)) missing.Add(scalar);
        }
        MissingGlyphs = missing.AsReadOnly();
    }
}
