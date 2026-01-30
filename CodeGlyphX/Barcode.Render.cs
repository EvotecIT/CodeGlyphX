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

public static partial class Barcode {
    /// <summary>
    /// Renders a barcode to the requested output format.
    /// </summary>
    public static RenderedOutput Render(BarcodeType type, string content, OutputFormat format, BarcodeOptions? options = null, RenderExtras? extras = null) {
        var barcode = Encode(type, content);
        return Render(barcode, format, options, extras);
    }

    /// <summary>
    /// Renders a barcode to the requested output format.
    /// </summary>
    public static RenderedOutput Render(Barcode1D barcode, OutputFormat format, BarcodeOptions? options = null, RenderExtras? extras = null) {
        if (barcode is null) throw new ArgumentNullException(nameof(barcode));
        if (format == OutputFormat.Unknown) throw new ArgumentOutOfRangeException(nameof(format));

        var opts = BuildPngOptions(options);
        switch (format) {
            case OutputFormat.Png:
                return RenderedOutput.FromBinary(format, BarcodePngRenderer.Render(barcode, opts));
            case OutputFormat.Svg:
                return RenderedOutput.FromText(format, SvgBarcodeRenderer.Render(barcode, BuildSvgOptions(options)));
            case OutputFormat.Svgz:
                return RenderedOutput.FromBinary(format, BarcodeSvgzRenderer.Render(barcode, BuildSvgOptions(options)));
            case OutputFormat.Html: {
                var html = HtmlBarcodeRenderer.Render(barcode, BuildHtmlOptions(options));
                var title = extras?.HtmlTitle;
                if (!string.IsNullOrEmpty(title)) {
                    html = html.WrapHtml(title);
                }
                return RenderedOutput.FromText(format, html);
            }
            case OutputFormat.Jpeg: {
                var jpegOptions = options?.JpegOptions;
                var data = jpegOptions is null
                    ? BarcodeJpegRenderer.Render(barcode, opts, options?.JpegQuality ?? 90)
                    : BarcodeJpegRenderer.Render(barcode, opts, jpegOptions);
                return RenderedOutput.FromBinary(format, data);
            }
            case OutputFormat.Webp: {
                var quality = options?.WebpQuality ?? 100;
                if (RenderAnimationHelpers.TryRenderBarcodeWebp(extras, opts, quality, out var webp)) {
                    return RenderedOutput.FromBinary(format, webp);
                }
                return RenderedOutput.FromBinary(format, BarcodeWebpRenderer.Render(barcode, opts, quality));
            }
            case OutputFormat.Bmp:
                return RenderedOutput.FromBinary(format, BarcodeBmpRenderer.Render(barcode, opts));
            case OutputFormat.Gif: {
                if (RenderAnimationHelpers.TryRenderBarcodeGif(extras, opts, out var gif)) {
                    return RenderedOutput.FromBinary(format, gif);
                }
                return RenderedOutput.FromBinary(format, BarcodeGifRenderer.Render(barcode, opts));
            }
            case OutputFormat.Tiff:
                return RenderedOutput.FromBinary(format, BarcodeTiffRenderer.Render(barcode, opts, extras?.TiffCompression ?? TiffCompressionMode.Auto));
            case OutputFormat.Ppm:
                return RenderedOutput.FromBinary(format, BarcodePpmRenderer.Render(barcode, opts));
            case OutputFormat.Pbm:
                return RenderedOutput.FromBinary(format, BarcodePbmRenderer.Render(barcode, opts));
            case OutputFormat.Pgm:
                return RenderedOutput.FromBinary(format, BarcodePgmRenderer.Render(barcode, opts));
            case OutputFormat.Pam:
                return RenderedOutput.FromBinary(format, BarcodePamRenderer.Render(barcode, opts));
            case OutputFormat.Xbm:
                return RenderedOutput.FromText(format, BarcodeXbmRenderer.Render(barcode, opts));
            case OutputFormat.Xpm:
                return RenderedOutput.FromText(format, BarcodeXpmRenderer.Render(barcode, opts));
            case OutputFormat.Tga:
                return RenderedOutput.FromBinary(format, BarcodeTgaRenderer.Render(barcode, opts));
            case OutputFormat.Ico:
                return RenderedOutput.FromBinary(format, BarcodeIcoRenderer.Render(barcode, opts, BuildIcoOptions(options)));
            case OutputFormat.Pdf:
                return RenderedOutput.FromBinary(format, BarcodePdfRenderer.Render(barcode, opts, extras?.VectorMode ?? RenderMode.Vector));
            case OutputFormat.Eps:
                return RenderedOutput.FromText(format, BarcodeEpsRenderer.Render(barcode, opts, extras?.VectorMode ?? RenderMode.Vector));
            case OutputFormat.Ascii:
                return RenderedOutput.FromText(format, BarcodeAsciiRenderer.Render(barcode, extras?.BarcodeAscii));
            default:
                throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported output format.");
        }
    }
}
