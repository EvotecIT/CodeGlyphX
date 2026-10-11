using System;
using System.IO;
using System.Threading;
using CodeGlyphX.DataBar;
using CodeGlyphX.Internal;
using CodeGlyphX.Rendering;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("ImageScannerSerial")]
public sealed class DataBarStackedImageScannerTests {
    [Theory]
    [InlineData(false, 0, false, 1)]
    [InlineData(false, 1, true, 3)]
    [InlineData(false, 2, false, 2)]
    [InlineData(false, 3, true, 1)]
    [InlineData(true, 0, true, 2)]
    [InlineData(true, 1, false, 1)]
    [InlineData(true, 2, true, 3)]
    [InlineData(true, 3, false, 2)]
    public void Scanner_RecognizesLayoutsRotationsAndPolarity(bool omni, int turns, bool inverted, int scale) {
        const string input = "0082193510642";
        var matrix = omni ? DataBar14Encoder.EncodeStackedOmnidirectional(input) : DataBar14Encoder.EncodeStacked(input);
        var pixels = Render(matrix, scale, out var width, out var height);
        for (var turn = 0; turn < turns; turn++) pixels = Rotate(pixels, ref width, ref height);
        if (inverted) for (var i = 0; i < pixels.Length; i += 4) pixels[i] = pixels[i + 1] = pixels[i + 2] = (byte)(255 - pixels[i]);
        var result = Scan(pixels, width, height);
        var symbol = Assert.Single(result.Symbols);
        Assert.Equal(omni ? SymbolFormat.Gs1DataBarStackedOmnidirectional : SymbolFormat.Gs1DataBarStacked, symbol.Format);
        Assert.Equal(input, symbol.Text);
        Assert.Equal(SymbolPayloadProfile.Gs1, symbol.PayloadProfile);
        Assert.Empty(result.UnsupportedFormats);
        Assert.True(SymbolCapabilities.Get(symbol.Format).CanScanImages);
        Assert.False(SymbolCapabilities.Get(symbol.Format).CanScanMultiple);
        // A narrow physical-format request must not accept the other separator layout.
        var options = Options();
        options.Formats = new[] { omni ? SymbolFormat.Gs1DataBarStacked : SymbolFormat.Gs1DataBarStackedOmnidirectional };
        Assert.Empty(SymbolScanner.Scan(ImageFrame.Packed(pixels, width, height, PixelFormat.Rgba32), options).Symbols);
    }

    [Theory]
    [InlineData("zxing-databar-stacked-2.png", "0082193510642")]
    [InlineData("zxing-databar-stacked-5.png", "0003456789012")]
    [InlineData("zxing-databar-stacked-photo.png", "0001234567890")]
    public void Scanner_RecognizesIndependentEncodedImages(string fixture, string expected) {
        using var stream = typeof(DataBarStackedImageScannerTests).Assembly.GetManifestResourceStream("CodeGlyphX.Tests.Fixtures.Linear." + fixture);
        Assert.NotNull(stream);
        using var bytes = new MemoryStream();
        stream!.CopyTo(bytes);
        var result = SymbolScanner.Scan(bytes.ToArray(), Options());
        var symbol = Assert.Single(result.Symbols);
        Assert.Equal(expected, symbol.Text);
        // The upstream oracle specifies RSS-14 payloads, without a physical variant.
        // Synthetic layout tests above independently establish variant selection.
        Assert.Contains(symbol.Format, new[] { SymbolFormat.Gs1DataBarStacked, SymbolFormat.Gs1DataBarStackedOmnidirectional });
        var defaults = Options(); defaults.Formats = null;
        var defaultSymbol = Assert.Single(SymbolScanner.Scan(bytes.ToArray(), defaults).Symbols);
        Assert.Equal(expected, defaultSymbol.Text);
        Assert.Equal(symbol.Format, defaultSymbol.Format);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Scanner_CompositesAlphaAgainstWhite(bool omni) {
        var matrix = omni ? DataBar14Encoder.EncodeStackedOmnidirectional("0001234567890") : DataBar14Encoder.EncodeStacked("0001234567890");
        var pixels = Render(matrix, 2, out var width, out var height);
        for (var i = 3; i < pixels.Length; i += 4) pixels[i] = 0;
        Assert.Empty(Scan(pixels, width, height).Symbols);
        // Partially transparent bars remain visible against the white background.
        for (var i = 3; i < pixels.Length; i += 4) pixels[i] = 96;
        Assert.Equal("0001234567890", Assert.Single(Scan(pixels, width, height).Symbols).Text);
    }

    [Fact]
    public void Scanner_ClassifiesVisibleBarHeightWithoutTransparentRgbExtensions() {
        const string input = "0001234567890";
        var pixels = CodeGlyphX.Rendering.Png.BarcodePngRenderer.RenderPixels(DataBar14Encoder.EncodeTruncated(input),
            new CodeGlyphX.Rendering.Png.BarcodePngRenderOptions { ModuleSize = 2, QuietZone = 10, HeightModules = 40 },
            out var width, out var height, out _);
        // Keep thirteen modules of visible bars, with invisible copies extending below them.
        for (var y = 0; y < height; y++) for (var x = 0; x < width; x++) if (y < 27 || y >= 53) pixels[(y * width + x) * 4 + 3] = 0;
        var options = Options(); options.Formats = new[] { SymbolFormat.Gs1DataBarTruncated, SymbolFormat.Gs1DataBarOmnidirectional };
        var symbol = Assert.Single(SymbolScanner.Scan(ImageFrame.Packed(pixels, width, height, PixelFormat.Rgba32), options).Symbols);
        Assert.Equal(input, symbol.Text);
        Assert.Equal(SymbolFormat.Gs1DataBarTruncated, symbol.Format);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Scanner_RejectsUnrelatedRowsAndMissingSeparators(bool unrelated) {
        var matrix = DataBar14Encoder.EncodeStackedOmnidirectional("0001234567890");
        var other = DataBar14Encoder.EncodeStackedOmnidirectional("0082193510642");
        for (var y = 0; y < matrix.Height; y++) for (var x = 0; x < matrix.Width; x++) {
            if (unrelated && y == matrix.Height - 1) matrix[x, y] = other[x, y];
            if (!unrelated && y > 0 && y < matrix.Height - 1) matrix[x, y] = false;
        }
        var pixels = Render(matrix, 3, out var width, out var height);
        var result = Scan(pixels, width, height);
        Assert.Empty(result.Symbols);
        Assert.Equal(ScanCompletionReason.Completed, result.CompletionReason);
    }

    [Fact]
    public void RowSearch_StopsAtMonotonicDeadlineAndReturnsPooledBuffer() {
        const int size = 1600;
        var noisy = new byte[size * size * 4];
        for (var y = 0; y < size; y++) for (var x = 0; x < size; x++) {
            var at = (y * size + x) * 4;
            noisy[at] = noisy[at + 1] = noisy[at + 2] = (byte)(((x / 2 + y / 3) & 1) * 255);
            noisy[at + 3] = 255;
        }
        using (DecodeBudget.Begin(20)) {
            Assert.False(DataBar14ImageDecoder.TryDecode(noisy, size, size, true, true, CancellationToken.None, out _, out _));
            Assert.True(DecodeBudget.IsExpired);
        }
        var pixels = Render(DataBar14Encoder.EncodeStacked("0001234567890"), 2, out var width, out var height);
        Assert.Equal("0001234567890", Assert.Single(Scan(pixels, width, height).Symbols).Text);
        var options = Options();
        using var source = new CancellationTokenSource();
        source.Cancel(); options.CancellationToken = source.Token;
        Assert.Equal(ScanCompletionReason.Cancelled, SymbolScanner.Scan(ImageFrame.Packed(pixels, width, height, PixelFormat.Rgba32), options).CompletionReason);
    }

    private static ScanOptions Options() {
        var options = ScanOptions.Robust(5000);
        options.Formats = new[] { SymbolFormat.Gs1DataBarStacked, SymbolFormat.Gs1DataBarStackedOmnidirectional };
        options.MaxSymbols = 1;
        options.Image = new ImageDecodeOptions { MaxDimension = 0 };
        return options;
    }

    private static ScanResult Scan(byte[] pixels, int width, int height) => SymbolScanner.Scan(ImageFrame.Packed(pixels, width, height, PixelFormat.Rgba32), Options());

    private static byte[] Render(BitMatrix matrix, int scale, out int width, out int height) {
        const int quiet = 4;
        var rowHeights = matrix.Height == 5 ? new[] { 33, 1, 1, 1, 33 } : new[] { 5, 1, 7 };
        var total = quiet * 2;
        foreach (var value in rowHeights) total += value;
        width = (50 + quiet * 2) * scale; height = total * scale;
        var pixels = new byte[width * height * 4];
        for (var i = 0; i < pixels.Length; i++) pixels[i] = 255;
        var rowStart = quiet * scale;
        for (var row = 0; row < matrix.Height; row++) {
            for (var y = rowStart; y < rowStart + rowHeights[row] * scale; y++) for (var x = 0; x < 50 * scale; x++) {
                var at = (y * width + quiet * scale + x) * 4;
                if (matrix[x / scale, row]) pixels[at] = pixels[at + 1] = pixels[at + 2] = 0;
            }
            rowStart += rowHeights[row] * scale;
        }
        return pixels;
    }

    private static byte[] Rotate(byte[] pixels, ref int width, ref int height) {
        var output = new byte[pixels.Length];
        for (var y = 0; y < height; y++) for (var x = 0; x < width; x++) Buffer.BlockCopy(pixels, (y * width + x) * 4, output, (x * height + height - 1 - y) * 4, 4);
        (width, height) = (height, width);
        return output;
    }
}
