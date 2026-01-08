using System;
using System.Text;

namespace CodeGlyphX.Rendering.Html;

/// <summary>
/// Renders 1D barcodes to HTML (table-based).
/// </summary>
public static class HtmlBarcodeRenderer {
    /// <summary>
    /// Renders the barcode to an HTML table.
    /// </summary>
    public static string Render(Barcode1D barcode, BarcodeHtmlRenderOptions opts) {
        if (barcode is null) throw new ArgumentNullException(nameof(barcode));
        if (opts is null) throw new ArgumentNullException(nameof(opts));
        if (opts.ModuleSize <= 0) throw new ArgumentOutOfRangeException(nameof(opts.ModuleSize));
        if (opts.QuietZone < 0) throw new ArgumentOutOfRangeException(nameof(opts.QuietZone));
        if (opts.HeightModules <= 0) throw new ArgumentOutOfRangeException(nameof(opts.HeightModules));

        var outModules = barcode.TotalModules + opts.QuietZone * 2;
        var heightPx = opts.HeightModules * opts.ModuleSize;

        var sb = new StringBuilder(outModules * 16);
        sb.Append("<table cellpadding=\"0\" cellspacing=\"0\" style=\"border-collapse:collapse;\">");
        sb.Append("<tr>");

        AppendCell(sb, opts.EmailSafeTable, opts.QuietZone * opts.ModuleSize, heightPx, opts.BackgroundColor);

        for (var i = 0; i < barcode.Segments.Count; i++) {
            var seg = barcode.Segments[i];
            var color = seg.IsBar ? opts.BarColor : opts.BackgroundColor;
            AppendCell(sb, opts.EmailSafeTable, seg.Modules * opts.ModuleSize, heightPx, color);
        }

        AppendCell(sb, opts.EmailSafeTable, opts.QuietZone * opts.ModuleSize, heightPx, opts.BackgroundColor);

        sb.Append("</tr></table>");
        return sb.ToString();
    }

    private static void AppendCell(StringBuilder sb, bool emailSafe, int widthPx, int heightPx, string color) {
        if (emailSafe) {
            sb.Append("<td width=\"").Append(widthPx).Append("\" height=\"").Append(heightPx)
                .Append("\" bgcolor=\"").Append(color).Append("\" style=\"line-height:0;font-size:0;\">&nbsp;</td>");
        } else {
            sb.Append("<td style=\"width:").Append(widthPx).Append("px;height:").Append(heightPx)
                .Append("px;background:").Append(color).Append(";\"></td>");
        }
    }
}
