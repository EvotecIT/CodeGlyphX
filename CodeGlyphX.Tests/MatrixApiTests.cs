using CodeGlyphX.Pdf417;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Ascii;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class MatrixApiTests {
    public static IEnumerable<object[]> MatrixFormats {
        get {
            yield return new object[] { OutputFormat.Png, OutputKind.Binary };
            yield return new object[] { OutputFormat.Svg, OutputKind.Text };
            yield return new object[] { OutputFormat.Svgz, OutputKind.Binary };
            yield return new object[] { OutputFormat.Html, OutputKind.Text };
            yield return new object[] { OutputFormat.Jpeg, OutputKind.Binary };
            yield return new object[] { OutputFormat.Webp, OutputKind.Binary };
            yield return new object[] { OutputFormat.Bmp, OutputKind.Binary };
            yield return new object[] { OutputFormat.Gif, OutputKind.Binary };
            yield return new object[] { OutputFormat.Tiff, OutputKind.Binary };
            yield return new object[] { OutputFormat.Ppm, OutputKind.Binary };
            yield return new object[] { OutputFormat.Pbm, OutputKind.Binary };
            yield return new object[] { OutputFormat.Pgm, OutputKind.Binary };
            yield return new object[] { OutputFormat.Pam, OutputKind.Binary };
            yield return new object[] { OutputFormat.Xbm, OutputKind.Text };
            yield return new object[] { OutputFormat.Xpm, OutputKind.Text };
            yield return new object[] { OutputFormat.Tga, OutputKind.Binary };
            yield return new object[] { OutputFormat.Ico, OutputKind.Binary };
            yield return new object[] { OutputFormat.Pdf, OutputKind.Binary };
            yield return new object[] { OutputFormat.Eps, OutputKind.Text };
            yield return new object[] { OutputFormat.Ascii, OutputKind.Text };
        }
    }

    [Fact]
    public void DataMatrixCode_Png_RoundTrip() {
        var png = DataMatrixCode.Render("DM-HELLO", OutputFormat.Png).Data;
        Assert.True(DataMatrixCode.TryDecodePng(png, out var text));
        Assert.Equal("DM-HELLO", text);
    }

    [Fact]
    public void DataMatrixCode_Image_RoundTrip() {
        var png = DataMatrixCode.Render("DM-IMG", OutputFormat.Png).Data;
        Assert.True(DataMatrixCode.TryDecodeImage(png, out var text));
        Assert.Equal("DM-IMG", text);
    }

    [Fact]
    public void Pdf417Code_Png_RoundTrip() {
        var options = new Pdf417EncodeOptions { ErrorCorrectionLevel = 2 };
        var png = Pdf417Code.Render("PDF-HELLO", OutputFormat.Png, options).Data;
        Assert.True(Pdf417Code.TryDecodePng(png, out string text));
        Assert.Equal("PDF-HELLO", text);
    }

    [Fact]
    public void Pdf417Code_Image_RoundTrip() {
        var options = new Pdf417EncodeOptions { ErrorCorrectionLevel = 2 };
        var png = Pdf417Code.Render("PDF-IMG", OutputFormat.Png, options).Data;
        Assert.True(Pdf417Code.TryDecodeImage(png, out string text));
        Assert.Equal("PDF-IMG", text);
    }

    [Fact]
    public void Pdf417Code_Macro_Png_RoundTrip() {
        var macro = new Pdf417MacroOptions {
            SegmentIndex = 0,
            FileId = "123",
            IsLastSegment = true,
            FileName = "macro.txt"
        };
        var png = Pdf417Code.RenderMacro("PDF-MACRO", macro, OutputFormat.Png).Data;

        Assert.True(Pdf417Code.TryDecodePng(png, out Pdf417Decoded decoded));
        Assert.Equal("PDF-MACRO", decoded.Text);
        Assert.NotNull(decoded.Macro);
        Assert.Equal("123", decoded.Macro!.FileId);
        Assert.True(decoded.Macro.IsLastSegment);
        Assert.Equal("macro.txt", decoded.Macro.FileName);
    }

    [Fact]
    public void MatrixBarcode_Save_DataMatrix_ByExtension() {
        var path = Path.Combine(Path.GetTempPath(), "codeglyphx-matrix-barcode-" + Guid.NewGuid().ToString("N") + ".png");
        try {
            MatrixBarcode.Save(BarcodeType.DataMatrix, "DM-FACADE", path);

            Assert.True(File.Exists(path));
            Assert.True(DataMatrixCode.TryDecodePngFile(path, out var text));
            Assert.Equal("DM-FACADE", text);
        } finally {
            if (File.Exists(path)) {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public void MatrixBarcode_Render_Pdf417_AsSvg() {
        var output = MatrixBarcode.Render(BarcodeType.PDF417, "PDF-FACADE", OutputFormat.Svg);

        Assert.Equal(OutputKind.Text, output.Kind);
        Assert.Contains("<svg", output.GetText(), StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [MemberData(nameof(MatrixFormats))]
    public void MatrixBarcode_Render_AllSupportedFormats_ProducesExpectedKind(OutputFormat format, OutputKind kind) {
        var options = new MatrixOptions {
            ModuleSize = 2,
            QuietZone = 1,
            JpegQuality = 75,
            WebpQuality = 80,
            IcoSizes = new[] { 16 },
            IcoPreserveAspectRatio = false,
            HtmlEmailSafeTable = true
        };

        var output = MatrixBarcode.Render(BarcodeType.DataMatrix, "DM-FORMATS", format, options);

        Assert.Equal(format, output.Format);
        Assert.Equal(kind, output.Kind);
        Assert.NotEmpty(output.Data);
    }

    [Theory]
    [InlineData(OutputFormat.Svg, OutputKind.Text)]
    [InlineData(OutputFormat.Html, OutputKind.Text)]
    [InlineData(OutputFormat.Ico, OutputKind.Binary)]
    public void DataMatrixCode_Render_UsesSharedMatrixOptions(OutputFormat format, OutputKind kind) {
        var output = DataMatrixCode.Render("DM-OPTIONS", format, options: new MatrixOptions {
            ModuleSize = 2,
            QuietZone = 1,
            IcoSizes = new[] { 16 },
            HtmlEmailSafeTable = true
        });

        Assert.Equal(kind, output.Kind);
        Assert.NotEmpty(output.Data);
    }

    [Theory]
    [InlineData(OutputFormat.Svg, OutputKind.Text)]
    [InlineData(OutputFormat.Html, OutputKind.Text)]
    [InlineData(OutputFormat.Ico, OutputKind.Binary)]
    public void Pdf417Code_Render_UsesSharedMatrixOptions(OutputFormat format, OutputKind kind) {
        var output = Pdf417Code.Render("PDF-OPTIONS", format, renderOptions: new MatrixOptions {
            ModuleSize = 2,
            QuietZone = 1,
            IcoSizes = new[] { 16 },
            HtmlEmailSafeTable = true
        });

        Assert.Equal(kind, output.Kind);
        Assert.NotEmpty(output.Data);
    }

    [Fact]
    public void MatrixBarcode_Render_AsAscii_HonorsMatrixQuietZone() {
        var modules = MatrixBarcode.Encode(BarcodeType.DataMatrix, "DM-ASCII");
        var output = MatrixBarcode.Render(modules, OutputFormat.Ascii, new MatrixOptions { QuietZone = 0 });
        var lines = SplitLines(output.GetText());

        Assert.Equal(OutputKind.Text, output.Kind);
        Assert.Equal(modules.Height, lines.Length);
        Assert.All(lines, line => Assert.Equal(modules.Width * 2, line.Length));
    }

    [Fact]
    public void AztecCode_Render_AsAscii_HonorsMatrixQuietZone() {
        var modules = AztecCode.Encode("AZTEC-ASCII");
        var output = AztecCode.Render("AZTEC-ASCII", OutputFormat.Ascii, renderOptions: new MatrixOptions { QuietZone = 0 });
        var lines = SplitLines(output.GetText());

        Assert.Equal(modules.Height, lines.Length);
        Assert.All(lines, line => Assert.Equal(modules.Width * 2, line.Length));
    }

    [Fact]
    public void MatrixBarcode_Render_AsAscii_PreservesExplicitAsciiOptions() {
        var modules = MatrixBarcode.Encode(BarcodeType.DataMatrix, "DM-ASCII-EXTRAS");
        var extras = new RenderExtras {
            MatrixAscii = new MatrixAsciiRenderOptions {
                QuietZone = 1,
                ModuleWidth = 1,
                Dark = "X",
                Light = "."
            }
        };

        var output = MatrixBarcode.Render(modules, OutputFormat.Ascii, new MatrixOptions { QuietZone = 0 }, extras);
        var lines = SplitLines(output.GetText());

        Assert.Equal(modules.Height + 2, lines.Length);
        Assert.All(lines, line => Assert.Equal(modules.Width + 2, line.Length));
        Assert.True(lines.First().All(c => c == '.'));
    }

    [Fact]
    public void MatrixBarcode_Render_Html_WrapsTitle() {
        var output = MatrixBarcode.Render(
            BarcodeType.DataMatrix,
            "DM-HTML",
            OutputFormat.Html,
            extras: new RenderExtras { HtmlTitle = "Matrix title" });

        Assert.Equal(OutputKind.Text, output.Kind);
        Assert.Contains("<title>Matrix title</title>", output.GetText(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MatrixBarcode_Save_Stream_WritesTextFormat() {
        using var stream = new MemoryStream();

        MatrixBarcode.Save(BarcodeType.PDF417, "PDF-STREAM", stream, OutputFormat.Svg);

        Assert.True(stream.Length > 0);
    }

    [Fact]
    public void MatrixBarcode_Render_RejectsNullModules() {
        Assert.Throws<ArgumentNullException>(() => MatrixBarcode.Render(null!, OutputFormat.Png));
    }

    [Fact]
    public void MatrixBarcode_Render_RejectsUnknownFormat() {
        var modules = MatrixBarcode.Encode(BarcodeType.DataMatrix, "DM-UNKNOWN");

        Assert.Throws<ArgumentOutOfRangeException>(() => MatrixBarcode.Render(modules, OutputFormat.Unknown));
    }

    private static string[] SplitLines(string text) {
        return text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
    }
}
