namespace CodeGlyphX;

/// <summary>Convenience facade for Han Xin Code encoding and exact-grid decoding.</summary>
public static class HanXinCode {
    /// <summary>Encodes text as Han Xin Code modules.</summary>
    public static BitMatrix Encode(string text) => HanXinEncoder.EncodeText(text).Modules;
    /// <summary>Encodes text with explicit options.</summary>
    public static BitMatrix Encode(string text, HanXinEncodingOptions options) => HanXinEncoder.EncodeText(text, options).Modules;
    /// <summary>Attempts to decode exact sampled modules.</summary>
    public static bool TryDecode(BitMatrix modules, out string text) => HanXinDecoder.TryDecode(modules, out text);
}
