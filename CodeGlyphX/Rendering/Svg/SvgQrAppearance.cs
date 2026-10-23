using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering.Svg;

/// <summary>Exports canonical styled coverage as coloured vector contours, without an embedded bitmap.</summary>
internal static class SvgQrAppearance {
    internal static QrPngRenderOptions Resolve(QrSvgRenderOptions options) {
        var appearance = new QrPngRenderOptions {
            ModuleSize = options.ModuleSize, QuietZone = options.QuietZone,
            Foreground = ParseColor(options.DarkColor, Rgba32.Black), Background = ParseColor(options.LightColor, Rgba32.White),
            ModuleShape = options.ModuleShape, ModuleScale = options.ModuleScale,
            ModuleCornerRadiusPx = options.ModuleCornerRadiusPx, ProtectFunctionalPatterns = options.ProtectFunctionalPatterns,
            ForegroundGradient = options.ForegroundGradient, Eyes = options.Eyes
        };
        if (options.Logo is not null) {
            options.Logo.Validate();
            var logo = QrRasterLogoOptions.FromPng(options.Logo.Png);
            logo.Scale = options.Logo.Scale; logo.PaddingPx = options.Logo.PaddingPx;
            logo.DrawBackground = options.Logo.DrawBackground; logo.Background = options.Logo.Background;
            logo.CornerRadiusPx = options.Logo.CornerRadiusPx;
            appearance.Logo = logo;
        }
        return appearance;
    }

    private static Rgba32 ParseColor(string css, Rgba32 fallback) {
        var value = RenderSanitizer.SafeCssColor(css, ColorUtils.ToCss(fallback));
        if (value[0] == '#') {
            var text = value.Substring(1);
            if (text.Length <= 4) {
                var expanded = new StringBuilder();
                foreach (var c in text) expanded.Append(c).Append(c);
                text = expanded.ToString();
            }
            if (text.Length == 6) text += "FF";
            var rgba = uint.Parse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            return new Rgba32((byte)(rgba >> 24), (byte)(rgba >> 16), (byte)(rgba >> 8), (byte)rgba);
        }
        if (value.StartsWith("rgb", StringComparison.OrdinalIgnoreCase)) {
            var body = value.Substring(value.IndexOf('(') + 1).TrimEnd(')');
            var parts = body.IndexOf(',') >= 0 ? body.Split(',')
                : body.Replace('/', ' ').Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length is not (3 or 4)) return fallback;
            var channels = new byte[] { 0, 0, 0, 255 };
            for (var i = 0; i < parts.Length; i++) {
                var part = parts[i].Trim();
                var percent = part.EndsWith("%", StringComparison.Ordinal);
                if (!double.TryParse(percent ? part.Substring(0, part.Length - 1) : part, NumberStyles.Float, CultureInfo.InvariantCulture, out var number)) return fallback;
                number *= percent ? 2.55 : i == 3 ? 255 : 1;
                channels[i] = (byte)Math.Max(0, Math.Min(255, Math.Round(number)));
            }
            return new Rgba32(channels[0], channels[1], channels[2], channels[3]);
        }
        if (string.Equals(value, "rebeccapurple", StringComparison.OrdinalIgnoreCase)) return new Rgba32(102, 51, 153);
        if (string.Equals(value, "none", StringComparison.OrdinalIgnoreCase)) return Rgba32.Transparent;
        var named = System.Drawing.Color.FromName(value.ToLowerInvariant().Replace("grey", "gray"));
        return named.A != 0 || string.Equals(value, "transparent", StringComparison.OrdinalIgnoreCase)
            ? new Rgba32(named.R, named.G, named.B, named.A) : fallback;
    }

    internal static string Render(BitMatrix modules, QrPngRenderOptions appearance) {
        var pixels = QrPngRenderer.RenderPixels(modules, appearance, out var width, out var height, out var stride);
        var paths = new Dictionary<uint, StringBuilder>();
        var active = new Dictionary<(uint Color, int Left, int Right), (int Top, int Height)>();
        var contours = 0;
        var textCost = 200L;
        var maxCharacters = RenderGuards.MaxOutputBytes > 0
            ? Math.Min(16_000_000, RenderGuards.MaxOutputBytes / 8) : 16_000_000;
        if (textCost > maxCharacters) throw new ArgumentException("SVG contour output exceeds size limits.");
        // Merge identical colour runs on adjacent rows. Pixel colours and alpha remain exact.
        for (var y = 0; y <= height; y++) {
            var next = new Dictionary<(uint Color, int Left, int Right), (int Top, int Height)>();
            if (y < height) for (var x = 0; x < width;) {
                var color = Read(pixels, y * stride + x * 4);
                var start = x++;
                while (x < width && Read(pixels, y * stride + x * 4) == color) x++;
                if ((color & 255) == 0) continue;
                var key = (color, start, x);
                if (active.TryGetValue(key, out var rectangle)) {
                    active.Remove(key);
                    next.Add(key, (rectangle.Top, rectangle.Height + 1));
                } else next.Add(key, (y, 1));
            }
            foreach (var rectangle in active) {
                if (++contours > 500_000) throw new ArgumentException("SVG contour output exceeds size limits.");
                if (!paths.TryGetValue(rectangle.Key.Color, out var path)) {
                    if (paths.Count >= 65_536) throw new ArgumentException("SVG paint count exceeds output limits.");
                    paths[rectangle.Key.Color] = path = new StringBuilder();
                    textCost += 96;
                }
                var before = path.Length;
                var runWidth = rectangle.Key.Right - rectangle.Key.Left;
                path.Append('M').Append(rectangle.Key.Left).Append(' ').Append(rectangle.Value.Top)
                    .Append('h').Append(runWidth).Append('v').Append(rectangle.Value.Height).Append("h-").Append(runWidth).Append('z');
                textCost += path.Length - before;
                if (textCost > maxCharacters) throw new ArgumentException("SVG contour output exceeds size limits.");
            }
            active = next;
        }
        var output = new StringBuilder((int)textCost);
        output.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"").Append(width).Append("\" height=\"").Append(height)
            .Append("\" viewBox=\"0 0 ").Append(width).Append(' ').Append(height).Append("\" shape-rendering=\"crispEdges\">");
        foreach (var entry in paths) {
            output.Append("<path fill=\"#").Append((entry.Key >> 8).ToString("X6", CultureInfo.InvariantCulture)).Append('"');
            if ((entry.Key & 255) != 255) output.Append(" fill-opacity=\"").Append(((entry.Key & 255) / 255.0).ToString("0.########", CultureInfo.InvariantCulture)).Append('"');
            output.Append(" d=\"").Append(entry.Value).Append("\"/>");
        }
        return output.Append("</svg>").ToString();
    }

    private static uint Read(byte[] pixels, int p) => (uint)pixels[p] << 24 | (uint)pixels[p + 1] << 16 | (uint)pixels[p + 2] << 8 | pixels[p + 3];
}
