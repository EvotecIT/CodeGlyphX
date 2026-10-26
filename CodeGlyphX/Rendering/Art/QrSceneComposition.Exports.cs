using CodeGlyphX.Rendering;
using System;
using System.Globalization;
using System.Text;
using System.Threading;
using CodeGlyphX.Rendering.Pdf;
using CodeGlyphX.Rendering.Png;
using CodeGlyphX.Rendering.Svg;

namespace CodeGlyphX.Rendering.Art;

public sealed partial class QrSceneComposition {
    /// <summary>Exports editable illustration shapes, gradients, caption outlines and pixel-aligned QR contours as SVG.
    /// Only an uploaded logo is raster content, normalized to a bounded embedded PNG. The reference grid
    /// preserves every PNG module shape and functional pattern. Zero millimeters keeps pixel dimensions.
    /// Gradient identifiers carry a content hash, so several scenes can share one host page.</summary>
    public string ToSvg(double widthMillimeters = 0) {
        ValidatePhysicalWidth(widthMillimeters, allowZero: true);
        var size = Image.Size;
        var dimension = widthMillimeters == 0 ? size.ToString(CultureInfo.InvariantCulture) : Number(widthMillimeters) + "mm";
        var output = new StringBuilder(65536);
        output.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"").Append(dimension).Append("\" height=\"").Append(dimension)
            .Append("\" viewBox=\"0 0 ").Append(size).Append(' ').Append(size).Append("\" overflow=\"hidden\" role=\"img\" aria-label=\"Illustrated QR scene\">")
            .Append("<g><rect width=\"").Append(size).Append("\" height=\"").Append(size).Append("\" fill=\"").Append(Color(_design.Paper)).Append("\"/>");
        var prefix = "cgx-scene-" + ContentHash().ToString("x8", CultureInfo.InvariantCulture) + "-";
        var gradient = 0;
        foreach (var shape in Geometry.Shapes) {
            var paint = shape.Paint;
            var fill = Color(paint.Color);
            if (paint.IsGradient) {
                var id = prefix + (gradient++).ToString(CultureInfo.InvariantCulture);
                AppendGradient(output, id, paint, size);
                fill = "url(#" + id + ")";
            }
            var single = shape.Contours.Count == 1;
            output.Append(single ? "<polygon fill=\"" : "<path fill=\"").Append(fill).Append('"');
            if (paint.Opacity < 1) output.Append(" fill-opacity=\"").Append(Number(paint.Opacity)).Append('"');
            output.Append(single ? " points=\"" : " d=\"");
            foreach (var contour in shape.Contours) {
                for (var i = 0; i < contour.Length; i += 2) {
                    if (!single) output.Append(i == 0 ? 'M' : 'L');
                    output.Append(Number(contour[i] * size)).Append(single ? ',' : ' ').Append(Number(contour[i + 1] * size)).Append(' ');
                }
                if (!single) output.Append('Z');
            }
            output.Append("\"/>");
        }
        AppendLogo(output);
        output.Append("<rect x=\"").Append(Image.QrOffsetX).Append("\" y=\"").Append(Image.QrOffsetY).Append("\" width=\"").Append(Image.QrSize)
            .Append("\" height=\"").Append(Image.QrSize).Append("\" fill=\"").Append(Color(_design.Paper)).Append("\"/>")
            .Append("<path fill=\"").Append(Color(_design.Ink)).Append("\" shape-rendering=\"crispEdges\" d=\"");
        SvgPixelContours.Append(output, Image.PixelSpan, size * 4, Image.QrOffsetX, Image.QrOffsetY, Image.QrSize, _design.Ink);
        return output.Append("\"/></g></svg>").ToString();
    }
    /// <summary>Saves vector scene geometry as SVG; uploaded logos remain embedded raster images.</summary>
    public string SaveSvg(string path, double widthMillimeters = 0) => RenderIO.WriteText(path, ToSvg(widthMillimeters));
    /// <summary>Returns a single-page PDF at the specified physical width (10..200 mm).
    /// The PDF embeds the finished opaque RGB artwork at its current pixel resolution.</summary>
    public byte[] ToPdf(double widthMillimeters = 100) {
        ValidatePhysicalWidth(widthMillimeters, allowZero: false);
        return PdfWriter.WriteRgba32(Image.Size, Image.Size, Image.PixelSpan, Image.Size * 4, widthMillimeters * 72 / 25.4, widthMillimeters * 72 / 25.4);
    }
    /// <summary>Saves a physically sized PDF containing the rendered artwork.</summary>
    public string SavePdf(string path, double widthMillimeters = 100) => RenderIO.WriteBinary(path, ToPdf(widthMillimeters));
    /// <summary>Renders this retained design at the pixel dimensions implied by width and DPI.
    /// The QR is rendered again on the new grid, preserving the full quiet zone without bitmap resizing.</summary>
    public QrSceneExport Export(QrSceneExportOptions? options = null, CancellationToken cancellationToken = default) {
        var settings = (options ?? new QrSceneExportOptions()).Copy();
        settings.Validate(); cancellationToken.ThrowIfCancellationRequested();
        var design = Design; design.Size = settings.PixelSize;
        var scene = design.Size == Image.Size ? this : QrArt.ComposeScene(Payload, design, cancellationToken);
        return new QrSceneExport(scene, settings);
    }
    private static void AppendGradient(StringBuilder output, string id, ScenePaint paint, int size) {
        if (paint.Radial) {
            output.Append("<radialGradient id=\"").Append(id).Append("\" gradientUnits=\"userSpaceOnUse\" cx=\"").Append(Number(paint.X1 * size))
                .Append("\" cy=\"").Append(Number(paint.Y1 * size)).Append("\" r=\"").Append(Number(paint.Radius * size)).Append("\">");
        } else {
            output.Append("<linearGradient id=\"").Append(id).Append("\" gradientUnits=\"userSpaceOnUse\" x1=\"").Append(Number(paint.X1 * size))
                .Append("\" y1=\"").Append(Number(paint.Y1 * size)).Append("\" x2=\"").Append(Number(paint.X2 * size)).Append("\" y2=\"").Append(Number(paint.Y2 * size)).Append("\">");
        }
        for (var i = 0; i < paint.Stops!.Length; i++) {
            output.Append("<stop offset=\"").Append(Number(paint.Offsets![i])).Append("\" stop-color=\"").Append(Color(paint.Stops[i])).Append('"');
            if (paint.Stops[i].A < 255) output.Append(" stop-opacity=\"").Append(Number(paint.Stops[i].A / 255.0)).Append('"');
            output.Append("/>");
        }
        output.Append(paint.Radial ? "</radialGradient>" : "</linearGradient>");
    }

    // FNV-1a over the rendered pixels: equal artwork gets equal identifiers, different artwork almost never collides.
    private uint ContentHash() {
        var hash = 2166136261u;
        var pixels = Image.PixelSpan;
        for (var i = 0; i < pixels.Length; i += 7) hash = unchecked((hash ^ pixels[i]) * 16777619u);
        return hash;
    }

    private void AppendLogo(StringBuilder output) {
        if (_design.LogoImage is null || !_design.Logo.Visible) return;
        var pixels = ImageReader.DecodeRgba32(_design.LogoImage, new ImageDecodeOptions { MaxBytes = 1024 * 1024, MaxPixels = 1_000_000, MaxDecodedBytes = 16_000_000 }, out var width, out var height);
        var layer = _design.Logo; var extent = layer.Scale * Image.Size;
        var w = extent * width / Math.Max(width, height); var h = extent * height / Math.Max(width, height);
        var x = layer.X * Image.Size; var y = layer.Y * Image.Size;
        output.Append("<image x=\"").Append(Number(x - w / 2)).Append("\" y=\"").Append(Number(y - h / 2)).Append("\" width=\"").Append(Number(w))
            .Append("\" height=\"").Append(Number(h)).Append("\" transform=\"rotate(").Append(Number(layer.RotationDegrees % 360)).Append(' ').Append(Number(x)).Append(' ').Append(Number(y))
            .Append(")\" image-rendering=\"pixelated\" href=\"data:image/png;base64,").Append(Convert.ToBase64String(PngImageEncoder.EncodeRgba32(pixels, width, height))).Append("\"/>");
    }
    internal static void ValidatePhysicalWidth(double value, bool allowZero) {
        if ((allowZero && value == 0) || (!double.IsNaN(value) && value >= 10 && value <= 200)) return;
        throw new ArgumentOutOfRangeException(nameof(value), "Physical width must be 10..200 millimeters.");
    }
    private static string Number(double value) => value.ToString("0.######", CultureInfo.InvariantCulture);
    private static string Color(Rgba32 color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";
}
