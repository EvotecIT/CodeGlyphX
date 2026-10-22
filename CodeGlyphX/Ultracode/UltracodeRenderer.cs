using System;
using System.Globalization;
using System.Text;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Ultracode;

internal static class UltracodeRenderer {
    internal static byte[] RenderPng(UltracodeGrid cells, UltracodeRenderOptions? options) {
        var pixels = RenderPixels(cells, options, out var width, out var height);
        return PngImageEncoder.EncodeRgba32(pixels, width, height);
    }

    internal static byte[] RenderPixels(UltracodeGrid cells, UltracodeRenderOptions? options, out int width, out int height) {
        options ??= new UltracodeRenderOptions();
        Dimensions(cells, options, out width, out height);
        var pixels = new byte[checked(width * height * 4)];
        for (var y = 0; y < height; y++) for (var x = 0; x < width; x++) {
            var cx = x / options.CellSize - options.QuietZone;
            var cy = y / options.CellSize - options.QuietZone;
            var color = (uint)cx < (uint)cells.Width && (uint)cy < (uint)cells.Height
                ? Color(cells[cx, cy], options) : options.Background;
            var at = (y * width + x) * 4;
            pixels[at] = color.R; pixels[at + 1] = color.G; pixels[at + 2] = color.B; pixels[at + 3] = color.A;
        }
        return pixels;
    }

    internal static string RenderSvg(UltracodeGrid cells, UltracodeRenderOptions? options) {
        options ??= new UltracodeRenderOptions();
        Dimensions(cells, options, out var width, out var height);
        var builder = new StringBuilder();
        builder.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"").Append(width)
            .Append("\" height=\"").Append(height).Append("\" viewBox=\"0 0 ").Append(width).Append(' ').Append(height)
            .Append("\" shape-rendering=\"crispEdges\">");
        // Paint disjoint regions so translucent cells retain the same RGBA as the raster export.
        var quiet = options.QuietZone * options.CellSize;
        Rect(builder, 0, 0, width, quiet, options.Background);
        Rect(builder, 0, height - quiet, width, quiet, options.Background);
        Rect(builder, 0, quiet, quiet, height - quiet * 2, options.Background);
        Rect(builder, width - quiet, quiet, quiet, height - quiet * 2, options.Background);
        for (var y = 0; y < cells.Height; y++) for (var x = 0; x < cells.Width; x++)
            Rect(builder, (x + options.QuietZone) * options.CellSize, (y + options.QuietZone) * options.CellSize,
                options.CellSize, options.CellSize, Color(cells[x, y], options));
        return builder.Append("</svg>").ToString();
    }

    private static void Dimensions(UltracodeGrid cells, UltracodeRenderOptions options, out int width, out int height) {
        if (options.CellSize < 1 || options.CellSize > 1024) throw new ArgumentOutOfRangeException(nameof(options.CellSize));
        if (options.QuietZone < 1 || options.QuietZone > 256) throw new ArgumentOutOfRangeException(nameof(options.QuietZone));
        width = checked((cells.Width + options.QuietZone * 2) * options.CellSize);
        height = checked((cells.Height + options.QuietZone * 2) * options.CellSize);
        if ((long)width * height > 16_000_000) throw new ArgumentException("Ultracode rendering exceeds the 16 million pixel limit.", nameof(options));
    }

    private static Rgba32 Color(UltracodeColor color, UltracodeRenderOptions options) => color switch {
        UltracodeColor.White => options.Background, UltracodeColor.Black => options.Foreground,
        UltracodeColor.Cyan => new Rgba32(0, 255, 255), UltracodeColor.Magenta => new Rgba32(255, 0, 255),
        UltracodeColor.Yellow => new Rgba32(255, 255, 0), UltracodeColor.Green => new Rgba32(0, 255, 0),
        _ => throw new ArgumentException("An encoded symbol cannot render unknown colour cells.")
    };

    private static void Rect(StringBuilder builder, int x, int y, int width, int height, Rgba32 color) {
        builder.Append("<rect x=\"").Append(x).Append("\" y=\"").Append(y).Append("\" width=\"").Append(width)
            .Append("\" height=\"").Append(height).Append("\" fill=\"#")
            .Append(color.R.ToString("x2", CultureInfo.InvariantCulture)).Append(color.G.ToString("x2", CultureInfo.InvariantCulture))
            .Append(color.B.ToString("x2", CultureInfo.InvariantCulture)).Append('"');
        if (color.A < 255) builder.Append(" fill-opacity=\"").Append((color.A / 255D).ToString("0.######", CultureInfo.InvariantCulture)).Append('"');
        builder.Append("/>");
    }
}
