using System;
using System.IO;
using CodeGlyphX.Rendering.Bmp;
using CodeGlyphX.Rendering.Gif;
using CodeGlyphX.Rendering.Png;
using CodeGlyphX.Rendering.Ppm;
using CodeGlyphX.Rendering.Tga;

namespace CodeGlyphX.Rendering;

/// <summary>
/// Auto-detects common image formats and decodes to RGBA buffers.
/// </summary>
public static class ImageReader {
    private static readonly byte[] PngSignature = { 137, 80, 78, 71, 13, 10, 26, 10 };

    /// <summary>
    /// Decodes an image to an RGBA buffer (auto-detected).
    /// </summary>
    public static byte[] DecodeRgba32(byte[] data, out int width, out int height) {
        if (data is null) throw new ArgumentNullException(nameof(data));
        return DecodeRgba32((ReadOnlySpan<byte>)data, out width, out height);
    }

    /// <summary>
    /// Decodes an image to an RGBA buffer (auto-detected).
    /// </summary>
    public static byte[] DecodeRgba32(ReadOnlySpan<byte> data, out int width, out int height) {
        if (data.Length < 2) throw new FormatException("Unknown image format.");

        if (IsPng(data)) return PngReader.DecodeRgba32(data, out width, out height);
        if (GifReader.IsGif(data)) return GifReader.DecodeRgba32(data, out width, out height);
        if (IsBmp(data)) return BmpReader.DecodeRgba32(data, out width, out height);
        if (IsPpm(data)) return PpmReader.DecodeRgba32(data, out width, out height);
        if (TgaReader.LooksLikeTga(data)) return TgaReader.DecodeRgba32(data, out width, out height);

        throw new FormatException("Unknown image format.");
    }

    /// <summary>
    /// Decodes an image stream to an RGBA buffer (auto-detected).
    /// </summary>
    public static byte[] DecodeRgba32(Stream stream, out int width, out int height) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (stream is MemoryStream memory && memory.TryGetBuffer(out var buffer)) {
            return DecodeRgba32(buffer.AsSpan(), out width, out height);
        }
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        if (ms.TryGetBuffer(out var segment)) {
            return DecodeRgba32(segment.AsSpan(), out width, out height);
        }
        return DecodeRgba32(ms.ToArray(), out width, out height);
    }

    /// <summary>
    /// Attempts to decode an image to an RGBA buffer (auto-detected).
    /// </summary>
    public static bool TryDecodeRgba32(ReadOnlySpan<byte> data, out byte[] rgba, out int width, out int height) {
        try {
            rgba = DecodeRgba32(data, out width, out height);
            return true;
        } catch (FormatException) {
            rgba = Array.Empty<byte>();
            width = 0;
            height = 0;
            return false;
        }
    }

    private static bool IsPng(ReadOnlySpan<byte> data) {
        if (data.Length < PngSignature.Length) return false;
        for (var i = 0; i < PngSignature.Length; i++) {
            if (data[i] != PngSignature[i]) return false;
        }
        return true;
    }

    private static bool IsBmp(ReadOnlySpan<byte> data) {
        return data.Length >= 2 && data[0] == (byte)'B' && data[1] == (byte)'M';
    }

    private static bool IsPpm(ReadOnlySpan<byte> data) {
        return data.Length >= 2 && data[0] == (byte)'P' && (data[1] == (byte)'5' || data[1] == (byte)'6');
    }
}
