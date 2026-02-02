using System;
using System.IO;
using System.Text;

namespace CodeGlyphX.Rendering.Html;

/// <summary>
/// Renders generic 2D matrices to HTML (table-based).
/// </summary>
public static class MatrixHtmlRenderer {
    /// <summary>
    /// Renders the matrix to an HTML table.
    /// </summary>
    public static string Render(BitMatrix modules, MatrixHtmlRenderOptions opts) {
        if (modules is null) throw new ArgumentNullException(nameof(modules));
        if (opts is null) throw new ArgumentNullException(nameof(opts));
        if (opts.ModuleSize <= 0) throw new ArgumentOutOfRangeException(nameof(opts.ModuleSize));
        if (opts.QuietZone < 0) throw new ArgumentOutOfRangeException(nameof(opts.QuietZone));

        var darkColor = RenderSanitizer.SafeCssColor(opts.DarkColor, RenderDefaults.QrForegroundCss);
        var lightColor = RenderSanitizer.SafeCssColor(opts.LightColor, RenderDefaults.QrBackgroundCss);

        var outWidth = modules.Width + opts.QuietZone * 2;
        var outHeight = modules.Height + opts.QuietZone * 2;

        var sb = new StringBuilder(outWidth * outHeight * 4);
        sb.Append("<table cellpadding=\"0\" cellspacing=\"0\" style=\"border-collapse:collapse;");
        if (!opts.EmailSafeTable) {
            sb.Append("background:").Append(lightColor).Append(';');
        }
        sb.Append("\">");

        for (var y = 0; y < outHeight; y++) {
            sb.Append("<tr>");

            var x = 0;
            while (x < outWidth) {
                var isDark = IsDark(modules, opts.QuietZone, x, y);
                var run = 1;
                while (x + run < outWidth && IsDark(modules, opts.QuietZone, x + run, y) == isDark) run++;

                if (opts.EmailSafeTable) {
                    sb.Append("<td colspan=\"").Append(run).Append("\" width=\"").Append(run * opts.ModuleSize)
                        .Append("\" height=\"").Append(opts.ModuleSize).Append("\" bgcolor=\"")
                        .Append(isDark ? darkColor : lightColor)
                        .Append("\" style=\"line-height:0;font-size:0;\">&nbsp;</td>");
                } else {
                    sb.Append("<td colspan=\"").Append(run).Append("\" style=\"width:").Append(run * opts.ModuleSize)
                        .Append("px;height:").Append(opts.ModuleSize).Append("px;background:")
                        .Append(isDark ? darkColor : lightColor)
                        .Append(";\"></td>");
                }

                x += run;
            }

            sb.Append("</tr>");
        }

        sb.Append("</table>");
        return sb.ToString();
    }

    /// <summary>
    /// Renders the matrix to an HTML stream.
    /// </summary>
    /// <param name="modules">Matrix modules.</param>
    /// <param name="opts">Rendering options.</param>
    /// <param name="stream">Target stream.</param>
    public static void RenderToStream(BitMatrix modules, MatrixHtmlRenderOptions opts, Stream stream) {
        var html = Render(modules, opts);
        RenderIO.WriteText(stream, html);
    }

    /// <summary>
    /// Renders the matrix to an HTML file.
    /// </summary>
    /// <param name="modules">Matrix modules.</param>
    /// <param name="opts">Rendering options.</param>
    /// <param name="path">Output file path.</param>
    /// <returns>The output file path.</returns>
    public static string RenderToFile(BitMatrix modules, MatrixHtmlRenderOptions opts, string path) {
        var html = Render(modules, opts);
        return RenderIO.WriteText(path, html);
    }

    /// <summary>
    /// Renders the matrix to an HTML file under the specified directory.
    /// </summary>
    /// <param name="modules">Matrix modules.</param>
    /// <param name="opts">Rendering options.</param>
    /// <param name="directory">Output directory.</param>
    /// <param name="fileName">Output file name.</param>
    /// <returns>The output file path.</returns>
    public static string RenderToFile(BitMatrix modules, MatrixHtmlRenderOptions opts, string directory, string fileName) {
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
