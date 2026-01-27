using System;

namespace CodeGlyphX.Rendering.Webp;

/// <summary>
/// Managed WebP writer scaffold (VP8L literal-only subset).
/// </summary>
public static class WebpWriter {
    /// <summary>
    /// Encodes an RGBA32 buffer as WebP lossless (managed VP8L literal-only subset).
    /// </summary>
    /// <remarks>
    /// Current limitations:
    /// - No transforms or LZ77/back-references.
    /// - Up to 2 unique values per channel (suited to binary/QR-style images).
    /// </remarks>
    public static byte[] WriteRgba32(int width, int height, ReadOnlySpan<byte> rgba, int stride) {
        if (WebpVp8lEncoder.TryEncodeLiteralRgba32(rgba, width, height, stride, out var webp, out var reason)) {
            return webp;
        }

        throw new NotSupportedException($"Managed WebP encode is limited to a minimal VP8L subset: {reason}");
    }

    /// <summary>
    /// Encodes an RGBA32 buffer as WebP lossless (managed VP8L literal-only subset).
    /// </summary>
    public static byte[] WriteRgba32(int width, int height, byte[] rgba, int stride) {
        if (rgba is null) throw new ArgumentNullException(nameof(rgba));
        return WriteRgba32(width, height, rgba.AsSpan(), stride);
    }
}

