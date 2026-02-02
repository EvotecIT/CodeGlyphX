using System;
using System.IO;
using System.Threading;
using CodeGlyphX.Aztec;
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
using CodeGlyphX.Rendering.Png;
using CodeGlyphX.Rendering.Pdf;
using CodeGlyphX.Rendering.Ppm;
using CodeGlyphX.Rendering.Svg;
using CodeGlyphX.Rendering.Svgz;
using CodeGlyphX.Rendering.Tga;
using CodeGlyphX.Rendering.Webp;
using CodeGlyphX.Rendering.Xbm;
using CodeGlyphX.Rendering.Xpm;

namespace CodeGlyphX;

/// <summary>
/// Aztec code helpers.
/// </summary>
/// <example>
/// <code>
/// using CodeGlyphX;
/// var png = AztecCode.Png("Ticket: CONF-2024");
/// </code>
/// </example>
public static partial class AztecCode {
    /// <summary>
    /// Encodes a text payload as Aztec.
    /// </summary>
    public static BitMatrix Encode(string text, AztecEncodeOptions? options = null) {
        return AztecEncoder.Encode(text, options);
    }

    /// <summary>
    /// Encodes binary payload as Aztec.
    /// </summary>
    public static BitMatrix Encode(ReadOnlySpan<byte> data, AztecEncodeOptions? options = null) {
        if (data.Length == 0) return AztecEncoder.Encode(Array.Empty<byte>(), options?.ErrorCorrectionPercent ?? 33, 0).Matrix;
        var bytes = data.ToArray();
        var eccPercent = options?.ErrorCorrectionPercent ?? 33;
        var userSpecifiedLayers = 0;
        if (options?.Layers is int layers && layers > 0) {
            var compact = options.Compact ?? layers <= 4;
            userSpecifiedLayers = compact ? -layers : layers;
        }
        return AztecEncoder.Encode(bytes, eccPercent, userSpecifiedLayers).Matrix;
    }

    /// <summary>
    /// Renders Aztec to PNG bytes.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static byte[] Png(string text, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = Encode(text, encodeOptions);
        return MatrixPngRenderer.Render(modules, ToPngOptions(renderOptions));
    }

    /// <summary>
    /// Renders Aztec to PNG bytes.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static byte[] Png(ReadOnlySpan<byte> data, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = Encode(data, encodeOptions);
        return MatrixPngRenderer.Render(modules, ToPngOptions(renderOptions));
    }

    /// <summary>
    /// Renders Aztec to SVG markup.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string Svg(string text, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = Encode(text, encodeOptions);
        return MatrixSvgRenderer.Render(modules, ToSvgOptions(renderOptions));
    }

    /// <summary>
    /// Renders Aztec to SVGZ.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static byte[] Svgz(string text, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = Encode(text, encodeOptions);
        return MatrixSvgzRenderer.Render(modules, ToSvgOptions(renderOptions));
    }

    /// <summary>
    /// Renders Aztec to SVG markup.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string Svg(ReadOnlySpan<byte> data, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = Encode(data, encodeOptions);
        return MatrixSvgRenderer.Render(modules, ToSvgOptions(renderOptions));
    }

    /// <summary>
    /// Renders Aztec to SVGZ.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static byte[] Svgz(ReadOnlySpan<byte> data, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = Encode(data, encodeOptions);
        return MatrixSvgzRenderer.Render(modules, ToSvgOptions(renderOptions));
    }

    /// <summary>
    /// Renders Aztec to HTML markup.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string Html(string text, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = Encode(text, encodeOptions);
        return MatrixHtmlRenderer.Render(modules, ToHtmlOptions(renderOptions));
    }

    /// <summary>
    /// Renders Aztec to HTML markup.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string Html(ReadOnlySpan<byte> data, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = Encode(data, encodeOptions);
        return MatrixHtmlRenderer.Render(modules, ToHtmlOptions(renderOptions));
    }

    /// <summary>
    /// Renders Aztec to JPEG bytes.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static byte[] Jpeg(string text, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = Encode(text, encodeOptions);
        var opts = ToPngOptions(renderOptions);
        return renderOptions?.JpegOptions is null
            ? MatrixJpegRenderer.Render(modules, opts, renderOptions?.JpegQuality ?? 85)
            : MatrixJpegRenderer.Render(modules, opts, renderOptions.JpegOptions);
    }

    /// <summary>
    /// Renders Aztec to WebP bytes.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static byte[] Webp(string text, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = Encode(text, encodeOptions);
        var opts = ToPngOptions(renderOptions);
        return MatrixWebpRenderer.Render(modules, opts, renderOptions?.WebpQuality ?? 100);
    }

    /// <summary>
    /// Renders Aztec to JPEG bytes.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static byte[] Jpeg(ReadOnlySpan<byte> data, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = Encode(data, encodeOptions);
        var opts = ToPngOptions(renderOptions);
        return renderOptions?.JpegOptions is null
            ? MatrixJpegRenderer.Render(modules, opts, renderOptions?.JpegQuality ?? 85)
            : MatrixJpegRenderer.Render(modules, opts, renderOptions.JpegOptions);
    }

    /// <summary>
    /// Renders Aztec to WebP bytes.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static byte[] Webp(ReadOnlySpan<byte> data, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = Encode(data, encodeOptions);
        var opts = ToPngOptions(renderOptions);
        return MatrixWebpRenderer.Render(modules, opts, renderOptions?.WebpQuality ?? 100);
    }

    /// <summary>
    /// Renders Aztec to BMP bytes.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static byte[] Bmp(string text, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = Encode(text, encodeOptions);
        return MatrixBmpRenderer.Render(modules, ToPngOptions(renderOptions));
    }

    /// <summary>
    /// Renders Aztec to BMP bytes.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static byte[] Bmp(ReadOnlySpan<byte> data, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = Encode(data, encodeOptions);
        return MatrixBmpRenderer.Render(modules, ToPngOptions(renderOptions));
    }

    /// <summary>
    /// Renders Aztec to PPM bytes.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static byte[] Ppm(string text, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = Encode(text, encodeOptions);
        return MatrixPpmRenderer.Render(modules, ToPngOptions(renderOptions));
    }

    /// <summary>
    /// Renders Aztec to PPM bytes.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static byte[] Ppm(ReadOnlySpan<byte> data, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = Encode(data, encodeOptions);
        return MatrixPpmRenderer.Render(modules, ToPngOptions(renderOptions));
    }

    /// <summary>
    /// Renders Aztec to PBM bytes.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static byte[] Pbm(string text, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = Encode(text, encodeOptions);
        return MatrixPbmRenderer.Render(modules, ToPngOptions(renderOptions));
    }

    /// <summary>
    /// Renders Aztec to PGM bytes.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static byte[] Pgm(string text, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = Encode(text, encodeOptions);
        return MatrixPgmRenderer.Render(modules, ToPngOptions(renderOptions));
    }

    /// <summary>
    /// Renders Aztec to PAM bytes.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static byte[] Pam(string text, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = Encode(text, encodeOptions);
        return MatrixPamRenderer.Render(modules, ToPngOptions(renderOptions));
    }

    /// <summary>
    /// Renders Aztec to XBM.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string Xbm(string text, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = Encode(text, encodeOptions);
        return MatrixXbmRenderer.Render(modules, ToPngOptions(renderOptions));
    }

    /// <summary>
    /// Renders Aztec to XPM.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string Xpm(string text, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = Encode(text, encodeOptions);
        return MatrixXpmRenderer.Render(modules, ToPngOptions(renderOptions));
    }

    /// <summary>
    /// Renders Aztec to PBM bytes.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static byte[] Pbm(ReadOnlySpan<byte> data, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = Encode(data, encodeOptions);
        return MatrixPbmRenderer.Render(modules, ToPngOptions(renderOptions));
    }

    /// <summary>
    /// Renders Aztec to PGM bytes.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static byte[] Pgm(ReadOnlySpan<byte> data, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = Encode(data, encodeOptions);
        return MatrixPgmRenderer.Render(modules, ToPngOptions(renderOptions));
    }

    /// <summary>
    /// Renders Aztec to PAM bytes.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static byte[] Pam(ReadOnlySpan<byte> data, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = Encode(data, encodeOptions);
        return MatrixPamRenderer.Render(modules, ToPngOptions(renderOptions));
    }

    /// <summary>
    /// Renders Aztec to XBM.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string Xbm(ReadOnlySpan<byte> data, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = Encode(data, encodeOptions);
        return MatrixXbmRenderer.Render(modules, ToPngOptions(renderOptions));
    }

    /// <summary>
    /// Renders Aztec to XPM.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string Xpm(ReadOnlySpan<byte> data, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = Encode(data, encodeOptions);
        return MatrixXpmRenderer.Render(modules, ToPngOptions(renderOptions));
    }

    /// <summary>
    /// Renders Aztec to TGA bytes.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static byte[] Tga(string text, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = Encode(text, encodeOptions);
        return MatrixTgaRenderer.Render(modules, ToPngOptions(renderOptions));
    }

    /// <summary>
    /// Renders Aztec to TGA bytes.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static byte[] Tga(ReadOnlySpan<byte> data, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = Encode(data, encodeOptions);
        return MatrixTgaRenderer.Render(modules, ToPngOptions(renderOptions));
    }

    /// <summary>
    /// Renders Aztec to ICO bytes.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static byte[] Ico(string text, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = Encode(text, encodeOptions);
        return MatrixIcoRenderer.Render(modules, ToPngOptions(renderOptions), ToIcoOptions(renderOptions));
    }

    /// <summary>
    /// Renders Aztec to ICO bytes.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static byte[] Ico(ReadOnlySpan<byte> data, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null) {
        var modules = Encode(data, encodeOptions);
        return MatrixIcoRenderer.Render(modules, ToPngOptions(renderOptions), ToIcoOptions(renderOptions));
    }

    /// <summary>
    /// Renders Aztec to PDF bytes.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static byte[] Pdf(string text, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null, RenderMode renderMode = RenderMode.Vector) {
        var modules = Encode(text, encodeOptions);
        return MatrixPdfRenderer.Render(modules, ToPngOptions(renderOptions), renderMode);
    }

    /// <summary>
    /// Renders Aztec to PDF bytes.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static byte[] Pdf(ReadOnlySpan<byte> data, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null, RenderMode renderMode = RenderMode.Vector) {
        var modules = Encode(data, encodeOptions);
        return MatrixPdfRenderer.Render(modules, ToPngOptions(renderOptions), renderMode);
    }

    /// <summary>
    /// Renders Aztec to EPS markup.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string Eps(string text, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null, RenderMode renderMode = RenderMode.Vector) {
        var modules = Encode(text, encodeOptions);
        return MatrixEpsRenderer.Render(modules, ToPngOptions(renderOptions), renderMode);
    }

    /// <summary>
    /// Renders Aztec to EPS markup.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string Eps(ReadOnlySpan<byte> data, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null, RenderMode renderMode = RenderMode.Vector) {
        var modules = Encode(data, encodeOptions);
        return MatrixEpsRenderer.Render(modules, ToPngOptions(renderOptions), renderMode);
    }

    /// <summary>
    /// Renders Aztec to ASCII text.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string Ascii(string text, AztecEncodeOptions? encodeOptions = null, MatrixAsciiRenderOptions? renderOptions = null) {
        var modules = Encode(text, encodeOptions);
        return MatrixAsciiRenderer.Render(modules, renderOptions);
    }

    /// <summary>
    /// Renders Aztec to ASCII text.
    /// </summary>
    [Obsolete(CodeGlyphX.Internal.ObsoleteMessages.RenderFormatHelpers)]
    public static string Ascii(ReadOnlySpan<byte> data, AztecEncodeOptions? encodeOptions = null, MatrixAsciiRenderOptions? renderOptions = null) {
        var modules = Encode(data, encodeOptions);
        return MatrixAsciiRenderer.Render(modules, renderOptions);
    }

    /// <summary>
    /// Saves Aztec to a file based on extension.
    /// Defaults to PNG when no extension is provided.
    /// </summary>
    [Obsolete("Use the overload with RenderExtras and set HtmlTitle instead.")]
    public static string Save(string text, string path, AztecEncodeOptions? encodeOptions, MatrixOptions? renderOptions, string? title) {
        var extras = string.IsNullOrEmpty(title) ? null : new RenderExtras { HtmlTitle = title };
        var format = OutputFormatInfo.Resolve(path, OutputFormat.Png);
        var output = Render(text, format, encodeOptions, renderOptions, extras);
        return OutputWriter.Write(path, output);
    }

    /// <summary>
    /// Saves Aztec to a file based on extension.
    /// Defaults to PNG when no extension is provided.
    /// </summary>
    public static string Save(string text, string path, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null, RenderExtras? extras = null) {
        var format = OutputFormatInfo.Resolve(path, OutputFormat.Png);
        var output = Render(text, format, encodeOptions, renderOptions, extras);
        return OutputWriter.Write(path, output);
    }

    private static MatrixPngRenderOptions ToPngOptions(MatrixOptions? options) {
        var opts = options ?? new MatrixOptions();
        return new MatrixPngRenderOptions {
            ModuleSize = opts.ModuleSize,
            QuietZone = opts.QuietZone,
            Foreground = opts.Foreground,
            Background = opts.Background
        };
    }

    private static IcoRenderOptions ToIcoOptions(MatrixOptions? options) {
        var opts = options ?? new MatrixOptions();
        return new IcoRenderOptions {
            Sizes = opts.IcoSizes ?? new[] { 16, 32, 48, 64, 128, 256 },
            PreserveAspectRatio = opts.IcoPreserveAspectRatio
        };
    }

    private static MatrixSvgRenderOptions ToSvgOptions(MatrixOptions? options) {
        var opts = options ?? new MatrixOptions();
        return new MatrixSvgRenderOptions {
            ModuleSize = opts.ModuleSize,
            QuietZone = opts.QuietZone,
            DarkColor = ToCss(opts.Foreground),
            LightColor = ToCss(opts.Background)
        };
    }

    private static MatrixHtmlRenderOptions ToHtmlOptions(MatrixOptions? options) {
        var opts = options ?? new MatrixOptions();
        return new MatrixHtmlRenderOptions {
            ModuleSize = opts.ModuleSize,
            QuietZone = opts.QuietZone,
            DarkColor = ToCss(opts.Foreground),
            LightColor = ToCss(opts.Background),
            EmailSafeTable = opts.HtmlEmailSafeTable
        };
    }

    private static string ToCss(Rgba32 color) {
        if (color.A == 255) {
            return $"rgb({color.R},{color.G},{color.B})";
        }
        var a = color.A / 255.0;
        return $"rgba({color.R},{color.G},{color.B},{a:0.###})";
    }
}
