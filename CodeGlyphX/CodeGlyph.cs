using System;
using System.IO;
using CodeGlyphX.Aztec;
using CodeGlyphX.DataMatrix;
using CodeGlyphX.Pdf417;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX;

/// <summary>
/// Unified decode helpers (QR + 1D + 2D barcodes).
/// </summary>
public static class CodeGlyph {
    /// <summary>
    /// Attempts to decode a QR or barcode from raw pixels.
    /// </summary>
    public static bool TryDecode(byte[] pixels, int width, int height, int stride, PixelFormat format, out CodeGlyphDecoded decoded, BarcodeType? expectedBarcode = null, bool preferBarcode = false) {
        decoded = null!;
        if (pixels is null) throw new ArgumentNullException(nameof(pixels));

        if (preferBarcode) {
            if (BarcodeDecoder.TryDecode(pixels, width, height, stride, format, expectedBarcode, out var barcode)) {
                decoded = new CodeGlyphDecoded(barcode);
                return true;
            }
            if (AztecDecoder.TryDecode(pixels, width, height, stride, format, out var aztec)) {
                decoded = new CodeGlyphDecoded(CodeGlyphKind.Aztec, aztec);
                return true;
            }
            if (QrDecoder.TryDecode(pixels, width, height, stride, format, out var qr)) {
                decoded = new CodeGlyphDecoded(qr);
                return true;
            }
            if (DataMatrixDecoder.TryDecode(pixels, width, height, stride, format, out var dataMatrix)) {
                decoded = new CodeGlyphDecoded(CodeGlyphKind.DataMatrix, dataMatrix);
                return true;
            }
            if (Pdf417Decoder.TryDecode(pixels, width, height, stride, format, out var pdf417)) {
                decoded = new CodeGlyphDecoded(CodeGlyphKind.Pdf417, pdf417);
                return true;
            }
            return false;
        }

        if (AztecDecoder.TryDecode(pixels, width, height, stride, format, out var aztecDecoded)) {
            decoded = new CodeGlyphDecoded(CodeGlyphKind.Aztec, aztecDecoded);
            return true;
        }
        if (QrDecoder.TryDecode(pixels, width, height, stride, format, out var qrDecoded)) {
            decoded = new CodeGlyphDecoded(qrDecoded);
            return true;
        }
        if (DataMatrixDecoder.TryDecode(pixels, width, height, stride, format, out var dataMatrixDecoded)) {
            decoded = new CodeGlyphDecoded(CodeGlyphKind.DataMatrix, dataMatrixDecoded);
            return true;
        }
        if (Pdf417Decoder.TryDecode(pixels, width, height, stride, format, out var pdf417Decoded)) {
            decoded = new CodeGlyphDecoded(CodeGlyphKind.Pdf417, pdf417Decoded);
            return true;
        }
        if (BarcodeDecoder.TryDecode(pixels, width, height, stride, format, expectedBarcode, out var barcodeDecoded)) {
            decoded = new CodeGlyphDecoded(barcodeDecoded);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Attempts to decode all QR codes and (optionally) a 1D barcode from raw pixels.
    /// </summary>
    public static bool TryDecodeAll(byte[] pixels, int width, int height, int stride, PixelFormat format, out CodeGlyphDecoded[] decoded, BarcodeType? expectedBarcode = null, bool includeBarcode = true, bool preferBarcode = false) {
        decoded = Array.Empty<CodeGlyphDecoded>();
        if (pixels is null) throw new ArgumentNullException(nameof(pixels));

        var list = new System.Collections.Generic.List<CodeGlyphDecoded>(4);

        if (includeBarcode && preferBarcode) {
            if (BarcodeDecoder.TryDecode(pixels, width, height, stride, format, expectedBarcode, out var barcode)) {
                list.Add(new CodeGlyphDecoded(barcode));
            }
        }

        if (QrDecoder.TryDecodeAll(pixels, width, height, stride, format, out var qrResults)) {
            for (var i = 0; i < qrResults.Length; i++) {
                list.Add(new CodeGlyphDecoded(qrResults[i]));
            }
        }

        if (AztecDecoder.TryDecode(pixels, width, height, stride, format, out var aztec)) {
            list.Add(new CodeGlyphDecoded(CodeGlyphKind.Aztec, aztec));
        }

        if (DataMatrixDecoder.TryDecode(pixels, width, height, stride, format, out var dataMatrix)) {
            list.Add(new CodeGlyphDecoded(CodeGlyphKind.DataMatrix, dataMatrix));
        }

        if (Pdf417Decoder.TryDecode(pixels, width, height, stride, format, out var pdf417)) {
            list.Add(new CodeGlyphDecoded(CodeGlyphKind.Pdf417, pdf417));
        }

        if (includeBarcode && !preferBarcode) {
            if (BarcodeDecoder.TryDecode(pixels, width, height, stride, format, expectedBarcode, out var barcode)) {
                list.Add(new CodeGlyphDecoded(barcode));
            }
        }

        if (list.Count == 0) return false;
        decoded = list.ToArray();
        return true;
    }

#if NET8_0_OR_GREATER
    /// <summary>
    /// Attempts to decode a QR or barcode from raw pixels.
    /// </summary>
    public static bool TryDecode(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat format, out CodeGlyphDecoded decoded, BarcodeType? expectedBarcode = null, bool preferBarcode = false) {
        decoded = null!;
        if (preferBarcode) {
            if (BarcodeDecoder.TryDecode(pixels, width, height, stride, format, expectedBarcode, out var barcode)) {
                decoded = new CodeGlyphDecoded(barcode);
                return true;
            }
            if (AztecDecoder.TryDecode(pixels, width, height, stride, format, out var aztec)) {
                decoded = new CodeGlyphDecoded(CodeGlyphKind.Aztec, aztec);
                return true;
            }
            if (QrDecoder.TryDecode(pixels, width, height, stride, format, out var qr)) {
                decoded = new CodeGlyphDecoded(qr);
                return true;
            }
            if (DataMatrixDecoder.TryDecode(pixels, width, height, stride, format, out var dataMatrix)) {
                decoded = new CodeGlyphDecoded(CodeGlyphKind.DataMatrix, dataMatrix);
                return true;
            }
            if (Pdf417Decoder.TryDecode(pixels, width, height, stride, format, out var pdf417)) {
                decoded = new CodeGlyphDecoded(CodeGlyphKind.Pdf417, pdf417);
                return true;
            }
            return false;
        }

        if (AztecDecoder.TryDecode(pixels, width, height, stride, format, out var aztecDecoded)) {
            decoded = new CodeGlyphDecoded(CodeGlyphKind.Aztec, aztecDecoded);
            return true;
        }
        if (QrDecoder.TryDecode(pixels, width, height, stride, format, out var qrDecoded)) {
            decoded = new CodeGlyphDecoded(qrDecoded);
            return true;
        }
        if (DataMatrixDecoder.TryDecode(pixels, width, height, stride, format, out var dataMatrixDecoded)) {
            decoded = new CodeGlyphDecoded(CodeGlyphKind.DataMatrix, dataMatrixDecoded);
            return true;
        }
        if (Pdf417Decoder.TryDecode(pixels, width, height, stride, format, out var pdf417Decoded)) {
            decoded = new CodeGlyphDecoded(CodeGlyphKind.Pdf417, pdf417Decoded);
            return true;
        }
        if (BarcodeDecoder.TryDecode(pixels, width, height, stride, format, expectedBarcode, out var barcodeDecoded)) {
            decoded = new CodeGlyphDecoded(barcodeDecoded);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Attempts to decode all QR codes and (optionally) a 1D barcode from raw pixels.
    /// </summary>
    public static bool TryDecodeAll(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat format, out CodeGlyphDecoded[] decoded, BarcodeType? expectedBarcode = null, bool includeBarcode = true, bool preferBarcode = false) {
        decoded = Array.Empty<CodeGlyphDecoded>();

        var list = new System.Collections.Generic.List<CodeGlyphDecoded>(4);

        if (includeBarcode && preferBarcode) {
            if (BarcodeDecoder.TryDecode(pixels, width, height, stride, format, expectedBarcode, out var barcode)) {
                list.Add(new CodeGlyphDecoded(barcode));
            }
        }

        if (QrDecoder.TryDecodeAll(pixels, width, height, stride, format, out var qrResults)) {
            for (var i = 0; i < qrResults.Length; i++) {
                list.Add(new CodeGlyphDecoded(qrResults[i]));
            }
        }

        if (AztecDecoder.TryDecode(pixels, width, height, stride, format, out var aztec)) {
            list.Add(new CodeGlyphDecoded(CodeGlyphKind.Aztec, aztec));
        }

        if (DataMatrixDecoder.TryDecode(pixels, width, height, stride, format, out var dataMatrix)) {
            list.Add(new CodeGlyphDecoded(CodeGlyphKind.DataMatrix, dataMatrix));
        }

        if (Pdf417Decoder.TryDecode(pixels, width, height, stride, format, out var pdf417)) {
            list.Add(new CodeGlyphDecoded(CodeGlyphKind.Pdf417, pdf417));
        }

        if (includeBarcode && !preferBarcode) {
            if (BarcodeDecoder.TryDecode(pixels, width, height, stride, format, expectedBarcode, out var barcode)) {
                list.Add(new CodeGlyphDecoded(barcode));
            }
        }

        if (list.Count == 0) return false;
        decoded = list.ToArray();
        return true;
    }
#endif

    /// <summary>
    /// Attempts to decode a QR or 1D barcode from PNG bytes.
    /// </summary>
    public static bool TryDecodePng(byte[] png, out CodeGlyphDecoded decoded, BarcodeType? expectedBarcode = null, bool preferBarcode = false) {
        decoded = null!;
        if (png is null) throw new ArgumentNullException(nameof(png));
        var rgba = PngReader.DecodeRgba32(png, out var width, out var height);
        return TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, out decoded, expectedBarcode, preferBarcode);
    }

    /// <summary>
    /// Attempts to decode a QR or barcode from common image formats (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA).
    /// </summary>
    public static bool TryDecodeImage(byte[] image, out CodeGlyphDecoded decoded, BarcodeType? expectedBarcode = null, bool preferBarcode = false) {
        decoded = null!;
        if (image is null) throw new ArgumentNullException(nameof(image));
        if (!ImageReader.TryDecodeRgba32(image, out var rgba, out var width, out var height)) return false;
        return TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, out decoded, expectedBarcode, preferBarcode);
    }

    /// <summary>
    /// Attempts to decode all QR codes and (optionally) a 1D barcode from PNG bytes.
    /// </summary>
    public static bool TryDecodeAllPng(byte[] png, out CodeGlyphDecoded[] decoded, BarcodeType? expectedBarcode = null, bool includeBarcode = true, bool preferBarcode = false) {
        decoded = Array.Empty<CodeGlyphDecoded>();
        if (png is null) throw new ArgumentNullException(nameof(png));
        var rgba = PngReader.DecodeRgba32(png, out var width, out var height);
        return TryDecodeAll(rgba, width, height, width * 4, PixelFormat.Rgba32, out decoded, expectedBarcode, includeBarcode, preferBarcode);
    }

    /// <summary>
    /// Attempts to decode all QR codes and (optionally) a 1D barcode from common image formats (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA).
    /// </summary>
    public static bool TryDecodeAllImage(byte[] image, out CodeGlyphDecoded[] decoded, BarcodeType? expectedBarcode = null, bool includeBarcode = true, bool preferBarcode = false) {
        decoded = Array.Empty<CodeGlyphDecoded>();
        if (image is null) throw new ArgumentNullException(nameof(image));
        if (!ImageReader.TryDecodeRgba32(image, out var rgba, out var width, out var height)) return false;
        return TryDecodeAll(rgba, width, height, width * 4, PixelFormat.Rgba32, out decoded, expectedBarcode, includeBarcode, preferBarcode);
    }

    /// <summary>
    /// Attempts to decode a QR or barcode from an image stream (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA).
    /// </summary>
    public static bool TryDecodeImage(Stream stream, out CodeGlyphDecoded decoded, BarcodeType? expectedBarcode = null, bool preferBarcode = false) {
        decoded = null!;
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        var rgba = ImageReader.DecodeRgba32(stream, out var width, out var height);
        return TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, out decoded, expectedBarcode, preferBarcode);
    }

    /// <summary>
    /// Attempts to decode all QR codes and (optionally) a 1D barcode from an image stream (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA).
    /// </summary>
    public static bool TryDecodeAllImage(Stream stream, out CodeGlyphDecoded[] decoded, BarcodeType? expectedBarcode = null, bool includeBarcode = true, bool preferBarcode = false) {
        decoded = Array.Empty<CodeGlyphDecoded>();
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        var rgba = ImageReader.DecodeRgba32(stream, out var width, out var height);
        return TryDecodeAll(rgba, width, height, width * 4, PixelFormat.Rgba32, out decoded, expectedBarcode, includeBarcode, preferBarcode);
    }

    /// <summary>
    /// Attempts to decode a QR or 1D barcode from a PNG file.
    /// </summary>
    public static bool TryDecodePngFile(string path, out CodeGlyphDecoded decoded, BarcodeType? expectedBarcode = null, bool preferBarcode = false) {
        decoded = null!;
        if (path is null) throw new ArgumentNullException(nameof(path));
        var png = RenderIO.ReadBinary(path);
        return TryDecodePng(png, out decoded, expectedBarcode, preferBarcode);
    }

    /// <summary>
    /// Attempts to decode all QR codes and (optionally) a 1D barcode from a PNG file.
    /// </summary>
    public static bool TryDecodeAllPngFile(string path, out CodeGlyphDecoded[] decoded, BarcodeType? expectedBarcode = null, bool includeBarcode = true, bool preferBarcode = false) {
        decoded = Array.Empty<CodeGlyphDecoded>();
        if (path is null) throw new ArgumentNullException(nameof(path));
        var png = RenderIO.ReadBinary(path);
        return TryDecodeAllPng(png, out decoded, expectedBarcode, includeBarcode, preferBarcode);
    }

    /// <summary>
    /// Attempts to decode a QR or 1D barcode from a PNG stream.
    /// </summary>
    public static bool TryDecodePng(Stream stream, out CodeGlyphDecoded decoded, BarcodeType? expectedBarcode = null, bool preferBarcode = false) {
        decoded = null!;
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        var png = RenderIO.ReadBinary(stream);
        return TryDecodePng(png, out decoded, expectedBarcode, preferBarcode);
    }

    /// <summary>
    /// Attempts to decode all QR codes and (optionally) a 1D barcode from a PNG stream.
    /// </summary>
    public static bool TryDecodeAllPng(Stream stream, out CodeGlyphDecoded[] decoded, BarcodeType? expectedBarcode = null, bool includeBarcode = true, bool preferBarcode = false) {
        decoded = Array.Empty<CodeGlyphDecoded>();
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        var png = RenderIO.ReadBinary(stream);
        return TryDecodeAllPng(png, out decoded, expectedBarcode, includeBarcode, preferBarcode);
    }

    /// <summary>
    /// Decodes a QR or 1D barcode from PNG bytes.
    /// </summary>
    public static CodeGlyphDecoded DecodePng(byte[] png, BarcodeType? expectedBarcode = null, bool preferBarcode = false) {
        if (!TryDecodePng(png, out var decoded, expectedBarcode, preferBarcode)) {
            throw new FormatException("PNG does not contain a decodable symbol.");
        }
        return decoded;
    }

    /// <summary>
    /// Decodes a QR or 1D barcode from a PNG file.
    /// </summary>
    public static CodeGlyphDecoded DecodePngFile(string path, BarcodeType? expectedBarcode = null, bool preferBarcode = false) {
        if (!TryDecodePngFile(path, out var decoded, expectedBarcode, preferBarcode)) {
            throw new FormatException("PNG file does not contain a decodable symbol.");
        }
        return decoded;
    }

    /// <summary>
    /// Decodes a QR or 1D barcode from a PNG stream.
    /// </summary>
    public static CodeGlyphDecoded DecodePng(Stream stream, BarcodeType? expectedBarcode = null, bool preferBarcode = false) {
        if (!TryDecodePng(stream, out var decoded, expectedBarcode, preferBarcode)) {
            throw new FormatException("PNG stream does not contain a decodable symbol.");
        }
        return decoded;
    }
}
