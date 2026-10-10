using System;
using System.Threading;
using CodeGlyphX.DataBar;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class DataBarExpandedImageScannerTests {
    [Theory]
    [InlineData("(01)12345678901231", 2, 0, false, 0)]
    [InlineData("(01)98898765432106(3103)000001", 2, 1, true, 6)]
    [InlineData("(91)ABCDEFGHIJKLMNOPQRST", 4, 2, false, 0)]
    [InlineData("(91)ABCDEFGHIJKLMNOPQRST", 3, 3, true, 6)]
    [InlineData("(91)ABCDEFGHIJKLMNOPQRSTUVWXYZABCDEFGHIJKLM", 1, 0, false, 6)]
    [InlineData("(91)ABCDEFGHIJKLMNOPQRSTUVWXYZABCDEFGHIJKLM", 5, 1, false, 0)]
    [InlineData("(10)123", 10, 2, true, 0)]
    public void Scanner_RecognizesStackedLayoutsRotationsAndPolarity(string input, int columns, int turns, bool inverted, int quiet) {
        var matrix = DataBarExpandedEncoder.EncodeExpandedStacked(input, columns);
        var pixels = Render(matrix, quiet, out var width, out var height);
        for (var turn = 0; turn < turns; turn++) pixels = Rotate(pixels, ref width, ref height);
        if (inverted) for (var i = 0; i < pixels.Length; i += 4)
            pixels[i] = pixels[i + 1] = pixels[i + 2] = (byte)(255 - pixels[i]);

        var result = Scan(pixels, width, height);

        var symbol = Assert.Single(result.Symbols);
        Assert.Equal(SymbolFormat.Gs1DataBarExpandedStacked, symbol.Format);
        Assert.Equal(Gs1.ElementString(input), symbol.Text);
        Assert.Equal(SymbolPayloadProfile.Gs1, symbol.PayloadProfile);
        Assert.Empty(result.UnsupportedFormats);
        Assert.True(SymbolCapabilities.Get(symbol.Format).CanScanImages);
        Assert.False(SymbolCapabilities.Get(symbol.Format).CanScanMultiple);
    }

    [Fact]
    public void Scanner_RecognizesIndependentStackedModulesThroughEncodedImage() {
        // Independent Zint 2.13.0 fixture, shared with module conformance proof.
        var rows = new[] {
            "518E2FF0A270DBEC4BC0EF4C74", "0E71D00A5D8F2413B42A10B380",
            "05555555555555555555555540", "03076A052FE740000000000000",
            "2CF891F85018A8000000000000"
        };
        var matrix = new BitMatrix(102, 5);
        for (var y = 0; y < rows.Length; y++)
            for (var x = 0; x < matrix.Width; x++) {
                var value = Convert.ToByte(rows[y].Substring((x / 8) * 2, 2), 16);
                matrix[x, y] = (value & (1 << (7 - x % 8))) != 0;
            }
        var png = MatrixPngRenderer.Render(matrix, new MatrixPngRenderOptions { ModuleSize = 3, QuietZone = 4 });
        var result = SymbolScanner.Scan(png, Options());
        Assert.Equal("01988987654321063103000001", Assert.Single(result.Symbols).Text);
    }

    [Fact]
    public void Scanner_RejectsTruncatedStackedRowsInsteadOfReturningPartialPayload() {
        var matrix = DataBarExpandedEncoder.EncodeExpandedStacked("(91)ABCDEFGHIJKLMNOPQRST", 2);
        var pixels = Render(matrix, 6, out var width, out var height);
        // Remove the final data row, leaving complete earlier rows and separators.
        height -= 38;
        Array.Resize(ref pixels, width * height * 4);
        var result = Scan(pixels, width, height);
        Assert.Empty(result.Symbols);
        Assert.Equal(ScanStatus.NoSymbolFound, result.Status);
        Assert.Equal(ScanCompletionReason.Completed, result.CompletionReason);
    }

    [Fact]
    public void Scanner_HonorsCancellationForStackedImageRequest() {
        var matrix = DataBarExpandedEncoder.EncodeExpandedStacked("(91)ABCDEFGHIJKLMNOPQRST", 2);
        var pixels = Render(matrix, 6, out var width, out var height);
        using var source = new CancellationTokenSource();
        source.Cancel();
        var options = Options();
        options.CancellationToken = source.Token;
        var result = SymbolScanner.Scan(ImageFrame.Packed(pixels, width, height, PixelFormat.Rgba32), options);
        Assert.Empty(result.Symbols);
        Assert.Equal(ScanCompletionReason.Cancelled, result.CompletionReason);
    }

    private static ScanOptions Options() => new() {
        Formats = new[] { SymbolFormat.Gs1DataBarExpandedStacked }, MaxSymbols = 1,
        TimeoutMilliseconds = 5000, EnableTileScan = false, Image = new ImageDecodeOptions { MaxDimension = 0 }
    };

    private static ScanResult Scan(byte[] pixels, int width, int height) =>
        SymbolScanner.Scan(ImageFrame.Packed(pixels, width, height, PixelFormat.Rgba32), Options());

    private static byte[] Render(BitMatrix matrix, int quiet, out int width, out int height) {
        width = matrix.Width * 2 + quiet * 2;
        height = quiet * 2;
        for (var y = 0; y < matrix.Height; y++) height += y % 4 == 0 ? 32 : 2;
        var pixels = new byte[width * height * 4];
        for (var i = 0; i < pixels.Length; i++) pixels[i] = 255;
        var top = quiet;
        for (var y = 0; y < matrix.Height; y++) {
            var rowHeight = y % 4 == 0 ? 32 : 2;
            for (var py = top; py < top + rowHeight; py++)
                for (var x = 0; x < matrix.Width; x++) if (matrix[x, y])
                    for (var dx = 0; dx < 2; dx++) {
                        var at = (py * width + quiet + x * 2 + dx) * 4;
                        pixels[at] = pixels[at + 1] = pixels[at + 2] = 0;
                    }
            top += rowHeight;
        }
        return pixels;
    }

    private static byte[] Rotate(byte[] pixels, ref int width, ref int height) {
        var rotated = new byte[pixels.Length];
        for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
                Buffer.BlockCopy(pixels, (y * width + x) * 4, rotated, (x * height + height - 1 - y) * 4, 4);
        var previousWidth = width; width = height; height = previousWidth;
        return rotated;
    }
}
