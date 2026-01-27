using System;
using System.Text;
using CodeGlyphX.Rendering.Png;

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
        var scale = Math.Max(1, opts.Scale);
        var moduleWidth = Math.Max(1, opts.ModuleWidth) * scale;
        var moduleHeight = Math.Max(1, opts.ModuleHeight) * scale;
        var dark = string.IsNullOrEmpty(opts.Dark) ? "#" : opts.Dark;
        var light = string.IsNullOrEmpty(opts.Light) ? " " : opts.Light;
        var newline = NormalizeNewLine(opts.NewLine) ?? Environment.NewLine;
        var useAnsi = opts.UseAnsiColors;
        var ansiTrueColor = opts.UseAnsiTrueColor;
        var ansiDarkColor = opts.AnsiDarkColor;
        var ansiLightColor = opts.AnsiLightColor;
        var ansiColorizeLight = opts.AnsiColorizeLight;

        if (opts.UseUnicodeBlocks) {
            if (string.IsNullOrEmpty(opts.Dark) || string.Equals(opts.Dark, "#", StringComparison.Ordinal)) {
                dark = "█";
            }
            if (string.IsNullOrEmpty(opts.Light) || string.Equals(opts.Light, " ", StringComparison.Ordinal)) {
                light = " ";
            }
        }

        if (opts.Invert) {
            (dark, light) = (light, dark);
            (ansiDarkColor, ansiLightColor) = (ansiLightColor, ansiDarkColor);
        }

        if (useAnsi && (string.IsNullOrEmpty(opts.Dark) || string.Equals(opts.Dark, "#", StringComparison.Ordinal))) {
            dark = "█";
        }

        var widthModules = modules.Width + quiet * 2;
        var heightModules = modules.Height + quiet * 2;
        if (widthModules <= 0 || heightModules <= 0) return string.Empty;

        var darkContent = Repeat(dark, moduleWidth);
        var lightContent = Repeat(light, moduleWidth);

        var darkCell = darkContent;
        var lightCell = lightContent;
        if (useAnsi) {
            var darkPrefix = BuildAnsiColorPrefix(CompositeOverWhite(ansiDarkColor), ansiTrueColor);
            darkCell = darkPrefix + darkContent + AnsiReset;
            if (ansiColorizeLight) {
                var lightPrefix = BuildAnsiColorPrefix(CompositeOverWhite(ansiLightColor), ansiTrueColor);
                lightCell = lightPrefix + lightContent + AnsiReset;
            }
        }

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

    private const string AnsiReset = "\u001b[0m";

    private static string BuildAnsiColorPrefix(Rgba32 color, bool trueColor) {
        if (trueColor) {
            return $"\u001b[38;2;{color.R};{color.G};{color.B}m";
        }
        var index = MapRgbToAnsi256(color.R, color.G, color.B);
        return $"\u001b[38;5;{index}m";
    }

    private static Rgba32 CompositeOverWhite(Rgba32 color) {
        if (color.A == 255) return color;
        var a = color.A;
        var invA = 255 - a;
        var r = (byte)((color.R * a + 255 * invA + 127) / 255);
        var g = (byte)((color.G * a + 255 * invA + 127) / 255);
        var b = (byte)((color.B * a + 255 * invA + 127) / 255);
        return new Rgba32(r, g, b, 255);
    }

    private static int MapRgbToAnsi256(byte r, byte g, byte b) {
        if (r == g && g == b) {
            if (r < 8) return 16;
            if (r > 248) return 231;
            var gray = (int)Math.Round((r - 8) / 247.0 * 24.0);
            if (gray < 0) gray = 0;
            if (gray > 23) gray = 23;
            return 232 + gray;
        }

        var ri = (int)Math.Round(r / 255.0 * 5.0);
        var gi = (int)Math.Round(g / 255.0 * 5.0);
        var bi = (int)Math.Round(b / 255.0 * 5.0);
        if (ri < 0) ri = 0;
        if (ri > 5) ri = 5;
        if (gi < 0) gi = 0;
        if (gi > 5) gi = 5;
        if (bi < 0) bi = 0;
        if (bi > 5) bi = 5;
        return 16 + 36 * ri + 6 * gi + bi;
    }

    private static string? NormalizeNewLine(string? value) {
        if (value is null) return null;
        if (value.IndexOf('\\') < 0) return value;
        return value
            .Replace("\\r\\n", "\r\n")
            .Replace("\\n", "\n")
            .Replace("\\r", "\r");
    }
}
