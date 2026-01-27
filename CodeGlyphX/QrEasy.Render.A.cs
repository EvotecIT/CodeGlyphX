using System;
using System.Globalization;
using System.IO;
using CodeGlyphX.Payloads;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Ascii;
using CodeGlyphX.Rendering.Bmp;
using CodeGlyphX.Rendering.Eps;
using CodeGlyphX.Rendering.Html;
using CodeGlyphX.Rendering.Ico;
using CodeGlyphX.Rendering.Jpeg;
using CodeGlyphX.Rendering.Pam;
using CodeGlyphX.Rendering.Pbm;
using CodeGlyphX.Rendering.Pgm;
using CodeGlyphX.Rendering.Pdf;
using CodeGlyphX.Rendering.Ppm;
using CodeGlyphX.Rendering.Png;
using CodeGlyphX.Rendering.Svg;
using CodeGlyphX.Rendering.Svgz;
using CodeGlyphX.Rendering.Tga;
using CodeGlyphX.Rendering.Xbm;
using CodeGlyphX.Rendering.Xpm;

namespace CodeGlyphX;

public static partial class QrEasy {
    /// <summary>
    /// Renders a QR code as PNG.
    /// </summary>
    public static byte[] RenderPng(string payload, QrEasyOptions? options = null) {
        var opts = options is null ? new QrEasyOptions() : CloneOptions(options);
        var qr = Encode(payload, opts);
        var render = BuildPngOptions(opts, qr);
        return QrPngRenderer.Render(qr.Modules, render);
    }

    /// <summary>
    /// Detects a payload type and renders a QR code as PNG.
    /// </summary>
    public static byte[] RenderPngAuto(string payload, QrPayloadDetectOptions? detectOptions = null, QrEasyOptions? options = null) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        var detected = QrPayloads.Detect(payload, detectOptions);
        return RenderPng(detected, options);
    }

    /// <summary>
    /// Renders a QR code as PNG to a stream.
    /// </summary>
    public static void RenderPngToStream(string payload, Stream stream, QrEasyOptions? options = null) {
        var opts = options is null ? new QrEasyOptions() : CloneOptions(options);
        var qr = Encode(payload, opts);
        var render = BuildPngOptions(opts, qr);
        QrPngRenderer.RenderToStream(qr.Modules, render, stream);
    }

    /// <summary>
    /// Renders a QR code as PNG and writes it to a file.
    /// </summary>
    /// <param name="payload">Payload text.</param>
    /// <param name="path">Output file path.</param>
    /// <param name="options">Optional rendering options.</param>
    /// <returns>The output file path.</returns>
    public static string RenderPngToFile(string payload, string path, QrEasyOptions? options = null) {
        var png = RenderPng(payload, options);
        return RenderIO.WriteBinary(path, png);
    }

    /// <summary>
    /// Renders a QR code as PNG and writes it to a file for a payload with embedded defaults.
    /// </summary>
    /// <param name="payload">Payload with embedded defaults.</param>
    /// <param name="path">Output file path.</param>
    /// <param name="options">Optional rendering options.</param>
    /// <returns>The output file path.</returns>
    public static string RenderPngToFile(QrPayloadData payload, string path, QrEasyOptions? options = null) {
        var png = RenderPng(payload, options);
        return RenderIO.WriteBinary(path, png);
    }

    /// <summary>
    /// Renders a QR code as PNG for a payload with embedded defaults.
    /// </summary>
    public static byte[] RenderPng(QrPayloadData payload, QrEasyOptions? options = null) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        var opts = MergeOptions(payload, options);
        var qr = Encode(payload.Text, opts);
        var render = BuildPngOptions(opts, qr);
        return QrPngRenderer.Render(qr.Modules, render);
    }

    /// <summary>
    /// Renders a QR code as PNG to a stream for a payload with embedded defaults.
    /// </summary>
    public static void RenderPngToStream(QrPayloadData payload, Stream stream, QrEasyOptions? options = null) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        var opts = MergeOptions(payload, options);
        var qr = Encode(payload.Text, opts);
        var render = BuildPngOptions(opts, qr);
        QrPngRenderer.RenderToStream(qr.Modules, render, stream);
    }

    /// <summary>
    /// Renders a QR code as SVG.
    /// </summary>
    public static string RenderSvg(string payload, QrEasyOptions? options = null) {
        var opts = options is null ? new QrEasyOptions() : CloneOptions(options);
        var qr = Encode(payload, opts);
        var baseRender = BuildPngOptions(opts, qr);
        var render = new QrSvgRenderOptions {
            ModuleSize = baseRender.ModuleSize,
            QuietZone = baseRender.QuietZone,
            DarkColor = ToCss(baseRender.Foreground),
            LightColor = ToCss(baseRender.Background),
            Logo = BuildLogoOptions(opts),
            ModuleShape = baseRender.ModuleShape,
            ModuleScale = baseRender.ModuleScale,
            ModuleCornerRadiusPx = baseRender.ModuleCornerRadiusPx,
            ForegroundGradient = baseRender.ForegroundGradient,
            Eyes = baseRender.Eyes,
        };
        return SvgQrRenderer.Render(qr.Modules, render);
    }

    /// <summary>
    /// Detects a payload type and renders a QR code as SVG.
    /// </summary>
    public static string RenderSvgAuto(string payload, QrPayloadDetectOptions? detectOptions = null, QrEasyOptions? options = null) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        var detected = QrPayloads.Detect(payload, detectOptions);
        return RenderSvg(detected, options);
    }

    /// <summary>
    /// Renders a QR code as SVG and writes it to a file.
    /// </summary>
    /// <param name="payload">Payload text.</param>
    /// <param name="path">Output file path.</param>
    /// <param name="options">Optional rendering options.</param>
    /// <returns>The output file path.</returns>
    public static string RenderSvgToFile(string payload, string path, QrEasyOptions? options = null) {
        var svg = RenderSvg(payload, options);
        return RenderIO.WriteText(path, svg);
    }

    /// <summary>
    /// Renders a QR code as SVG and writes it to a file for a payload with embedded defaults.
    /// </summary>
    /// <param name="payload">Payload with embedded defaults.</param>
    /// <param name="path">Output file path.</param>
    /// <param name="options">Optional rendering options.</param>
    /// <returns>The output file path.</returns>
    public static string RenderSvgToFile(QrPayloadData payload, string path, QrEasyOptions? options = null) {
        var svg = RenderSvg(payload, options);
        return RenderIO.WriteText(path, svg);
    }

    /// <summary>
    /// Renders a QR code as SVG for a payload with embedded defaults.
    /// </summary>
    public static string RenderSvg(QrPayloadData payload, QrEasyOptions? options = null) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        var opts = MergeOptions(payload, options);
        var qr = Encode(payload.Text, opts);
        var baseRender = BuildPngOptions(opts, qr);
        var render = new QrSvgRenderOptions {
            ModuleSize = baseRender.ModuleSize,
            QuietZone = baseRender.QuietZone,
            DarkColor = ToCss(baseRender.Foreground),
            LightColor = ToCss(baseRender.Background),
            Logo = BuildLogoOptions(opts),
            ModuleShape = baseRender.ModuleShape,
            ModuleScale = baseRender.ModuleScale,
            ModuleCornerRadiusPx = baseRender.ModuleCornerRadiusPx,
            ForegroundGradient = baseRender.ForegroundGradient,
            Eyes = baseRender.Eyes,
        };
        return SvgQrRenderer.Render(qr.Modules, render);
    }

    /// <summary>
    /// Renders a QR code as HTML (table-based).
    /// </summary>
    public static string RenderHtml(string payload, QrEasyOptions? options = null) {
        var opts = options is null ? new QrEasyOptions() : CloneOptions(options);
        var qr = Encode(payload, opts);
        var baseRender = BuildPngOptions(opts, qr);
        var render = new QrHtmlRenderOptions {
            ModuleSize = baseRender.ModuleSize,
            QuietZone = baseRender.QuietZone,
            DarkColor = ToCss(baseRender.Foreground),
            LightColor = ToCss(baseRender.Background),
            EmailSafeTable = opts.HtmlEmailSafeTable,
            Logo = BuildLogoOptions(opts),
            ModuleShape = baseRender.ModuleShape,
            ModuleScale = baseRender.ModuleScale,
            ModuleCornerRadiusPx = baseRender.ModuleCornerRadiusPx,
            ForegroundGradient = baseRender.ForegroundGradient,
            Eyes = baseRender.Eyes,
        };
        return HtmlQrRenderer.Render(qr.Modules, render);
    }

    /// <summary>
    /// Detects a payload type and renders a QR code as HTML.
    /// </summary>
    public static string RenderHtmlAuto(string payload, QrPayloadDetectOptions? detectOptions = null, QrEasyOptions? options = null) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        var detected = QrPayloads.Detect(payload, detectOptions);
        return RenderHtml(detected, options);
    }

    /// <summary>
    /// Renders a QR code as HTML and writes it to a file.
    /// </summary>
    /// <param name="payload">Payload text.</param>
    /// <param name="path">Output file path.</param>
    /// <param name="options">Optional rendering options.</param>
    /// <returns>The output file path.</returns>
    public static string RenderHtmlToFile(string payload, string path, QrEasyOptions? options = null) {
        var html = RenderHtml(payload, options);
        return RenderIO.WriteText(path, html);
    }

    /// <summary>
    /// Renders a QR code as HTML and writes it to a file for a payload with embedded defaults.
    /// </summary>
    /// <param name="payload">Payload with embedded defaults.</param>
    /// <param name="path">Output file path.</param>
    /// <param name="options">Optional rendering options.</param>
    /// <returns>The output file path.</returns>
    public static string RenderHtmlToFile(QrPayloadData payload, string path, QrEasyOptions? options = null) {
        var html = RenderHtml(payload, options);
        return RenderIO.WriteText(path, html);
    }

    /// <summary>
    /// Renders a QR code as HTML for a payload with embedded defaults.
    /// </summary>
    public static string RenderHtml(QrPayloadData payload, QrEasyOptions? options = null) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        var opts = MergeOptions(payload, options);
        var qr = Encode(payload.Text, opts);
        var baseRender = BuildPngOptions(opts, qr);
        var render = new QrHtmlRenderOptions {
            ModuleSize = baseRender.ModuleSize,
            QuietZone = baseRender.QuietZone,
            DarkColor = ToCss(baseRender.Foreground),
            LightColor = ToCss(baseRender.Background),
            EmailSafeTable = opts.HtmlEmailSafeTable,
            Logo = BuildLogoOptions(opts),
            ModuleShape = baseRender.ModuleShape,
            ModuleScale = baseRender.ModuleScale,
            ModuleCornerRadiusPx = baseRender.ModuleCornerRadiusPx,
            ForegroundGradient = baseRender.ForegroundGradient,
            Eyes = baseRender.Eyes,
        };
        return HtmlQrRenderer.Render(qr.Modules, render);
    }

    /// <summary>
    /// Renders a QR code as JPEG.
    /// </summary>
    public static byte[] RenderJpeg(string payload, QrEasyOptions? options = null) {
        var opts = options is null ? new QrEasyOptions() : CloneOptions(options);
        var qr = Encode(payload, opts);
        var render = BuildPngOptions(opts, qr);
        return QrJpegRenderer.Render(qr.Modules, render, opts.JpegQuality);
    }

    /// <summary>
    /// Detects a payload type and renders a QR code as JPEG.
    /// </summary>
    public static byte[] RenderJpegAuto(string payload, QrPayloadDetectOptions? detectOptions = null, QrEasyOptions? options = null) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        var detected = QrPayloads.Detect(payload, detectOptions);
        return RenderJpeg(detected, options);
    }

    /// <summary>
    /// Renders a QR code as JPEG to a stream.
    /// </summary>
    public static void RenderJpegToStream(string payload, Stream stream, QrEasyOptions? options = null) {
        var opts = options is null ? new QrEasyOptions() : CloneOptions(options);
        var qr = Encode(payload, opts);
        var render = BuildPngOptions(opts, qr);
        QrJpegRenderer.RenderToStream(qr.Modules, render, stream, opts.JpegQuality);
    }

    /// <summary>
    /// Renders a QR code as JPEG and writes it to a file.
    /// </summary>
    /// <param name="payload">Payload text.</param>
    /// <param name="path">Output file path.</param>
    /// <param name="options">Optional rendering options.</param>
    /// <returns>The output file path.</returns>
    public static string RenderJpegToFile(string payload, string path, QrEasyOptions? options = null) {
        var jpeg = RenderJpeg(payload, options);
        return RenderIO.WriteBinary(path, jpeg);
    }

    /// <summary>
    /// Renders a QR code as JPEG and writes it to a file for a payload with embedded defaults.
    /// </summary>
    /// <param name="payload">Payload with embedded defaults.</param>
    /// <param name="path">Output file path.</param>
    /// <param name="options">Optional rendering options.</param>
    /// <returns>The output file path.</returns>
    public static string RenderJpegToFile(QrPayloadData payload, string path, QrEasyOptions? options = null) {
        var jpeg = RenderJpeg(payload, options);
        return RenderIO.WriteBinary(path, jpeg);
    }

    /// <summary>
    /// Renders a QR code as JPEG for a payload with embedded defaults.
    /// </summary>
    public static byte[] RenderJpeg(QrPayloadData payload, QrEasyOptions? options = null) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        var opts = MergeOptions(payload, options);
        var qr = Encode(payload.Text, opts);
        var render = BuildPngOptions(opts, qr);
        return QrJpegRenderer.Render(qr.Modules, render, opts.JpegQuality);
    }

    /// <summary>
    /// Renders a QR code as JPEG to a stream for a payload with embedded defaults.
    /// </summary>
    public static void RenderJpegToStream(QrPayloadData payload, Stream stream, QrEasyOptions? options = null) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        var opts = MergeOptions(payload, options);
        var qr = Encode(payload.Text, opts);
        var render = BuildPngOptions(opts, qr);
        QrJpegRenderer.RenderToStream(qr.Modules, render, stream, opts.JpegQuality);
    }

    /// <summary>
    /// Renders a QR code as BMP.
    /// </summary>
    public static byte[] RenderBmp(string payload, QrEasyOptions? options = null) {
        var opts = options is null ? new QrEasyOptions() : CloneOptions(options);
        var qr = Encode(payload, opts);
        var render = BuildPngOptions(opts, qr);
        return QrBmpRenderer.Render(qr.Modules, render);
    }

    /// <summary>
    /// Detects a payload type and renders a QR code as BMP.
    /// </summary>
    public static byte[] RenderBmpAuto(string payload, QrPayloadDetectOptions? detectOptions = null, QrEasyOptions? options = null) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        var detected = QrPayloads.Detect(payload, detectOptions);
        return RenderBmp(detected, options);
    }

    /// <summary>
    /// Renders a QR code as BMP to a stream.
    /// </summary>
    public static void RenderBmpToStream(string payload, Stream stream, QrEasyOptions? options = null) {
        var opts = options is null ? new QrEasyOptions() : CloneOptions(options);
        var qr = Encode(payload, opts);
        var render = BuildPngOptions(opts, qr);
        QrBmpRenderer.RenderToStream(qr.Modules, render, stream);
    }

    /// <summary>
    /// Renders a QR code as BMP and writes it to a file.
    /// </summary>
    public static string RenderBmpToFile(string payload, string path, QrEasyOptions? options = null) {
        var bmp = RenderBmp(payload, options);
        return RenderIO.WriteBinary(path, bmp);
    }

    /// <summary>
    /// Renders a QR code as BMP and writes it to a file for a payload with embedded defaults.
    /// </summary>
    public static string RenderBmpToFile(QrPayloadData payload, string path, QrEasyOptions? options = null) {
        var bmp = RenderBmp(payload, options);
        return RenderIO.WriteBinary(path, bmp);
    }

    /// <summary>
    /// Renders a QR code as BMP for a payload with embedded defaults.
    /// </summary>
    public static byte[] RenderBmp(QrPayloadData payload, QrEasyOptions? options = null) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        var opts = MergeOptions(payload, options);
        var qr = Encode(payload.Text, opts);
        var render = BuildPngOptions(opts, qr);
        return QrBmpRenderer.Render(qr.Modules, render);
    }

    /// <summary>
    /// Renders a QR code as BMP to a stream for a payload with embedded defaults.
    /// </summary>
    public static void RenderBmpToStream(QrPayloadData payload, Stream stream, QrEasyOptions? options = null) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        var opts = MergeOptions(payload, options);
        var qr = Encode(payload.Text, opts);
        var render = BuildPngOptions(opts, qr);
        QrBmpRenderer.RenderToStream(qr.Modules, render, stream);
    }

    /// <summary>
    /// Renders a QR code as PPM.
    /// </summary>
    public static byte[] RenderPpm(string payload, QrEasyOptions? options = null) {
        var opts = options is null ? new QrEasyOptions() : CloneOptions(options);
        var qr = Encode(payload, opts);
        var render = BuildPngOptions(opts, qr);
        return QrPpmRenderer.Render(qr.Modules, render);
    }

    /// <summary>
    /// Detects a payload type and renders a QR code as PPM.
    /// </summary>
    public static byte[] RenderPpmAuto(string payload, QrPayloadDetectOptions? detectOptions = null, QrEasyOptions? options = null) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        var detected = QrPayloads.Detect(payload, detectOptions);
        return RenderPpm(detected, options);
    }

    /// <summary>
    /// Renders a QR code as PPM to a stream.
    /// </summary>
    public static void RenderPpmToStream(string payload, Stream stream, QrEasyOptions? options = null) {
        var opts = options is null ? new QrEasyOptions() : CloneOptions(options);
        var qr = Encode(payload, opts);
        var render = BuildPngOptions(opts, qr);
        QrPpmRenderer.RenderToStream(qr.Modules, render, stream);
    }

    /// <summary>
    /// Renders a QR code as PPM and writes it to a file.
    /// </summary>
    public static string RenderPpmToFile(string payload, string path, QrEasyOptions? options = null) {
        var ppm = RenderPpm(payload, options);
        return RenderIO.WriteBinary(path, ppm);
    }

    /// <summary>
    /// Renders a QR code as PPM and writes it to a file for a payload with embedded defaults.
    /// </summary>
    public static string RenderPpmToFile(QrPayloadData payload, string path, QrEasyOptions? options = null) {
        var ppm = RenderPpm(payload, options);
        return RenderIO.WriteBinary(path, ppm);
    }

    /// <summary>
    /// Renders a QR code as PPM for a payload with embedded defaults.
    /// </summary>
    public static byte[] RenderPpm(QrPayloadData payload, QrEasyOptions? options = null) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        var opts = MergeOptions(payload, options);
        var qr = Encode(payload.Text, opts);
        var render = BuildPngOptions(opts, qr);
        return QrPpmRenderer.Render(qr.Modules, render);
    }

    /// <summary>
    /// Renders a QR code as PPM to a stream for a payload with embedded defaults.
    /// </summary>
    public static void RenderPpmToStream(QrPayloadData payload, Stream stream, QrEasyOptions? options = null) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        var opts = MergeOptions(payload, options);
        var qr = Encode(payload.Text, opts);
        var render = BuildPngOptions(opts, qr);
        QrPpmRenderer.RenderToStream(qr.Modules, render, stream);
    }

    /// <summary>
    /// Renders a QR code as PBM.
    /// </summary>
    public static byte[] RenderPbm(string payload, QrEasyOptions? options = null) {
        var opts = options is null ? new QrEasyOptions() : CloneOptions(options);
        var qr = Encode(payload, opts);
        var render = BuildPngOptions(opts, qr);
        return QrPbmRenderer.Render(qr.Modules, render);
    }

    /// <summary>
    /// Detects a payload type and renders a QR code as PBM.
    /// </summary>
    public static byte[] RenderPbmAuto(string payload, QrPayloadDetectOptions? detectOptions = null, QrEasyOptions? options = null) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        var detected = QrPayloads.Detect(payload, detectOptions);
        return RenderPbm(detected, options);
    }

    /// <summary>
    /// Renders a QR code as PBM to a stream.
    /// </summary>
    public static void RenderPbmToStream(string payload, Stream stream, QrEasyOptions? options = null) {
        var opts = options is null ? new QrEasyOptions() : CloneOptions(options);
        var qr = Encode(payload, opts);
        var render = BuildPngOptions(opts, qr);
        QrPbmRenderer.RenderToStream(qr.Modules, render, stream);
    }

    /// <summary>
    /// Renders a QR code as PBM and writes it to a file.
    /// </summary>
    public static string RenderPbmToFile(string payload, string path, QrEasyOptions? options = null) {
        var pbm = RenderPbm(payload, options);
        return RenderIO.WriteBinary(path, pbm);
    }

    /// <summary>
    /// Renders a QR code as PBM and writes it to a file for a payload with embedded defaults.
    /// </summary>
    public static string RenderPbmToFile(QrPayloadData payload, string path, QrEasyOptions? options = null) {
        var pbm = RenderPbm(payload, options);
        return RenderIO.WriteBinary(path, pbm);
    }

    /// <summary>
    /// Renders a QR code as PBM for a payload with embedded defaults.
    /// </summary>
    public static byte[] RenderPbm(QrPayloadData payload, QrEasyOptions? options = null) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        var opts = MergeOptions(payload, options);
        var qr = Encode(payload.Text, opts);
        var render = BuildPngOptions(opts, qr);
        return QrPbmRenderer.Render(qr.Modules, render);
    }

    /// <summary>
    /// Renders a QR code as PBM to a stream for a payload with embedded defaults.
    /// </summary>
    public static void RenderPbmToStream(QrPayloadData payload, Stream stream, QrEasyOptions? options = null) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        var opts = MergeOptions(payload, options);
        var qr = Encode(payload.Text, opts);
        var render = BuildPngOptions(opts, qr);
        QrPbmRenderer.RenderToStream(qr.Modules, render, stream);
    }

    /// <summary>
    /// Renders a QR code as PGM.
    /// </summary>
    public static byte[] RenderPgm(string payload, QrEasyOptions? options = null) {
        var opts = options is null ? new QrEasyOptions() : CloneOptions(options);
        var qr = Encode(payload, opts);
        var render = BuildPngOptions(opts, qr);
        return QrPgmRenderer.Render(qr.Modules, render);
    }

    /// <summary>
    /// Detects a payload type and renders a QR code as PGM.
    /// </summary>
    public static byte[] RenderPgmAuto(string payload, QrPayloadDetectOptions? detectOptions = null, QrEasyOptions? options = null) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        var detected = QrPayloads.Detect(payload, detectOptions);
        return RenderPgm(detected, options);
    }

    /// <summary>
    /// Renders a QR code as PGM to a stream.
    /// </summary>
    public static void RenderPgmToStream(string payload, Stream stream, QrEasyOptions? options = null) {
        var opts = options is null ? new QrEasyOptions() : CloneOptions(options);
        var qr = Encode(payload, opts);
        var render = BuildPngOptions(opts, qr);
        QrPgmRenderer.RenderToStream(qr.Modules, render, stream);
    }

    /// <summary>
    /// Renders a QR code as PGM and writes it to a file.
    /// </summary>
    public static string RenderPgmToFile(string payload, string path, QrEasyOptions? options = null) {
        var pgm = RenderPgm(payload, options);
        return RenderIO.WriteBinary(path, pgm);
    }

    /// <summary>
    /// Renders a QR code as PGM and writes it to a file for a payload with embedded defaults.
    /// </summary>
    public static string RenderPgmToFile(QrPayloadData payload, string path, QrEasyOptions? options = null) {
        var pgm = RenderPgm(payload, options);
        return RenderIO.WriteBinary(path, pgm);
    }

    /// <summary>
    /// Renders a QR code as PGM for a payload with embedded defaults.
    /// </summary>
    public static byte[] RenderPgm(QrPayloadData payload, QrEasyOptions? options = null) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        var opts = MergeOptions(payload, options);
        var qr = Encode(payload.Text, opts);
        var render = BuildPngOptions(opts, qr);
        return QrPgmRenderer.Render(qr.Modules, render);
    }

    /// <summary>
    /// Renders a QR code as PGM to a stream for a payload with embedded defaults.
    /// </summary>
    public static void RenderPgmToStream(QrPayloadData payload, Stream stream, QrEasyOptions? options = null) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        var opts = MergeOptions(payload, options);
        var qr = Encode(payload.Text, opts);
        var render = BuildPngOptions(opts, qr);
        QrPgmRenderer.RenderToStream(qr.Modules, render, stream);
    }

    /// <summary>
    /// Renders a QR code as PAM.
    /// </summary>
    public static byte[] RenderPam(string payload, QrEasyOptions? options = null) {
        var opts = options is null ? new QrEasyOptions() : CloneOptions(options);
        var qr = Encode(payload, opts);
        var render = BuildPngOptions(opts, qr);
        return QrPamRenderer.Render(qr.Modules, render);
    }

}
