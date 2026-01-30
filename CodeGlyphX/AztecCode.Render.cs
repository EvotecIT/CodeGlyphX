using System;
using CodeGlyphX.Aztec;
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

public static partial class AztecCode {
    /// <summary>
    /// Renders an Aztec payload to the requested output format.
    /// </summary>
    public static RenderedOutput Render(string text, OutputFormat format, AztecEncodeOptions? encodeOptions = null, MatrixOptions? renderOptions = null, RenderExtras? extras = null) {
        var modules = Encode(text, encodeOptions);
        return Render(modules, format, renderOptions, extras);
    }

    private static RenderedOutput Render(BitMatrix modules, OutputFormat format, MatrixOptions? renderOptions, RenderExtras? extras) {
        if (modules is null) throw new ArgumentNullException(nameof(modules));
        if (format == OutputFormat.Unknown) throw new ArgumentOutOfRangeException(nameof(format));

        var pngOptions = ToPngOptions(renderOptions);
        switch (format) {
            case OutputFormat.Png:
                return RenderedOutput.FromBinary(format, MatrixPngRenderer.Render(modules, pngOptions));
            case OutputFormat.Svg:
                return RenderedOutput.FromText(format, MatrixSvgRenderer.Render(modules, ToSvgOptions(renderOptions)));
            case OutputFormat.Svgz:
                return RenderedOutput.FromBinary(format, MatrixSvgzRenderer.Render(modules, ToSvgOptions(renderOptions)));
            case OutputFormat.Html: {
                var html = MatrixHtmlRenderer.Render(modules, ToHtmlOptions(renderOptions));
                var title = extras?.HtmlTitle;
                if (!string.IsNullOrEmpty(title)) {
                    html = html.WrapHtml(title);
                }
                return RenderedOutput.FromText(format, html);
            }
            case OutputFormat.Jpeg:
                return RenderedOutput.FromBinary(format, MatrixJpegRenderer.Render(modules, pngOptions, renderOptions?.JpegQuality ?? 85));
            case OutputFormat.Webp:
                return RenderedOutput.FromBinary(format, MatrixWebpRenderer.Render(modules, pngOptions, renderOptions?.WebpQuality ?? 100));
            case OutputFormat.Gif:
                return RenderedOutput.FromBinary(format, MatrixGifRenderer.Render(modules, pngOptions));
            case OutputFormat.Tiff:
                return RenderedOutput.FromBinary(format, MatrixTiffRenderer.Render(modules, pngOptions, extras?.TiffCompression ?? TiffCompressionMode.Auto));
            case OutputFormat.Bmp:
                return RenderedOutput.FromBinary(format, MatrixBmpRenderer.Render(modules, pngOptions));
            case OutputFormat.Gif: {
                var extrasFrames = extras?.GifFrames;
                if (extrasFrames is not null && extrasFrames.Length > 0) {
                    var duration = extras?.AnimationDurationMs ?? 100;
                    var durations = extras?.AnimationDurationsMs;
                    var gif = durations is not null
                        ? MatrixGifRenderer.RenderAnimation(extrasFrames, pngOptions, durations, extras?.GifAnimationOptions ?? default)
                        : MatrixGifRenderer.RenderAnimation(extrasFrames, pngOptions, duration, extras?.GifAnimationOptions ?? default);
                    return RenderedOutput.FromBinary(format, gif);
                }
                return RenderedOutput.FromBinary(format, MatrixGifRenderer.Render(modules, pngOptions));
            }
            case OutputFormat.Tiff:
                return RenderedOutput.FromBinary(format, MatrixTiffRenderer.Render(modules, pngOptions));
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
                return RenderedOutput.FromBinary(format, MatrixIcoRenderer.Render(modules, pngOptions, ToIcoOptions(renderOptions)));
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
