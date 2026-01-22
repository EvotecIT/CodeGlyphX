namespace CodeGlyphX.Code25;

/// <summary>
/// Encodes IATA 2 of 5 barcodes.
/// </summary>
public static class Iata2Of5Encoder {
    private static readonly int[] StartBars = { 1, 1, 1 };
    private static readonly int[] StopBars = { 3, 1, 1 };

    /// <summary>
    /// Encodes an IATA 2 of 5 barcode. When <paramref name="includeChecksum"/> is true,
    /// a Mod-10 check digit is appended.
    /// </summary>
    public static Barcode1D Encode(string content, bool includeChecksum = false) {
        return Discrete2Of5Encoder.Encode(content, includeChecksum, StartBars, StopBars);
    }
}
