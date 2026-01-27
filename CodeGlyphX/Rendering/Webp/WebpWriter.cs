using System;

namespace CodeGlyphX.Rendering.Webp;

/// <summary>
/// Managed WebP writer (VP8L lossless subset).
/// </summary>
public static class WebpWriter {
    /// <summary>
    /// Encodes an RGBA32 buffer as WebP lossless (managed VP8L subset).
    /// </summary>
    /// <remarks>
    /// Current limitations:
    /// - Lossless only (VP8L). No lossy VP8 or animation.
    /// - Single prefix-code group; no entropy tiling.
    /// - Limited LZ77/back-reference search; favors short distances.
    /// - Color indexing is limited to palettes up to 16 colors.
    /// - Metadata chunks (VP8X/ICCP/EXIF/XMP) are not written.
    /// </remarks>
    public static byte[] WriteRgba32(int width, int height, ReadOnlySpan<byte> rgba, int stride) {
        if (WebpVp8lEncoder.TryEncodeLiteralRgba32(rgba, width, height, stride, out var webp, out var reason)) {
            return webp;
        }

        throw new NotSupportedException($"Managed WebP encode is limited to a minimal VP8L subset: {reason}");
    }

    /// <summary>
    /// Encodes an RGBA32 buffer as WebP lossless (managed VP8L subset).
    /// </summary>
    public static byte[] WriteRgba32(int width, int height, byte[] rgba, int stride) {
        if (rgba is null) throw new ArgumentNullException(nameof(rgba));
        return WriteRgba32(width, height, rgba.AsSpan(), stride);
    }
}
