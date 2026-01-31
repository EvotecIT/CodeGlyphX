using System;
using CodeGlyphX.Pdf417;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Ascii;
using CodeGlyphX.Rendering.Bmp;
using CodeGlyphX.Rendering.Eps;
using CodeGlyphX.Rendering.Gif;
using CodeGlyphX.Rendering.Html;
using CodeGlyphX.Rendering.Ico;
using CodeGlyphX.Rendering.Jpeg;
using CodeGlyphX.Rendering.Pam;
using CodeGlyphX.Rendering.Pbm;
using CodeGlyphX.Rendering.Pdf;
using CodeGlyphX.Rendering.Pgm;
using CodeGlyphX.Rendering.Ppm;
using CodeGlyphX.Rendering.Png;
using CodeGlyphX.Rendering.Svg;
using CodeGlyphX.Rendering.Svgz;
using CodeGlyphX.Rendering.Tga;
using CodeGlyphX.Rendering.Tiff;
using CodeGlyphX.Rendering.Webp;
using CodeGlyphX.Rendering.Xbm;
using CodeGlyphX.Rendering.Xpm;

namespace CodeGlyphX;

public static partial class Pdf417Code {
    /// <summary>
    /// Renders a PDF417 payload to the requested output format.
    /// </summary>
    public static RenderedOutput Render(string text, OutputFormat format, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null, RenderExtras? extras = null) {
        var modules = Encode(text, encodeOptions);
        return Render(modules, format, renderOptions, extras);
    }

    /// <summary>
    /// Renders a Macro PDF417 payload to the requested output format.
    /// </summary>
    public static RenderedOutput RenderMacro(string text, Pdf417MacroOptions macro, OutputFormat format, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null, RenderExtras? extras = null) {
        var modules = EncodeMacro(text, macro, encodeOptions);
        return Render(modules, format, renderOptions, extras);
    }

    /// <summary>
    /// Renders a PDF417 byte payload to the requested output format.
    /// </summary>
    public static RenderedOutput Render(byte[] data, OutputFormat format, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null, RenderExtras? extras = null) {
        var modules = EncodeBytes(data, encodeOptions);
        return Render(modules, format, renderOptions, extras);
    }

#if NET8_0_OR_GREATER
    /// <summary>
    /// Renders a PDF417 byte payload to the requested output format.
    /// </summary>
    public static RenderedOutput Render(ReadOnlySpan<byte> data, OutputFormat format, Pdf417EncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null, RenderExtras? extras = null) {
        var modules = EncodeBytes(data, encodeOptions);
        return Render(modules, format, renderOptions, extras);
    }
#endif

    private static RenderedOutput Render(BitMatrix modules, OutputFormat format, MatrixOptions? renderOptions, RenderExtras? extras) {
        if (modules is null) throw new ArgumentNullException(nameof(modules));
        if (format == OutputFormat.Unknown) throw new ArgumentOutOfRangeException(nameof(format));

        var pngOptions = BuildPngOptions(renderOptions);
        switch (format) {
            case OutputFormat.Png:
                return RenderedOutput.FromBinary(format, MatrixPngRenderer.Render(modules, pngOptions));
            case OutputFormat.Svg:
                return RenderedOutput.FromText(format, MatrixSvgRenderer.Render(modules, BuildSvgOptions(renderOptions)));
            case OutputFormat.Svgz:
                return RenderedOutput.FromBinary(format, MatrixSvgzRenderer.Render(modules, BuildSvgOptions(renderOptions)));
            case OutputFormat.Html: {
                var html = MatrixHtmlRenderer.Render(modules, BuildHtmlOptions(renderOptions));
                var title = extras?.HtmlTitle;
                if (!string.IsNullOrEmpty(title)) {
                    html = html.WrapHtml(title);
                }
                return RenderedOutput.FromText(format, html);
            }
            case OutputFormat.Jpeg:
                return RenderedOutput.FromBinary(format, MatrixJpegRenderer.Render(modules, pngOptions, renderOptions?.JpegQuality ?? 85));
            case OutputFormat.Webp: {
                var quality = renderOptions?.WebpQuality ?? 100;
                if (RenderAnimationHelpers.TryRenderMatrixWebp(extras, pngOptions, quality, out var webp)) {
                    return RenderedOutput.FromBinary(format, webp);
                }
                return RenderedOutput.FromBinary(format, MatrixWebpRenderer.Render(modules, pngOptions, quality));
            }
            case OutputFormat.Bmp:
                return RenderedOutput.FromBinary(format, MatrixBmpRenderer.Render(modules, pngOptions));
            case OutputFormat.Gif: {
                if (RenderAnimationHelpers.TryRenderMatrixGif(extras, pngOptions, out var gif)) {
                    return RenderedOutput.FromBinary(format, gif);
                }
                return RenderedOutput.FromBinary(format, MatrixGifRenderer.Render(modules, pngOptions));
            }
            case OutputFormat.Tiff:
                return RenderedOutput.FromBinary(format, MatrixTiffRenderer.Render(modules, pngOptions, extras?.TiffCompression ?? TiffCompressionMode.Auto));
            case OutputFormat.Ppm:
                return RenderedOutput.FromBinary(format, MatrixPpmRenderer.Render(modules, pngOptions));
            case OutputFormat.Pbm:
                return RenderedOutput.FromBinary(format, MatrixPbmRenderer.Render(modules, pngOptions));
            case OutputFormat.Pgm:
                return RenderedOutput.FromBinary(format, MatrixPgmRenderer.Render(modules, pngOptions));
            case OutputFormat.Pam:
                return RenderedOutput.FromBinary(format, MatrixPamRenderer.Render(modules, pngOptions));
            case OutputFormat.Xbm:
                return RenderedOutput.FromText(format, MatrixXbmRenderer.Render(modules, pngOptions));
            case OutputFormat.Xpm:
                return RenderedOutput.FromText(format, MatrixXpmRenderer.Render(modules, pngOptions));
            case OutputFormat.Tga:
                return RenderedOutput.FromBinary(format, MatrixTgaRenderer.Render(modules, pngOptions));
            case OutputFormat.Ico:
                return RenderedOutput.FromBinary(format, MatrixIcoRenderer.Render(modules, pngOptions, BuildIcoOptions(renderOptions)));
            case OutputFormat.Pdf:
                return RenderedOutput.FromBinary(format, MatrixPdfRenderer.Render(modules, pngOptions, extras?.VectorMode ?? RenderMode.Vector));
            case OutputFormat.Eps:
                return RenderedOutput.FromText(format, MatrixEpsRenderer.Render(modules, pngOptions, extras?.VectorMode ?? RenderMode.Vector));
            case OutputFormat.Ascii:
                return RenderedOutput.FromText(format, MatrixAsciiRenderer.Render(modules, extras?.MatrixAscii ?? new MatrixAsciiRenderOptions()));
            default:
                throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported output format.");
        }
    }
}
