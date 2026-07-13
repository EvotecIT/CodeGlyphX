using CodeGlyphX.Rendering;
using System;
using System.IO;
using Xunit;

namespace CodeGlyphX.Tests;

public class RenderOutputTests {
    [Fact]
    public void RenderBarcodeSvgReturnsText() {
        var output = Barcode.Render(BarcodeType.Code128, "CODE128-12345", OutputFormat.Svg);

        Assert.Equal(OutputFormat.Svg, output.Format);
        Assert.True(output.IsText);

        var text = output.GetText();
        Assert.Contains("<svg", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RenderQrPngReturnsBinary() {
        var output = QrCode.Render("HELLO-QR", OutputFormat.Png);

        Assert.Equal(OutputFormat.Png, output.Format);
        Assert.False(output.IsText);
        Assert.True(output.Data.Length > 8);

        // PNG signature (89 50 4E 47 0D 0A 1A 0A)
        Assert.Equal(0x89, output.Data[0]);
        Assert.Equal(0x50, output.Data[1]);
        Assert.Equal(0x4E, output.Data[2]);
        Assert.Equal(0x47, output.Data[3]);
    }

    [Fact]
    public void RenderHtmlHonorsTitleExtras() {
        var extras = new RenderExtras { HtmlTitle = "Render Output Test" };
        var output = Barcode.Render(BarcodeType.Code128, "CODE128-HTML", OutputFormat.Html, extras: extras);

        var text = output.GetText();
        Assert.Contains("<title>Render Output Test</title>", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BarcodeBuilder_Uses_Generic_Render_And_Save_Surface() {
        var builder = Barcode.Create(BarcodeType.Code128, "BUILDER-123");
        var svg = builder.Render(OutputFormat.Svg);

        Assert.Equal(OutputFormat.Svg, svg.Format);
        Assert.Contains("<svg", svg.GetText(), StringComparison.OrdinalIgnoreCase);

        using var stream = new MemoryStream();
        builder.Save(stream, OutputFormat.Png);
        Assert.True(stream.Length > 8);
        Assert.Equal(0x89, stream.GetBuffer()[0]);

        var path = Path.Combine(Path.GetTempPath(), $"codeglyphx-barcode-{Guid.NewGuid():N}.svg");
        try {
            Assert.Equal(path, builder.Save(path));
            Assert.Contains("<svg", File.ReadAllText(path), StringComparison.OrdinalIgnoreCase);
        } finally {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void CreateFacades_Return_TopLevel_Builder_Types() {
        QrBuilder qr = QR.Create("TOP-LEVEL-QR");
        BarcodeBuilder barcode = Barcode.Create(BarcodeType.Code128, "TOP-LEVEL-BARCODE");
        DataMatrixBuilder dataMatrix = DataMatrixCode.Create("TOP-LEVEL-DATA-MATRIX");
        Pdf417Builder pdf417 = Pdf417Code.Create("TOP-LEVEL-PDF417");

        Assert.IsType<QrBuilder>(qr);
        Assert.IsType<BarcodeBuilder>(barcode);
        Assert.IsType<DataMatrixBuilder>(dataMatrix);
        Assert.IsType<Pdf417Builder>(pdf417);
    }

    [Fact]
    public void OutputFormatInfoDetectsSvgzAndSvgGz() {
        Assert.Equal(OutputFormat.Svgz, OutputFormatInfo.FromPath("code.svgz"));
        Assert.Equal(OutputFormat.Svgz, OutputFormatInfo.FromPath("code.svg.gz"));
    }

    [Fact]
    public void OutputFormatInfoDetectsGifAndTiff() {
        Assert.Equal(OutputFormat.Gif, OutputFormatInfo.FromPath("code.gif"));
        Assert.Equal(OutputFormat.Tiff, OutputFormatInfo.FromPath("code.tif"));
        Assert.Equal(OutputFormat.Tiff, OutputFormatInfo.FromPath("code.tiff"));
    }
}