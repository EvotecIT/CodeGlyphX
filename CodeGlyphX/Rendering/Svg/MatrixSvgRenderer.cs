using System;
using System.IO;
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

        var darkColor = RenderSanitizer.SafeCssColor(opts.DarkColor, RenderDefaults.QrForegroundCss);
        var lightColor = RenderSanitizer.SafeCssColor(opts.LightColor, RenderDefaults.QrBackgroundCss);

        var outWidth = modules.Width + opts.QuietZone * 2;
        var outHeight = modules.Height + opts.QuietZone * 2;
        var pxWidth = outWidth * opts.ModuleSize;
        var pxHeight = outHeight * opts.ModuleSize;

        var sb = new StringBuilder(outWidth * outHeight * 2);
        sb.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"").Append(pxWidth)
            .Append("\" height=\"").Append(pxHeight)
            .Append("\" viewBox=\"0 0 ").Append(outWidth).Append(' ').Append(outHeight)
            .Append("\" shape-rendering=\"crispEdges\">");

        sb.Append("<rect width=\"100%\" height=\"100%\" fill=\"").Append(lightColor).Append("\"/>");

        sb.Append("<path fill=\"").Append(darkColor).Append("\" d=\"");
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

    /// <summary>
    /// Renders the matrix to an SVG stream.
    /// </summary>
    /// <param name="modules">Matrix modules.</param>
    /// <param name="opts">Rendering options.</param>
    /// <param name="stream">Target stream.</param>
    public static void RenderToStream(BitMatrix modules, MatrixSvgRenderOptions opts, Stream stream) {
        var svg = Render(modules, opts);
        RenderIO.WriteText(stream, svg);
    }

    /// <summary>
    /// Renders the matrix to an SVG file.
    /// </summary>
    /// <param name="modules">Matrix modules.</param>
    /// <param name="opts">Rendering options.</param>
    /// <param name="path">Output file path.</param>
    /// <returns>The output file path.</returns>
    public static string RenderToFile(BitMatrix modules, MatrixSvgRenderOptions opts, string path) {
        var svg = Render(modules, opts);
        return RenderIO.WriteText(path, svg);
    }

    /// <summary>
    /// Renders the matrix to an SVG file under the specified directory.
    /// </summary>
    /// <param name="modules">Matrix modules.</param>
    /// <param name="opts">Rendering options.</param>
    /// <param name="directory">Output directory.</param>
    /// <param name="fileName">Output file name.</param>
    /// <returns>The output file path.</returns>
    public static string RenderToFile(BitMatrix modules, MatrixSvgRenderOptions opts, string directory, string fileName) {
        var svg = Render(modules, opts);
        return RenderIO.WriteText(directory, fileName, svg);
    }
}
