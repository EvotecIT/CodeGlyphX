using System;
using System.Text;
using CodeMatrix.Rendering;

namespace CodeMatrix.Rendering.Svg;

/// <summary>
/// Renders 1D barcodes to SVG.
/// </summary>
public static class SvgBarcodeRenderer {
    /// <summary>
    /// Renders the barcode to an SVG string.
    /// </summary>
    public static string Render(Barcode1D barcode, BarcodeSvgRenderOptions opts) {
        if (barcode is null) throw new ArgumentNullException(nameof(barcode));
        if (opts is null) throw new ArgumentNullException(nameof(opts));
        if (opts.ModuleSize <= 0) throw new ArgumentOutOfRangeException(nameof(opts.ModuleSize));
        if (opts.QuietZone < 0) throw new ArgumentOutOfRangeException(nameof(opts.QuietZone));
        if (opts.HeightModules <= 0) throw new ArgumentOutOfRangeException(nameof(opts.HeightModules));

        var outWidthModules = barcode.TotalModules + opts.QuietZone * 2;
        var widthPx = outWidthModules * opts.ModuleSize;
        var heightPx = opts.HeightModules * opts.ModuleSize;

        var sb = new StringBuilder(outWidthModules * 8);
        sb.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"").Append(widthPx).Append("\" height=\"")
            .Append(heightPx).Append("\" viewBox=\"0 0 ").Append(outWidthModules).Append(' ')
            .Append(opts.HeightModules).Append("\" shape-rendering=\"crispEdges\">");

        sb.Append("<rect width=\"100%\" height=\"100%\" fill=\"").Append(opts.BackgroundColor).Append("\"/>");

        sb.Append("<g fill=\"").Append(opts.BarColor).Append("\">");
        var x = opts.QuietZone;
        for (var i = 0; i < barcode.Segments.Count; i++) {
            var seg = barcode.Segments[i];
            if (seg.IsBar) {
                sb.Append("<rect x=\"").Append(x).Append("\" y=\"0\" width=\"").Append(seg.Modules)
                    .Append("\" height=\"").Append(opts.HeightModules).Append("\"/>");
            }
            x += seg.Modules;
        }
        sb.Append("</g>");

        sb.Append("</svg>");
        return sb.ToString();
    }

    /// <summary>
    /// Renders the barcode to an SVG file.
    /// </summary>
    /// <param name="barcode">Barcode to render.</param>
    /// <param name="opts">Rendering options.</param>
    /// <param name="path">Output file path.</param>
    /// <returns>The output file path.</returns>
    public static string RenderToFile(Barcode1D barcode, BarcodeSvgRenderOptions opts, string path) {
        var svg = Render(barcode, opts);
        return RenderIO.WriteText(path, svg);
    }

    /// <summary>
    /// Renders the barcode to an SVG file under the specified directory.
    /// </summary>
    /// <param name="barcode">Barcode to render.</param>
    /// <param name="opts">Rendering options.</param>
    /// <param name="directory">Output directory.</param>
    /// <param name="fileName">Output file name.</param>
    /// <returns>The output file path.</returns>
    public static string RenderToFile(Barcode1D barcode, BarcodeSvgRenderOptions opts, string directory, string fileName) {
        var svg = Render(barcode, opts);
        return RenderIO.WriteText(directory, fileName, svg);
    }
}
