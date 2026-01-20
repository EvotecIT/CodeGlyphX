using System;
using System.Collections.Generic;
using System.Globalization;
using CodeGlyphX.Internal;

namespace CodeGlyphX.Pharmacode;

/// <summary>
/// Encodes Pharmacode (one-track) barcodes.
/// </summary>
public static class PharmacodeEncoder {
    internal const int MinValue = 3;
    internal const int MaxValue = 131070;
    internal const int MinBars = 2;
    internal const int MaxBars = 16;
    internal const int NarrowWidth = 1;
    internal const int WideWidth = 2;

    /// <summary>
    /// Encodes a Pharmacode (one-track) barcode value.
    /// </summary>
    public static Barcode1D Encode(string content) {
        if (content is null) throw new ArgumentNullException(nameof(content));
        content = content.Trim();
        if (content.Length == 0) throw new InvalidOperationException("Pharmacode content cannot be empty.");
        if (!int.TryParse(content, NumberStyles.None, CultureInfo.InvariantCulture, out var value)) {
            throw new InvalidOperationException("Pharmacode expects a numeric value.");
        }
        return Encode(value);
    }

    /// <summary>
    /// Encodes a Pharmacode (one-track) barcode value.
    /// </summary>
    public static Barcode1D Encode(int value) {
        if (value < MinValue || value > MaxValue) {
            throw new InvalidOperationException($"Pharmacode value must be in range {MinValue}-{MaxValue}.");
        }

        var barCount = GetBarCount(value);
        var baseValue = (1 << barCount) - 1;
        var remainder = value - baseValue;

        var segments = new List<BarSegment>(barCount * 2 - 1);
        for (var i = barCount - 1; i >= 0; i--) {
            var wide = ((remainder >> i) & 1) != 0;
            var width = wide ? WideWidth : NarrowWidth;
            AppendElements(segments, isBar: true, width);
            if (i > 0) {
                AppendElements(segments, isBar: false, NarrowWidth);
            }
        }

        return new Barcode1D(segments);
    }

    private static int GetBarCount(int value) {
        for (var bars = MinBars; bars <= MaxBars; bars++) {
            var maxForBars = (1 << (bars + 1)) - 2;
            if (value <= maxForBars) return bars;
        }
        return MaxBars;
    }

    private static void AppendElements(List<BarSegment> segments, bool isBar, int width) {
        for (var i = 0; i < width; i++) {
            BarcodeSegments.AppendBit(segments, isBar);
        }
    }
}
