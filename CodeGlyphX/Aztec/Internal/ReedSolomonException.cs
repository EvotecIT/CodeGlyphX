using System;

namespace CodeGlyphX.Aztec.Internal;

internal sealed class ReedSolomonException : Exception {
    public ReedSolomonException(string message) : base(message) { }
}
