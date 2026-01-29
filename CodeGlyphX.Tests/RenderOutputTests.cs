using System;
using CodeGlyphX.Rendering;
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
    public void OutputFormatInfoDetectsSvgzAndSvgGz() {
        Assert.Equal(OutputFormat.Svgz, OutputFormatInfo.FromPath("code.svgz"));
        Assert.Equal(OutputFormat.Svgz, OutputFormatInfo.FromPath("code.svg.gz"));
    }
}
