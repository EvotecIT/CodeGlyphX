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
        var invert = opts.Invert;
        var halfBlockBackground = opts.UseHalfBlockBackground;
        var perModuleColor = useAnsi && (opts.AnsiDarkGradient is not null || (opts.AnsiDarkPalette?.Colors?.Length ?? 0) > 0);
        var ensureDarkContrast = opts.EnsureDarkContrast;
        var maxDarkLuminance = opts.MaxDarkLuminance;

        if (opts.UseUnicodeBlocks) {
            if (string.IsNullOrEmpty(opts.Dark) || string.Equals(opts.Dark, "#", StringComparison.Ordinal)) {
                dark = "█";
            }
            if (string.IsNullOrEmpty(opts.Light) || string.Equals(opts.Light, " ", StringComparison.Ordinal)) {
                light = " ";
            }
        }

        if (useAnsi && (string.IsNullOrEmpty(opts.Dark) || string.Equals(opts.Dark, "#", StringComparison.Ordinal))) {
            dark = "█";
        }

        if (ensureDarkContrast) {
            ansiDarkColor = ClampDarkColor(ansiDarkColor, maxDarkLuminance);
        }

        if (opts.UseHalfBlocks) {
            return RenderHalfBlocks(modules, quiet, moduleWidth, moduleHeight, newline, useAnsi, ansiTrueColor, ansiDarkColor, ansiLightColor, invert, halfBlockBackground, opts, perModuleColor);
        }

        var widthModules = modules.Width + quiet * 2;
        var heightModules = modules.Height + quiet * 2;
        if (widthModules <= 0 || heightModules <= 0) return string.Empty;

        var darkContent = Repeat(dark, moduleWidth);
        var lightContent = Repeat(light, moduleWidth);

        var darkCell = darkContent;
        var lightCell = lightContent;
        var lightCellAnsi = lightContent;
        if (useAnsi) {
            var darkPrefix = BuildAnsiColorPrefix(CompositeOverWhite(ansiDarkColor), ansiTrueColor);
            darkCell = darkPrefix + darkContent + AnsiReset;
            if (ansiColorizeLight) {
                var useBackground = IsWhitespaceOnly(lightContent);
                var lightPrefix = BuildAnsiColorPrefix(CompositeOverWhite(ansiLightColor), ansiTrueColor, useBackground);
                lightCell = lightPrefix + lightContent + AnsiReset;
            }
            lightCellAnsi = lightCell;
        }

        var lineCapacity = widthModules * darkCell.Length;
        var sb = new StringBuilder((lineCapacity + newline.Length) * heightModules * moduleHeight);
        var row = new StringBuilder(lineCapacity);

        for (var y = 0; y < heightModules; y++) {
            row.Clear();
            var my = y - quiet;
            for (var x = 0; x < widthModules; x++) {
                var mx = x - quiet;
                var inBounds = (uint)mx < (uint)modules.Width && (uint)my < (uint)modules.Height;
                var isDark = inBounds && modules[mx, my];
                if (invert) isDark = !isDark;
                if (perModuleColor && useAnsi && isDark && inBounds) {
                    var color = GetDarkColor(mx, my, modules.Width, modules.Height, opts, ansiDarkColor);
                    var prefix = BuildAnsiColorPrefix(CompositeOverWhite(color), ansiTrueColor);
                    row.Append(prefix).Append(darkContent).Append(AnsiReset);
                } else {
                    row.Append(isDark ? darkCell : lightCellAnsi);
                }
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

    private static string RenderHalfBlocks(
        BitMatrix modules,
        int quiet,
        int moduleWidth,
        int moduleHeight,
        string newline,
        bool useAnsi,
        bool ansiTrueColor,
        Rgba32 ansiDarkColor,
        Rgba32 ansiLightColor,
        bool invert,
        bool useBackground,
        MatrixAsciiRenderOptions opts,
        bool perModuleColor) {
        var widthModules = modules.Width + quiet * 2;
        var heightModules = modules.Height + quiet * 2;
        if (widthModules <= 0 || heightModules <= 0) return string.Empty;

        var cellDarkDark = string.Empty;
        var cellDarkLight = string.Empty;
        var cellLightDark = string.Empty;
        var cellLightLight = string.Empty;

        if (useAnsi && !perModuleColor) {
            var dark = CompositeOverWhite(ansiDarkColor);
            var light = CompositeOverWhite(ansiLightColor);
            var darkFg = BuildAnsiColorPrefix(dark, ansiTrueColor, background: false);
            var lightFg = BuildAnsiColorPrefix(light, ansiTrueColor, background: false);

            if (useBackground) {
                var darkBg = BuildAnsiColorPrefix(dark, ansiTrueColor, background: true);
                var lightBg = BuildAnsiColorPrefix(light, ansiTrueColor, background: true);
                var glyph = Repeat('▀', moduleWidth);

                cellDarkDark = darkFg + darkBg + glyph + AnsiReset;
                cellDarkLight = darkFg + lightBg + glyph + AnsiReset;
                cellLightDark = lightFg + darkBg + glyph + AnsiReset;
                cellLightLight = lightFg + lightBg + glyph + AnsiReset;
            } else {
                var glyphDarkDark = Repeat('█', moduleWidth);
                var glyphDarkLight = Repeat('▀', moduleWidth);
                var glyphLightDark = Repeat('▄', moduleWidth);
                var glyphLightLight = Repeat(' ', moduleWidth);

                cellDarkDark = darkFg + glyphDarkDark + AnsiReset;
                cellDarkLight = darkFg + glyphDarkLight + AnsiReset;
                cellLightDark = darkFg + glyphLightDark + AnsiReset;
                cellLightLight = glyphLightLight;
            }
        } else if (!useAnsi) {
            cellDarkDark = Repeat('█', moduleWidth);
            cellDarkLight = Repeat('▀', moduleWidth);
            cellLightDark = Repeat('▄', moduleWidth);
            cellLightLight = Repeat(' ', moduleWidth);
        }

        var glyphTop = string.Empty;
        var glyphFull = string.Empty;
        var glyphBottom = string.Empty;
        var glyphSpace = string.Empty;
        var lightComposite = default(Rgba32);
        if (useAnsi && perModuleColor) {
            glyphTop = Repeat('▀', moduleWidth);
            glyphFull = Repeat('█', moduleWidth);
            glyphBottom = Repeat('▄', moduleWidth);
            glyphSpace = Repeat(' ', moduleWidth);
            lightComposite = CompositeOverWhite(ansiLightColor);
        }

        var lineCapacity = widthModules * Math.Max(1, cellDarkDark.Length);
        var sb = new StringBuilder((lineCapacity + newline.Length) * ((heightModules + 1) / 2) * moduleHeight);
        var row = new StringBuilder(lineCapacity);

        for (var y = 0; y < heightModules; y += 2) {
            row.Clear();
            var myTop = y - quiet;
            var myBottom = y + 1 - quiet;
            for (var x = 0; x < widthModules; x++) {
                var mx = x - quiet;
                var topInBounds = (uint)mx < (uint)modules.Width && (uint)myTop < (uint)modules.Height;
                var bottomInBounds = (uint)mx < (uint)modules.Width && (uint)myBottom < (uint)modules.Height;
                var topDark = topInBounds && modules[mx, myTop];
                var bottomDark = bottomInBounds && modules[mx, myBottom];
                if (invert) {
                    topDark = !topDark;
                    bottomDark = !bottomDark;
                }

                if (!useAnsi || !perModuleColor) {
                    if (topDark) {
                        row.Append(bottomDark ? cellDarkDark : cellDarkLight);
                    } else {
                        row.Append(bottomDark ? cellLightDark : cellLightLight);
                    }
                } else if (useBackground) {
                    var topColor = topDark
                        ? CompositeOverWhite(topInBounds ? GetDarkColor(mx, myTop, modules.Width, modules.Height, opts, ansiDarkColor) : ansiDarkColor)
                        : lightComposite;
                    var bottomColor = bottomDark
                        ? CompositeOverWhite(bottomInBounds ? GetDarkColor(mx, myBottom, modules.Width, modules.Height, opts, ansiDarkColor) : ansiDarkColor)
                        : lightComposite;
                    var fg = BuildAnsiColorPrefix(topColor, ansiTrueColor, background: false);
                    var bg = BuildAnsiColorPrefix(bottomColor, ansiTrueColor, background: true);
                    row.Append(fg).Append(bg).Append(glyphTop).Append(AnsiReset);
                } else {
                    string glyph;
                    Rgba32? color = null;
                    if (topDark) {
                        if (bottomDark) {
                            var topColor = topInBounds ? GetDarkColor(mx, myTop, modules.Width, modules.Height, opts, ansiDarkColor) : ansiDarkColor;
                            var bottomColor = bottomInBounds ? GetDarkColor(mx, myBottom, modules.Width, modules.Height, opts, ansiDarkColor) : ansiDarkColor;
                            color = Lerp(topColor, bottomColor, 0.5);
                            glyph = glyphFull;
                        } else {
                            color = topInBounds ? GetDarkColor(mx, myTop, modules.Width, modules.Height, opts, ansiDarkColor) : ansiDarkColor;
                            glyph = glyphTop;
                        }
                    } else if (bottomDark) {
                        color = bottomInBounds ? GetDarkColor(mx, myBottom, modules.Width, modules.Height, opts, ansiDarkColor) : ansiDarkColor;
                        glyph = glyphBottom;
                    } else {
                        glyph = glyphSpace;
                    }

                    if (color.HasValue) {
                        var prefix = BuildAnsiColorPrefix(CompositeOverWhite(color.Value), ansiTrueColor, background: false);
                        row.Append(prefix).Append(glyph).Append(AnsiReset);
                    } else {
                        row.Append(glyph);
                    }
                }
            }

            var rowText = row.ToString();
            for (var rep = 0; rep < moduleHeight; rep++) {
                sb.Append(rowText);
                if (y + 2 < heightModules || rep != moduleHeight - 1) {
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

    private static string Repeat(char ch, int count) {
        if (count <= 1) return ch.ToString();
        return new string(ch, count);
    }

    private const string AnsiReset = "\u001b[0m";

    private static string BuildAnsiColorPrefix(Rgba32 color, bool trueColor, bool background = false) {
        var mode = background ? 48 : 38;
        if (trueColor) {
            return $"\u001b[{mode};2;{color.R};{color.G};{color.B}m";
        }
        var index = MapRgbToAnsi256(color.R, color.G, color.B);
        return $"\u001b[{mode};5;{index}m";
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

    private static bool IsWhitespaceOnly(string value) {
        for (var i = 0; i < value.Length; i++) {
            if (!char.IsWhiteSpace(value[i])) {
                return false;
            }
        }
        return value.Length > 0;
    }

    private static Rgba32 GetDarkColor(int mx, int my, int width, int height, MatrixAsciiRenderOptions opts, Rgba32 fallback) {
        var gradient = opts.AnsiDarkGradient;
        if (gradient is not null) {
            var gx = width <= 1 ? 0.0 : mx / (double)(width - 1);
            var gy = height <= 1 ? 0.0 : my / (double)(height - 1);
            var t = gradient.Type switch {
                AsciiGradientType.Vertical => gy,
                AsciiGradientType.Diagonal => (gx + gy) * 0.5,
                AsciiGradientType.Radial => ComputeRadialT(gx, gy, gradient.CenterX, gradient.CenterY),
                _ => gx
            };
            t = Clamp01(t);
            var color = Lerp(gradient.StartColor, gradient.EndColor, t);
            return opts.EnsureDarkContrast ? ClampDarkColor(color, opts.MaxDarkLuminance) : color;
        }

        var palette = opts.AnsiDarkPalette;
        if (palette is not null && palette.Colors.Length > 0) {
            var count = palette.Colors.Length;
            var index = palette.Mode switch {
                AsciiPaletteMode.CycleRows => my,
                AsciiPaletteMode.CycleDiagonal => mx + my,
                AsciiPaletteMode.Random => Hash(mx, my, palette.Seed),
                _ => mx
            };
            if (index < 0) index = -index;
            index %= count;
            var color = palette.Colors[index];
            return opts.EnsureDarkContrast ? ClampDarkColor(color, opts.MaxDarkLuminance) : color;
        }

        return opts.EnsureDarkContrast ? ClampDarkColor(fallback, opts.MaxDarkLuminance) : fallback;
    }

    private static double ComputeRadialT(double gx, double gy, double cx, double cy) {
        var dx = gx - cx;
        var dy = gy - cy;
        var dist = Math.Sqrt(dx * dx + dy * dy);
        var maxDist = 0.0;
        maxDist = Math.Max(maxDist, Math.Sqrt(cx * cx + cy * cy));
        maxDist = Math.Max(maxDist, Math.Sqrt((1 - cx) * (1 - cx) + cy * cy));
        maxDist = Math.Max(maxDist, Math.Sqrt(cx * cx + (1 - cy) * (1 - cy)));
        maxDist = Math.Max(maxDist, Math.Sqrt((1 - cx) * (1 - cx) + (1 - cy) * (1 - cy)));
        if (maxDist <= 0) return 0.0;
        return dist / maxDist;
    }

    private static double Clamp01(double value) {
        if (value <= 0) return 0;
        if (value >= 1) return 1;
        return value;
    }

    private static Rgba32 Lerp(Rgba32 a, Rgba32 b, double t) {
        t = Clamp01(t);
        var r = (byte)Math.Round(a.R + (b.R - a.R) * t);
        var g = (byte)Math.Round(a.G + (b.G - a.G) * t);
        var bch = (byte)Math.Round(a.B + (b.B - a.B) * t);
        var aCh = (byte)Math.Round(a.A + (b.A - a.A) * t);
        return new Rgba32(r, g, bch, aCh);
    }

    private static Rgba32 ClampDarkColor(Rgba32 color, double maxLuminance) {
        if (maxLuminance <= 0) return color;
        if (maxLuminance > 1) maxLuminance = 1;

        var luminance = (0.2126 * color.R + 0.7152 * color.G + 0.0722 * color.B) / 255.0;
        if (luminance <= maxLuminance) return color;

        var scale = maxLuminance / luminance;
        var r = (byte)Math.Round(color.R * scale);
        var g = (byte)Math.Round(color.G * scale);
        var b = (byte)Math.Round(color.B * scale);
        return new Rgba32(r, g, b, color.A);
    }

    private static int Hash(int x, int y, int seed) {
        unchecked {
            var h = seed;
            h = (h * 16777619) ^ x;
            h = (h * 16777619) ^ y;
            h ^= h >> 16;
            h *= 0x7feb352d;
            h ^= h >> 15;
            h *= unchecked((int)0x846ca68b);
            h ^= h >> 16;
            return h & int.MaxValue;
        }
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
