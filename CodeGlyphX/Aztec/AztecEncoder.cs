using System;

namespace CodeGlyphX.Aztec;

internal static class AztecEncoder {
    public static BitMatrix Encode(string text, AztecEncodeOptions? options = null) {
        if (text is null) throw new ArgumentNullException(nameof(text));
        throw new NotSupportedException("Aztec encoding is not implemented yet.");
    }
}
