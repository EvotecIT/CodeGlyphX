using System;
using System.Text;

namespace CodeGlyphX.Rendering.Ascii;

/// <summary>
/// Renders 1D barcodes as ASCII text.
/// </summary>
public static class BarcodeAsciiRenderer {
    /// <summary>
    /// Renders the barcode to an ASCII string.
    /// </summary>
    public static string Render(Barcode1D barcode, BarcodeAsciiRenderOptions? options = null) {
        if (barcode is null) throw new ArgumentNullException(nameof(barcode));
        var opts = options ?? new BarcodeAsciiRenderOptions();

        var moduleWidth = Math.Max(1, opts.ModuleWidth);
        var quiet = Math.Max(0, opts.QuietZone);
        var height = Math.Max(1, opts.Height);
        var dark = string.IsNullOrEmpty(opts.Dark) ? "#" : opts.Dark;
        var light = string.IsNullOrEmpty(opts.Light) ? " " : opts.Light;
        var newline = opts.NewLine ?? Environment.NewLine;

        var darkCell = Repeat(dark, moduleWidth);
        var lightCell = Repeat(light, moduleWidth);

        var lineCapacity = (barcode.TotalModules + quiet * 2) * darkCell.Length;
        var row = new StringBuilder(lineCapacity);

        for (var i = 0; i < quiet; i++) row.Append(lightCell);
        for (var i = 0; i < barcode.Segments.Count; i++) {
            var segment = barcode.Segments[i];
            var cell = segment.IsBar ? darkCell : lightCell;
            for (var j = 0; j < segment.Modules; j++) row.Append(cell);
        }
        for (var i = 0; i < quiet; i++) row.Append(lightCell);

        var line = row.ToString();
        var sb = new StringBuilder((line.Length + newline.Length) * height);
        for (var i = 0; i < height; i++) {
            sb.Append(line);
            if (i < height - 1) sb.Append(newline);
        }
        return sb.ToString();
    }

    private static string Repeat(string text, int count) {
        if (count <= 1) return text;
        var sb = new StringBuilder(text.Length * count);
        for (var i = 0; i < count; i++) sb.Append(text);
        return sb.ToString();
    }
}
