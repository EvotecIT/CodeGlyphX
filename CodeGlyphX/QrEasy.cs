using System;
using System.Globalization;
using System.IO;
using CodeGlyphX.Payloads;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Ascii;
using CodeGlyphX.Rendering.Bmp;
using CodeGlyphX.Rendering.Html;
using CodeGlyphX.Rendering.Jpeg;
using CodeGlyphX.Rendering.Png;
using CodeGlyphX.Rendering.Svg;

namespace CodeGlyphX;

/// <summary>
/// One-line QR generation helpers with sane defaults.
/// </summary>
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
        var render = BuildPngOptions(opts, payload);
        return QrArtSafety.Evaluate(qr, render);
    }

    /// <summary>
    /// Evaluates QR art safety for a payload with embedded defaults.
    /// </summary>
    public static QrArtSafetyReport EvaluateSafety(QrPayloadData payload, QrEasyOptions? options = null) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        var opts = MergeOptions(payload, options);
        var qr = Encode(payload.Text, opts);
        var render = BuildPngOptions(opts, payload.Text);
        return QrArtSafety.Evaluate(qr, render);
    }

    /// <summary>
    /// Renders a QR code as PNG.
    /// </summary>
    public static byte[] RenderPng(string payload, QrEasyOptions? options = null) {
        var opts = options ?? new QrEasyOptions();
        var qr = Encode(payload, opts);
        var render = BuildPngOptions(opts, payload);
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
        var render = BuildPngOptions(opts, payload);
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
        var render = BuildPngOptions(opts, payload.Text);
        return QrPngRenderer.Render(qr.Modules, render);
    }

    /// <summary>
    /// Renders a QR code as PNG to a stream for a payload with embedded defaults.
    /// </summary>
    public static void RenderPngToStream(QrPayloadData payload, Stream stream, QrEasyOptions? options = null) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        var opts = MergeOptions(payload, options);
        var qr = Encode(payload.Text, opts);
        var render = BuildPngOptions(opts, payload.Text);
        QrPngRenderer.RenderToStream(qr.Modules, render, stream);
    }

    /// <summary>
    /// Renders a QR code as SVG.
    /// </summary>
    public static string RenderSvg(string payload, QrEasyOptions? options = null) {
        var opts = options ?? new QrEasyOptions();
        var qr = Encode(payload, opts);
        var baseRender = BuildPngOptions(opts, payload);
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
        var baseRender = BuildPngOptions(opts, payload.Text);
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
        var baseRender = BuildPngOptions(opts, payload);
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
        var baseRender = BuildPngOptions(opts, payload.Text);
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
        var render = BuildPngOptions(opts, payload);
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
        var render = BuildPngOptions(opts, payload);
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
        var render = BuildPngOptions(opts, payload.Text);
        return QrJpegRenderer.Render(qr.Modules, render, opts.JpegQuality);
    }

    /// <summary>
    /// Renders a QR code as JPEG to a stream for a payload with embedded defaults.
    /// </summary>
    public static void RenderJpegToStream(QrPayloadData payload, Stream stream, QrEasyOptions? options = null) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        var opts = MergeOptions(payload, options);
        var qr = Encode(payload.Text, opts);
        var render = BuildPngOptions(opts, payload.Text);
        QrJpegRenderer.RenderToStream(qr.Modules, render, stream, opts.JpegQuality);
    }

    /// <summary>
    /// Renders a QR code as BMP.
    /// </summary>
    public static byte[] RenderBmp(string payload, QrEasyOptions? options = null) {
        var opts = options ?? new QrEasyOptions();
        var qr = Encode(payload, opts);
        var render = BuildPngOptions(opts, payload);
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
        var render = BuildPngOptions(opts, payload);
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
        var render = BuildPngOptions(opts, payload.Text);
        return QrBmpRenderer.Render(qr.Modules, render);
    }

    /// <summary>
    /// Renders a QR code as BMP to a stream for a payload with embedded defaults.
    /// </summary>
    public static void RenderBmpToStream(QrPayloadData payload, Stream stream, QrEasyOptions? options = null) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        var opts = MergeOptions(payload, options);
        var qr = Encode(payload.Text, opts);
        var render = BuildPngOptions(opts, payload.Text);
        QrBmpRenderer.RenderToStream(qr.Modules, render, stream);
    }

    /// <summary>
    /// Renders a QR code as ASCII text.
    /// </summary>
    public static string RenderAscii(string payload, MatrixAsciiRenderOptions? asciiOptions = null, QrEasyOptions? options = null) {
        var opts = options ?? new QrEasyOptions();
        var qr = Encode(payload, opts);
        return MatrixAsciiRenderer.Render(qr.Modules, asciiOptions);
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
        return MatrixAsciiRenderer.Render(qr.Modules, asciiOptions);
    }

    /// <summary>
    /// Renders a QR code to a raw RGBA pixel buffer (no PNG encoding).
    /// </summary>
    public static byte[] RenderPixels(string payload, out int widthPx, out int heightPx, out int stride, QrEasyOptions? options = null) {
        var opts = options ?? new QrEasyOptions();
        var qr = Encode(payload, opts);
        var render = BuildPngOptions(opts, payload);
        return QrPngRenderer.RenderPixels(qr.Modules, render, out widthPx, out heightPx, out stride);
    }

    /// <summary>
    /// Renders a QR code to a raw RGBA pixel buffer (no PNG encoding) for a payload with embedded defaults.
    /// </summary>
    public static byte[] RenderPixels(QrPayloadData payload, out int widthPx, out int heightPx, out int stride, QrEasyOptions? options = null) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        var opts = MergeOptions(payload, options);
        var qr = Encode(payload.Text, opts);
        var render = BuildPngOptions(opts, payload.Text);
        return QrPngRenderer.RenderPixels(qr.Modules, render, out widthPx, out heightPx, out stride);
    }

    private static QrPngRenderOptions BuildPngOptions(QrEasyOptions opts, string payload) {
        var render = new QrPngRenderOptions {
            ModuleSize = opts.ModuleSize,
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
