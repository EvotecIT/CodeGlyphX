using System;
using System.Linq;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("ImageScannerSerial")]
public sealed class SymbolScannerTileTests {
    [Fact]
    public void FindingQrDoesNotExcludeOtherRequestedMatrixFamilies() {
        var qr = QrCodeEncoder.EncodeText("MIXED-QR").Modules;
        var dm = DataMatrix.DataMatrixEncoder.Encode("MIXED-DM");
        var render = new MatrixPngRenderOptions { ModuleSize = 5, QuietZone = 4 };
        var a = MatrixPngRenderer.RenderPixels(qr, render, out var aw, out var ah, out _);
        var b = MatrixPngRenderer.RenderPixels(dm, render, out var bw, out var bh, out _);
        var cellWidth = Math.Max(aw, bw) + 32;
        var cellHeight = Math.Max(ah, bh) + 32;
        var width = cellWidth * 2;
        var height = cellHeight * 2;
        var canvas = Enumerable.Repeat((byte)255, width * height * 4).ToArray();
        for (var row = 0; row < ah; row++) Array.Copy(a, row * aw * 4, canvas, ((row + 16) * width + 16) * 4, aw * 4);
        for (var row = 0; row < bh; row++) Array.Copy(b, row * bw * 4, canvas, ((row + cellHeight + 16) * width + cellWidth + 16) * 4, bw * 4);
        var result = SymbolScanner.Scan(ImageFrame.Packed(canvas, width, height, PixelFormat.Rgba32), new ScanOptions {
            Formats = new[] { SymbolFormat.QrCode, SymbolFormat.DataMatrix }, EnableTileScan = true, TileGrid = 2,
            TimeoutMilliseconds = TestBudget.Adjust(10000)
        });
        Assert.Contains(result.Symbols, symbol => symbol.Format == SymbolFormat.QrCode && symbol.Text == "MIXED-QR");
        Assert.Contains(result.Symbols, symbol => symbol.Format == SymbolFormat.DataMatrix && symbol.Text == "MIXED-DM");
    }

    [Theory]
    [InlineData(SymbolFormat.DataMatrix)]
    [InlineData(SymbolFormat.Pdf417)]
    [InlineData(SymbolFormat.Aztec)]
    public void MatrixTileSearchPreservesMultiplePayloadsAndSourceRegions(SymbolFormat format) {
        BitMatrix Encode(string text) => format switch {
            SymbolFormat.DataMatrix => DataMatrix.DataMatrixEncoder.Encode(text),
            SymbolFormat.Pdf417 => Pdf417.Pdf417Encoder.Encode(text),
            SymbolFormat.Aztec => Aztec.AztecEncoder.Encode(text),
            _ => throw new ArgumentOutOfRangeException(nameof(format))
        };
        var render = new MatrixPngRenderOptions { ModuleSize = 4, QuietZone = 4 };
        var left = MatrixPngRenderer.RenderPixels(Encode("TILE-LEFT"), render, out var lw, out var lh, out _);
        var right = MatrixPngRenderer.RenderPixels(Encode("TILE-RIGHT"), render, out var rw, out var rh, out _);
        const int border = 24;
        var cellWidth = Math.Max(lw, rw) + border * 2;
        var cellHeight = Math.Max(lh, rh) + border * 2;
        var width = cellWidth * 2 + border * 2;
        var height = cellHeight * 2 + border * 2;
        var canvas = Enumerable.Repeat((byte)255, width * height * 4).ToArray();
        void Place(byte[] image, int w, int h, int x, int y) {
            for (var row = 0; row < h; row++) Array.Copy(image, row * w * 4, canvas, ((y + row) * width + x) * 4, w * 4);
        }
        Place(left, lw, lh, border * 2, border * 2);
        Place(right, rw, rh, cellWidth + border * 2, cellHeight + border * 2);
        var region = new ImageRegion(border, border, cellWidth * 2, cellHeight * 2);
        var result = SymbolScanner.Scan(ImageFrame.Packed(canvas, width, height, PixelFormat.Rgba32), new ScanOptions {
            Formats = new[] { format }, Region = region, EnableTileScan = true, TileGrid = 2,
            TimeoutMilliseconds = TestBudget.Adjust(10000)
        });
        Assert.Contains(result.Symbols, symbol => symbol.Text == "TILE-LEFT");
        Assert.Contains(result.Symbols, symbol => symbol.Text == "TILE-RIGHT");
        Assert.Equal(2, result.Symbols.Count);
        Assert.All(result.Symbols, symbol => {
            Assert.Equal(format, symbol.Format);
            Assert.NotNull(symbol.SearchRegion);
            var searched = symbol.SearchRegion!.Value;
            Assert.InRange(searched.X, region.X, region.Right - 1);
            Assert.InRange(searched.Y, region.Y, region.Bottom - 1);
            Assert.True(searched.Right <= region.Right && searched.Bottom <= region.Bottom);
        });
        Assert.Contains(result.Symbols, symbol => symbol.SearchRegion != region);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    [InlineData(5)]
    public void InvalidTileGridIsRejectedBeforeRecognition(int grid) {
        Assert.Throws<ArgumentOutOfRangeException>(() => SymbolScanner.Scan(new byte[1], new ScanOptions { TileGrid = grid }));
    }

    [Fact]
    public void NegativePresetDeadlineDoesNotDisableTheGuard() {
        Assert.Throws<ArgumentOutOfRangeException>(() => ScanOptions.Balanced(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => ScanOptions.Screen(maxDimension: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => SymbolScanner.Scan(new byte[1], new ScanOptions {
            Image = new ImageDecodeOptions { MaxBytes = -1 }
        }));
    }
}
