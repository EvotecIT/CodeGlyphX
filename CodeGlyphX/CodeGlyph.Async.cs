using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CodeGlyphX.Rendering;

namespace CodeGlyphX;

public static partial class CodeGlyph {
    /// <summary>
    /// Attempts to decode a QR or barcode from PNG bytes asynchronously.
    /// </summary>
    public static async Task<CodeGlyphDecoded?> TryDecodePngAsync(Stream stream, BarcodeType? expectedBarcode = null, bool preferBarcode = false, QrPixelDecodeOptions? qrOptions = null, CancellationToken cancellationToken = default, BarcodeDecodeOptions? barcodeOptions = null) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        var png = await RenderIO.ReadBinaryAsync(stream, cancellationToken).ConfigureAwait(false);
        if (cancellationToken.IsCancellationRequested) return null;
        return TryDecodePng(png, out var decoded, expectedBarcode, preferBarcode, qrOptions, cancellationToken, barcodeOptions) ? decoded : null;
    }

    /// <summary>
    /// Attempts to decode a QR or barcode from a PNG file asynchronously.
    /// </summary>
    public static async Task<CodeGlyphDecoded?> TryDecodePngFileAsync(string path, BarcodeType? expectedBarcode = null, bool preferBarcode = false, QrPixelDecodeOptions? qrOptions = null, CancellationToken cancellationToken = default, BarcodeDecodeOptions? barcodeOptions = null) {
        if (path is null) throw new ArgumentNullException(nameof(path));
        var png = await RenderIO.ReadBinaryAsync(path, cancellationToken).ConfigureAwait(false);
        if (cancellationToken.IsCancellationRequested) return null;
        return TryDecodePng(png, out var decoded, expectedBarcode, preferBarcode, qrOptions, cancellationToken, barcodeOptions) ? decoded : null;
    }

    /// <summary>
    /// Attempts to decode all QR codes and (optionally) a 1D barcode from PNG bytes asynchronously.
    /// </summary>
    public static async Task<CodeGlyphDecoded[]> TryDecodeAllPngAsync(Stream stream, BarcodeType? expectedBarcode = null, bool includeBarcode = true, bool preferBarcode = false, QrPixelDecodeOptions? qrOptions = null, CancellationToken cancellationToken = default, BarcodeDecodeOptions? barcodeOptions = null) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        var png = await RenderIO.ReadBinaryAsync(stream, cancellationToken).ConfigureAwait(false);
        if (cancellationToken.IsCancellationRequested) return Array.Empty<CodeGlyphDecoded>();
        return TryDecodeAllPng(png, out var decoded, expectedBarcode, includeBarcode, preferBarcode, qrOptions, cancellationToken, barcodeOptions) ? decoded : Array.Empty<CodeGlyphDecoded>();
    }

    /// <summary>
    /// Attempts to decode all QR codes and (optionally) a 1D barcode from a PNG file asynchronously.
    /// </summary>
    public static async Task<CodeGlyphDecoded[]> TryDecodeAllPngFileAsync(string path, BarcodeType? expectedBarcode = null, bool includeBarcode = true, bool preferBarcode = false, QrPixelDecodeOptions? qrOptions = null, CancellationToken cancellationToken = default, BarcodeDecodeOptions? barcodeOptions = null) {
        if (path is null) throw new ArgumentNullException(nameof(path));
        var png = await RenderIO.ReadBinaryAsync(path, cancellationToken).ConfigureAwait(false);
        if (cancellationToken.IsCancellationRequested) return Array.Empty<CodeGlyphDecoded>();
        return TryDecodeAllPng(png, out var decoded, expectedBarcode, includeBarcode, preferBarcode, qrOptions, cancellationToken, barcodeOptions) ? decoded : Array.Empty<CodeGlyphDecoded>();
    }

    /// <summary>
    /// Attempts to decode a QR or barcode from an image stream asynchronously (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA).
    /// </summary>
    public static async Task<CodeGlyphDecoded?> TryDecodeImageAsync(Stream stream, BarcodeType? expectedBarcode = null, bool preferBarcode = false, QrPixelDecodeOptions? qrOptions = null, CancellationToken cancellationToken = default, BarcodeDecodeOptions? barcodeOptions = null) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        var image = await RenderIO.ReadBinaryAsync(stream, cancellationToken).ConfigureAwait(false);
        if (cancellationToken.IsCancellationRequested) return null;
        return TryDecodeImage(image, out var decoded, expectedBarcode, preferBarcode, qrOptions, cancellationToken, barcodeOptions) ? decoded : null;
    }

    /// <summary>
    /// Attempts to decode a QR or barcode from an image file asynchronously (PNG/BMP/PPM/PBM/PGM/PAM/XBM/XPM/TGA).
    /// </summary>
    public static async Task<CodeGlyphDecoded?> TryDecodeImageFileAsync(string path, BarcodeType? expectedBarcode = null, bool preferBarcode = false, QrPixelDecodeOptions? qrOptions = null, CancellationToken cancellationToken = default, BarcodeDecodeOptions? barcodeOptions = null) {
        if (path is null) throw new ArgumentNullException(nameof(path));
        var image = await RenderIO.ReadBinaryAsync(path, cancellationToken).ConfigureAwait(false);
        if (cancellationToken.IsCancellationRequested) return null;
        return TryDecodeImage(image, out var decoded, expectedBarcode, preferBarcode, qrOptions, cancellationToken, barcodeOptions) ? decoded : null;
    }

    /// <summary>
    /// Attempts to decode all QR codes and (optionally) a 1D barcode from an image stream asynchronously.
    /// </summary>
    public static async Task<CodeGlyphDecoded[]> TryDecodeAllImageAsync(Stream stream, BarcodeType? expectedBarcode = null, bool includeBarcode = true, bool preferBarcode = false, QrPixelDecodeOptions? qrOptions = null, CancellationToken cancellationToken = default, BarcodeDecodeOptions? barcodeOptions = null) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        var image = await RenderIO.ReadBinaryAsync(stream, cancellationToken).ConfigureAwait(false);
        if (cancellationToken.IsCancellationRequested) return Array.Empty<CodeGlyphDecoded>();
        return TryDecodeAllImage(image, out var decoded, expectedBarcode, includeBarcode, preferBarcode, qrOptions, cancellationToken, barcodeOptions) ? decoded : Array.Empty<CodeGlyphDecoded>();
    }

    /// <summary>
    /// Attempts to decode all QR codes and (optionally) a 1D barcode from an image file asynchronously.
    /// </summary>
    public static async Task<CodeGlyphDecoded[]> TryDecodeAllImageFileAsync(string path, BarcodeType? expectedBarcode = null, bool includeBarcode = true, bool preferBarcode = false, QrPixelDecodeOptions? qrOptions = null, CancellationToken cancellationToken = default, BarcodeDecodeOptions? barcodeOptions = null) {
        if (path is null) throw new ArgumentNullException(nameof(path));
        var image = await RenderIO.ReadBinaryAsync(path, cancellationToken).ConfigureAwait(false);
        if (cancellationToken.IsCancellationRequested) return Array.Empty<CodeGlyphDecoded>();
        return TryDecodeAllImage(image, out var decoded, expectedBarcode, includeBarcode, preferBarcode, qrOptions, cancellationToken, barcodeOptions) ? decoded : Array.Empty<CodeGlyphDecoded>();
    }

    /// <summary>
    /// Attempts to decode a QR or barcode from an image stream using a single options object asynchronously.
    /// </summary>
    public static async Task<CodeGlyphDecoded?> TryDecodeImageAsync(Stream stream, CodeGlyphDecodeOptions? options) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        var token = options?.CancellationToken ?? default;
        var image = await RenderIO.ReadBinaryAsync(stream, token).ConfigureAwait(false);
        if (token.IsCancellationRequested) return null;
        return TryDecodeImage(image, out var decoded, options) ? decoded : null;
    }

    /// <summary>
    /// Attempts to decode all symbols from an image stream using a single options object asynchronously.
    /// </summary>
    public static async Task<CodeGlyphDecoded[]> TryDecodeAllImageAsync(Stream stream, CodeGlyphDecodeOptions? options) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        var token = options?.CancellationToken ?? default;
        var image = await RenderIO.ReadBinaryAsync(stream, token).ConfigureAwait(false);
        if (token.IsCancellationRequested) return Array.Empty<CodeGlyphDecoded>();
        return TryDecodeAllImage(image, out var decoded, options) ? decoded : Array.Empty<CodeGlyphDecoded>();
    }

    /// <summary>
    /// Decodes a QR or barcode from an image stream asynchronously.
    /// </summary>
    public static async Task<CodeGlyphDecoded> DecodeImageAsync(Stream stream, BarcodeType? expectedBarcode = null, bool preferBarcode = false, QrPixelDecodeOptions? qrOptions = null, CancellationToken cancellationToken = default, BarcodeDecodeOptions? barcodeOptions = null) {
        var decoded = await TryDecodeImageAsync(stream, expectedBarcode, preferBarcode, qrOptions, cancellationToken, barcodeOptions).ConfigureAwait(false);
        if (decoded is null) throw new FormatException("Image does not contain a decodable symbol.");
        return decoded;
    }

    /// <summary>
    /// Decodes a QR or barcode from a PNG stream asynchronously.
    /// </summary>
    public static async Task<CodeGlyphDecoded> DecodePngAsync(Stream stream, BarcodeType? expectedBarcode = null, bool preferBarcode = false, QrPixelDecodeOptions? qrOptions = null, CancellationToken cancellationToken = default, BarcodeDecodeOptions? barcodeOptions = null) {
        var decoded = await TryDecodePngAsync(stream, expectedBarcode, preferBarcode, qrOptions, cancellationToken, barcodeOptions).ConfigureAwait(false);
        if (decoded is null) throw new FormatException("PNG does not contain a decodable symbol.");
        return decoded;
    }
}
