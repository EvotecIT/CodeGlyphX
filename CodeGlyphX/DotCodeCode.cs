namespace CodeGlyphX;

/// <summary>Convenience facade for DotCode encoding and exact-grid decoding.</summary>
public static class DotCodeCode {
    /// <summary>Encodes text as DotCode modules.</summary>
    public static DotCodeSymbol Encode(string text) => DotCodeEncoder.EncodeText(text);
    /// <summary>Encodes text as DotCode modules with explicit options.</summary>
    public static DotCodeSymbol Encode(string text, DotCodeEncodingOptions options) => DotCodeEncoder.EncodeText(text, options);
    /// <summary>Attempts to decode exact sampled DotCode modules.</summary>
    public static bool TryDecode(BitMatrix modules, out string text) => DotCodeDecoder.TryDecode(modules, out text);
}
