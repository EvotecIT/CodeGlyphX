using System;
using System.IO;
using System.Runtime.InteropServices;

namespace CodeGlyphX.Rendering.Png;

/// <summary>
/// Decodes PNG images to RGBA buffers.
/// </summary>
public static class PngReader {
    /// <summary>
    /// Decodes a PNG image to an RGBA buffer.
    /// </summary>
    /// <param name="png">PNG bytes.</param>
    /// <param name="width">Image width in pixels.</param>
    /// <param name="height">Image height in pixels.</param>
    /// <returns>RGBA buffer (width * height * 4 bytes).</returns>
    public static byte[] DecodeRgba32(byte[] png, out int width, out int height) {
        if (png is null) throw new ArgumentNullException(nameof(png));
        return PngDecoder.DecodeRgba32(png, out width, out height);
    }

    /// <summary>
    /// Decodes a PNG image to an RGBA buffer from a read-only span.
    /// </summary>
    public static byte[] DecodeRgba32(ReadOnlySpan<byte> png, out int width, out int height) {
        if (png.IsEmpty) throw new ArgumentException("PNG data is empty.", nameof(png));
        return PngDecoder.DecodeRgba32(png.ToArray(), out width, out height);
    }

    /// <summary>
    /// Decodes a PNG image to an RGBA buffer from a read-only memory.
    /// </summary>
    public static byte[] DecodeRgba32(ReadOnlyMemory<byte> png, out int width, out int height) {
        if (png.IsEmpty) throw new ArgumentException("PNG data is empty.", nameof(png));
        if (MemoryMarshal.TryGetArray<byte>(png, out var segment) && segment.Array is not null) {
            return PngDecoder.DecodeRgba32(segment.Array, segment.Offset, segment.Count, out width, out height);
        }
        return PngDecoder.DecodeRgba32(png.ToArray(), out width, out height);
    }

    /// <summary>
    /// Decodes a PNG stream to an RGBA buffer.
    /// </summary>
    /// <param name="stream">PNG stream.</param>
    /// <param name="width">Image width in pixels.</param>
    /// <param name="height">Image height in pixels.</param>
    /// <returns>RGBA buffer (width * height * 4 bytes).</returns>
    public static byte[] DecodeRgba32(Stream stream, out int width, out int height) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (stream is MemoryStream memory) {
            if (memory.TryGetBuffer(out var segment)) {
                return PngDecoder.DecodeRgba32(segment.Array!, segment.Offset, segment.Count, out width, out height);
            }
            return DecodeRgba32(memory.ToArray(), out width, out height);
        }
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        if (ms.TryGetBuffer(out var buffer)) {
            return PngDecoder.DecodeRgba32(buffer.Array!, buffer.Offset, buffer.Count, out width, out height);
        }
        return DecodeRgba32(ms.ToArray(), out width, out height);
    }
}
