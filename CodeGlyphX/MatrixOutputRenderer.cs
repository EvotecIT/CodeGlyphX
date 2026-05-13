using System;
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
using CodeGlyphX.Rendering.Png;
using CodeGlyphX.Rendering.Ppm;
using CodeGlyphX.Rendering.Svg;
using CodeGlyphX.Rendering.Svgz;
using CodeGlyphX.Rendering.Tga;
using CodeGlyphX.Rendering.Tiff;
using CodeGlyphX.Rendering.Webp;
using CodeGlyphX.Rendering.Xbm;
using CodeGlyphX.Rendering.Xpm;

namespace CodeGlyphX;

internal static class MatrixOutputRenderer {
    public static RenderedOutput Render(BitMatrix modules, OutputFormat format, MatrixOptions? options, RenderExtras? extras) {
        if (modules is null) throw new ArgumentNullException(nameof(modules));
        if (format == OutputFormat.Unknown) throw new ArgumentOutOfRangeException(nameof(format));

        var png = MatrixRenderOptionsBuilder.BuildPng(options);
        return format switch {
            OutputFormat.Png => Binary(format, MatrixPngRenderer.Render(modules, png)),
            OutputFormat.Svg => Text(format, MatrixSvgRenderer.Render(modules, MatrixRenderOptionsBuilder.BuildSvg(options))),
            OutputFormat.Svgz => Binary(format, MatrixSvgzRenderer.Render(modules, MatrixRenderOptionsBuilder.BuildSvg(options))),
            OutputFormat.Html => Text(format, RenderHtml(modules, options, extras)),
            OutputFormat.Jpeg => Binary(format, RenderJpeg(modules, png, options)),
            OutputFormat.Webp => Binary(format, RenderWebp(modules, png, options, extras)),
            OutputFormat.Bmp => Binary(format, MatrixBmpRenderer.Render(modules, png)),
            OutputFormat.Gif => Binary(format, RenderGif(modules, png, extras)),
            OutputFormat.Tiff => Binary(format, MatrixTiffRenderer.Render(modules, png, extras?.TiffCompression ?? TiffCompressionMode.Auto)),
            OutputFormat.Ppm => Binary(format, MatrixPpmRenderer.Render(modules, png)),
            OutputFormat.Pbm => Binary(format, MatrixPbmRenderer.Render(modules, png)),
            OutputFormat.Pgm => Binary(format, MatrixPgmRenderer.Render(modules, png)),
            OutputFormat.Pam => Binary(format, MatrixPamRenderer.Render(modules, png)),
            OutputFormat.Xbm => Text(format, MatrixXbmRenderer.Render(modules, png)),
            OutputFormat.Xpm => Text(format, MatrixXpmRenderer.Render(modules, png)),
            OutputFormat.Tga => Binary(format, MatrixTgaRenderer.Render(modules, png)),
            OutputFormat.Ico => Binary(format, MatrixIcoRenderer.Render(modules, png, MatrixRenderOptionsBuilder.BuildIco(options))),
            OutputFormat.Pdf => Binary(format, MatrixPdfRenderer.Render(modules, png, extras?.VectorMode ?? RenderMode.Vector)),
            OutputFormat.Eps => Text(format, MatrixEpsRenderer.Render(modules, png, extras?.VectorMode ?? RenderMode.Vector)),
            OutputFormat.Ascii => Text(format, MatrixAsciiRenderer.Render(modules, MatrixRenderOptionsBuilder.BuildAscii(options, extras))),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported output format.")
        };
    }

    private static RenderedOutput Binary(OutputFormat format, byte[] data) => RenderedOutput.FromBinary(format, data);

    private static RenderedOutput Text(OutputFormat format, string text) => RenderedOutput.FromText(format, text);

    private static string RenderHtml(BitMatrix modules, MatrixOptions? options, RenderExtras? extras) {
        var html = MatrixHtmlRenderer.Render(modules, MatrixRenderOptionsBuilder.BuildHtml(options));
        var title = extras?.HtmlTitle;
        return string.IsNullOrEmpty(title) ? html : html.WrapHtml(title);
    }

    private static byte[] RenderJpeg(BitMatrix modules, MatrixPngRenderOptions pngOptions, MatrixOptions? options) {
        var jpegOptions = options?.JpegOptions;
        return jpegOptions is null
            ? MatrixJpegRenderer.Render(modules, pngOptions, options?.JpegQuality ?? 85)
            : MatrixJpegRenderer.Render(modules, pngOptions, jpegOptions);
    }

    private static byte[] RenderWebp(BitMatrix modules, MatrixPngRenderOptions pngOptions, MatrixOptions? options, RenderExtras? extras) {
        var quality = options?.WebpQuality ?? 100;
        return RenderAnimationHelpers.TryRenderMatrixWebp(extras, pngOptions, quality, out var webp)
            ? webp
            : MatrixWebpRenderer.Render(modules, pngOptions, quality);
    }

    private static byte[] RenderGif(BitMatrix modules, MatrixPngRenderOptions pngOptions, RenderExtras? extras) {
        return RenderAnimationHelpers.TryRenderMatrixGif(extras, pngOptions, out var gif)
            ? gif
            : MatrixGifRenderer.Render(modules, pngOptions);
    }

}
