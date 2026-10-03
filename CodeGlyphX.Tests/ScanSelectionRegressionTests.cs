using System;
using System.Linq;
using CodeGlyphX.Pdf417;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("ImageScannerSerial")]
public sealed class ScanSelectionRegressionTests {
#if NET8_0_OR_GREATER
    [Theory]
    [InlineData(900, 300)]
    [InlineData(300, 900)]
    public void OffCenterQrInRectangularFrameAgreesAcrossEntryPoints(int width, int height) {
        const string value = "https://example.com/wide";
        var png = QrCode.Render(value, OutputFormat.Png, new QrEasyOptions { ModuleSize = 6 }).Data;
        var qr = ImageReader.DecodeRgba32(png, out var qrWidth, out var qrHeight);
        var pixels = WhiteFrame(width, height);
        Place(qr, qrWidth, qrHeight, pixels, width, 35, 35);
        var qrOptions = new QrPixelDecodeOptions { Profile = QrDecodeProfile.Balanced, BudgetMilliseconds = 5000 };

        Assert.True(CodeGlyph.TryDecode(pixels, width, height, width * 4, PixelFormat.Rgba32,
            out var single, qrOptions: qrOptions));
        AssertQr(single, value);
        Assert.True(CodeGlyph.TryDecode(pixels, width, height, width * 4, PixelFormat.Rgba32,
            out var diagnostic, out var trace, qrOptions: qrOptions));
        AssertQr(diagnostic, value);
        Assert.True(trace.Success);
        Assert.Equal(CodeGlyphKind.Qr, trace.SuccessKind);

        var options = new CodeGlyphDecodeOptions {
            Qr = qrOptions,
            Image = new ImageDecodeOptions { RecognitionBudgetMilliseconds = 5000 }
        };
        Assert.True(CodeGlyph.TryDecode(pixels, width, height, width * 4, PixelFormat.Rgba32,
            out var budgeted, options));
        AssertQr(budgeted, value);
        Assert.True(CodeGlyph.TryDecodeAll(pixels, width, height, width * 4, PixelFormat.Rgba32,
            out var all, includeBarcode: false, qrOptions: qrOptions));
        AssertQr(Assert.Single(all), value);

        var scan = SymbolScanner.Scan(ImageFrame.Packed(pixels, width, height, PixelFormat.Rgba32),
            new ScanOptions { Formats = new[] { SymbolFormat.QrCode }, MaxSymbols = 1, TimeoutMilliseconds = 5000 });
        Assert.Equal(ScanStatus.Success, scan.Status);
        Assert.Equal(value, Assert.Single(scan.Symbols).Text);
    }
#endif

#if NET8_0_OR_GREATER
    [Fact]
    public void SingleResultScannerCanFindQrInMultiSymbolFrame() {
        var left = QrEasy.RenderPixels("SCANNER-LEFT", out var lw, out var lh, out _);
        var right = QrEasy.RenderPixels("SCANNER-RIGHT", out var rw, out var rh, out _);
        var cellWidth = Math.Max(lw, rw);
        var cellHeight = Math.Max(lh, rh);
        var width = cellWidth * 2 + 48;
        var height = cellHeight * 2 + 48;
        var pixels = WhiteFrame(width, height);
        Place(left, lw, lh, pixels, width, 16, 16);
        Place(right, rw, rh, pixels, width, cellWidth + 32, cellHeight + 32);

        var result = SymbolScanner.Scan(ImageFrame.Packed(pixels, width, height, PixelFormat.Rgba32), new ScanOptions {
            Formats = new[] { SymbolFormat.QrCode }, MaxSymbols = 1, TimeoutMilliseconds = 5000,
            Qr = new QrPixelDecodeOptions {
                Profile = QrDecodeProfile.Fast, MaxScale = 1, DisableTransforms = true, EnableTileScan = true, TileGrid = 2
            }
        });
        Assert.Equal(ScanStatus.Success, result.Status);
        Assert.Contains(Assert.Single(result.Symbols).Text, new[] { "SCANNER-LEFT", "SCANNER-RIGHT" });
    }
#endif

    [Fact]
    public void Pdf417MacroMetadataSurvivesDiagnosticAndBudgetedDecoding() {
        var matrix = Pdf417Code.EncodeMacro("MACRO-PIXELS", new Pdf417MacroOptions {
            SegmentIndex = 0, FileId = "123", IsLastSegment = true, FileName = "file.txt"
        });
        var pixels = MatrixPngRenderer.RenderPixels(matrix,
            new MatrixPngRenderOptions { ModuleSize = 3, QuietZone = 4 }, out var width, out var height, out var stride);

        Assert.True(CodeGlyph.TryDecode(pixels, width, height, stride, PixelFormat.Rgba32, out var single));
        AssertMacro(single);
        Assert.True(CodeGlyph.TryDecode(pixels, width, height, stride, PixelFormat.Rgba32, out var diagnostic, out var trace));
        AssertMacro(diagnostic);
        Assert.True(trace.Pdf417?.Success);
        Assert.True(trace.Pdf417?.AttemptCount > 0);
        Assert.True(CodeGlyph.TryDecode(pixels, width, height, stride, PixelFormat.Rgba32,
            out var budgeted, new CodeGlyphDecodeOptions { Image = new ImageDecodeOptions { RecognitionBudgetMilliseconds = 5000 } }));
        AssertMacro(budgeted);
    }

    [Fact]
    public void PreferBarcodeKeepsExplicitSelectionAndDiagnostics() {
        var png = Barcode.Render(BarcodeType.Code128, "BARCODE-FIRST", OutputFormat.Png,
            new BarcodeOptions { ModuleSize = 3, QuietZone = 10, HeightModules = 40 }).Data;
        var pixels = ImageReader.DecodeRgba32(png, out var width, out var height);
        Assert.True(CodeGlyph.TryDecode(pixels, width, height, width * 4, PixelFormat.Rgba32,
            out var decoded, out var trace, expectedBarcode: BarcodeType.Code128, preferBarcode: true));
        Assert.Equal("BARCODE-FIRST", decoded.Text);
        Assert.Equal(BarcodeType.Code128, decoded.Barcode!.Type);
        Assert.True(trace.Barcode?.Success);
    }

    private static void AssertQr(CodeGlyphDecoded decoded, string expected) {
        Assert.Equal(CodeGlyphKind.Qr, decoded.Kind);
        Assert.Equal(expected, decoded.Text);
    }

    private static void AssertMacro(CodeGlyphDecoded decoded) {
        Assert.Equal(CodeGlyphKind.Pdf417, decoded.Kind);
        Assert.Equal("MACRO-PIXELS", decoded.Text);
        Assert.NotNull(decoded.Pdf417Macro);
        Assert.Equal("123", decoded.Pdf417Macro!.FileId);
        Assert.Equal("file.txt", decoded.Pdf417Macro.FileName);
        Assert.True(decoded.Pdf417Macro.IsLastSegment);
    }

    private static byte[] WhiteFrame(int width, int height) => Enumerable.Repeat((byte)255, width * height * 4).ToArray();

    private static void Place(byte[] source, int width, int height, byte[] target, int targetWidth, int x, int y) {
        for (var row = 0; row < height; row++)
            Array.Copy(source, row * width * 4, target, ((y + row) * targetWidth + x) * 4, width * 4);
    }
}
