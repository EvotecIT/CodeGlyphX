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
        var opts = options is null ? new QrEasyOptions() : CloneOptions(options);
        var qr = Encode(payload, opts);
        var render = BuildPngOptions(opts, qr);
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
        var render = BuildPngOptions(opts, qr);
        return QrPamRenderer.Render(qr.Modules, render);
    }

    /// <summary>
    /// Renders a QR code as PAM to a stream for a payload with embedded defaults.
    /// </summary>
    public static void RenderPamToStream(QrPayloadData payload, Stream stream, QrEasyOptions? options = null) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        var opts = MergeOptions(payload, options);
        var qr = Encode(payload.Text, opts);
        var render = BuildPngOptions(opts, qr);
        QrPamRenderer.RenderToStream(qr.Modules, render, stream);
    }

    /// <summary>
    /// Renders a QR code as XBM.
    /// </summary>
    public static string RenderXbm(string payload, QrEasyOptions? options = null) {
        var opts = options is null ? new QrEasyOptions() : CloneOptions(options);
        var qr = Encode(payload, opts);
        var render = BuildPngOptions(opts, qr);
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
        var render = BuildPngOptions(opts, qr);
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
        var opts = options is null ? new QrEasyOptions() : CloneOptions(options);
        var qr = Encode(payload, opts);
        var render = BuildPngOptions(opts, qr);
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
        var render = BuildPngOptions(opts, qr);
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
        var opts = options is null ? new QrEasyOptions() : CloneOptions(options);
        var qr = Encode(payload, opts);
        var render = BuildPngOptions(opts, qr);
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
        var opts = options is null ? new QrEasyOptions() : CloneOptions(options);
        var qr = Encode(payload, opts);
        var render = BuildPngOptions(opts, qr);
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
        var render = BuildPngOptions(opts, qr);
        return QrTgaRenderer.Render(qr.Modules, render);
    }

    /// <summary>
    /// Renders a QR code as TGA to a stream for a payload with embedded defaults.
    /// </summary>
    public static void RenderTgaToStream(QrPayloadData payload, Stream stream, QrEasyOptions? options = null) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        var opts = MergeOptions(payload, options);
        var qr = Encode(payload.Text, opts);
        var render = BuildPngOptions(opts, qr);
        QrTgaRenderer.RenderToStream(qr.Modules, render, stream);
    }

    /// <summary>
    /// Renders a QR code as ICO.
    /// </summary>
    public static byte[] RenderIco(string payload, QrEasyOptions? options = null) {
        var opts = options is null ? new QrEasyOptions() : CloneOptions(options);
        var qr = Encode(payload, opts);
        var render = BuildPngOptions(opts, qr);
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
        var opts = options is null ? new QrEasyOptions() : CloneOptions(options);
        var qr = Encode(payload, opts);
        var render = BuildPngOptions(opts, qr);
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
        var render = BuildPngOptions(opts, qr);
        return QrIcoRenderer.Render(qr.Modules, render, BuildIcoOptions(opts));
    }

    /// <summary>
    /// Renders a QR code as ICO to a stream for a payload with embedded defaults.
    /// </summary>
    public static void RenderIcoToStream(QrPayloadData payload, Stream stream, QrEasyOptions? options = null) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        var opts = MergeOptions(payload, options);
        var qr = Encode(payload.Text, opts);
        var render = BuildPngOptions(opts, qr);
        QrIcoRenderer.RenderToStream(qr.Modules, render, stream, BuildIcoOptions(opts));
    }

    /// <summary>
    /// Renders a QR code as SVGZ.
    /// </summary>
    public static byte[] RenderSvgz(string payload, QrEasyOptions? options = null) {
        var opts = options is null ? new QrEasyOptions() : CloneOptions(options);
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
        var opts = options is null ? new QrEasyOptions() : CloneOptions(options);
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
        var opts = options is null ? new QrEasyOptions() : CloneOptions(options);
        var qr = Encode(payload, opts);
        var render = BuildPngOptions(opts, qr);
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
        var opts = options is null ? new QrEasyOptions() : CloneOptions(options);
        var qr = Encode(payload, opts);
        var render = BuildPngOptions(opts, qr);
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
        var render = BuildPngOptions(opts, qr);
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
        var render = BuildPngOptions(opts, qr);
        QrPdfRenderer.RenderToStream(qr.Modules, render, stream, mode);
    }

    /// <summary>
    /// Renders a QR code as EPS.
    /// </summary>
    /// <param name="payload">The payload text.</param>
    /// <param name="options">Optional rendering options.</param>
    /// <param name="mode">Vector or raster output.</param>
    public static string RenderEps(string payload, QrEasyOptions? options = null, RenderMode mode = RenderMode.Vector) {
        var opts = options is null ? new QrEasyOptions() : CloneOptions(options);
        var qr = Encode(payload, opts);
        var render = BuildPngOptions(opts, qr);
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
        var opts = options is null ? new QrEasyOptions() : CloneOptions(options);
        var qr = Encode(payload, opts);
        var render = BuildPngOptions(opts, qr);
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
        var render = BuildPngOptions(opts, qr);
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
        var render = BuildPngOptions(opts, qr);
        QrEpsRenderer.RenderToStream(qr.Modules, render, stream, mode);
    }

    /// <summary>
    /// Renders a QR code as ASCII text.
    /// </summary>
    public static string RenderAscii(string payload, MatrixAsciiRenderOptions? asciiOptions = null, QrEasyOptions? options = null) {
        var opts = options is null ? new QrEasyOptions() : CloneOptions(options);
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
    /// Renders a QR code as console-friendly ASCII using scan-oriented defaults.
    /// </summary>
    public static string RenderAsciiConsole(
        string payload,
        int scale = 4,
        bool useAnsiColors = true,
        bool trueColor = true,
        Rgba32? darkColor = null,
        QrEasyOptions? options = null) {
        var preset = AsciiPresets.Console(scale, useAnsiColors, trueColor, darkColor);
        return RenderAscii(payload, preset, options);
    }

    /// <summary>
    /// Renders a QR code as console-friendly ASCII using scan-oriented defaults for a payload with embedded defaults.
    /// </summary>
    public static string RenderAsciiConsole(
        QrPayloadData payload,
        int scale = 4,
        bool useAnsiColors = true,
        bool trueColor = true,
        Rgba32? darkColor = null,
        QrEasyOptions? options = null) {
        var preset = AsciiPresets.Console(scale, useAnsiColors, trueColor, darkColor);
        return RenderAscii(payload, preset, options);
    }

    /// <summary>
    /// Renders a QR code to a raw RGBA pixel buffer (no PNG encoding).
    /// </summary>
    public static byte[] RenderPixels(string payload, out int widthPx, out int heightPx, out int stride, QrEasyOptions? options = null) {
        var opts = options is null ? new QrEasyOptions() : CloneOptions(options);
        var qr = Encode(payload, opts);
        var render = BuildPngOptions(opts, qr);
        return QrPngRenderer.RenderPixels(qr.Modules, render, out widthPx, out heightPx, out stride);
    }

    /// <summary>
    /// Renders a QR code to a raw RGBA pixel buffer (no PNG encoding) for a payload with embedded defaults.
    /// </summary>
    public static byte[] RenderPixels(QrPayloadData payload, out int widthPx, out int heightPx, out int stride, QrEasyOptions? options = null) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        var opts = MergeOptions(payload, options);
        var qr = Encode(payload.Text, opts);
        var render = BuildPngOptions(opts, qr);
        return QrPngRenderer.RenderPixels(qr.Modules, render, out widthPx, out heightPx, out stride);
    }


}
