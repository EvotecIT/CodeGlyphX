using System;

namespace CodeGlyphX.Internal.Fonts;

// Resource exhaustion must escape provider-decline catches. BarcodeFont translates it
// to a public invalid-font failure instead of silently painting a nominal fallback.
internal sealed class ManagedFontResourceLimitException : Exception {
    internal ManagedFontResourceLimitException(string message) : base(message) { }
}
