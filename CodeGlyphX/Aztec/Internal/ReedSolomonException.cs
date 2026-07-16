using System;

namespace CodeGlyphX.Internal.ReedSolomon;

internal sealed class ReedSolomonException : Exception {
    public ReedSolomonException(string message) : base(message) { }
}
