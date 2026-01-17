using System;
using CodeGlyphX.Aztec;

namespace CodeGlyphX;

/// <summary>
/// Aztec code helpers (scaffolded).
/// </summary>
public static class AztecCode {
    /// <summary>
    /// Encodes a text payload as Aztec.
    /// </summary>
    public static BitMatrix Encode(string text, AztecEncodeOptions? options = null) {
        return AztecEncoder.Encode(text, options);
    }

    /// <summary>
    /// Attempts to decode an Aztec symbol from a module matrix.
    /// </summary>
    public static bool TryDecode(BitMatrix modules, out string value) {
        return AztecDecoder.TryDecode(modules, out value);
    }
}
