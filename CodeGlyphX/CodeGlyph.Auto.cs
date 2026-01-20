using System;
using System.IO;
using System.Threading;
using CodeGlyphX.Rendering;

namespace CodeGlyphX;

public static partial class CodeGlyph {
    /// <summary>
    /// Attempts to decode a QR or barcode from auto-detected image bytes.
    /// </summary>
    public static bool TryDecodeAuto(byte[] image, out CodeGlyphDecoded decoded, CodeGlyphDecodeOptions? options = null) {
        decoded = null!;
        if (image is null) throw new ArgumentNullException(nameof(image));
        if (!ImageReader.TryDecodeRgba32(image, out var rgba, out var width, out var height)) return false;
        return TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, out decoded, options);
    }

    /// <summary>
    /// Attempts to decode all QR codes and (optionally) a 1D barcode from auto-detected image bytes.
    /// </summary>
    public static bool TryDecodeAllAuto(byte[] image, out CodeGlyphDecoded[] decoded, CodeGlyphDecodeOptions? options = null) {
        decoded = Array.Empty<CodeGlyphDecoded>();
        if (image is null) throw new ArgumentNullException(nameof(image));
        if (!ImageReader.TryDecodeRgba32(image, out var rgba, out var width, out var height)) return false;
        return TryDecodeAll(rgba, width, height, width * 4, PixelFormat.Rgba32, out decoded, options);
    }

    /// <summary>
    /// Attempts to decode a QR or barcode from an auto-detected image stream.
    /// </summary>
    public static bool TryDecodeAuto(Stream stream, out CodeGlyphDecoded decoded, CodeGlyphDecodeOptions? options = null) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        var image = RenderIO.ReadBinary(stream);
        return TryDecodeAuto(image, out decoded, options);
    }

    /// <summary>
    /// Attempts to decode all QR codes and (optionally) a 1D barcode from an auto-detected image stream.
    /// </summary>
    public static bool TryDecodeAllAuto(Stream stream, out CodeGlyphDecoded[] decoded, CodeGlyphDecodeOptions? options = null) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        var image = RenderIO.ReadBinary(stream);
        return TryDecodeAllAuto(image, out decoded, options);
    }

    /// <summary>
    /// Attempts to decode a QR or barcode from an auto-detected image file.
    /// </summary>
    public static bool TryDecodeAutoFile(string path, out CodeGlyphDecoded decoded, CodeGlyphDecodeOptions? options = null) {
        if (path is null) throw new ArgumentNullException(nameof(path));
        var image = RenderIO.ReadBinary(path);
        return TryDecodeAuto(image, out decoded, options);
    }

    /// <summary>
    /// Attempts to decode all QR codes and (optionally) a 1D barcode from an auto-detected image file.
    /// </summary>
    public static bool TryDecodeAllAutoFile(string path, out CodeGlyphDecoded[] decoded, CodeGlyphDecodeOptions? options = null) {
        if (path is null) throw new ArgumentNullException(nameof(path));
        var image = RenderIO.ReadBinary(path);
        return TryDecodeAllAuto(image, out decoded, options);
    }

    /// <summary>
    /// Decodes a QR or barcode from auto-detected image bytes.
    /// </summary>
    public static CodeGlyphDecoded DecodeAuto(byte[] image, CodeGlyphDecodeOptions? options = null) {
        if (!TryDecodeAuto(image, out var decoded, options)) {
            throw new FormatException("Image does not contain a decodable symbol.");
        }
        return decoded;
    }

    /// <summary>
    /// Decodes all QR codes and (optionally) a 1D barcode from auto-detected image bytes.
    /// </summary>
    public static CodeGlyphDecoded[] DecodeAllAuto(byte[] image, CodeGlyphDecodeOptions? options = null) {
        if (!TryDecodeAllAuto(image, out var decoded, options)) {
            throw new FormatException("Image does not contain a decodable symbol.");
        }
        return decoded;
    }
}
