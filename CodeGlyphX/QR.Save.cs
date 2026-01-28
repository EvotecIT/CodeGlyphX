using System;
using System.IO;
using System.Threading;
using CodeGlyphX.Payloads;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Ascii;
using CodeGlyphX.Rendering.Bmp;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX;

public static partial class QR {
    /// <summary>
    /// Saves a PNG QR to a file.
    /// </summary>
    public static string SavePng(string payload, string path, QrEasyOptions? options = null) {
        return Png(payload, options).WriteBinary(path);
    }

    /// <summary>
    /// Saves a PNG QR to a file for a payload with embedded defaults.
    /// </summary>
    public static string SavePng(QrPayloadData payload, string path, QrEasyOptions? options = null) {
        return Png(payload, options).WriteBinary(path);
    }

    /// <summary>
    /// Saves a PNG QR to a stream.
    /// </summary>
    public static void SavePng(string payload, Stream stream, QrEasyOptions? options = null) {
        QrEasy.RenderPngToStream(payload, stream, options);
    }

    /// <summary>
    /// Saves a PNG QR to a stream for a payload with embedded defaults.
    /// </summary>
    public static void SavePng(QrPayloadData payload, Stream stream, QrEasyOptions? options = null) {
        QrEasy.RenderPngToStream(payload, stream, options);
    }

    /// <summary>
    /// Saves an SVG QR to a file.
    /// </summary>
    public static string SaveSvg(string payload, string path, QrEasyOptions? options = null) {
        return Svg(payload, options).WriteText(path);
    }

    /// <summary>
    /// Saves an SVG QR to a file for a payload with embedded defaults.
    /// </summary>
    public static string SaveSvg(QrPayloadData payload, string path, QrEasyOptions? options = null) {
        return Svg(payload, options).WriteText(path);
    }

    /// <summary>
    /// Saves an SVG QR to a stream.
    /// </summary>
    public static void SaveSvg(string payload, Stream stream, QrEasyOptions? options = null) {
        Svg(payload, options).WriteText(stream);
    }

    /// <summary>
    /// Saves an SVG QR to a stream for a payload with embedded defaults.
    /// </summary>
    public static void SaveSvg(QrPayloadData payload, Stream stream, QrEasyOptions? options = null) {
        Svg(payload, options).WriteText(stream);
    }

    /// <summary>
    /// Saves an HTML QR to a file.
    /// </summary>
    public static string SaveHtml(string payload, string path, QrEasyOptions? options = null, string? title = null) {
        var html = Html(payload, options);
        if (!string.IsNullOrEmpty(title)) html = html.WrapHtml(title);
        return html.WriteText(path);
    }

    /// <summary>
    /// Saves an HTML QR to a file for a payload with embedded defaults.
    /// </summary>
    public static string SaveHtml(QrPayloadData payload, string path, QrEasyOptions? options = null, string? title = null) {
        var html = Html(payload, options);
        if (!string.IsNullOrEmpty(title)) html = html.WrapHtml(title);
        return html.WriteText(path);
    }

    /// <summary>
    /// Saves an HTML QR to a stream.
    /// </summary>
    public static void SaveHtml(string payload, Stream stream, QrEasyOptions? options = null, string? title = null) {
        var html = Html(payload, options);
        if (!string.IsNullOrEmpty(title)) html = html.WrapHtml(title);
        html.WriteText(stream);
    }

    /// <summary>
    /// Saves an HTML QR to a stream for a payload with embedded defaults.
    /// </summary>
    public static void SaveHtml(QrPayloadData payload, Stream stream, QrEasyOptions? options = null, string? title = null) {
        var html = Html(payload, options);
        if (!string.IsNullOrEmpty(title)) html = html.WrapHtml(title);
        html.WriteText(stream);
    }

    /// <summary>
    /// Saves a JPEG QR to a file.
    /// </summary>
    public static string SaveJpeg(string payload, string path, QrEasyOptions? options = null) {
        return Jpeg(payload, options).WriteBinary(path);
    }

    /// <summary>
    /// Saves a JPEG QR to a file for a payload with embedded defaults.
    /// </summary>
    public static string SaveJpeg(QrPayloadData payload, string path, QrEasyOptions? options = null) {
        return Jpeg(payload, options).WriteBinary(path);
    }

    /// <summary>
    /// Saves a JPEG QR to a stream.
    /// </summary>
    public static void SaveJpeg(string payload, Stream stream, QrEasyOptions? options = null) {
        QrEasy.RenderJpegToStream(payload, stream, options);
    }

    /// <summary>
    /// Saves a JPEG QR to a stream for a payload with embedded defaults.
    /// </summary>
    public static void SaveJpeg(QrPayloadData payload, Stream stream, QrEasyOptions? options = null) {
        QrEasy.RenderJpegToStream(payload, stream, options);
    }

    /// <summary>
    /// Saves a WebP QR to a file.
    /// </summary>
    public static string SaveWebp(string payload, string path, QrEasyOptions? options = null) {
        return Webp(payload, options).WriteBinary(path);
    }

    /// <summary>
    /// Saves a WebP QR to a file for a payload with embedded defaults.
    /// </summary>
    public static string SaveWebp(QrPayloadData payload, string path, QrEasyOptions? options = null) {
        return Webp(payload, options).WriteBinary(path);
    }

    /// <summary>
    /// Saves a WebP QR to a stream.
    /// </summary>
    public static void SaveWebp(string payload, Stream stream, QrEasyOptions? options = null) {
        QrEasy.RenderWebpToStream(payload, stream, options);
    }

    /// <summary>
    /// Saves a WebP QR to a stream for a payload with embedded defaults.
    /// </summary>
    public static void SaveWebp(QrPayloadData payload, Stream stream, QrEasyOptions? options = null) {
        QrEasy.RenderWebpToStream(payload, stream, options);
    }

    /// <summary>
    /// Saves a BMP QR to a file.
    /// </summary>
    public static string SaveBmp(string payload, string path, QrEasyOptions? options = null) {
        return Bmp(payload, options).WriteBinary(path);
    }

    /// <summary>
    /// Saves a BMP QR to a file for a payload with embedded defaults.
    /// </summary>
    public static string SaveBmp(QrPayloadData payload, string path, QrEasyOptions? options = null) {
        return Bmp(payload, options).WriteBinary(path);
    }

    /// <summary>
    /// Saves a BMP QR to a stream.
    /// </summary>
    public static void SaveBmp(string payload, Stream stream, QrEasyOptions? options = null) {
        QrEasy.RenderBmpToStream(payload, stream, options);
    }

    /// <summary>
    /// Saves a BMP QR to a stream for a payload with embedded defaults.
    /// </summary>
    public static void SaveBmp(QrPayloadData payload, Stream stream, QrEasyOptions? options = null) {
        QrEasy.RenderBmpToStream(payload, stream, options);
    }

    /// <summary>
    /// Renders a QR code as PPM and writes it to a file.
    /// </summary>
    public static string SavePpm(string payload, string path, QrEasyOptions? options = null) {
        var ppm = Ppm(payload, options);
        return ppm.WriteBinary(path);
    }

    /// <summary>
    /// Renders a QR code as PPM and writes it to a file for a payload with embedded defaults.
    /// </summary>
    public static string SavePpm(QrPayloadData payload, string path, QrEasyOptions? options = null) {
        var ppm = Ppm(payload, options);
        return ppm.WriteBinary(path);
    }

    /// <summary>
    /// Renders a QR code as PPM and writes it to a stream.
    /// </summary>
    public static void SavePpm(string payload, Stream stream, QrEasyOptions? options = null) {
        QrEasy.RenderPpmToStream(payload, stream, options);
    }

    /// <summary>
    /// Renders a QR code as PPM and writes it to a stream for a payload with embedded defaults.
    /// </summary>
    public static void SavePpm(QrPayloadData payload, Stream stream, QrEasyOptions? options = null) {
        QrEasy.RenderPpmToStream(payload, stream, options);
    }

    /// <summary>
    /// Renders a QR code as PBM and writes it to a file.
    /// </summary>
    public static string SavePbm(string payload, string path, QrEasyOptions? options = null) {
        var pbm = Pbm(payload, options);
        return pbm.WriteBinary(path);
    }

    /// <summary>
    /// Renders a QR code as PBM and writes it to a file for a payload with embedded defaults.
    /// </summary>
    public static string SavePbm(QrPayloadData payload, string path, QrEasyOptions? options = null) {
        var pbm = Pbm(payload, options);
        return pbm.WriteBinary(path);
    }

    /// <summary>
    /// Renders a QR code as PBM and writes it to a stream.
    /// </summary>
    public static void SavePbm(string payload, Stream stream, QrEasyOptions? options = null) {
        QrEasy.RenderPbmToStream(payload, stream, options);
    }

    /// <summary>
    /// Renders a QR code as PBM and writes it to a stream for a payload with embedded defaults.
    /// </summary>
    public static void SavePbm(QrPayloadData payload, Stream stream, QrEasyOptions? options = null) {
        QrEasy.RenderPbmToStream(payload, stream, options);
    }

    /// <summary>
    /// Renders a QR code as PGM and writes it to a file.
    /// </summary>
    public static string SavePgm(string payload, string path, QrEasyOptions? options = null) {
        var pgm = Pgm(payload, options);
        return pgm.WriteBinary(path);
    }

    /// <summary>
    /// Renders a QR code as PGM and writes it to a file for a payload with embedded defaults.
    /// </summary>
    public static string SavePgm(QrPayloadData payload, string path, QrEasyOptions? options = null) {
        var pgm = Pgm(payload, options);
        return pgm.WriteBinary(path);
    }

    /// <summary>
    /// Renders a QR code as PGM and writes it to a stream.
    /// </summary>
    public static void SavePgm(string payload, Stream stream, QrEasyOptions? options = null) {
        QrEasy.RenderPgmToStream(payload, stream, options);
    }

    /// <summary>
    /// Renders a QR code as PGM and writes it to a stream for a payload with embedded defaults.
    /// </summary>
    public static void SavePgm(QrPayloadData payload, Stream stream, QrEasyOptions? options = null) {
        QrEasy.RenderPgmToStream(payload, stream, options);
    }

    /// <summary>
    /// Renders a QR code as PAM and writes it to a file.
    /// </summary>
    public static string SavePam(string payload, string path, QrEasyOptions? options = null) {
        var pam = Pam(payload, options);
        return pam.WriteBinary(path);
    }

    /// <summary>
    /// Renders a QR code as PAM and writes it to a file for a payload with embedded defaults.
    /// </summary>
    public static string SavePam(QrPayloadData payload, string path, QrEasyOptions? options = null) {
        var pam = Pam(payload, options);
        return pam.WriteBinary(path);
    }

    /// <summary>
    /// Renders a QR code as PAM and writes it to a stream.
    /// </summary>
    public static void SavePam(string payload, Stream stream, QrEasyOptions? options = null) {
        QrEasy.RenderPamToStream(payload, stream, options);
    }

    /// <summary>
    /// Renders a QR code as PAM and writes it to a stream for a payload with embedded defaults.
    /// </summary>
    public static void SavePam(QrPayloadData payload, Stream stream, QrEasyOptions? options = null) {
        QrEasy.RenderPamToStream(payload, stream, options);
    }

    /// <summary>
    /// Renders a QR code as XBM and writes it to a file.
    /// </summary>
    public static string SaveXbm(string payload, string path, QrEasyOptions? options = null) {
        var xbm = Xbm(payload, options);
        return xbm.WriteText(path);
    }

    /// <summary>
    /// Renders a QR code as XBM and writes it to a file for a payload with embedded defaults.
    /// </summary>
    public static string SaveXbm(QrPayloadData payload, string path, QrEasyOptions? options = null) {
        var xbm = Xbm(payload, options);
        return xbm.WriteText(path);
    }

    /// <summary>
    /// Renders a QR code as XBM and writes it to a stream.
    /// </summary>
    public static void SaveXbm(string payload, Stream stream, QrEasyOptions? options = null) {
        QrEasy.RenderXbmToStream(payload, stream, options);
    }

    /// <summary>
    /// Renders a QR code as XBM and writes it to a stream for a payload with embedded defaults.
    /// </summary>
    public static void SaveXbm(QrPayloadData payload, Stream stream, QrEasyOptions? options = null) {
        QrEasy.RenderXbmToStream(payload, stream, options);
    }

    /// <summary>
    /// Renders a QR code as XPM and writes it to a file.
    /// </summary>
    public static string SaveXpm(string payload, string path, QrEasyOptions? options = null) {
        var xpm = Xpm(payload, options);
        return xpm.WriteText(path);
    }

    /// <summary>
    /// Renders a QR code as XPM and writes it to a file for a payload with embedded defaults.
    /// </summary>
    public static string SaveXpm(QrPayloadData payload, string path, QrEasyOptions? options = null) {
        var xpm = Xpm(payload, options);
        return xpm.WriteText(path);
    }

    /// <summary>
    /// Renders a QR code as XPM and writes it to a stream.
    /// </summary>
    public static void SaveXpm(string payload, Stream stream, QrEasyOptions? options = null) {
        QrEasy.RenderXpmToStream(payload, stream, options);
    }

    /// <summary>
    /// Renders a QR code as XPM and writes it to a stream for a payload with embedded defaults.
    /// </summary>
    public static void SaveXpm(QrPayloadData payload, Stream stream, QrEasyOptions? options = null) {
        QrEasy.RenderXpmToStream(payload, stream, options);
    }

    /// <summary>
    /// Renders a QR code as TGA and writes it to a file.
    /// </summary>
    public static string SaveTga(string payload, string path, QrEasyOptions? options = null) {
        var tga = Tga(payload, options);
        return tga.WriteBinary(path);
    }

    /// <summary>
    /// Renders a QR code as TGA and writes it to a file for a payload with embedded defaults.
    /// </summary>
    public static string SaveTga(QrPayloadData payload, string path, QrEasyOptions? options = null) {
        var tga = Tga(payload, options);
        return tga.WriteBinary(path);
    }

    /// <summary>
    /// Renders a QR code as TGA and writes it to a stream.
    /// </summary>
    public static void SaveTga(string payload, Stream stream, QrEasyOptions? options = null) {
        QrEasy.RenderTgaToStream(payload, stream, options);
    }

    /// <summary>
    /// Renders a QR code as TGA and writes it to a stream for a payload with embedded defaults.
    /// </summary>
    public static void SaveTga(QrPayloadData payload, Stream stream, QrEasyOptions? options = null) {
        QrEasy.RenderTgaToStream(payload, stream, options);
    }

    /// <summary>
    /// Renders a QR code as ICO and writes it to a file.
    /// </summary>
    public static string SaveIco(string payload, string path, QrEasyOptions? options = null) {
        var ico = Ico(payload, options);
        return ico.WriteBinary(path);
    }

    /// <summary>
    /// Renders a QR code as ICO and writes it to a file for a payload with embedded defaults.
    /// </summary>
    public static string SaveIco(QrPayloadData payload, string path, QrEasyOptions? options = null) {
        var ico = Ico(payload, options);
        return ico.WriteBinary(path);
    }

    /// <summary>
    /// Renders a QR code as ICO and writes it to a stream.
    /// </summary>
    public static void SaveIco(string payload, Stream stream, QrEasyOptions? options = null) {
        QrEasy.RenderIcoToStream(payload, stream, options);
    }

    /// <summary>
    /// Renders a QR code as ICO and writes it to a stream for a payload with embedded defaults.
    /// </summary>
    public static void SaveIco(QrPayloadData payload, Stream stream, QrEasyOptions? options = null) {
        QrEasy.RenderIcoToStream(payload, stream, options);
    }

    /// <summary>
    /// Renders a QR code as SVGZ and writes it to a file.
    /// </summary>
    public static string SaveSvgz(string payload, string path, QrEasyOptions? options = null) {
        var svgz = Svgz(payload, options);
        return svgz.WriteBinary(path);
    }

    /// <summary>
    /// Renders a QR code as SVGZ and writes it to a file for a payload with embedded defaults.
    /// </summary>
    public static string SaveSvgz(QrPayloadData payload, string path, QrEasyOptions? options = null) {
        var svgz = Svgz(payload, options);
        return svgz.WriteBinary(path);
    }

    /// <summary>
    /// Renders a QR code as SVGZ and writes it to a stream.
    /// </summary>
    public static void SaveSvgz(string payload, Stream stream, QrEasyOptions? options = null) {
        QrEasy.RenderSvgzToStream(payload, stream, options);
    }

    /// <summary>
    /// Renders a QR code as SVGZ and writes it to a stream for a payload with embedded defaults.
    /// </summary>
    public static void SaveSvgz(QrPayloadData payload, Stream stream, QrEasyOptions? options = null) {
        QrEasy.RenderSvgzToStream(payload, stream, options);
    }

    /// <summary>
    /// Saves a PDF QR to a file.
    /// </summary>
    /// <param name="payload">The payload text.</param>
    /// <param name="path">Output file path.</param>
    /// <param name="options">Optional rendering options.</param>
    /// <param name="mode">Vector or raster output.</param>
    public static string SavePdf(string payload, string path, QrEasyOptions? options = null, RenderMode mode = RenderMode.Vector) {
        return Pdf(payload, options, mode).WriteBinary(path);
    }

    /// <summary>
    /// Saves a PDF QR to a file for a payload with embedded defaults.
    /// </summary>
    /// <param name="payload">The payload text.</param>
    /// <param name="path">Output file path.</param>
    /// <param name="options">Optional rendering options.</param>
    /// <param name="mode">Vector or raster output.</param>
    public static string SavePdf(QrPayloadData payload, string path, QrEasyOptions? options = null, RenderMode mode = RenderMode.Vector) {
        return Pdf(payload, options, mode).WriteBinary(path);
    }

    /// <summary>
    /// Saves a PDF QR to a stream.
    /// </summary>
    /// <param name="payload">The payload text.</param>
    /// <param name="stream">Destination stream.</param>
    /// <param name="options">Optional rendering options.</param>
    /// <param name="mode">Vector or raster output.</param>
    public static void SavePdf(string payload, Stream stream, QrEasyOptions? options = null, RenderMode mode = RenderMode.Vector) {
        QrEasy.RenderPdfToStream(payload, stream, options, mode);
    }

    /// <summary>
    /// Saves a PDF QR to a stream for a payload with embedded defaults.
    /// </summary>
    /// <param name="payload">The payload text.</param>
    /// <param name="stream">Destination stream.</param>
    /// <param name="options">Optional rendering options.</param>
    /// <param name="mode">Vector or raster output.</param>
    public static void SavePdf(QrPayloadData payload, Stream stream, QrEasyOptions? options = null, RenderMode mode = RenderMode.Vector) {
        QrEasy.RenderPdfToStream(payload, stream, options, mode);
    }

    /// <summary>
    /// Saves an EPS QR to a file.
    /// </summary>
    /// <param name="payload">The payload text.</param>
    /// <param name="path">Output file path.</param>
    /// <param name="options">Optional rendering options.</param>
    /// <param name="mode">Vector or raster output.</param>
    public static string SaveEps(string payload, string path, QrEasyOptions? options = null, RenderMode mode = RenderMode.Vector) {
        return Eps(payload, options, mode).WriteText(path);
    }

    /// <summary>
    /// Saves an EPS QR to a file for a payload with embedded defaults.
    /// </summary>
    /// <param name="payload">The payload text.</param>
    /// <param name="path">Output file path.</param>
    /// <param name="options">Optional rendering options.</param>
    /// <param name="mode">Vector or raster output.</param>
    public static string SaveEps(QrPayloadData payload, string path, QrEasyOptions? options = null, RenderMode mode = RenderMode.Vector) {
        return Eps(payload, options, mode).WriteText(path);
    }

    /// <summary>
    /// Saves an EPS QR to a stream.
    /// </summary>
    /// <param name="payload">The payload text.</param>
    /// <param name="stream">Destination stream.</param>
    /// <param name="options">Optional rendering options.</param>
    /// <param name="mode">Vector or raster output.</param>
    public static void SaveEps(string payload, Stream stream, QrEasyOptions? options = null, RenderMode mode = RenderMode.Vector) {
        QrEasy.RenderEpsToStream(payload, stream, options, mode);
    }

    /// <summary>
    /// Saves an EPS QR to a stream for a payload with embedded defaults.
    /// </summary>
    /// <param name="payload">The payload text.</param>
    /// <param name="stream">Destination stream.</param>
    /// <param name="options">Optional rendering options.</param>
    /// <param name="mode">Vector or raster output.</param>
    public static void SaveEps(QrPayloadData payload, Stream stream, QrEasyOptions? options = null, RenderMode mode = RenderMode.Vector) {
        QrEasy.RenderEpsToStream(payload, stream, options, mode);
    }

    /// <summary>
    /// Saves a QR code to a file based on the file extension (.png/.webp/.svg/.svgz/.html/.jpg/.bmp/.ppm/.pbm/.pgm/.pam/.xbm/.xpm/.tga/.ico/.pdf/.eps).
    /// Defaults to PNG when no extension is provided.
    /// </summary>
    public static string Save(string payload, string path, QrEasyOptions? options = null, string? title = null) {
        return SaveByExtension(path, payload, null, options, title);
    }

    /// <summary>
    /// Detects a payload type and saves a QR code to a file based on the file extension (.png/.webp/.svg/.svgz/.html/.jpg/.bmp/.ppm/.pbm/.pgm/.pam/.xbm/.xpm/.tga/.ico/.pdf/.eps).
    /// Defaults to PNG when no extension is provided.
    /// </summary>
    public static string SaveAuto(string payload, string path, QrPayloadDetectOptions? detectOptions = null, QrEasyOptions? options = null, string? title = null) {
        var detected = QrPayloads.Detect(payload, detectOptions);
        return SaveByExtension(path, detected.Text, detected, options, title);
    }

    /// <summary>
    /// Saves a QR code to a file based on the file extension (.png/.webp/.svg/.svgz/.html/.jpg/.bmp/.ppm/.pbm/.pgm/.pam/.xbm/.xpm/.tga/.ico/.pdf/.eps).
    /// Defaults to PNG when no extension is provided.
    /// </summary>
    public static string Save(QrPayloadData payload, string path, QrEasyOptions? options = null, string? title = null) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        return SaveByExtension(path, payload.Text, payload, options, title);
    }

    private static string SaveByExtension(string path, string payload, QrPayloadData? payloadData, QrEasyOptions? options, string? title) {
        if (path.EndsWith(".svgz", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".svg.gz", StringComparison.OrdinalIgnoreCase)) {
            return payloadData is null ? SaveSvgz(payload, path, options) : SaveSvgz(payloadData, path, options);
        }

        var ext = Path.GetExtension(path);
        if (string.IsNullOrWhiteSpace(ext)) {
            return payloadData is null ? SavePng(payload, path, options) : SavePng(payloadData, path, options);
        }

        switch (ext.ToLowerInvariant()) {
            case ".png":
                return payloadData is null ? SavePng(payload, path, options) : SavePng(payloadData, path, options);
            case ".webp":
                return payloadData is null ? SaveWebp(payload, path, options) : SaveWebp(payloadData, path, options);
            case ".svg":
                return payloadData is null ? SaveSvg(payload, path, options) : SaveSvg(payloadData, path, options);
            case ".html":
            case ".htm":
                return payloadData is null ? SaveHtml(payload, path, options, title) : SaveHtml(payloadData, path, options, title);
            case ".jpg":
            case ".jpeg":
                return payloadData is null ? SaveJpeg(payload, path, options) : SaveJpeg(payloadData, path, options);
            case ".bmp":
                return payloadData is null ? SaveBmp(payload, path, options) : SaveBmp(payloadData, path, options);
            case ".ppm":
                return payloadData is null ? SavePpm(payload, path, options) : SavePpm(payloadData, path, options);
            case ".pbm":
                return payloadData is null ? SavePbm(payload, path, options) : SavePbm(payloadData, path, options);
            case ".pgm":
                return payloadData is null ? SavePgm(payload, path, options) : SavePgm(payloadData, path, options);
            case ".pam":
                return payloadData is null ? SavePam(payload, path, options) : SavePam(payloadData, path, options);
            case ".xbm":
                return payloadData is null ? SaveXbm(payload, path, options) : SaveXbm(payloadData, path, options);
            case ".xpm":
                return payloadData is null ? SaveXpm(payload, path, options) : SaveXpm(payloadData, path, options);
            case ".tga":
                return payloadData is null ? SaveTga(payload, path, options) : SaveTga(payloadData, path, options);
            case ".ico":
                return payloadData is null ? SaveIco(payload, path, options) : SaveIco(payloadData, path, options);
            case ".svgz":
                return payloadData is null ? SaveSvgz(payload, path, options) : SaveSvgz(payloadData, path, options);
            case ".pdf":
                return payloadData is null ? SavePdf(payload, path, options) : SavePdf(payloadData, path, options);
            case ".eps":
            case ".ps":
                return payloadData is null ? SaveEps(payload, path, options) : SaveEps(payloadData, path, options);
            default:
                // Fallback to PNG for unknown extensions to keep the API forgiving.
                return payloadData is null ? SavePng(payload, path, options) : SavePng(payloadData, path, options);
        }
    }
}
