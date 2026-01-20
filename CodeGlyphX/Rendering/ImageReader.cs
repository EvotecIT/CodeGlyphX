using System;
using System.IO;
using CodeGlyphX.Rendering.Bmp;
using CodeGlyphX.Rendering.Gif;
using CodeGlyphX.Rendering.Ico;
using CodeGlyphX.Rendering.Pam;
using CodeGlyphX.Rendering.Pgm;
using CodeGlyphX.Rendering.Pbm;
using CodeGlyphX.Rendering.Jpeg;
using CodeGlyphX.Rendering.Png;
using CodeGlyphX.Rendering.Ppm;
using CodeGlyphX.Rendering.Tiff;
using CodeGlyphX.Rendering.Tga;
using CodeGlyphX.Rendering.Xbm;
using CodeGlyphX.Rendering.Xpm;

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
        if (TiffReader.IsTiff(data)) return TiffReader.DecodeRgba32(data, out width, out height);
        if (IcoReader.IsIco(data)) return IcoReader.DecodeRgba32(data, out width, out height);
        if (JpegReader.IsJpeg(data)) return JpegReader.DecodeRgba32(data, out width, out height);
        if (GifReader.IsGif(data)) return GifReader.DecodeRgba32(data, out width, out height);
        if (IsBmp(data)) return BmpReader.DecodeRgba32(data, out width, out height);
        if (IsPbm(data)) return PbmReader.DecodeRgba32(data, out width, out height);
        if (IsPgm(data)) return PgmReader.DecodeRgba32(data, out width, out height);
        if (IsPam(data)) return PamReader.DecodeRgba32(data, out width, out height);
        if (IsPpm(data)) return PpmReader.DecodeRgba32(data, out width, out height);
        if (TgaReader.LooksLikeTga(data)) return TgaReader.DecodeRgba32(data, out width, out height);
        if (IsXpm(data)) return XpmReader.DecodeRgba32(data, out width, out height);
        if (IsXbm(data)) return XbmReader.DecodeRgba32(data, out width, out height);

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

    /// <summary>
    /// Detects the image format from a byte buffer.
    /// </summary>
    public static ImageFormat DetectFormat(ReadOnlySpan<byte> data) {
        if (TryDetectFormat(data, out var format)) return format;
        throw new FormatException("Unknown image format.");
    }

    /// <summary>
    /// Attempts to detect the image format from a byte buffer.
    /// </summary>
    public static bool TryDetectFormat(ReadOnlySpan<byte> data, out ImageFormat format) {
        format = ImageFormat.Unknown;
        if (data.Length < 2) return false;

        if (IsPng(data)) { format = ImageFormat.Png; return true; }
        if (TiffReader.IsTiff(data)) { format = ImageFormat.Tiff; return true; }
        if (IcoReader.IsIco(data)) { format = ImageFormat.Ico; return true; }
        if (JpegReader.IsJpeg(data)) { format = ImageFormat.Jpeg; return true; }
        if (GifReader.IsGif(data)) { format = ImageFormat.Gif; return true; }
        if (IsBmp(data)) { format = ImageFormat.Bmp; return true; }
        if (IsPbm(data)) { format = ImageFormat.Pbm; return true; }
        if (IsPgm(data)) { format = ImageFormat.Pgm; return true; }
        if (IsPam(data)) { format = ImageFormat.Pam; return true; }
        if (IsPpm(data)) { format = ImageFormat.Ppm; return true; }
        if (TgaReader.LooksLikeTga(data)) { format = ImageFormat.Tga; return true; }
        if (IsXpm(data)) { format = ImageFormat.Xpm; return true; }
        if (IsXbm(data)) { format = ImageFormat.Xbm; return true; }

        return false;
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

    private static bool IsPbm(ReadOnlySpan<byte> data) {
        return data.Length >= 2 && data[0] == (byte)'P' && (data[1] == (byte)'1' || data[1] == (byte)'4');
    }

    private static bool IsPgm(ReadOnlySpan<byte> data) {
        return data.Length >= 2 && data[0] == (byte)'P' && (data[1] == (byte)'2' || data[1] == (byte)'5');
    }

    private static bool IsPam(ReadOnlySpan<byte> data) {
        return data.Length >= 2 && data[0] == (byte)'P' && data[1] == (byte)'7';
    }

    private static bool IsXpm(ReadOnlySpan<byte> data) {
        return StartsWithAscii(data, "/* XPM */");
    }

    private static bool IsXbm(ReadOnlySpan<byte> data) {
        return StartsWithAscii(data, "#define") && ContainsAscii(data, "_width");
    }

    private static bool StartsWithAscii(ReadOnlySpan<byte> data, string prefix) {
        var pos = 0;
        while (pos < data.Length && data[pos] <= 32) pos++;
        if (pos + prefix.Length > data.Length) return false;
        for (var i = 0; i < prefix.Length; i++) {
            if (data[pos + i] != (byte)prefix[i]) return false;
        }
        return true;
    }

    private static bool ContainsAscii(ReadOnlySpan<byte> data, string token) {
        if (token.Length == 0) return false;
        for (var i = 0; i <= data.Length - token.Length; i++) {
            var match = true;
            for (var j = 0; j < token.Length; j++) {
                var c = data[i + j];
                var t = (byte)token[j];
                if (c == t) continue;
                if (c >= (byte)'A' && c <= (byte)'Z' && c + 32 == t) continue;
                if (c >= (byte)'a' && c <= (byte)'z' && c - 32 == t) continue;
                match = false;
                break;
            }
            if (match) return true;
        }
        return false;
    }
}
