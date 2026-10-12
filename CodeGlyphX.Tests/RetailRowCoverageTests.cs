using System;
using System.IO;
using System.Linq;
using CodeGlyphX.Rendering;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("ImageScannerSerial")]
public sealed class RetailRowCoverageTests {
    [Theory]
    [InlineData("zxing-ean-glare-adjacent-rows.png", "9780441014989", false, false)]
    [InlineData("zxing-ean-glare-adjacent-rows.png", "9780441014989", false, true)]
    [InlineData("zxing-ean-glare-adjacent-rows.png", "9780441014989", true, false)]
    [InlineData("zxing-ean-glare-adjacent-rows.png", "9780441014989", true, true)]
    [InlineData("zxing-ean-fine-bars.png", "9780345348036", false, false)]
    [InlineData("zxing-ean-fine-bars.png", "9780345348036", false, true)]
    [InlineData("zxing-ean-fine-bars.png", "9780345348036", true, false)]
    [InlineData("zxing-ean-fine-bars.png", "9780345348036", true, true)]
    [InlineData("zxing-ean-glare-threshold.png", "9780441014989", false, false)]
    [InlineData("zxing-ean-glare-threshold.png", "9780441014989", false, true)]
    [InlineData("zxing-ean-glare-threshold.png", "9780441014989", true, false)]
    [InlineData("zxing-ean-glare-threshold.png", "9780441014989", true, true)]
    public void Decode_IndependentRetailPhotograph_RecoversCorroboratedRows(string name, string expected, bool vertical, bool inverted) {
        using var fixture = typeof(RetailRowCoverageTests).Assembly.GetManifestResourceStream(
            "CodeGlyphX.Tests.Fixtures.Linear." + name);
        Assert.NotNull(fixture);
        using var buffer = new MemoryStream(); fixture!.CopyTo(buffer);
        var pixels = ImageReader.DecodeRgba32(buffer.ToArray(), out var width, out var height);
        if (vertical) {
            var rotated = new byte[pixels.Length];
            for (var y = 0; y < height; y++) for (var x = 0; x < width; x++)
                Buffer.BlockCopy(pixels, (y * width + x) * 4, rotated, (x * height + height - 1 - y) * 4, 4);
            pixels = rotated; (width, height) = (height, width);
        }
        if (inverted) for (var at = 0; at < pixels.Length; at += 4)
            for (var channel = 0; channel < 3; channel++) pixels[at + channel] = (byte)(255 - pixels[at + channel]);
        Assert.True(BarcodeDecoder.TryDecode(pixels, width, height, width * 4, PixelFormat.Rgba32, BarcodeType.EAN, out var decoded));
        Assert.Equal(expected, decoded.Text);
        Assert.True(BarcodeDecoder.TryDecodeAll(pixels, width, height, width * 4, PixelFormat.Rgba32, out var all, BarcodeType.EAN));
        Assert.Equal(expected, Assert.Single(all).Text);
        Assert.Equal(expected, Assert.Single(Scan(pixels, width, height).Symbols).Text);
    }

    [Fact]
    public void Decode_OnlyOneReadablePhysicalRow_DoesNotBypassConfirmation() {
        var pixels = RenderRows(new[] { 180 }, false, out var width, out var height);
        var row = new byte[width * 4]; Buffer.BlockCopy(pixels, 180 * width * 4, row, 0, row.Length);
        Assert.True(BarcodeDecoder.TryDecode(row, width, 1, width * 4, PixelFormat.Rgba32, BarcodeType.EAN, out var decoded));
        Assert.Equal("9780441014989", decoded.Text);
        Assert.False(BarcodeDecoder.TryDecode(pixels, width, height, width * 4, PixelFormat.Rgba32, BarcodeType.EAN, out _));
        Assert.False(BarcodeDecoder.TryDecodeAll(pixels, width, height, width * 4, PixelFormat.Rgba32, out _, BarcodeType.EAN));
        Assert.Empty(Scan(pixels, width, height).Symbols);
    }

    [Fact]
    public void Decode_MatchingRowsWithInvalidChecksum_RemainRejected() {
        var pixels = RenderRows(new[] { 180, 181, 182, 183 }, true, out var width, out var height);
        Assert.False(BarcodeDecoder.TryDecode(pixels, width, height, width * 4, PixelFormat.Rgba32, BarcodeType.EAN, out _));
        Assert.False(BarcodeDecoder.TryDecodeAll(pixels, width, height, width * 4, PixelFormat.Rgba32, out _, BarcodeType.EAN));
        Assert.Empty(Scan(pixels, width, height).Symbols);
    }

    private static byte[] RenderRows(int[] rows, bool invalidChecksum, out int width, out int height) {
        var modules = BarcodeEncoder.Encode(BarcodeType.EAN, "9780441014989").Segments
            .SelectMany(segment => Enumerable.Repeat(segment.IsBar, segment.Modules)).ToArray();
        if (invalidChecksum) {
            const string wrongFinalDigit = "1110010";
            for (var i = 0; i < 7; i++) modules[85 + i] = wrongFinalDigit[i] == '1';
        }
        width = (modules.Length + 24) * 2; height = 240;
        var pixels = Enumerable.Repeat((byte)255, width * height * 4).ToArray();
        foreach (var y in rows) for (var module = 0; module < modules.Length; module++) {
            if (!modules[module]) continue;
            for (var repeat = 0; repeat < 2; repeat++) {
                var at = (y * width + (module + 12) * 2 + repeat) * 4;
                pixels[at] = pixels[at + 1] = pixels[at + 2] = 0;
            }
        }
        return pixels;
    }

    private static ScanResult Scan(byte[] pixels, int width, int height) => SymbolScanner.Scan(
        ImageFrame.Packed(pixels, width, height, PixelFormat.Rgba32),
        new ScanOptions { Profile = ScanProfile.Robust, Formats = new[] { SymbolFormat.Ean }, MaxSymbols = 1,
            EnableTileScan = false, TimeoutMilliseconds = TestBudget.Adjust(1500) });
}
