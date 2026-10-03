using System;
using System.Globalization;
using System.Text;
using System.Threading;
using CodeGlyphX.Rendering.Pdf;
using CodeGlyphX.Rendering.Png;
using CodeGlyphX.Rendering.Svg;

namespace CodeGlyphX.Rendering.Art;

public sealed partial class QrSceneComposition {
    /// <summary>Exports editable illustration polygons, caption outlines and pixel-aligned QR contours as SVG.
    /// Only an uploaded logo is raster content, normalized to a bounded embedded PNG. The reference grid
    /// preserves every PNG module shape and functional pattern. Zero millimeters keeps pixel dimensions.</summary>
    public string ToSvg(double widthMillimeters = 0) {
        ValidatePhysicalWidth(widthMillimeters, allowZero: true);
        var size = Image.Size;
        var dimension = widthMillimeters == 0 ? size.ToString(CultureInfo.InvariantCulture) : Number(widthMillimeters) + "mm";
        var output = new StringBuilder(32768);
        output.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"").Append(dimension).Append("\" height=\"").Append(dimension)
            .Append("\" viewBox=\"0 0 ").Append(size).Append(' ').Append(size).Append("\" role=\"img\" aria-label=\"Illustrated QR scene\">")
            .Append("<defs><clipPath id=\"scene-canvas\"><rect width=\"").Append(size).Append("\" height=\"").Append(size).Append("\"/></clipPath></defs>")
            .Append("<g clip-path=\"url(#scene-canvas)\"><rect width=\"").Append(size).Append("\" height=\"").Append(size).Append("\" fill=\"").Append(Color(_design.Paper)).Append("\"/>");
        foreach (var shape in Geometry.Shapes) {
            output.Append("<polygon fill=\"").Append(Color(shape.Color)).Append("\" points=\"");
            for (var i = 0; i < shape.Points.Length; i += 2) output.Append(Number(shape.Points[i] * size)).Append(',').Append(Number(shape.Points[i + 1] * size)).Append(' ');
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
