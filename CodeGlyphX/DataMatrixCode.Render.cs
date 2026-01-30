using System;
using CodeGlyphX.DataMatrix;
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

public static partial class DataMatrixCode {
    /// <summary>
    /// Renders a Data Matrix payload to the requested output format.
    /// </summary>
    public static RenderedOutput Render(string text, OutputFormat format, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null, RenderExtras? extras = null) {
        var modules = Encode(text, mode);
        return Render(modules, format, options, extras);
    }

    /// <summary>
    /// Renders a Data Matrix byte payload to the requested output format.
    /// </summary>
    public static RenderedOutput Render(byte[] data, OutputFormat format, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null, RenderExtras? extras = null) {
        var modules = EncodeBytes(data, mode);
        return Render(modules, format, options, extras);
    }

#if NET8_0_OR_GREATER
    /// <summary>
    /// Renders a Data Matrix byte payload to the requested output format.
    /// </summary>
    public static RenderedOutput Render(ReadOnlySpan<byte> data, OutputFormat format, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null, RenderExtras? extras = null) {
        var modules = EncodeBytes(data, mode);
        return Render(modules, format, options, extras);
    }
#endif

    private static RenderedOutput Render(BitMatrix modules, OutputFormat format, MatrixOptions? options, RenderExtras? extras) {
        if (modules is null) throw new ArgumentNullException(nameof(modules));
        if (format == OutputFormat.Unknown) throw new ArgumentOutOfRangeException(nameof(format));

        var pngOptions = BuildPngOptions(options);
        switch (format) {
            case OutputFormat.Png:
                return RenderedOutput.FromBinary(format, MatrixPngRenderer.Render(modules, pngOptions));
            case OutputFormat.Svg:
                return RenderedOutput.FromText(format, MatrixSvgRenderer.Render(modules, BuildSvgOptions(options)));
            case OutputFormat.Svgz:
                return RenderedOutput.FromBinary(format, MatrixSvgzRenderer.Render(modules, BuildSvgOptions(options)));
            case OutputFormat.Html: {
                var html = MatrixHtmlRenderer.Render(modules, BuildHtmlOptions(options));
                var title = extras?.HtmlTitle;
                if (!string.IsNullOrEmpty(title)) {
                    html = html.WrapHtml(title);
                }
                return RenderedOutput.FromText(format, html);
            }
            case OutputFormat.Jpeg:
                return RenderedOutput.FromBinary(format, MatrixJpegRenderer.Render(modules, pngOptions, options?.JpegQuality ?? 85));
            case OutputFormat.Webp:
                return RenderedOutput.FromBinary(format, MatrixWebpRenderer.Render(modules, pngOptions, options?.WebpQuality ?? 100));
            case OutputFormat.Gif:
                return RenderedOutput.FromBinary(format, MatrixGifRenderer.Render(modules, pngOptions));
            case OutputFormat.Tiff:
                return RenderedOutput.FromBinary(format, MatrixTiffRenderer.Render(modules, pngOptions, extras?.TiffCompression ?? TiffCompressionMode.Auto));
            case OutputFormat.Bmp:
                return RenderedOutput.FromBinary(format, MatrixBmpRenderer.Render(modules, pngOptions));
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
                return RenderedOutput.FromBinary(format, MatrixIcoRenderer.Render(modules, pngOptions, BuildIcoOptions(options)));
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
