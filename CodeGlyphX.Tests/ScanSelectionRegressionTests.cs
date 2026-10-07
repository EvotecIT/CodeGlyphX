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
    public void OffCenterQrInRectangularFrameIsRecognized(int width, int height) {
        const string value = "https://example.com/wide";
        var png = QR.Render(value, OutputFormat.Png, new QrRenderOptions { ModuleSize = 6 }).Data;
        var qr = ImageReader.DecodeRgba32(png, out var qrWidth, out var qrHeight);
        var pixels = WhiteFrame(width, height);
        Place(qr, qrWidth, qrHeight, pixels, width, 35, 35);
        var qrOptions = new QrPixelDecodeOptions { Profile = QrDecodeProfile.Balanced, BudgetMilliseconds = 5000 };

        var scan = SymbolScanner.Scan(ImageFrame.Packed(pixels, width, height, PixelFormat.Rgba32),
            new ScanOptions { Formats = new[] { SymbolFormat.QrCode }, MaxSymbols = 1, TimeoutMilliseconds = 5000 });
        Assert.Equal(ScanStatus.Success, scan.Status);
        Assert.Equal(value, Assert.Single(scan.Symbols).Text);
    }
#endif

#if NET8_0_OR_GREATER
    [Fact]
    public void SingleResultScannerCanFindQrInMultiSymbolFrame() {
        var left = QR.RenderPixels("SCANNER-LEFT", out var lw, out var lh, out _);
        var right = QR.RenderPixels("SCANNER-RIGHT", out var rw, out var rh, out _);
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
    public void Pdf417MacroMetadataSurvivesCanonicalPixelScanning() {
        var matrix = Pdf417Code.EncodeMacro("MACRO-PIXELS", new Pdf417MacroOptions {
            SegmentIndex = 0, FileId = "123", IsLastSegment = true, FileName = "file.txt"
        }).Modules;
        var pixels = MatrixPngRenderer.RenderPixels(matrix,
            new MatrixPngRenderOptions { ModuleSize = 3, QuietZone = 4 }, out var width, out var height, out var stride);

        var result = SymbolScanner.Scan(new ImageFrame(pixels, width, height, stride, PixelFormat.Rgba32),
            new ScanOptions { Formats = new[] { SymbolFormat.Pdf417 }, MaxSymbols = 1, TimeoutMilliseconds = TestBudget.Adjust(5000) });
        var symbol = Assert.Single(result.Symbols);
        Assert.Equal(SymbolFormat.Pdf417, symbol.Format);
        Assert.Equal("MACRO-PIXELS", symbol.Text);
        var macro = Assert.IsType<Pdf417SymbolMetadata>(symbol.Metadata).Macro;
        Assert.NotNull(macro);
        Assert.Equal("123", macro!.FileId);
        Assert.Equal("file.txt", macro.FileName);
        Assert.True(macro.IsLastSegment);
    }

    private static byte[] WhiteFrame(int width, int height) => Enumerable.Repeat((byte)255, width * height * 4).ToArray();

    private static void Place(byte[] source, int width, int height, byte[] target, int targetWidth, int x, int y) {
        for (var row = 0; row < height; row++)
            Array.Copy(source, row * width * 4, target, ((y + row) * targetWidth + x) * 4, width * 4);
    }
}
