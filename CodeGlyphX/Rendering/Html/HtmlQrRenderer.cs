using System;
using System.Text;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering.Html;

/// <summary>
/// Renders QR modules to HTML (table-based).
/// </summary>
public static class HtmlQrRenderer {
    /// <summary>
    /// Renders the QR module matrix to an HTML table.
    /// </summary>
    public static string Render(BitMatrix modules, QrHtmlRenderOptions opts) {
        if (modules is null) throw new ArgumentNullException(nameof(modules));
        if (opts is null) throw new ArgumentNullException(nameof(opts));
        if (modules.Width != modules.Height) throw new ArgumentException("Matrix must be square.", nameof(modules));
        if (opts.ModuleSize <= 0) throw new ArgumentOutOfRangeException(nameof(opts.ModuleSize));
        if (opts.QuietZone < 0) throw new ArgumentOutOfRangeException(nameof(opts.QuietZone));

        var size = modules.Width;
        var outSize = size + opts.QuietZone * 2;
        var px = outSize * opts.ModuleSize;

        var sb = new StringBuilder(outSize * outSize * 4);
        sb.Append("<table cellpadding=\"0\" cellspacing=\"0\" style=\"border-collapse:collapse;");
        if (!opts.EmailSafeTable) {
            sb.Append("background:").Append(opts.LightColor).Append(';');
        }
        sb.Append("\">");

        for (var y = 0; y < outSize; y++) {
            sb.Append("<tr>");

            var x = 0;
            while (x < outSize) {
                var isDark = IsDark(modules, opts.QuietZone, x, y);
                var run = 1;
                while (x + run < outSize && IsDark(modules, opts.QuietZone, x + run, y) == isDark) run++;

                if (opts.EmailSafeTable) {
                    sb.Append("<td colspan=\"").Append(run).Append("\" width=\"").Append(run * opts.ModuleSize)
                        .Append("\" height=\"").Append(opts.ModuleSize).Append("\" bgcolor=\"")
                        .Append(isDark ? opts.DarkColor : opts.LightColor)
                        .Append("\" style=\"line-height:0;font-size:0;\">&nbsp;</td>");
                } else {
                    sb.Append("<td colspan=\"").Append(run).Append("\" style=\"width:").Append(run * opts.ModuleSize)
                        .Append("px;height:").Append(opts.ModuleSize).Append("px;background:")
                        .Append(isDark ? opts.DarkColor : opts.LightColor)
                        .Append(";\"></td>");
                }

                x += run;
            }

            sb.Append("</tr>");
        }

        sb.Append("</table>");

        if (opts.Logo is null &&
            opts.ModuleShape == QrPngModuleShape.Square &&
            Math.Abs(opts.ModuleScale - 1.0) < 0.0001 &&
            opts.ModuleCornerRadiusPx == 0 &&
            opts.ForegroundGradient is null &&
            opts.Eyes is null) {
            return sb.ToString();
        }

        var svg = new CodeGlyphX.Rendering.Svg.QrSvgRenderOptions {
            ModuleSize = opts.ModuleSize,
            QuietZone = opts.QuietZone,
            DarkColor = opts.DarkColor,
            LightColor = opts.LightColor,
            Logo = opts.Logo,
            ModuleShape = opts.ModuleShape,
            ModuleScale = opts.ModuleScale,
            ModuleCornerRadiusPx = opts.ModuleCornerRadiusPx,
            ForegroundGradient = opts.ForegroundGradient,
            Eyes = opts.Eyes,
        };
        return CodeGlyphX.Rendering.Svg.SvgQrRenderer.Render(modules, svg);
    }

    /// <summary>
    /// Renders the QR module matrix to an HTML file.
    /// </summary>
    /// <param name="modules">QR modules.</param>
    /// <param name="opts">Rendering options.</param>
    /// <param name="path">Output file path.</param>
    /// <returns>The output file path.</returns>
    public static string RenderToFile(BitMatrix modules, QrHtmlRenderOptions opts, string path) {
        var html = Render(modules, opts);
        return RenderIO.WriteText(path, html);
    }

    /// <summary>
    /// Renders the QR module matrix to an HTML file under the specified directory.
    /// </summary>
    /// <param name="modules">QR modules.</param>
    /// <param name="opts">Rendering options.</param>
    /// <param name="directory">Output directory.</param>
    /// <param name="fileName">Output file name.</param>
    /// <returns>The output file path.</returns>
    public static string RenderToFile(BitMatrix modules, QrHtmlRenderOptions opts, string directory, string fileName) {
        var html = Render(modules, opts);
        return RenderIO.WriteText(directory, fileName, html);
    }

    private static bool IsDark(BitMatrix modules, int quietZone, int xOut, int yOut) {
        var x = xOut - quietZone;
        var y = yOut - quietZone;
        if ((uint)x >= (uint)modules.Width || (uint)y >= (uint)modules.Height) return false;
        return modules[x, y];
    }
}
