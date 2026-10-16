using System;
using System.Linq;
using CodeGlyphX.Aztec;
using CodeGlyphX.DataMatrix;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("ImageScannerSerial")]
public sealed class MultiSymbolImageTests {
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void Scan_ComposedMixedFamilies_ReturnsEveryPayload(bool encoded, bool defaultFormats) {
        var pixels = MixedCanvas(out var width, out var height);
        var options = Options();
        if (defaultFormats) options.Formats = null;
        var scan = encoded
            ? SymbolScanner.Scan(EncodePng(pixels, width, height), options)
            : SymbolScanner.Scan(ImageFrame.Packed(pixels, width, height, PixelFormat.Rgba32), options);
        Assert.Equal(new[] { "AZTEC", "CODE128", "DATA-MATRIX", "QR" }, scan.Symbols.Select(s => s.Text).OrderBy(s => s, StringComparer.Ordinal));
        Assert.Equal(new[] { SymbolFormat.QrCode, SymbolFormat.DataMatrix, SymbolFormat.Aztec, SymbolFormat.Code128 }.OrderBy(f => f),
            scan.Symbols.Select(s => s.Format).OrderBy(f => f));
        Assert.Equal(ScanCompletionReason.SymbolLimitReached, scan.CompletionReason);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void Scan_ThreeQrCodes_ReturnsEveryPayload(int grid) {
        const int width = 840, height = 420;
        var pixels = White(width, height);
        PasteQr("QR-LEFT", pixels, width, 45, 30);
        PasteQr("QR-CENTRE", pixels, width, 340, 130);
        PasteQr("QR-RIGHT", pixels, width, 635, 250);
        var options = Options(SymbolFormat.QrCode);
        options.TileGrid = grid;
        options.MaxSymbols = 3;
        var scan = SymbolScanner.Scan(ImageFrame.Packed(pixels, width, height, PixelFormat.Rgba32), options);
        Assert.Equal(new[] { "QR-CENTRE", "QR-LEFT", "QR-RIGHT" }, scan.Symbols.Select(s => s.Text).OrderBy(s => s, StringComparer.Ordinal));
        Assert.Equal(ScanCompletionReason.SymbolLimitReached, scan.CompletionReason);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(0)]
    public void Scan_MixedFamilies_RespectsMaximum(int maximum) {
        var pixels = MixedCanvas(out var width, out var height);
        var options = Options();
        options.MaxSymbols = maximum;
        var scan = SymbolScanner.Scan(ImageFrame.Packed(pixels, width, height, PixelFormat.Rgba32), options);
        Assert.Equal(maximum == 0 ? 4 : maximum, scan.Symbols.Count);
        if (maximum > 0) Assert.Equal(ScanCompletionReason.SymbolLimitReached, scan.CompletionReason);
    }

    [Fact]
    public void Scan_ThreeDataMatrixCodesInsideOneGridCell_ReturnsAllThree() {
        const int width = 840, height = 420;
        var pixels = White(width, height);
        PasteMatrix(DataMatrixEncoder.Encode("DM-LEFT"), pixels, width, 30, 80);
        PasteMatrix(DataMatrixEncoder.Encode("DM-CENTRE"), pixels, width, 170, 80);
        PasteMatrix(DataMatrixEncoder.Encode("DM-RIGHT"), pixels, width, 310, 80);
        var options = Options(SymbolFormat.DataMatrix);
        options.TileGrid = 2;
        options.MaxSymbols = 3;
        var scan = SymbolScanner.Scan(ImageFrame.Packed(pixels, width, height, PixelFormat.Rgba32), options);
        Assert.Equal(new[] { "DM-CENTRE", "DM-LEFT", "DM-RIGHT" }, scan.Symbols.Select(s => s.Text).OrderBy(s => s, StringComparer.Ordinal));
        Assert.Equal(ScanCompletionReason.SymbolLimitReached, scan.CompletionReason);
    }

    [Fact]
    public void Scan_SeparatedMicroQr_PreservesPayloadMetadataAndSourceGeometryAfterDownscale() {
        Assert.True(SymbolCapabilities.Get(SymbolFormat.MicroQrCode).CanScanMultiple);
        const int width = 840, height = 420;
        var pixels = White(width, height);
        foreach (var position in new[] { (Text: "ONE", X: 100, Y: 40), (Text: "TWO", X: 500, Y: 230) }) {
            var matrix = MicroQrCodeEncoder.EncodeAlphanumeric(position.Text, minVersion: 4, maxVersion: 4);
            var code = MatrixPngRenderer.RenderPixels(matrix.Modules,
                new MatrixPngRenderOptions { ModuleSize = 6, QuietZone = 2 }, out var w, out var h, out var stride);
            Paste(code, w, h, stride, pixels, width, position.X, position.Y);
        }
        var options = Options(SymbolFormat.MicroQrCode);
        options.MaxSymbols = 2;
        options.Region = new ImageRegion(60, 20, 730, 380);
        options.Image = new ImageDecodeOptions { MaxDimension = 365 };
        var scan = SymbolScanner.Scan(ImageFrame.Packed(pixels, width, height, PixelFormat.Rgba32), options);
        Assert.Equal(new[] { "ONE", "TWO" }, scan.Symbols.Select(s => s.Text).OrderBy(s => s, StringComparer.Ordinal));
        foreach (var symbol in scan.Symbols) {
            Assert.IsType<MicroQrSymbolMetadata>(symbol.Metadata);
            Assert.Equal(System.Text.Encoding.ASCII.GetBytes(symbol.Text), symbol.RawBytes.ToArray());
            Assert.NotNull(symbol.Geometry);
            var expectedX = symbol.Text == "ONE" ? 112 : 512;
            var expectedY = symbol.Text == "ONE" ? 52 : 242;
            Assert.InRange(symbol.Geometry!.Bounds.X, expectedX - 3, expectedX + 3);
            Assert.InRange(symbol.Geometry.Bounds.Y, expectedY - 3, expectedY + 3);
            Assert.True(symbol.SearchRegion!.Value.X >= 60);
            Assert.True(symbol.SearchRegion.Value.Y >= 20);
        }
    }

    [Fact]
    public void Scan_ExplicitQrTileOptOut_RetainsWholeFrameQrSearchRegion() {
        var pixels = MixedCanvas(out var width, out var height);
        var options = Options();
        options.Qr = new QrPixelDecodeOptions { EnableTileScan = false };
        var scan = SymbolScanner.Scan(ImageFrame.Packed(pixels, width, height, PixelFormat.Rgba32), options);
#if NET8_0_OR_GREATER
        Assert.Equal(new ImageRegion(0, 0, width, height), Assert.Single(scan.Symbols, s => s.Format == SymbolFormat.QrCode).SearchRegion);
        Assert.Equal(4, scan.Symbols.Count);
#else
        // The legacy decoder may need tiles to recognize the QR in a composed
        // image. With tiles disabled, any QR result must come from the full frame.
        Assert.DoesNotContain(scan.Symbols, s => s.Format == SymbolFormat.QrCode && s.SearchRegion != new ImageRegion(0, 0, width, height));
        Assert.Equal(3, scan.Symbols.Count(s => s.Format != SymbolFormat.QrCode));
#endif
    }

    internal static byte[] MixedCanvas(out int width, out int height) {
        width = 840; height = 420;
        var pixels = White(width, height);
        PasteQr("QR", pixels, width, 25, 30);
        PasteMatrix(DataMatrixEncoder.Encode("DATA-MATRIX"), pixels, width, 250, 90);
        PasteMatrix(AztecEncoder.Encode("AZTEC"), pixels, width, 510, 250);
        var bars = BarcodePngRenderer.RenderPixels(Barcode.Encode(SymbolFormat.Code128, "CODE128"),
            new BarcodePngRenderOptions { ModuleSize = 2, HeightModules = 40, QuietZone = 10 }, out var w, out var h, out var stride);
        Paste(bars, w, h, stride, pixels, width, 60, 285);
        return pixels;
    }

    private static ScanOptions Options(params SymbolFormat[] formats) => new() {
        Formats = formats.Length == 0 ? new[] { SymbolFormat.QrCode, SymbolFormat.DataMatrix, SymbolFormat.Aztec, SymbolFormat.Code128 } : formats,
        TimeoutMilliseconds = TestBudget.Adjust(5000),
        MaxSymbols = 4,
        EnableTileScan = true
    };

    private static void PasteQr(string text, byte[] canvas, int width, int x, int y) {
        var pixels = QR.RenderPixels(text, out var w, out var h, out var stride, new QrRenderOptions { ModuleSize = 4 });
        Paste(pixels, w, h, stride, canvas, width, x, y);
    }

    private static void PasteMatrix(BitMatrix matrix, byte[] canvas, int width, int x, int y) {
        var pixels = MatrixPngRenderer.RenderPixels(matrix, new MatrixPngRenderOptions { ModuleSize = 4 }, out var w, out var h, out var stride);
        Paste(pixels, w, h, stride, canvas, width, x, y);
    }

    private static byte[] White(int width, int height) {
        var pixels = new byte[width * height * 4];
        for (var i = 0; i < pixels.Length; i++) pixels[i] = 255;
        return pixels;
    }

    private static byte[] EncodePng(byte[] pixels, int width, int height) {
        var rows = new byte[(width * 4 + 1) * height];
        for (var y = 0; y < height; y++)
            Buffer.BlockCopy(pixels, y * width * 4, rows, y * (width * 4 + 1) + 1, width * 4);
        return PngWriter.WriteRgba8(width, height, rows);
    }

    private static void Paste(byte[] source, int width, int height, int stride, byte[] canvas, int canvasWidth, int x, int y) {
        for (var row = 0; row < height; row++)
            Buffer.BlockCopy(source, row * stride, canvas, ((y + row) * canvasWidth + x) * 4, width * 4);
    }
}
