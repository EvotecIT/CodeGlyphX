using System;
using System.IO;
using System.Linq;
using CodeGlyphX.DataBar;
using CodeGlyphX.Rendering;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("ImageScannerSerial")]
public sealed class DataBarSeparatorGeometryTests {
    [Theory]
    [InlineData("20", 0, false)] [InlineData("20", 1, false)]
    [InlineData("20", 2, false)] [InlineData("20", 3, false)]
    [InlineData("20", 0, true)] [InlineData("20", 1, true)]
    [InlineData("20", 2, true)] [InlineData("20", 3, true)]
    [InlineData("21", 0, false)] [InlineData("21", 1, false)]
    [InlineData("21", 2, false)] [InlineData("21", 3, false)]
    [InlineData("21", 0, true)] [InlineData("21", 1, true)]
    [InlineData("21", 2, true)] [InlineData("21", 3, true)]
    public void Scanner_PhotographedSeparator_PreservesPayloadAndPhysicalVariant(string fixture, int turns, bool inverted) {
        var bytes = Fixture(fixture);
        var pixels = ImageReader.DecodeRgba32(bytes, out var width, out var height);
        Transform(ref pixels, ref width, ref height, turns, inverted);
        var before = (byte[])pixels.Clone();
        var options = Options();
        var symbol = Assert.Single(SymbolScanner.Scan(ImageFrame.Packed(pixels, width, height, PixelFormat.Rgba32), options).Symbols);
        Assert.Equal("0001234567890", symbol.Text);
        Assert.Equal(SymbolFormat.Gs1DataBarStacked, symbol.Format);
        Assert.Equal(SymbolPayloadProfile.Gs1, symbol.PayloadProfile);
        // A fitted single-row separator cannot stand in for the distinct
        // three-row omnidirectional separator requested by this caller.
        options.Formats = new[] { SymbolFormat.Gs1DataBarStackedOmnidirectional };
        Assert.Empty(SymbolScanner.Scan(ImageFrame.Packed(pixels, width, height, PixelFormat.Rgba32), options).Symbols);
        Assert.Equal(before, pixels);
        if (turns == 0 && !inverted)
            Assert.Equal("0001234567890", Assert.Single(SymbolScanner.Scan(bytes, Options()).Symbols).Text);
    }

    [Theory]
    [InlineData("0001234567890", false)] [InlineData("0001234567890", true)]
    [InlineData("0082193510642", false)] [InlineData("0082193510642", true)]
    [InlineData("1234567890123", false)] [InlineData("1234567890123", true)]
    public void Scanner_SlopedSeparator_PreservesBothLayoutsAndRejectsMissingEvidence(string text, bool omni) {
        var matrix = omni ? DataBar14Encoder.EncodeStackedOmnidirectional(text) : DataBar14Encoder.EncodeStacked(text);
        foreach (var erase in new[] { false, true }) {
            var pixels = Render(matrix, erase, out var width, out var height);
            foreach (var direction in new[] { -1, 1 }) {
                var tilted = Shear(pixels, width, height, direction * 16);
                var options = Options();
                var result = SymbolScanner.Scan(ImageFrame.Packed(tilted, width, height, PixelFormat.Rgba32), options);
                if (erase) {
                    Assert.Empty(result.Symbols);
                    Assert.Equal(ScanCompletionReason.Completed, result.CompletionReason);
                } else {
                    var symbol = Assert.Single(result.Symbols);
                    Assert.Equal(text, symbol.Text);
                    Assert.Equal(omni ? SymbolFormat.Gs1DataBarStackedOmnidirectional : SymbolFormat.Gs1DataBarStacked, symbol.Format);
                    options.Formats = new[] { omni ? SymbolFormat.Gs1DataBarStacked : SymbolFormat.Gs1DataBarStackedOmnidirectional };
                    Assert.Empty(SymbolScanner.Scan(ImageFrame.Packed(tilted, width, height, PixelFormat.Rgba32), options).Symbols);
                }
            }
        }
    }

    private static ScanOptions Options() {
        var options = ScanOptions.Robust(0);
        options.Formats = new[] { SymbolFormat.Gs1DataBarStacked, SymbolFormat.Gs1DataBarStackedOmnidirectional };
        options.MaxSymbols = 1; options.EnableTileScan = false;
        return options;
    }

    private static byte[] Render(BitMatrix matrix, bool eraseSeparator, out int width, out int height) {
        const int scale = 4, quiet = 4;
        var heights = matrix.Height == 5 ? new[] { 33, 1, 1, 1, 33 } : new[] { 5, 1, 7 };
        width = (matrix.Width + quiet * 2) * scale;
        height = (heights.Sum() + quiet * 2) * scale;
        var pixels = Enumerable.Repeat((byte)255, width * height * 4).ToArray();
        var top = quiet * scale;
        for (var row = 0; row < matrix.Height; row++) {
            for (var y = top; y < top + heights[row] * scale; y++)
                for (var x = 0; x < matrix.Width * scale; x++) {
                    if (!matrix[x / scale, row] || (eraseSeparator && row > 0 && row < matrix.Height - 1)) continue;
                    var p = (y * width + quiet * scale + x) * 4;
                    pixels[p] = pixels[p + 1] = pixels[p + 2] = 0;
                }
            top += heights[row] * scale;
        }
        return pixels;
    }

    private static byte[] Shear(byte[] pixels, int width, int height, int skew) {
        var output = Enumerable.Repeat((byte)255, pixels.Length).ToArray();
        for (var y = 0; y < height; y++) for (var x = 0; x < width; x++) {
            var sourceY = y - (int)Math.Round((x - (width - 1) / 2.0) * skew / width);
            if ((uint)sourceY < (uint)height) Buffer.BlockCopy(pixels, (sourceY * width + x) * 4, output, (y * width + x) * 4, 4);
        }
        return output;
    }

    private static void Transform(ref byte[] pixels, ref int width, ref int height, int turns, bool inverted) {
        if (inverted) for (var p = 0; p < pixels.Length; p += 4)
            for (var channel = 0; channel < 3; channel++) pixels[p + channel] = (byte)(255 - pixels[p + channel]);
        for (var turn = 0; turn < turns; turn++) {
            var output = new byte[pixels.Length];
            for (var y = 0; y < height; y++) for (var x = 0; x < width; x++)
                Buffer.BlockCopy(pixels, (y * width + x) * 4, output, (x * height + height - 1 - y) * 4, 4);
            pixels = output; var previousWidth = width; width = height; height = previousWidth;
        }
    }

    private static byte[] Fixture(string number) {
        var assembly = typeof(DataBarSeparatorGeometryTests).Assembly;
        var name = assembly.GetManifestResourceNames().Single(n => n.EndsWith(".zxing-databar-stacked-separator-" + number + ".png", StringComparison.Ordinal));
        using var stream = assembly.GetManifestResourceStream(name)!;
        using var memory = new MemoryStream(); stream.CopyTo(memory); return memory.ToArray();
    }
}
