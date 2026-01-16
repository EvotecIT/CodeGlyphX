using System;
using System.Text;

namespace CodeGlyphX.Rendering.Ascii;

/// <summary>
/// Renders 2D matrices as ASCII text.
/// </summary>
public static class MatrixAsciiRenderer {
    /// <summary>
    /// Renders the matrix to an ASCII string.
    /// </summary>
    public static string Render(BitMatrix modules, MatrixAsciiRenderOptions? options = null) {
        if (modules is null) throw new ArgumentNullException(nameof(modules));
        var opts = options ?? new MatrixAsciiRenderOptions();

        var quiet = Math.Max(0, opts.QuietZone);
        var moduleWidth = Math.Max(1, opts.ModuleWidth);
        var moduleHeight = Math.Max(1, opts.ModuleHeight);
        var dark = string.IsNullOrEmpty(opts.Dark) ? "#" : opts.Dark;
        var light = string.IsNullOrEmpty(opts.Light) ? " " : opts.Light;
        var newline = opts.NewLine ?? Environment.NewLine;

        var widthModules = modules.Width + quiet * 2;
        var heightModules = modules.Height + quiet * 2;
        if (widthModules <= 0 || heightModules <= 0) return string.Empty;

        var darkCell = Repeat(dark, moduleWidth);
        var lightCell = Repeat(light, moduleWidth);

        var lineCapacity = widthModules * darkCell.Length;
        var sb = new StringBuilder((lineCapacity + newline.Length) * heightModules * moduleHeight);
        var row = new StringBuilder(lineCapacity);

        for (var y = 0; y < heightModules; y++) {
            row.Clear();
            var my = y - quiet;
            for (var x = 0; x < widthModules; x++) {
                var mx = x - quiet;
                var isDark = (uint)mx < (uint)modules.Width && (uint)my < (uint)modules.Height && modules[mx, my];
                row.Append(isDark ? darkCell : lightCell);
            }

            var rowText = row.ToString();
            for (var rep = 0; rep < moduleHeight; rep++) {
                sb.Append(rowText);
                if (y != heightModules - 1 || rep != moduleHeight - 1) {
                    sb.Append(newline);
                }
            }
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
