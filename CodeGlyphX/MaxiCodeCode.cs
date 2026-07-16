namespace CodeGlyphX;

/// <summary>
/// Convenience facade for MaxiCode encoding and module decoding.
/// </summary>
public static class MaxiCodeCode {
    /// <summary>Encodes text using automatic Mode 2/3/4 selection.</summary>
    public static MaxiCodeSymbol Encode(string text) => MaxiCodeEncoder.EncodeText(text);
    /// <summary>Encodes text using the supplied options.</summary>
    public static MaxiCodeSymbol Encode(string text, MaxiCodeEncodingOptions options) => MaxiCodeEncoder.EncodeText(text, options);
    /// <summary>Attempts to decode an exact sampled MaxiCode module grid.</summary>
    public static bool TryDecode(BitMatrix modules, out MaxiCodeDecoded decoded) => MaxiCodeDecoder.TryDecodeDetailed(modules, out decoded);
}
