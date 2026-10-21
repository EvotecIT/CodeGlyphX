using System;
using System.Collections.Generic;
using System.Text;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Printing;

internal static partial class BarcodePrintRenderer {
    private static string Svg(BarcodePrintLayout layout, BarcodePrintPlacement? placement) {
        var t = PrintTransform.Resolve(layout, placement, out var width, out var height, out _);
        var b = new StringBuilder("<svg xmlns=\"http://www.w3.org/2000/svg\" xmlns:xlink=\"http://www.w3.org/1999/xlink\" width=\"");
        b.Append(N(width)).Append("mm\" height=\"").Append(N(height)).Append("mm\" viewBox=\"0 0 ")
            .Append(N(width)).Append(' ').Append(N(height)).Append("\"><defs><clipPath id=\"canvas\"><rect width=\"")
            .Append(N(width)).Append("\" height=\"").Append(N(height)).Append("\"/></clipPath>");
        var paints = new Dictionary<BarcodePaint, int>();
        AddPaint(b, paints, layout.Background);
        foreach (var path in layout.Paths) AddPaint(b, paints, path.Paint);
        b.Append("</defs><g clip-path=\"url(#canvas)\"><rect width=\"").Append(N(width)).Append("\" height=\"").Append(N(height)).Append('"');
        Fill(b, paints, layout.Background);
        b.Append("/><g transform=\"matrix(").Append(N(t.A)).Append(' ').Append(N(t.B)).Append(' ').Append(N(t.C)).Append(' ')
            .Append(N(t.D)).Append(' ').Append(N(t.X)).Append(' ').Append(N(t.Y)).Append(")\">");
        foreach (var path in layout.Paths) {
            b.Append("<path fill-rule=\"nonzero\" d=\"");
            foreach (var contour in path.Contours) {
                b.Append('M').Append(N(contour[0].X)).Append(' ').Append(N(contour[0].Y));
                for (var i = 1; i < contour.Count; i++) b.Append('L').Append(N(contour[i].X)).Append(' ').Append(N(contour[i].Y));
                b.Append('Z');
            }
            b.Append('"'); Fill(b, paints, path.Paint); b.Append("/>");
        }
        return b.Append("</g></g></svg>").ToString();
    }

    private static void AddPaint(StringBuilder b, Dictionary<BarcodePaint, int> paints, BarcodePaint paint) {
        if (paint.Kind == BarcodePaint.PaintKind.Solid || paints.ContainsKey(paint)) return;
        var id = paints.Count;
        paints.Add(paint, id);
        if (paint.Kind == BarcodePaint.PaintKind.Gradient) {
            b.Append("<linearGradient id=\"paint").Append(id).Append("\" gradientUnits=\"userSpaceOnUse\" x1=\"")
                .Append(N(paint.Start.X)).Append("\" y1=\"").Append(N(paint.Start.Y)).Append("\" x2=\"").Append(N(paint.End.X))
                .Append("\" y2=\"").Append(N(paint.End.Y)).Append("\">");
            Stop(b, "0", paint.StartColor); Stop(b, "1", paint.EndColor);
            b.Append("</linearGradient>");
        } else {
            var png = PngImageEncoder.EncodeRgba32(paint.Pixels!, paint.PixelWidth, paint.PixelHeight);
            b.Append("<pattern id=\"paint").Append(id).Append("\" patternUnits=\"userSpaceOnUse\" width=\"")
                .Append(N(paint.TileWidth)).Append("\" height=\"").Append(N(paint.TileHeight)).Append("\"><image width=\"")
                .Append(N(paint.TileWidth)).Append("\" height=\"").Append(N(paint.TileHeight))
                .Append("\" preserveAspectRatio=\"none\" style=\"image-rendering:pixelated\" xlink:href=\"data:image/png;base64,")
                .Append(Convert.ToBase64String(png)).Append("\"/></pattern>");
        }
    }
    private static void Stop(StringBuilder b, string offset, Rgba32 color) => b.Append("<stop offset=\"").Append(offset)
        .Append("\" stop-color=\"").Append(Hex(color)).Append("\" stop-opacity=\"").Append(N(color.A / 255D)).Append("\"/>");
    private static void Fill(StringBuilder b, Dictionary<BarcodePaint, int> paints, BarcodePaint paint) {
        if (paint.Kind != BarcodePaint.PaintKind.Solid) b.Append(" fill=\"url(#paint").Append(paints[paint]).Append(")\"");
        else b.Append(" fill=\"").Append(Hex(paint.StartColor)).Append("\" fill-opacity=\"").Append(N(paint.StartColor.A / 255D)).Append('"');
    }
    private static string Hex(Rgba32 c) => "#" + c.R.ToString("x2") + c.G.ToString("x2") + c.B.ToString("x2");
}
