using System;
using System.IO;

namespace CodeMatrix.Rendering.Png;

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
    /// Decodes a PNG stream to an RGBA buffer.
    /// </summary>
    /// <param name="stream">PNG stream.</param>
    /// <param name="width">Image width in pixels.</param>
    /// <param name="height">Image height in pixels.</param>
    /// <returns>RGBA buffer (width * height * 4 bytes).</returns>
    public static byte[] DecodeRgba32(Stream stream, out int width, out int height) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (stream is MemoryStream memory) {
            return DecodeRgba32(memory.ToArray(), out width, out height);
        }
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return DecodeRgba32(ms.ToArray(), out width, out height);
    }
}
