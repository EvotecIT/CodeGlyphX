using System;
using System.Linq;
using System.Xml.Linq;
using CodeGlyphX.DataMatrix;
using CodeGlyphX.Rendering.Png;
using CodeGlyphX.Rendering.Svg;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class SvgInteroperabilityTests {
    private static readonly XNamespace SvgNamespace = "http://www.w3.org/2000/svg";

    [Fact]
    public void QrSvg_UsesNumericBackgroundGeometry() {
        var qr = QrCodeEncoder.EncodeText("SVG-INTEROP-QR");

        var svg = SvgQrRenderer.Render(qr.Modules, new QrSvgRenderOptions { QuietZone = 4 });
        var document = XDocument.Parse(svg);
        var background = document.Root!.Elements(SvgNamespace + "rect").First();
        var expectedSize = (qr.Modules.Width + 8).ToString();

        Assert.Equal(expectedSize, background.Attribute("width")!.Value);
        Assert.Equal(expectedSize, background.Attribute("height")!.Value);
        Assert.DoesNotContain('%', svg);
    }

    [Fact]
    public void StyledQrSvg_RemainsWellFormedWithNumericBackgroundGeometry() {
        var qr = QrCodeEncoder.EncodeText("SVG-INTEROP-STYLED");
        var options = new QrSvgRenderOptions {
            QuietZone = 3,
            ModuleShape = QrPngModuleShape.Rounded,
            ModuleScale = 0.85,
            Eyes = new QrPngEyeOptions {
                UseFrame = true,
                OuterShape = QrPngModuleShape.Rounded,
                InnerShape = QrPngModuleShape.Circle
            }
        };

        var svg = SvgQrRenderer.Render(qr.Modules, options);
        var document = XDocument.Parse(svg);
        var background = document.Root!.Elements(SvgNamespace + "rect").First();

        Assert.Equal((qr.Modules.Width + 6).ToString(), background.Attribute("width")!.Value);
        Assert.NotEmpty(document.Descendants(SvgNamespace + "circle"));
        Assert.DoesNotContain('%', svg);
    }

    [Fact]
    public void MatrixSvg_UsesNumericRectangularBackgroundGeometry() {
        var matrix = DataMatrixEncoder.Encode("SVG-INTEROP-MATRIX");

        var svg = MatrixSvgRenderer.Render(matrix, new MatrixSvgRenderOptions { QuietZone = 2 });
        var document = XDocument.Parse(svg);
        var background = document.Root!.Element(SvgNamespace + "rect")!;

        Assert.Equal((matrix.Width + 4).ToString(), background.Attribute("width")!.Value);
        Assert.Equal((matrix.Height + 4).ToString(), background.Attribute("height")!.Value);
        Assert.DoesNotContain('%', svg);
    }

    [Fact]
    public void BarcodeSvg_UsesNumericBackgroundAndPortableTextBaseline() {
        var barcode = BarcodeEncoder.Encode(BarcodeType.Code128, "SVG-123");
        var options = new BarcodeSvgRenderOptions {
            ModuleSize = 3,
            QuietZone = 5,
            HeightModules = 30,
            LabelText = "Human & readable",
            LabelFontSize = 11,
            LabelMargin = 2
        };

        var svg = SvgBarcodeRenderer.Render(barcode, options);
        var document = XDocument.Parse(svg);
        var background = document.Root!.Element(SvgNamespace + "rect")!;
        var text = document.Root.Element(SvgNamespace + "text")!;

        Assert.Equal((barcode.TotalModules + 10).ToString(), background.Attribute("width")!.Value);
        Assert.Equal("middle", text.Attribute("text-anchor")!.Value);
        Assert.Null(text.Attribute("dominant-baseline"));
        Assert.Equal("Human & readable", text.Value);
        Assert.DoesNotContain('%', svg);
        Assert.DoesNotContain(',', text.Attribute("x")!.Value);
        Assert.DoesNotContain(',', text.Attribute("y")!.Value);
    }
}
