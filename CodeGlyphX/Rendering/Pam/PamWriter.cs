using System;
using System.Buffers;
using System.IO;
using System.Text;
using CodeGlyphX.Rendering;

namespace CodeGlyphX.Rendering.Pam;

/// <summary>
/// Writes PAM (P7) images from RGBA buffers.
/// </summary>
public static class PamWriter {
    private const string PamOutputLimitMessage = "PAM output exceeds size limits.";
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
        WriteRgba32Core(stream, width, height, rgba, stride, rowOffset: 0, rowStride: stride, nameof(rgba), "RGBA buffer is too small.");
    }

    /// <summary>
    /// Writes a PAM byte array from a PNG scanline buffer (filter byte per row).
    /// </summary>
    public static byte[] WriteRgba32Scanlines(int width, int height, ReadOnlySpan<byte> scanlines, int stride) {
        using var ms = new MemoryStream();
        WriteRgba32Scanlines(ms, width, height, scanlines, stride);
        return ms.ToArray();
    }

    /// <summary>
    /// Writes a PAM (P7) stream from a PNG scanline buffer (filter byte per row).
    /// </summary>
    public static void WriteRgba32Scanlines(Stream stream, int width, int height, ReadOnlySpan<byte> scanlines, int stride) {
        WriteRgba32Core(stream, width, height, scanlines, stride, rowOffset: 1, rowStride: stride + 1, nameof(scanlines), "Scanline buffer is too small.");
    }

    private static void WriteRgba32Core(Stream stream, int width, int height, ReadOnlySpan<byte> rgba, int stride, int rowOffset, int rowStride, string bufferName, string bufferMessage) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        _ = RenderGuards.EnsureOutputPixels(width, height, PamOutputLimitMessage);
        if (stride < width * 4) throw new ArgumentOutOfRangeException(nameof(stride));
        if (rowStride < rowOffset + stride) throw new ArgumentOutOfRangeException(nameof(rowStride));
        if (rgba.Length < (height - 1) * rowStride + rowOffset + width * 4) throw new ArgumentException(bufferMessage, bufferName);

        var header = Encoding.ASCII.GetBytes(
            $"P7\nWIDTH {width}\nHEIGHT {height}\nDEPTH 4\nMAXVAL 255\nTUPLTYPE RGB_ALPHA\nENDHDR\n");
        stream.Write(header, 0, header.Length);

        var rowBytes = RenderGuards.EnsureOutputBytes((long)width * 4, PamOutputLimitMessage);
        _ = RenderGuards.EnsureOutputBytes((long)height * rowBytes, PamOutputLimitMessage);
        var row = ArrayPool<byte>.Shared.Rent(rowBytes);
        try {
            for (var y = 0; y < height; y++) {
                var srcRow = y * rowStride + rowOffset;
                rgba.Slice(srcRow, rowBytes).CopyTo(row);
                stream.Write(row, 0, rowBytes);
            }
        } finally {
            ArrayPool<byte>.Shared.Return(row);
        }
    }
}
