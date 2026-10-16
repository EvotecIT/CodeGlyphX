namespace CodeGlyphX.Code25;

/// <summary>Encodes Datalogic 2 of 5 barcodes.</summary>
public static class Datalogic2Of5Encoder {
    /// <summary>Encodes decimal digits, optionally appending a Mod-10 check digit.</summary>
    public static Barcode1D Encode(string content, bool includeChecksum = false) =>
        Discrete2Of5Encoder.Encode(content, includeChecksum, SymbolFormat.Datalogic2Of5);
}
