using System;
using System.Text;

namespace CodeMatrix.Rendering.Html;

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

        var outWidth = modules.Width + opts.QuietZone * 2;
        var outHeight = modules.Height + opts.QuietZone * 2;

        var sb = new StringBuilder(outWidth * outHeight * 4);
        sb.Append("<table cellpadding=\"0\" cellspacing=\"0\" style=\"border-collapse:collapse;");
        if (!opts.EmailSafeTable) {
            sb.Append("background:").Append(opts.LightColor).Append(';');
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
        return sb.ToString();
    }

    private static bool IsDark(BitMatrix modules, int quietZone, int xOut, int yOut) {
        var x = xOut - quietZone;
        var y = yOut - quietZone;
        if ((uint)x >= (uint)modules.Width || (uint)y >= (uint)modules.Height) return false;
        return modules[x, y];
    }
}
