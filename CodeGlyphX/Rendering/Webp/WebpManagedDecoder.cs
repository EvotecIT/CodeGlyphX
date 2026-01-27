using System;

namespace CodeGlyphX.Rendering.Webp;

/// <summary>
/// Managed WebP decoder entry point (work in progress).
/// </summary>
internal static class WebpManagedDecoder {
    /// <summary>
    /// Attempts to decode WebP to RGBA32 using a managed implementation.
    /// </summary>
    /// <remarks>
    /// This currently returns <c>false</c> until the managed decoder is implemented.
    /// The call site prefers this path before falling back to native decode.
    /// </remarks>
    public static bool TryDecodeRgba32(ReadOnlySpan<byte> data, out byte[] rgba, out int width, out int height) {
        rgba = Array.Empty<byte>();
        width = 0;
        height = 0;
        return false;
    }
}

