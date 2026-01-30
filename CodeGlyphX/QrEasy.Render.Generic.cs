using System;
using System.Text;
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

public static partial class QrEasy {
    internal static RenderedOutput Render(QrCode qr, OutputFormat format, QrEasyOptions? options = null, RenderExtras? extras = null) {
        if (qr is null) throw new ArgumentNullException(nameof(qr));
        if (format == OutputFormat.Unknown) throw new ArgumentOutOfRangeException(nameof(format));

        var opts = options is null ? new QrEasyOptions() : CloneOptions(options);
        switch (format) {
            case OutputFormat.Png: {
                var render = BuildPngOptions(opts, qr);
                return RenderedOutput.FromBinary(format, QrPngRenderer.Render(qr.Modules, render));
            }
            case OutputFormat.Svg: {
                var render = BuildSvgOptions(opts, qr.Modules.Width);
                return RenderedOutput.FromText(format, SvgQrRenderer.Render(qr.Modules, render));
            }
            case OutputFormat.Svgz: {
                var render = BuildSvgOptions(opts, qr.Modules.Width);
                return RenderedOutput.FromBinary(format, QrSvgzRenderer.Render(qr.Modules, render));
            }
            case OutputFormat.Html: {
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
                var html = HtmlQrRenderer.Render(qr.Modules, render);
                var title = extras?.HtmlTitle;
                if (!string.IsNullOrEmpty(title)) {
                    html = html.WrapHtml(title);
                }
                return RenderedOutput.FromText(format, html);
            }
            case OutputFormat.Jpeg: {
                var render = BuildPngOptions(opts, qr);
                var quality = opts.JpegQuality;
                return RenderedOutput.FromBinary(format, QrJpegRenderer.Render(qr.Modules, render, quality));
            }
            case OutputFormat.Webp: {
                var render = BuildPngOptions(opts, qr);
                var quality = opts.WebpQuality;
                return RenderedOutput.FromBinary(format, QrWebpRenderer.Render(qr.Modules, render, quality));
            }
            case OutputFormat.Gif: {
                var render = BuildPngOptions(opts, qr);
                return RenderedOutput.FromBinary(format, QrGifRenderer.Render(qr.Modules, render));
            }
            case OutputFormat.Tiff: {
                var render = BuildPngOptions(opts, qr);
                var compression = extras?.TiffCompression ?? TiffCompressionMode.Auto;
                return RenderedOutput.FromBinary(format, QrTiffRenderer.Render(qr.Modules, render, compression));
            }
            case OutputFormat.Bmp: {
                var render = BuildPngOptions(opts, qr);
                return RenderedOutput.FromBinary(format, QrBmpRenderer.Render(qr.Modules, render));
            }
            case OutputFormat.Ppm: {
                var render = BuildPngOptions(opts, qr);
                return RenderedOutput.FromBinary(format, QrPpmRenderer.Render(qr.Modules, render));
            }
            case OutputFormat.Pbm: {
                var render = BuildPngOptions(opts, qr);
                return RenderedOutput.FromBinary(format, QrPbmRenderer.Render(qr.Modules, render));
            }
            case OutputFormat.Pgm: {
                var render = BuildPngOptions(opts, qr);
                return RenderedOutput.FromBinary(format, QrPgmRenderer.Render(qr.Modules, render));
            }
            case OutputFormat.Pam: {
                var render = BuildPngOptions(opts, qr);
                return RenderedOutput.FromBinary(format, QrPamRenderer.Render(qr.Modules, render));
            }
            case OutputFormat.Xbm: {
                var render = BuildPngOptions(opts, qr);
                return RenderedOutput.FromText(format, QrXbmRenderer.Render(qr.Modules, render));
            }
            case OutputFormat.Xpm: {
                var render = BuildPngOptions(opts, qr);
                return RenderedOutput.FromText(format, QrXpmRenderer.Render(qr.Modules, render));
            }
            case OutputFormat.Tga: {
                var render = BuildPngOptions(opts, qr);
                return RenderedOutput.FromBinary(format, QrTgaRenderer.Render(qr.Modules, render));
            }
            case OutputFormat.Ico: {
                var render = BuildPngOptions(opts, qr);
                return RenderedOutput.FromBinary(format, QrIcoRenderer.Render(qr.Modules, render, BuildIcoOptions(opts)));
            }
            case OutputFormat.Pdf: {
                var render = BuildPngOptions(opts, qr);
                return RenderedOutput.FromBinary(format, QrPdfRenderer.Render(qr.Modules, render, extras?.VectorMode ?? RenderMode.Vector));
            }
            case OutputFormat.Eps: {
                var render = BuildPngOptions(opts, qr);
                var eps = QrEpsRenderer.Render(qr.Modules, render, extras?.VectorMode ?? RenderMode.Vector);
                return RenderedOutput.FromText(format, eps, Encoding.ASCII);
            }
            case OutputFormat.Ascii: {
                var asciiOptions = BuildAsciiOptions(extras?.MatrixAscii, opts);
                return RenderedOutput.FromText(format, MatrixAsciiRenderer.Render(qr.Modules, asciiOptions));
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported output format.");
        }
    }
}
