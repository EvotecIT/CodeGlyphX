using System;
using CodeGlyphX.Internal;

namespace CodeGlyphX.Code25;

/// <summary>
/// Encodes Matrix (Standard) 2 of 5 barcodes.
/// </summary>
public static class Matrix2of5Encoder {
    private static readonly int[] StartBars = { 3, 1, 1, 1, 1 };
    private static readonly int[] StopBars = { 3, 1, 1, 1, 1 };

    /// <summary>
    /// Encodes a Matrix 2 of 5 barcode. When <paramref name="includeChecksum"/> is true,
    /// a Mod-10 check digit is appended.
    /// </summary>
    public static Barcode1D Encode(string content, bool includeChecksum = false) {
        return Discrete2of5Encoder.Encode(content, includeChecksum, StartBars, StopBars);
    }
}
