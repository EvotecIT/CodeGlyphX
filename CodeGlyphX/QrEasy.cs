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

/// <summary>
/// One-line QR generation helpers with sane defaults.
/// </summary>
/// <example>
/// <code>
/// using CodeGlyphX;
/// var png = QrEasy.RenderPng("https://example.com");
/// var svg = QrEasy.RenderSvg("https://example.com");
/// </code>
/// </example>
public static class QrEasy {
    /// <summary>
    /// Encodes a payload into a <see cref="QrCode"/> with defaults.
    /// </summary>
    public static QrCode Encode(string payload, QrEasyOptions? options = null) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        var opts = options ?? new QrEasyOptions();
        return EncodePayload(payload, opts);
    }

    /// <summary>
    /// Detects a payload type and encodes it into a <see cref="QrCode"/>.
    /// </summary>
    public static QrCode EncodeAuto(string payload, QrPayloadDetectOptions? detectOptions = null, QrEasyOptions? options = null) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        var detected = QrPayloads.Detect(payload, detectOptions);
        return Encode(detected, options);
    }

    /// <summary>
    /// Encodes a payload with embedded defaults.
    /// </summary>
    public static QrCode Encode(QrPayloadData payload, QrEasyOptions? options = null) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        var opts = MergeOptions(payload, options);
        return EncodePayload(payload.Text, opts);
    }

    /// <summary>
    /// Evaluates QR art safety for a payload and options.
    /// </summary>
    public static QrArtSafetyReport EvaluateSafety(string payload, QrEasyOptions? options = null) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        var opts = options ?? new QrEasyOptions();
        var qr = Encode(payload, opts);
        var render = BuildPngOptions(opts, payload, qr.Modules.Width);
        return QrArtSafety.Evaluate(qr, render);
    }

    /// <summary>
    /// Evaluates QR art safety for a payload with embedded defaults.
    /// </summary>
    public static QrArtSafetyReport EvaluateSafety(QrPayloadData payload, QrEasyOptions? options = null) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        var opts = MergeOptions(payload, options);
        var qr = Encode(payload.Text, opts);
        var render = BuildPngOptions(opts, payload.Text, qr.Modules.Width);
        return QrArtSafety.Evaluate(qr, render);
    }

    /// <summary>
    /// Renders a QR code as PNG.
    /// </summary>
    public static byte[] RenderPng(string payload, QrEasyOptions? options = null) {
        var opts = options ?? new QrEasyOptions();
        var qr = Encode(payload, opts);
        var render = BuildPngOptions(opts, payload, qr.Modules.Width);
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
        var opts = options ?? new QrEasyOptions();
        var qr = Encode(payload, opts);
        var render = BuildPngOptions(opts, payload, qr.Modules.Width);
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
        var render = BuildPngOptions(opts, payload.Text, qr.Modules.Width);
        return QrPngRenderer.Render(qr.Modules, render);
    }

    /// <summary>
    /// Renders a QR code as PNG to a stream for a payload with embedded defaults.
    /// </summary>
    public static void RenderPngToStream(QrPayloadData payload, Stream stream, QrEasyOptions? options = null) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        var opts = MergeOptions(payload, options);
        var qr = Encode(payload.Text, opts);
        var render = BuildPngOptions(opts, payload.Text, qr.Modules.Width);
        QrPngRenderer.RenderToStream(qr.Modules, render, stream);
    }

    /// <summary>
    /// Renders a QR code as SVG.
    /// </summary>
    public static string RenderSvg(string payload, QrEasyOptions? options = null) {
        var opts = options ?? new QrEasyOptions();
        var qr = Encode(payload, opts);
        var baseRender = BuildPngOptions(opts, payload, qr.Modules.Width);
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
        var baseRender = BuildPngOptions(opts, payload.Text, qr.Modules.Width);
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
        var opts = options ?? new QrEasyOptions();
        var qr = Encode(payload, opts);
        var baseRender = BuildPngOptions(opts, payload, qr.Modules.Width);
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
        var baseRender = BuildPngOptions(opts, payload.Text, qr.Modules.Width);
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
        var opts = options ?? new QrEasyOptions();
        var qr = Encode(payload, opts);
        var render = BuildPngOptions(opts, payload, qr.Modules.Width);
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
        var opts = options ?? new QrEasyOptions();
        var qr = Encode(payload, opts);
        var render = BuildPngOptions(opts, payload, qr.Modules.Width);
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
        var render = BuildPngOptions(opts, payload.Text, qr.Modules.Width);
        return QrJpegRenderer.Render(qr.Modules, render, opts.JpegQuality);
    }

    /// <summary>
    /// Renders a QR code as JPEG to a stream for a payload with embedded defaults.
    /// </summary>
    public static void RenderJpegToStream(QrPayloadData payload, Stream stream, QrEasyOptions? options = null) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        var opts = MergeOptions(payload, options);
        var qr = Encode(payload.Text, opts);
        var render = BuildPngOptions(opts, payload.Text, qr.Modules.Width);
        QrJpegRenderer.RenderToStream(qr.Modules, render, stream, opts.JpegQuality);
    }

    /// <summary>
    /// Renders a QR code as BMP.
    /// </summary>
    public static byte[] RenderBmp(string payload, QrEasyOptions? options = null) {
        var opts = options ?? new QrEasyOptions();
        var qr = Encode(payload, opts);
        var render = BuildPngOptions(opts, payload, qr.Modules.Width);
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
        var opts = options ?? new QrEasyOptions();
        var qr = Encode(payload, opts);
        var render = BuildPngOptions(opts, payload, qr.Modules.Width);
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
        var render = BuildPngOptions(opts, payload.Text, qr.Modules.Width);
        return QrBmpRenderer.Render(qr.Modules, render);
    }

    /// <summary>
    /// Renders a QR code as BMP to a stream for a payload with embedded defaults.
    /// </summary>
    public static void RenderBmpToStream(QrPayloadData payload, Stream stream, QrEasyOptions? options = null) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        var opts = MergeOptions(payload, options);
        var qr = Encode(payload.Text, opts);
        var render = BuildPngOptions(opts, payload.Text, qr.Modules.Width);
        QrBmpRenderer.RenderToStream(qr.Modules, render, stream);
    }

    /// <summary>
    /// Renders a QR code as PPM.
    /// </summary>
    public static byte[] RenderPpm(string payload, QrEasyOptions? options = null) {
        var opts = options ?? new QrEasyOptions();
        var qr = Encode(payload, opts);
        var render = BuildPngOptions(opts, payload, qr.Modules.Width);
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
        var opts = options ?? new QrEasyOptions();
        var qr = Encode(payload, opts);
        var render = BuildPngOptions(opts, payload, qr.Modules.Width);
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
        var render = BuildPngOptions(opts, payload.Text, qr.Modules.Width);
        return QrPpmRenderer.Render(qr.Modules, render);
    }

    /// <summary>
    /// Renders a QR code as PPM to a stream for a payload with embedded defaults.
    /// </summary>
    public static void RenderPpmToStream(QrPayloadData payload, Stream stream, QrEasyOptions? options = null) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        var opts = MergeOptions(payload, options);
        var qr = Encode(payload.Text, opts);
        var render = BuildPngOptions(opts, payload.Text, qr.Modules.Width);
        QrPpmRenderer.RenderToStream(qr.Modules, render, stream);
    }

    /// <summary>
    /// Renders a QR code as PBM.
    /// </summary>
    public static byte[] RenderPbm(string payload, QrEasyOptions? options = null) {
        var opts = options ?? new QrEasyOptions();
        var qr = Encode(payload, opts);
        var render = BuildPngOptions(opts, payload, qr.Modules.Width);
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
        var opts = options ?? new QrEasyOptions();
        var qr = Encode(payload, opts);
        var render = BuildPngOptions(opts, payload, qr.Modules.Width);
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
        var render = BuildPngOptions(opts, payload.Text, qr.Modules.Width);
        return QrPbmRenderer.Render(qr.Modules, render);
    }

    /// <summary>
    /// Renders a QR code as PBM to a stream for a payload with embedded defaults.
    /// </summary>
    public static void RenderPbmToStream(QrPayloadData payload, Stream stream, QrEasyOptions? options = null) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        var opts = MergeOptions(payload, options);
        var qr = Encode(payload.Text, opts);
        var render = BuildPngOptions(opts, payload.Text, qr.Modules.Width);
        QrPbmRenderer.RenderToStream(qr.Modules, render, stream);
    }

    /// <summary>
    /// Renders a QR code as PGM.
    /// </summary>
    public static byte[] RenderPgm(string payload, QrEasyOptions? options = null) {
        var opts = options ?? new QrEasyOptions();
        var qr = Encode(payload, opts);
        var render = BuildPngOptions(opts, payload, qr.Modules.Width);
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
        var opts = options ?? new QrEasyOptions();
        var qr = Encode(payload, opts);
        var render = BuildPngOptions(opts, payload, qr.Modules.Width);
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
        var render = BuildPngOptions(opts, payload.Text, qr.Modules.Width);
        return QrPgmRenderer.Render(qr.Modules, render);
    }

    /// <summary>
    /// Renders a QR code as PGM to a stream for a payload with embedded defaults.
    /// </summary>
    public static void RenderPgmToStream(QrPayloadData payload, Stream stream, QrEasyOptions? options = null) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        var opts = MergeOptions(payload, options);
        var qr = Encode(payload.Text, opts);
        var render = BuildPngOptions(opts, payload.Text, qr.Modules.Width);
        QrPgmRenderer.RenderToStream(qr.Modules, render, stream);
    }

    /// <summary>
    /// Renders a QR code as PAM.
    /// </summary>
    public static byte[] RenderPam(string payload, QrEasyOptions? options = null) {
        var opts = options ?? new QrEasyOptions();
        var qr = Encode(payload, opts);
        var render = BuildPngOptions(opts, payload, qr.Modules.Width);
        return QrPamRenderer.Render(qr.Modules, render);
    }

    /// <summary>
    /// Detects a payload type and renders a QR code as PAM.
    /// </summary>
    public static byte[] RenderPamAuto(string payload, QrPayloadDetectOptions? detectOptions = null, QrEasyOptions? options = null) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        var detected = QrPayloads.Detect(payload, detectOptions);
        return RenderPam(detected, options);
    }

    /// <summary>
    /// Renders a QR code as PAM to a stream.
    /// </summary>
    public static void RenderPamToStream(string payload, Stream stream, QrEasyOptions? options = null) {
        var opts = options ?? new QrEasyOptions();
        var qr = Encode(payload, opts);
        var render = BuildPngOptions(opts, payload, qr.Modules.Width);
        QrPamRenderer.RenderToStream(qr.Modules, render, stream);
    }

    /// <summary>
    /// Renders a QR code as PAM and writes it to a file.
    /// </summary>
    public static string RenderPamToFile(string payload, string path, QrEasyOptions? options = null) {
        var pam = RenderPam(payload, options);
        return RenderIO.WriteBinary(path, pam);
    }

    /// <summary>
    /// Renders a QR code as PAM and writes it to a file for a payload with embedded defaults.
    /// </summary>
    public static string RenderPamToFile(QrPayloadData payload, string path, QrEasyOptions? options = null) {
        var pam = RenderPam(payload, options);
        return RenderIO.WriteBinary(path, pam);
    }

    /// <summary>
    /// Renders a QR code as PAM for a payload with embedded defaults.
    /// </summary>
    public static byte[] RenderPam(QrPayloadData payload, QrEasyOptions? options = null) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        var opts = MergeOptions(payload, options);
        var qr = Encode(payload.Text, opts);
        var render = BuildPngOptions(opts, payload.Text, qr.Modules.Width);
        return QrPamRenderer.Render(qr.Modules, render);
    }

    /// <summary>
    /// Renders a QR code as PAM to a stream for a payload with embedded defaults.
    /// </summary>
    public static void RenderPamToStream(QrPayloadData payload, Stream stream, QrEasyOptions? options = null) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        var opts = MergeOptions(payload, options);
        var qr = Encode(payload.Text, opts);
        var render = BuildPngOptions(opts, payload.Text, qr.Modules.Width);
        QrPamRenderer.RenderToStream(qr.Modules, render, stream);
    }

    /// <summary>
    /// Renders a QR code as XBM.
    /// </summary>
    public static string RenderXbm(string payload, QrEasyOptions? options = null) {
        var opts = options ?? new QrEasyOptions();
        var qr = Encode(payload, opts);
        var render = BuildPngOptions(opts, payload, qr.Modules.Width);
        return QrXbmRenderer.Render(qr.Modules, render);
    }

    /// <summary>
    /// Detects a payload type and renders a QR code as XBM.
    /// </summary>
    public static string RenderXbmAuto(string payload, QrPayloadDetectOptions? detectOptions = null, QrEasyOptions? options = null) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        var detected = QrPayloads.Detect(payload, detectOptions);
        return RenderXbm(detected, options);
    }

    /// <summary>
    /// Renders a QR code as XBM to a stream.
    /// </summary>
    public static void RenderXbmToStream(string payload, Stream stream, QrEasyOptions? options = null) {
        var xbm = RenderXbm(payload, options);
        xbm.WriteText(stream);
    }

    /// <summary>
    /// Renders a QR code as XBM and writes it to a file.
    /// </summary>
    public static string RenderXbmToFile(string payload, string path, QrEasyOptions? options = null) {
        var xbm = RenderXbm(payload, options);
        return xbm.WriteText(path);
    }

    /// <summary>
    /// Renders a QR code as XBM and writes it to a file for a payload with embedded defaults.
    /// </summary>
    public static string RenderXbmToFile(QrPayloadData payload, string path, QrEasyOptions? options = null) {
        var xbm = RenderXbm(payload, options);
        return xbm.WriteText(path);
    }

    /// <summary>
    /// Renders a QR code as XBM for a payload with embedded defaults.
    /// </summary>
    public static string RenderXbm(QrPayloadData payload, QrEasyOptions? options = null) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        var opts = MergeOptions(payload, options);
        var qr = Encode(payload.Text, opts);
        var render = BuildPngOptions(opts, payload.Text, qr.Modules.Width);
        return QrXbmRenderer.Render(qr.Modules, render);
    }

    /// <summary>
    /// Renders a QR code as XBM to a stream for a payload with embedded defaults.
    /// </summary>
    public static void RenderXbmToStream(QrPayloadData payload, Stream stream, QrEasyOptions? options = null) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        var xbm = RenderXbm(payload, options);
        xbm.WriteText(stream);
    }

    /// <summary>
    /// Renders a QR code as XPM.
    /// </summary>
    public static string RenderXpm(string payload, QrEasyOptions? options = null) {
        var opts = options ?? new QrEasyOptions();
        var qr = Encode(payload, opts);
        var render = BuildPngOptions(opts, payload, qr.Modules.Width);
        return QrXpmRenderer.Render(qr.Modules, render);
    }

    /// <summary>
    /// Detects a payload type and renders a QR code as XPM.
    /// </summary>
    public static string RenderXpmAuto(string payload, QrPayloadDetectOptions? detectOptions = null, QrEasyOptions? options = null) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        var detected = QrPayloads.Detect(payload, detectOptions);
        return RenderXpm(detected, options);
    }

    /// <summary>
    /// Renders a QR code as XPM to a stream.
    /// </summary>
    public static void RenderXpmToStream(string payload, Stream stream, QrEasyOptions? options = null) {
        var xpm = RenderXpm(payload, options);
        xpm.WriteText(stream);
    }

    /// <summary>
    /// Renders a QR code as XPM and writes it to a file.
    /// </summary>
    public static string RenderXpmToFile(string payload, string path, QrEasyOptions? options = null) {
        var xpm = RenderXpm(payload, options);
        return xpm.WriteText(path);
    }

    /// <summary>
    /// Renders a QR code as XPM and writes it to a file for a payload with embedded defaults.
    /// </summary>
    public static string RenderXpmToFile(QrPayloadData payload, string path, QrEasyOptions? options = null) {
        var xpm = RenderXpm(payload, options);
        return xpm.WriteText(path);
    }

    /// <summary>
    /// Renders a QR code as XPM for a payload with embedded defaults.
    /// </summary>
    public static string RenderXpm(QrPayloadData payload, QrEasyOptions? options = null) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        var opts = MergeOptions(payload, options);
        var qr = Encode(payload.Text, opts);
        var render = BuildPngOptions(opts, payload.Text, qr.Modules.Width);
        return QrXpmRenderer.Render(qr.Modules, render);
    }

    /// <summary>
    /// Renders a QR code as XPM to a stream for a payload with embedded defaults.
    /// </summary>
    public static void RenderXpmToStream(QrPayloadData payload, Stream stream, QrEasyOptions? options = null) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        var xpm = RenderXpm(payload, options);
        xpm.WriteText(stream);
    }

    /// <summary>
    /// Renders a QR code as TGA.
    /// </summary>
    public static byte[] RenderTga(string payload, QrEasyOptions? options = null) {
        var opts = options ?? new QrEasyOptions();
        var qr = Encode(payload, opts);
        var render = BuildPngOptions(opts, payload, qr.Modules.Width);
        return QrTgaRenderer.Render(qr.Modules, render);
    }

    /// <summary>
    /// Detects a payload type and renders a QR code as TGA.
    /// </summary>
    public static byte[] RenderTgaAuto(string payload, QrPayloadDetectOptions? detectOptions = null, QrEasyOptions? options = null) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        var detected = QrPayloads.Detect(payload, detectOptions);
        return RenderTga(detected, options);
    }

    /// <summary>
    /// Renders a QR code as TGA to a stream.
    /// </summary>
    public static void RenderTgaToStream(string payload, Stream stream, QrEasyOptions? options = null) {
        var opts = options ?? new QrEasyOptions();
        var qr = Encode(payload, opts);
        var render = BuildPngOptions(opts, payload, qr.Modules.Width);
        QrTgaRenderer.RenderToStream(qr.Modules, render, stream);
    }

    /// <summary>
    /// Renders a QR code as TGA and writes it to a file.
    /// </summary>
    public static string RenderTgaToFile(string payload, string path, QrEasyOptions? options = null) {
        var tga = RenderTga(payload, options);
        return RenderIO.WriteBinary(path, tga);
    }

    /// <summary>
    /// Renders a QR code as TGA and writes it to a file for a payload with embedded defaults.
    /// </summary>
    public static string RenderTgaToFile(QrPayloadData payload, string path, QrEasyOptions? options = null) {
        var tga = RenderTga(payload, options);
        return RenderIO.WriteBinary(path, tga);
    }

    /// <summary>
    /// Renders a QR code as TGA for a payload with embedded defaults.
    /// </summary>
    public static byte[] RenderTga(QrPayloadData payload, QrEasyOptions? options = null) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        var opts = MergeOptions(payload, options);
        var qr = Encode(payload.Text, opts);
        var render = BuildPngOptions(opts, payload.Text, qr.Modules.Width);
        return QrTgaRenderer.Render(qr.Modules, render);
    }

    /// <summary>
    /// Renders a QR code as TGA to a stream for a payload with embedded defaults.
    /// </summary>
    public static void RenderTgaToStream(QrPayloadData payload, Stream stream, QrEasyOptions? options = null) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        var opts = MergeOptions(payload, options);
        var qr = Encode(payload.Text, opts);
        var render = BuildPngOptions(opts, payload.Text, qr.Modules.Width);
        QrTgaRenderer.RenderToStream(qr.Modules, render, stream);
    }

    /// <summary>
    /// Renders a QR code as ICO.
    /// </summary>
    public static byte[] RenderIco(string payload, QrEasyOptions? options = null) {
        var opts = options ?? new QrEasyOptions();
        var qr = Encode(payload, opts);
        var render = BuildPngOptions(opts, payload, qr.Modules.Width);
        return QrIcoRenderer.Render(qr.Modules, render, BuildIcoOptions(opts));
    }

    /// <summary>
    /// Detects a payload type and renders a QR code as ICO.
    /// </summary>
    public static byte[] RenderIcoAuto(string payload, QrPayloadDetectOptions? detectOptions = null, QrEasyOptions? options = null) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        var detected = QrPayloads.Detect(payload, detectOptions);
        return RenderIco(detected, options);
    }

    /// <summary>
    /// Renders a QR code as ICO to a stream.
    /// </summary>
    public static void RenderIcoToStream(string payload, Stream stream, QrEasyOptions? options = null) {
        var opts = options ?? new QrEasyOptions();
        var qr = Encode(payload, opts);
        var render = BuildPngOptions(opts, payload, qr.Modules.Width);
        QrIcoRenderer.RenderToStream(qr.Modules, render, stream, BuildIcoOptions(opts));
    }

    /// <summary>
    /// Renders a QR code as ICO and writes it to a file.
    /// </summary>
    public static string RenderIcoToFile(string payload, string path, QrEasyOptions? options = null) {
        var ico = RenderIco(payload, options);
        return RenderIO.WriteBinary(path, ico);
    }

    /// <summary>
    /// Renders a QR code as ICO and writes it to a file for a payload with embedded defaults.
    /// </summary>
    public static string RenderIcoToFile(QrPayloadData payload, string path, QrEasyOptions? options = null) {
        var ico = RenderIco(payload, options);
        return RenderIO.WriteBinary(path, ico);
    }

    /// <summary>
    /// Renders a QR code as ICO for a payload with embedded defaults.
    /// </summary>
    public static byte[] RenderIco(QrPayloadData payload, QrEasyOptions? options = null) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        var opts = MergeOptions(payload, options);
        var qr = Encode(payload.Text, opts);
        var render = BuildPngOptions(opts, payload.Text, qr.Modules.Width);
        return QrIcoRenderer.Render(qr.Modules, render, BuildIcoOptions(opts));
    }

    /// <summary>
    /// Renders a QR code as ICO to a stream for a payload with embedded defaults.
    /// </summary>
    public static void RenderIcoToStream(QrPayloadData payload, Stream stream, QrEasyOptions? options = null) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        var opts = MergeOptions(payload, options);
        var qr = Encode(payload.Text, opts);
        var render = BuildPngOptions(opts, payload.Text, qr.Modules.Width);
        QrIcoRenderer.RenderToStream(qr.Modules, render, stream, BuildIcoOptions(opts));
    }

    /// <summary>
    /// Renders a QR code as SVGZ.
    /// </summary>
    public static byte[] RenderSvgz(string payload, QrEasyOptions? options = null) {
        var opts = options ?? new QrEasyOptions();
        var qr = Encode(payload, opts);
        var render = BuildSvgOptions(opts, qr.Modules.Width);
        return QrSvgzRenderer.Render(qr.Modules, render);
    }

    /// <summary>
    /// Detects a payload type and renders a QR code as SVGZ.
    /// </summary>
    public static byte[] RenderSvgzAuto(string payload, QrPayloadDetectOptions? detectOptions = null, QrEasyOptions? options = null) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        var detected = QrPayloads.Detect(payload, detectOptions);
        return RenderSvgz(detected, options);
    }

    /// <summary>
    /// Renders a QR code as SVGZ to a stream.
    /// </summary>
    public static void RenderSvgzToStream(string payload, Stream stream, QrEasyOptions? options = null) {
        var opts = options ?? new QrEasyOptions();
        var qr = Encode(payload, opts);
        var render = BuildSvgOptions(opts, qr.Modules.Width);
        QrSvgzRenderer.RenderToStream(qr.Modules, render, stream);
    }

    /// <summary>
    /// Renders a QR code as SVGZ and writes it to a file.
    /// </summary>
    public static string RenderSvgzToFile(string payload, string path, QrEasyOptions? options = null) {
        var svgz = RenderSvgz(payload, options);
        return RenderIO.WriteBinary(path, svgz);
    }

    /// <summary>
    /// Renders a QR code as SVGZ and writes it to a file for a payload with embedded defaults.
    /// </summary>
    public static string RenderSvgzToFile(QrPayloadData payload, string path, QrEasyOptions? options = null) {
        var svgz = RenderSvgz(payload, options);
        return RenderIO.WriteBinary(path, svgz);
    }

    /// <summary>
    /// Renders a QR code as SVGZ for a payload with embedded defaults.
    /// </summary>
    public static byte[] RenderSvgz(QrPayloadData payload, QrEasyOptions? options = null) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        var opts = MergeOptions(payload, options);
        var qr = Encode(payload.Text, opts);
        var render = BuildSvgOptions(opts, qr.Modules.Width);
        return QrSvgzRenderer.Render(qr.Modules, render);
    }

    /// <summary>
    /// Renders a QR code as SVGZ to a stream for a payload with embedded defaults.
    /// </summary>
    public static void RenderSvgzToStream(QrPayloadData payload, Stream stream, QrEasyOptions? options = null) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        var opts = MergeOptions(payload, options);
        var qr = Encode(payload.Text, opts);
        var render = BuildSvgOptions(opts, qr.Modules.Width);
        QrSvgzRenderer.RenderToStream(qr.Modules, render, stream);
    }

    /// <summary>
    /// Renders a QR code as PDF.
    /// </summary>
    /// <param name="payload">The payload text.</param>
    /// <param name="options">Optional rendering options.</param>
    /// <param name="mode">Vector or raster output.</param>
    public static byte[] RenderPdf(string payload, QrEasyOptions? options = null, RenderMode mode = RenderMode.Vector) {
        var opts = options ?? new QrEasyOptions();
        var qr = Encode(payload, opts);
        var render = BuildPngOptions(opts, payload, qr.Modules.Width);
        return QrPdfRenderer.Render(qr.Modules, render, mode);
    }

    /// <summary>
    /// Detects a payload type and renders a QR code as PDF.
    /// </summary>
    /// <param name="payload">The payload text.</param>
    /// <param name="detectOptions">Payload detection options.</param>
    /// <param name="options">Optional rendering options.</param>
    /// <param name="mode">Vector or raster output.</param>
    public static byte[] RenderPdfAuto(string payload, QrPayloadDetectOptions? detectOptions = null, QrEasyOptions? options = null, RenderMode mode = RenderMode.Vector) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        var detected = QrPayloads.Detect(payload, detectOptions);
        return RenderPdf(detected, options, mode);
    }

    /// <summary>
    /// Renders a QR code as PDF to a stream.
    /// </summary>
    /// <param name="payload">The payload text.</param>
    /// <param name="stream">Destination stream.</param>
    /// <param name="options">Optional rendering options.</param>
    /// <param name="mode">Vector or raster output.</param>
    public static void RenderPdfToStream(string payload, Stream stream, QrEasyOptions? options = null, RenderMode mode = RenderMode.Vector) {
        var opts = options ?? new QrEasyOptions();
        var qr = Encode(payload, opts);
        var render = BuildPngOptions(opts, payload, qr.Modules.Width);
        QrPdfRenderer.RenderToStream(qr.Modules, render, stream, mode);
    }

    /// <summary>
    /// Renders a QR code as PDF and writes it to a file.
    /// </summary>
    /// <param name="payload">The payload text.</param>
    /// <param name="path">Output file path.</param>
    /// <param name="options">Optional rendering options.</param>
    /// <param name="mode">Vector or raster output.</param>
    public static string RenderPdfToFile(string payload, string path, QrEasyOptions? options = null, RenderMode mode = RenderMode.Vector) {
        var pdf = RenderPdf(payload, options, mode);
        return RenderIO.WriteBinary(path, pdf);
    }

    /// <summary>
    /// Renders a QR code as PDF and writes it to a file for a payload with embedded defaults.
    /// </summary>
    /// <param name="payload">The payload text.</param>
    /// <param name="path">Output file path.</param>
    /// <param name="options">Optional rendering options.</param>
    /// <param name="mode">Vector or raster output.</param>
    public static string RenderPdfToFile(QrPayloadData payload, string path, QrEasyOptions? options = null, RenderMode mode = RenderMode.Vector) {
        var pdf = RenderPdf(payload, options, mode);
        return RenderIO.WriteBinary(path, pdf);
    }

    /// <summary>
    /// Renders a QR code as PDF for a payload with embedded defaults.
    /// </summary>
    /// <param name="payload">The payload text.</param>
    /// <param name="options">Optional rendering options.</param>
    /// <param name="mode">Vector or raster output.</param>
    public static byte[] RenderPdf(QrPayloadData payload, QrEasyOptions? options = null, RenderMode mode = RenderMode.Vector) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        var opts = MergeOptions(payload, options);
        var qr = Encode(payload.Text, opts);
        var render = BuildPngOptions(opts, payload.Text, qr.Modules.Width);
        return QrPdfRenderer.Render(qr.Modules, render, mode);
    }

    /// <summary>
    /// Renders a QR code as PDF to a stream for a payload with embedded defaults.
    /// </summary>
    /// <param name="payload">The payload text.</param>
    /// <param name="stream">Destination stream.</param>
    /// <param name="options">Optional rendering options.</param>
    /// <param name="mode">Vector or raster output.</param>
    public static void RenderPdfToStream(QrPayloadData payload, Stream stream, QrEasyOptions? options = null, RenderMode mode = RenderMode.Vector) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        var opts = MergeOptions(payload, options);
        var qr = Encode(payload.Text, opts);
        var render = BuildPngOptions(opts, payload.Text, qr.Modules.Width);
        QrPdfRenderer.RenderToStream(qr.Modules, render, stream, mode);
    }

    /// <summary>
    /// Renders a QR code as EPS.
    /// </summary>
    /// <param name="payload">The payload text.</param>
    /// <param name="options">Optional rendering options.</param>
    /// <param name="mode">Vector or raster output.</param>
    public static string RenderEps(string payload, QrEasyOptions? options = null, RenderMode mode = RenderMode.Vector) {
        var opts = options ?? new QrEasyOptions();
        var qr = Encode(payload, opts);
        var render = BuildPngOptions(opts, payload, qr.Modules.Width);
        return QrEpsRenderer.Render(qr.Modules, render, mode);
    }

    /// <summary>
    /// Detects a payload type and renders a QR code as EPS.
    /// </summary>
    /// <param name="payload">The payload text.</param>
    /// <param name="detectOptions">Payload detection options.</param>
    /// <param name="options">Optional rendering options.</param>
    /// <param name="mode">Vector or raster output.</param>
    public static string RenderEpsAuto(string payload, QrPayloadDetectOptions? detectOptions = null, QrEasyOptions? options = null, RenderMode mode = RenderMode.Vector) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        var detected = QrPayloads.Detect(payload, detectOptions);
        return RenderEps(detected, options, mode);
    }

    /// <summary>
    /// Renders a QR code as EPS to a stream.
    /// </summary>
    /// <param name="payload">The payload text.</param>
    /// <param name="stream">Destination stream.</param>
    /// <param name="options">Optional rendering options.</param>
    /// <param name="mode">Vector or raster output.</param>
    public static void RenderEpsToStream(string payload, Stream stream, QrEasyOptions? options = null, RenderMode mode = RenderMode.Vector) {
        var opts = options ?? new QrEasyOptions();
        var qr = Encode(payload, opts);
        var render = BuildPngOptions(opts, payload, qr.Modules.Width);
        QrEpsRenderer.RenderToStream(qr.Modules, render, stream, mode);
    }

    /// <summary>
    /// Renders a QR code as EPS and writes it to a file.
    /// </summary>
    /// <param name="payload">The payload text.</param>
    /// <param name="path">Output file path.</param>
    /// <param name="options">Optional rendering options.</param>
    /// <param name="mode">Vector or raster output.</param>
    public static string RenderEpsToFile(string payload, string path, QrEasyOptions? options = null, RenderMode mode = RenderMode.Vector) {
        var eps = RenderEps(payload, options, mode);
        return RenderIO.WriteText(path, eps);
    }

    /// <summary>
    /// Renders a QR code as EPS and writes it to a file for a payload with embedded defaults.
    /// </summary>
    /// <param name="payload">The payload text.</param>
    /// <param name="path">Output file path.</param>
    /// <param name="options">Optional rendering options.</param>
    /// <param name="mode">Vector or raster output.</param>
    public static string RenderEpsToFile(QrPayloadData payload, string path, QrEasyOptions? options = null, RenderMode mode = RenderMode.Vector) {
        var eps = RenderEps(payload, options, mode);
        return RenderIO.WriteText(path, eps);
    }

    /// <summary>
    /// Renders a QR code as EPS for a payload with embedded defaults.
    /// </summary>
    /// <param name="payload">The payload text.</param>
    /// <param name="options">Optional rendering options.</param>
    /// <param name="mode">Vector or raster output.</param>
    public static string RenderEps(QrPayloadData payload, QrEasyOptions? options = null, RenderMode mode = RenderMode.Vector) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        var opts = MergeOptions(payload, options);
        var qr = Encode(payload.Text, opts);
        var render = BuildPngOptions(opts, payload.Text, qr.Modules.Width);
        return QrEpsRenderer.Render(qr.Modules, render, mode);
    }

    /// <summary>
    /// Renders a QR code as EPS to a stream for a payload with embedded defaults.
    /// </summary>
    /// <param name="payload">The payload text.</param>
    /// <param name="stream">Destination stream.</param>
    /// <param name="options">Optional rendering options.</param>
    /// <param name="mode">Vector or raster output.</param>
    public static void RenderEpsToStream(QrPayloadData payload, Stream stream, QrEasyOptions? options = null, RenderMode mode = RenderMode.Vector) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        var opts = MergeOptions(payload, options);
        var qr = Encode(payload.Text, opts);
        var render = BuildPngOptions(opts, payload.Text, qr.Modules.Width);
        QrEpsRenderer.RenderToStream(qr.Modules, render, stream, mode);
    }

    /// <summary>
    /// Renders a QR code as ASCII text.
    /// </summary>
    public static string RenderAscii(string payload, MatrixAsciiRenderOptions? asciiOptions = null, QrEasyOptions? options = null) {
        var opts = options ?? new QrEasyOptions();
        var qr = Encode(payload, opts);
        var resolved = BuildAsciiOptions(asciiOptions, opts);
        return MatrixAsciiRenderer.Render(qr.Modules, resolved);
    }

    /// <summary>
    /// Detects a payload type and renders a QR code as ASCII text.
    /// </summary>
    public static string RenderAsciiAuto(string payload, QrPayloadDetectOptions? detectOptions = null, MatrixAsciiRenderOptions? asciiOptions = null, QrEasyOptions? options = null) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        var detected = QrPayloads.Detect(payload, detectOptions);
        return RenderAscii(detected, asciiOptions, options);
    }

    /// <summary>
    /// Renders a QR code as ASCII text for a payload with embedded defaults.
    /// </summary>
    public static string RenderAscii(QrPayloadData payload, MatrixAsciiRenderOptions? asciiOptions = null, QrEasyOptions? options = null) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        var opts = MergeOptions(payload, options);
        var qr = Encode(payload.Text, opts);
        var resolved = BuildAsciiOptions(asciiOptions, opts);
        return MatrixAsciiRenderer.Render(qr.Modules, resolved);
    }

    /// <summary>
    /// Renders a QR code to a raw RGBA pixel buffer (no PNG encoding).
    /// </summary>
    public static byte[] RenderPixels(string payload, out int widthPx, out int heightPx, out int stride, QrEasyOptions? options = null) {
        var opts = options ?? new QrEasyOptions();
        var qr = Encode(payload, opts);
        var render = BuildPngOptions(opts, payload, qr.Modules.Width);
        return QrPngRenderer.RenderPixels(qr.Modules, render, out widthPx, out heightPx, out stride);
    }

    /// <summary>
    /// Renders a QR code to a raw RGBA pixel buffer (no PNG encoding) for a payload with embedded defaults.
    /// </summary>
    public static byte[] RenderPixels(QrPayloadData payload, out int widthPx, out int heightPx, out int stride, QrEasyOptions? options = null) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        var opts = MergeOptions(payload, options);
        var qr = Encode(payload.Text, opts);
        var render = BuildPngOptions(opts, payload.Text, qr.Modules.Width);
        return QrPngRenderer.RenderPixels(qr.Modules, render, out widthPx, out heightPx, out stride);
    }

    private static QrPngRenderOptions BuildPngOptions(QrEasyOptions opts, string payload, int moduleCount) {
        var render = new QrPngRenderOptions {
            ModuleSize = ResolveModuleSize(opts, moduleCount),
            QuietZone = opts.QuietZone,
            Foreground = opts.Foreground,
            Background = opts.Background,
        };

        if (opts.Style == QrRenderStyle.Rounded) {
            render.ModuleShape = QrPngModuleShape.Rounded;
            render.ModuleScale = 0.9;
            render.ModuleCornerRadiusPx = 2;
        } else if (opts.Style == QrRenderStyle.Fancy) {
            var start = opts.Foreground;
            var end = Blend(opts.Foreground, Rgba32.White, 0.35);
            render.ModuleShape = QrPngModuleShape.Rounded;
            render.ModuleScale = 0.85;
            render.ModuleCornerRadiusPx = 3;
            render.ForegroundGradient = new QrPngGradientOptions {
                Type = QrPngGradientType.DiagonalDown,
                StartColor = start,
                EndColor = end,
            };
            render.Eyes = new QrPngEyeOptions {
                UseFrame = true,
                OuterShape = QrPngModuleShape.Rounded,
                InnerShape = QrPngModuleShape.Circle,
                OuterCornerRadiusPx = 5,
                InnerCornerRadiusPx = 4,
                OuterGradient = new QrPngGradientOptions {
                    Type = QrPngGradientType.Radial,
                    StartColor = start,
                    EndColor = end,
                    CenterX = 0.35,
                    CenterY = 0.35,
                },
                InnerColor = start,
            };
        }

        if (opts.ModuleShape.HasValue) render.ModuleShape = opts.ModuleShape.Value;
        if (opts.ModuleScale.HasValue) render.ModuleScale = opts.ModuleScale.Value;
        if (opts.ModuleCornerRadiusPx.HasValue) render.ModuleCornerRadiusPx = opts.ModuleCornerRadiusPx.Value;
        if (opts.ForegroundGradient is not null) render.ForegroundGradient = opts.ForegroundGradient;
        if (opts.Eyes is not null) render.Eyes = opts.Eyes;

        var logo = BuildPngLogo(opts);
        if (logo is not null) render.Logo = logo;

        return render;
    }

    private static IcoRenderOptions BuildIcoOptions(QrEasyOptions opts) {
        return new IcoRenderOptions {
            Sizes = opts.IcoSizes ?? new[] { 16, 32, 48, 64, 128, 256 },
            PreserveAspectRatio = opts.IcoPreserveAspectRatio
        };
    }

    private static QrSvgRenderOptions BuildSvgOptions(QrEasyOptions opts, int moduleCount) {
        var render = new QrSvgRenderOptions {
            ModuleSize = ResolveModuleSize(opts, moduleCount),
            QuietZone = opts.QuietZone,
            DarkColor = ToCss(opts.Foreground),
            LightColor = ToCss(opts.Background),
        };

        if (opts.Style == QrRenderStyle.Rounded) {
            render.ModuleShape = QrPngModuleShape.Rounded;
            render.ModuleScale = 0.9;
            render.ModuleCornerRadiusPx = 2;
        } else if (opts.Style == QrRenderStyle.Fancy) {
            var start = opts.Foreground;
            var end = Blend(opts.Foreground, Rgba32.White, 0.35);
            render.ModuleShape = QrPngModuleShape.Rounded;
            render.ModuleScale = 0.85;
            render.ModuleCornerRadiusPx = 3;
            render.ForegroundGradient = new QrPngGradientOptions {
                Type = QrPngGradientType.DiagonalDown,
                StartColor = start,
                EndColor = end,
            };
            render.Eyes = new QrPngEyeOptions {
                UseFrame = true,
                OuterShape = QrPngModuleShape.Rounded,
                InnerShape = QrPngModuleShape.Circle,
                OuterCornerRadiusPx = 5,
                InnerCornerRadiusPx = 4,
                OuterGradient = new QrPngGradientOptions {
                    Type = QrPngGradientType.Radial,
                    StartColor = start,
                    EndColor = end,
                    CenterX = 0.35,
                    CenterY = 0.35,
                },
                InnerColor = start,
            };
        }

        if (opts.ModuleShape.HasValue) render.ModuleShape = opts.ModuleShape.Value;
        if (opts.ModuleScale.HasValue) render.ModuleScale = opts.ModuleScale.Value;
        if (opts.ModuleCornerRadiusPx.HasValue) render.ModuleCornerRadiusPx = opts.ModuleCornerRadiusPx.Value;
        if (opts.ForegroundGradient is not null) render.ForegroundGradient = opts.ForegroundGradient;
        if (opts.Eyes is not null) render.Eyes = opts.Eyes;

        var logo = BuildLogoOptions(opts);
        if (logo is not null) render.Logo = logo;

        return render;
    }

    private static int ResolveModuleSize(QrEasyOptions opts, int moduleCount) {
        if (moduleCount <= 0) return opts.ModuleSize;
        if (opts.TargetSizePx <= 0) return opts.ModuleSize;

        var targetModules = moduleCount;
        if (opts.TargetSizeIncludesQuietZone) {
            targetModules += opts.QuietZone * 2;
        }
        if (targetModules <= 0) return opts.ModuleSize;

        var moduleSize = opts.TargetSizePx / targetModules;
        if (moduleSize < 1) moduleSize = 1;
        return moduleSize;
    }

    private static MatrixAsciiRenderOptions BuildAsciiOptions(MatrixAsciiRenderOptions? asciiOptions, QrEasyOptions opts) {
        var resolved = asciiOptions ?? new MatrixAsciiRenderOptions();
        if (asciiOptions is null || asciiOptions.QuietZone == RenderDefaults.QrQuietZone) {
            resolved.QuietZone = opts.QuietZone;
        }
        return resolved;
    }

    private static QrCode EncodePayload(string payload, QrEasyOptions opts) {
        var ecc = opts.ErrorCorrectionLevel ?? GuessEcc(payload, opts.LogoPng is { Length: > 0 });
        if (opts.TextEncoding.HasValue) {
            return QrCodeEncoder.EncodeText(payload, opts.TextEncoding.Value, ecc, opts.MinVersion, opts.MaxVersion, opts.ForceMask, opts.IncludeEci);
        }
        return QrCodeEncoder.EncodeText(payload, ecc, opts.MinVersion, opts.MaxVersion, opts.ForceMask);
    }

    private static QrEasyOptions MergeOptions(QrPayloadData payload, QrEasyOptions? options) {
        var opts = options is null ? new QrEasyOptions() : CloneOptions(options);
        if (!opts.RespectPayloadDefaults) return opts;

        if (opts.ErrorCorrectionLevel is null && payload.ErrorCorrectionLevel.HasValue) {
            opts.ErrorCorrectionLevel = payload.ErrorCorrectionLevel;
        }
        if (opts.TextEncoding is null && payload.TextEncoding.HasValue) {
            opts.TextEncoding = payload.TextEncoding;
        }
        if (payload.MinVersion.HasValue) {
            opts.MinVersion = Math.Max(opts.MinVersion, payload.MinVersion.Value);
        }
        if (payload.MaxVersion.HasValue) {
            opts.MaxVersion = Math.Min(opts.MaxVersion, payload.MaxVersion.Value);
        }
        if (opts.MinVersion > opts.MaxVersion) {
            throw new ArgumentOutOfRangeException(nameof(options), "QR version range is invalid for the payload.");
        }
        return opts;
    }

    private static QrEasyOptions CloneOptions(QrEasyOptions opts) {
        return new QrEasyOptions {
            ModuleSize = opts.ModuleSize,
            QuietZone = opts.QuietZone,
            ErrorCorrectionLevel = opts.ErrorCorrectionLevel,
            TextEncoding = opts.TextEncoding,
            IncludeEci = opts.IncludeEci,
            RespectPayloadDefaults = opts.RespectPayloadDefaults,
            MinVersion = opts.MinVersion,
            MaxVersion = opts.MaxVersion,
            ForceMask = opts.ForceMask,
            Foreground = opts.Foreground,
            Background = opts.Background,
            Style = opts.Style,
            ModuleShape = opts.ModuleShape,
            ModuleScale = opts.ModuleScale,
            ModuleCornerRadiusPx = opts.ModuleCornerRadiusPx,
            ForegroundGradient = opts.ForegroundGradient,
            Eyes = opts.Eyes,
            LogoPng = opts.LogoPng,
            LogoScale = opts.LogoScale,
            LogoPaddingPx = opts.LogoPaddingPx,
            LogoDrawBackground = opts.LogoDrawBackground,
            LogoBackground = opts.LogoBackground,
            LogoCornerRadiusPx = opts.LogoCornerRadiusPx,
            JpegQuality = opts.JpegQuality,
            HtmlEmailSafeTable = opts.HtmlEmailSafeTable
        };
    }

    private static QrPngLogoOptions? BuildPngLogo(QrEasyOptions opts) {
        if (opts.LogoPng is null || opts.LogoPng.Length == 0) return null;
        var logo = QrPngLogoOptions.FromPng(opts.LogoPng);
        logo.Scale = opts.LogoScale;
        logo.PaddingPx = opts.LogoPaddingPx;
        logo.DrawBackground = opts.LogoDrawBackground;
        logo.Background = opts.LogoBackground ?? opts.Background;
        logo.CornerRadiusPx = opts.LogoCornerRadiusPx;
        return logo;
    }

    private static QrLogoOptions? BuildLogoOptions(QrEasyOptions opts) {
        if (opts.LogoPng is null || opts.LogoPng.Length == 0) return null;
        return new QrLogoOptions(opts.LogoPng) {
            Scale = opts.LogoScale,
            PaddingPx = opts.LogoPaddingPx,
            DrawBackground = opts.LogoDrawBackground,
            Background = opts.LogoBackground ?? opts.Background,
            CornerRadiusPx = opts.LogoCornerRadiusPx,
        };
    }

    private static QrErrorCorrectionLevel GuessEcc(string payload, bool hasLogo) {
        if (hasLogo) return QrErrorCorrectionLevel.H;
        return payload.StartsWith("otpauth://", StringComparison.OrdinalIgnoreCase)
            ? QrErrorCorrectionLevel.H
            : QrErrorCorrectionLevel.M;
    }

    private static Rgba32 Blend(Rgba32 a, Rgba32 b, double t) {
        if (t <= 0) return a;
        if (t >= 1) return b;
        var r = (byte)Math.Round(a.R + (b.R - a.R) * t);
        var g = (byte)Math.Round(a.G + (b.G - a.G) * t);
        var bch = (byte)Math.Round(a.B + (b.B - a.B) * t);
        var aCh = (byte)Math.Round(a.A + (b.A - a.A) * t);
        return new Rgba32(r, g, bch, aCh);
    }

    private static string ToCss(Rgba32 color) {
        if (color.A == 255) return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        var a = color.A / 255.0;
        return $"rgba({color.R},{color.G},{color.B},{a.ToString("0.###", CultureInfo.InvariantCulture)})";
    }
}
