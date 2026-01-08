using System;
using System.Text;

namespace CodeGlyphX.Rendering.Svg;

/// <summary>
/// Renders generic 2D matrices to SVG.
/// </summary>
public static class MatrixSvgRenderer {
    /// <summary>
    /// Renders the matrix to an SVG string.
    /// </summary>
    public static string Render(BitMatrix modules, MatrixSvgRenderOptions opts) {
        if (modules is null) throw new ArgumentNullException(nameof(modules));
        if (opts is null) throw new ArgumentNullException(nameof(opts));
        if (opts.ModuleSize <= 0) throw new ArgumentOutOfRangeException(nameof(opts.ModuleSize));
        if (opts.QuietZone < 0) throw new ArgumentOutOfRangeException(nameof(opts.QuietZone));

        var outWidth = modules.Width + opts.QuietZone * 2;
        var outHeight = modules.Height + opts.QuietZone * 2;
        var pxWidth = outWidth * opts.ModuleSize;
        var pxHeight = outHeight * opts.ModuleSize;

        var sb = new StringBuilder(outWidth * outHeight * 2);
        sb.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"").Append(pxWidth)
            .Append("\" height=\"").Append(pxHeight)
            .Append("\" viewBox=\"0 0 ").Append(outWidth).Append(' ').Append(outHeight)
            .Append("\" shape-rendering=\"crispEdges\">");

        sb.Append("<rect width=\"100%\" height=\"100%\" fill=\"").Append(opts.LightColor).Append("\"/>");

        sb.Append("<path fill=\"").Append(opts.DarkColor).Append("\" d=\"");
        for (var y = 0; y < modules.Height; y++) {
            var outY = y + opts.QuietZone;
            var runStart = -1;
            for (var x = 0; x < modules.Width; x++) {
                var dark = modules[x, y];
                if (dark && runStart < 0) runStart = x;
                if ((!dark || x == modules.Width - 1) && runStart >= 0) {
                    var runEnd = dark && x == modules.Width - 1 ? x + 1 : x;
                    sb.Append('M').Append(runStart + opts.QuietZone).Append(' ').Append(outY)
                        .Append('h').Append(runEnd - runStart).Append("v1h-")
                        .Append(runEnd - runStart).Append('z');
                    runStart = -1;
                }
            }
        }
        sb.Append("\"/>");

        sb.Append("</svg>");
        return sb.ToString();
    }
}
