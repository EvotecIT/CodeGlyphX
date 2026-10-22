using System;
using System.Globalization;
using System.Text;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Pdf;
using CodeGlyphX.Rendering.Png;
using CodeGlyphX.Rendering.Svgz;

namespace CodeGlyphX.Printing;

internal static partial class BarcodePrintRenderer {
    internal static RenderedOutput Render(BarcodePrintLayout layout, OutputFormat format, BarcodePrintPlacement? placement) {
        if (format == OutputFormat.Svg) return RenderedOutput.FromText(format, Svg(layout, placement));
        if (format == OutputFormat.Svgz) return RenderedOutput.FromOwnedBinary(format, SvgzWriter.WriteSvg(Svg(layout, placement)));
        if (format == OutputFormat.Eps) return RenderedOutput.FromText(format, Eps(layout, placement));
        if (format == OutputFormat.Pdf && HasOnlyOpaqueSolids(layout)) return RenderedOutput.FromOwnedBinary(format, Pdf(layout, placement));
        if (format is not (OutputFormat.Png or OutputFormat.Pdf or OutputFormat.Bmp or OutputFormat.Webp or OutputFormat.Pam or OutputFormat.Tga))
            throw new NotSupportedException("Physical print exports support PNG, SVG/SVGZ, PDF, EPS and lossless BMP, WebP, PAM and TGA raster fallbacks. Use a supported format to preserve the measured scene.");
        var pixels = BarcodePrintRasterizer.Render(layout, placement, out var width, out var height);
        byte[] bytes;
        switch (format) {
            case OutputFormat.Png: bytes = PngImageEncoder.EncodeRgba32(pixels, width, height, 6, layout.Dpi); break;
            case OutputFormat.Pdf:
                PrintTransform.Resolve(layout, placement, out var pageWidth, out var pageHeight, out _);
                bytes = PdfWriter.WriteRgba32(width, height, pixels, width * 4, pageWidth * 72 / 25.4, pageHeight * 72 / 25.4, Rgba32.White); break;
            case OutputFormat.Bmp: bytes = Rendering.Bmp.BmpWriter.WriteRgba32(width, height, pixels, width * 4); break;
            case OutputFormat.Webp: bytes = Rendering.Webp.WebpWriter.WriteRgba32(width, height, pixels, width * 4); break;
            case OutputFormat.Pam: bytes = Rendering.Pam.PamWriter.WriteRgba32(width, height, pixels, width * 4); break;
            default: bytes = Rendering.Tga.TgaWriter.WriteRgba32(width, height, pixels, width * 4); break;
        }
        return RenderedOutput.FromOwnedBinary(format, bytes);
    }

    internal static bool HasOnlyOpaqueSolids(BarcodePrintLayout layout) {
        if (layout.Background.Kind != BarcodePaint.PaintKind.Solid || !layout.Background.IsOpaque) return false;
        foreach (var path in layout.Paths) if (path.Paint.Kind != BarcodePaint.PaintKind.Solid || !path.Paint.IsOpaque) return false;
        return true;
    }

    private static byte[] Pdf(BarcodePrintLayout layout, BarcodePrintPlacement? placement) {
        var t = PrintTransform.Resolve(layout, placement, out var width, out var height, out _);
        var content = new StringBuilder("q\n");
        content.Append("1 0 0 -1 0 ").Append(N(height * 72 / 25.4)).Append(" cm\n");
        content.Append(N(72 / 25.4)).Append(" 0 0 ").Append(N(72 / 25.4)).Append(" 0 0 cm\n");
        content.Append("0 0 ").Append(N(width)).Append(' ').Append(N(height)).Append(" re W n\n");
        PdfColor(content, layout.Background.StartColor);
        content.Append("0 0 ").Append(N(width)).Append(' ').Append(N(height)).Append(" re f\n");
        content.Append(N(t.A)).Append(' ').Append(N(t.B)).Append(' ').Append(N(t.C)).Append(' ')
            .Append(N(t.D)).Append(' ').Append(N(t.X)).Append(' ').Append(N(t.Y)).Append(" cm\n");
        foreach (var path in layout.Paths) {
            PdfColor(content, path.Paint.StartColor);
            foreach (var contour in path.Contours) {
                content.Append(N(contour[0].X)).Append(' ').Append(N(contour[0].Y)).Append(" m\n");
                for (var i = 1; i < contour.Count; i++) content.Append(N(contour[i].X)).Append(' ').Append(N(contour[i].Y)).Append(" l\n");
                content.Append("h\n");
            }
            content.Append("f\n");
        }
        content.Append("Q\n");
        return PdfVectorWriter.Write(width * 72 / 25.4, height * 72 / 25.4, content.ToString());
    }

    private static string Eps(BarcodePrintLayout layout, BarcodePrintPlacement? placement) {
        if (!HasOnlyOpaqueSolids(layout)) throw new NotSupportedException("EPS print output requires opaque solid paints. Use SVG, PNG or PDF for gradients, patterns or alpha.");
        var t = PrintTransform.Resolve(layout, placement, out var width, out var height, out _);
        var content = new StringBuilder("%!PS-Adobe-3.0 EPSF-3.0\n%%BoundingBox: 0 0 ");
        content.Append(N(Math.Ceiling(width * 72 / 25.4))).Append(' ').Append(N(Math.Ceiling(height * 72 / 25.4)))
            .Append("\n%%HiResBoundingBox: 0 0 ").Append(N(width * 72 / 25.4)).Append(' ').Append(N(height * 72 / 25.4))
            .Append("\n%%EndComments\ngsave\n0 ").Append(N(height * 72 / 25.4)).Append(" translate\n")
            .Append(N(72 / 25.4)).Append(' ').Append(N(-72 / 25.4)).Append(" scale\nnewpath 0 0 moveto ")
            .Append(N(width)).Append(" 0 lineto ").Append(N(width)).Append(' ').Append(N(height)).Append(" lineto 0 ")
            .Append(N(height)).Append(" lineto closepath clip newpath\n");
        EpsColor(content, layout.Background.StartColor);
        content.Append("0 0 ").Append(N(width)).Append(' ').Append(N(height)).Append(" rectfill\n[")
            .Append(N(t.A)).Append(' ').Append(N(t.B)).Append(' ').Append(N(t.C)).Append(' ').Append(N(t.D)).Append(' ')
            .Append(N(t.X)).Append(' ').Append(N(t.Y)).Append("] concat\n");
        foreach (var path in layout.Paths) {
            EpsColor(content, path.Paint.StartColor);
            content.Append("newpath\n");
            foreach (var contour in path.Contours) {
                content.Append(N(contour[0].X)).Append(' ').Append(N(contour[0].Y)).Append(" moveto\n");
                for (var i = 1; i < contour.Count; i++) content.Append(N(contour[i].X)).Append(' ').Append(N(contour[i].Y)).Append(" lineto\n");
                content.Append("closepath\n");
            }
            content.Append("fill\n");
        }
        return content.Append("grestore\nshowpage\n%%EOF\n").ToString();
    }

    private static void PdfColor(StringBuilder b, Rgba32 c) => b.Append(N(c.R / 255D)).Append(' ').Append(N(c.G / 255D)).Append(' ').Append(N(c.B / 255D)).Append(" rg\n");
    private static void EpsColor(StringBuilder b, Rgba32 c) => b.Append(N(c.R / 255D)).Append(' ').Append(N(c.G / 255D)).Append(' ').Append(N(c.B / 255D)).Append(" setrgbcolor\n");
    private static string N(double number) => number.ToString("0.########", CultureInfo.InvariantCulture);
}
