using System;
using System.IO;
using System.Text;

namespace CodeGlyphX.Rendering.Pam;

/// <summary>
/// Writes PAM (P7) images from RGBA buffers.
/// </summary>
public static class PamWriter {
    /// <summary>
    /// Writes a PAM byte array from an RGBA buffer.
    /// </summary>
    public static byte[] WriteRgba32(int width, int height, ReadOnlySpan<byte> rgba, int stride) {
        using var ms = new MemoryStream();
        WriteRgba32(ms, width, height, rgba, stride);
        return ms.ToArray();
    }

    /// <summary>
    /// Writes a PAM (P7) stream from an RGBA buffer.
    /// </summary>
    public static void WriteRgba32(Stream stream, int width, int height, ReadOnlySpan<byte> rgba, int stride) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        if (stride < width * 4) throw new ArgumentOutOfRangeException(nameof(stride));
        if (rgba.Length < (height - 1) * stride + width * 4) throw new ArgumentException("RGBA buffer is too small.", nameof(rgba));

        var header = Encoding.ASCII.GetBytes(
            $"P7\nWIDTH {width}\nHEIGHT {height}\nDEPTH 4\nMAXVAL 255\nTUPLTYPE RGB_ALPHA\nENDHDR\n");
        stream.Write(header, 0, header.Length);

        var row = new byte[width * 4];
        for (var y = 0; y < height; y++) {
            var srcRow = y * stride;
            rgba.Slice(srcRow, width * 4).CopyTo(row);
            stream.Write(row, 0, row.Length);
        }
    }
}
