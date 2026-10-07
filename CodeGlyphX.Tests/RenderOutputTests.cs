using CodeGlyphX.DataMatrix;
using CodeGlyphX.Payloads;
using CodeGlyphX.Pdf417;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Jpeg;
using CodeGlyphX.Rendering.Png;
using System;
using System.IO;
using System.Text;
using Xunit;

namespace CodeGlyphX.Tests;

public class RenderOutputTests {
    [Fact]
    public void RenderBarcodeSvgReturnsText() {
        var output = Barcode.Render(SymbolFormat.Code128, "CODE128-12345", OutputFormat.Svg);

        Assert.Equal(OutputFormat.Svg, output.Format);
        Assert.True(output.IsText);

        var text = output.GetText();
        Assert.Contains("<svg", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RenderQrPngReturnsBinary() {
        var output = QR.Render("HELLO-QR", OutputFormat.Png);

        Assert.Equal(OutputFormat.Png, output.Format);
        Assert.False(output.IsText);
        Assert.True(output.Data.Length > 8);

        // PNG signature (89 50 4E 47 0D 0A 1A 0A)
        Assert.Equal(0x89, output.Data.Span[0]);
        Assert.Equal(0x50, output.Data.Span[1]);
        Assert.Equal(0x4E, output.Data.Span[2]);
        Assert.Equal(0x47, output.Data.Span[3]);
    }

    [Fact]
    public void RenderHtmlHonorsTitleExtras() {
        var extras = new OutputOptions { HtmlTitle = "Render Output Test" };
        var output = Barcode.Render(SymbolFormat.Code128, "CODE128-HTML", OutputFormat.Html, extras: extras);

        var text = output.GetText();
        Assert.Contains("<title>Render Output Test</title>", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BarcodeBuilder_Uses_Generic_Render_And_Save_Surface() {
        var builder = Barcode.Create(SymbolFormat.Code128, "BUILDER-123");
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
        BarcodeBuilder barcode = Barcode.Create(SymbolFormat.Code128, "TOP-LEVEL-BARCODE");
        DataMatrixBuilder dataMatrix = DataMatrixCode.Create("TOP-LEVEL-DATA-MATRIX");
        Pdf417Builder pdf417 = Pdf417Code.Create("TOP-LEVEL-PDF417");

        Assert.IsType<QrBuilder>(qr);
        Assert.IsType<BarcodeBuilder>(barcode);
        Assert.IsType<DataMatrixBuilder>(dataMatrix);
        Assert.IsType<Pdf417Builder>(pdf417);
    }

    [Fact]
    public void QrBuilder_Uses_Generic_Render_And_Save_Surface() {
        var builder = QR.Create("QR-BUILDER-CONTRACT")
            .WithModuleSize(6)
            .WithQuietZone(4)
            .WithColors(Rgba32.Black, Rgba32.White)
            .WithErrorCorrection(QrErrorCorrectionLevel.H);

        Assert.True(builder.Encode().Size > 0);
        Assert.Contains("<svg", builder.Render(OutputFormat.Svg).GetText(), StringComparison.OrdinalIgnoreCase);

        using var stream = new MemoryStream();
        builder.Save(stream, OutputFormat.Png);
        Assert.True(stream.Length > 8);
        Assert.Equal(0x89, stream.GetBuffer()[0]);
    }

    [Fact]
    public void DataMatrixBuilder_Uses_Generic_Render_And_Save_Surface() {
        var builder = DataMatrixCode.Create("DATA-MATRIX-BUILDER-CONTRACT")
            .WithModuleSize(5)
            .WithQuietZone(2)
            .WithColors(Rgba32.Black, Rgba32.White)
            .WithOutputOptions(options => options.HtmlEmailSafeTable = true);

        Assert.True(builder.Encode().Width > 0);
        Assert.Contains("<svg", builder.Render(OutputFormat.Svg).GetText(), StringComparison.OrdinalIgnoreCase);

        using var stream = new MemoryStream();
        builder.Save(stream, OutputFormat.Png);
        Assert.True(stream.Length > 8);
        Assert.Equal(0x89, stream.GetBuffer()[0]);
    }

    [Fact]
    public void DataMatrixBuilder_Configures_Text_And_Binary_Contracts() {
        var jpeg = new JpegEncodeOptions { Quality = 90 };
        var textBuilder = DataMatrixCode.Create("DATA-MATRIX-OPTIONS")
            .WithOptions(options => options.ModuleSize = 3)
            .WithMode(DataMatrixEncodingMode.Ascii)
            .WithModuleSize(4)
            .WithQuietZone(2)
            .WithColors(Rgba32.Black, Rgba32.White)
            .WithJpegQuality(88)
            .WithJpegOptions(jpeg)
            .WithHtmlEmailSafeTable()
            .WithIcoSizes(32, 64)
            .WithIcoPreserveAspectRatio(false);

        Assert.True(textBuilder.Encode().Width > 0);

        var binaryBuilder = DataMatrixCode.Create(new byte[] { 0, 1, 2, 3 }, DataMatrixEncodingMode.Base256);
        Assert.True(binaryBuilder.Encode().Width > 0);
        Assert.Contains("<svg", binaryBuilder.Render(OutputFormat.Svg).GetText(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Pdf417Builder_Uses_Generic_Render_And_Save_Surface() {
        var builder = Pdf417Code.Create("PDF417-BUILDER-CONTRACT")
            .WithModuleSize(3)
            .WithQuietZone(2)
            .WithColors(Rgba32.Black, Rgba32.White)
            .WithCompact();

        Assert.True(builder.Encode().Width > 0);
        Assert.Contains("<svg", builder.Render(OutputFormat.Svg).GetText(), StringComparison.OrdinalIgnoreCase);

        using var stream = new MemoryStream();
        builder.Save(stream, OutputFormat.Png);
        Assert.True(stream.Length > 8);
        Assert.Equal(0x89, stream.GetBuffer()[0]);
    }

    [Fact]
    public void Pdf417Builder_Configures_Text_And_Binary_Contracts() {
        var jpeg = new JpegEncodeOptions { Quality = 90 };
        var textBuilder = Pdf417Code.Create("123456789012345678901234567890")
            .WithEncodeOptions(options => options.ErrorCorrectionLevel = 1)
            .WithRenderOptions(options => options.ModuleSize = 2)
            .WithModuleSize(3)
            .WithQuietZone(2)
            .WithColors(Rgba32.Black, Rgba32.White)
            .WithJpegQuality(88)
            .WithJpegOptions(jpeg)
            .WithHtmlEmailSafeTable()
            .WithIcoSizes(32, 64)
            .WithIcoPreserveAspectRatio(false)
            .WithCompaction(Pdf417Compaction.Numeric)
            .WithErrorCorrection(2)
            .WithTextEncoding(Encoding.UTF8)
            .WithAspectRatio(2.5f)
            .WithColumns(2, 10)
            .WithRows(3, 30)
            .WithCompact();

        Assert.True(textBuilder.Encode().Width > 0);

        var binaryBuilder = Pdf417Code.Create(new byte[] { 1, 2, 3, 4 });
        Assert.True(binaryBuilder.Encode().Width > 0);
        Assert.Contains("<svg", binaryBuilder.Render(OutputFormat.Svg).GetText(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BarcodeBuilder_Configures_The_Public_Rendering_Surface() {
        var jpeg = new JpegEncodeOptions { Quality = 90 };
        var builder = Barcode.Create(SymbolFormat.Code128, "BARCODE-OPTIONS")
            .WithOptions(options => options.ModuleSize = 2)
            .WithModuleSize(3)
            .WithQuietZone(4)
            .WithHeight(70)
            .WithColors(Rgba32.Black, Rgba32.White)
            .WithForeground(new Rgba32(1, 2, 3, 255))
            .WithBackground(new Rgba32(250, 249, 248, 255))
            .WithTransparentBackground()
            .WithJpegQuality(88)
            .WithJpegOptions(jpeg)
            .WithLabel("Contract label")
            .WithLabelFontSize(14)
            .WithLabelMargin(5)
            .WithLabelColor(Rgba32.Black)
            .WithLabelFontFamily("sans-serif")
            .WithIcoSizes(32, 64)
            .WithIcoPreserveAspectRatio(false);

        Assert.Equal(3, builder.Options.ModuleSize);
        Assert.Equal("Contract label", builder.Options.LabelText);
        Assert.Equal(new[] { 32, 64 }, builder.OutputOptions.IcoSizes);
        Assert.True(builder.Encode().TotalModules > 0);
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
