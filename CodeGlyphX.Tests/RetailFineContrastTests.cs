using System;
using System.IO;
using CodeGlyphX.Rendering;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("ImageScannerSerial")]
public sealed class RetailFineContrastTests {
    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(3, false)]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    public void Decode_TiltedPhotograph_ConfirmsFineContrastOnAdjacentRows(int quarterTurns, bool inverted) {
        var pixels = ReadFixture("zxing-ean-fine-contrast.png", quarterTurns, inverted, out var width, out var height);

        const string expected = "9780441014989";
        Assert.True(BarcodeDecoder.TryDecode(pixels, width, height, width * 4, PixelFormat.Rgba32, BarcodeType.EAN, out var decoded));
        Assert.Equal(expected, decoded.Text);
        Assert.True(BarcodeDecoder.TryDecodeAll(pixels, width, height, width * 4, PixelFormat.Rgba32, out var all, BarcodeType.EAN));
        Assert.Equal(expected, Assert.Single(all).Text);
        var result = SymbolScanner.Scan(ImageFrame.Packed(pixels, width, height, PixelFormat.Rgba32),
            new ScanOptions {
                Profile = ScanProfile.Robust, Formats = new[] { SymbolFormat.Ean }, MaxSymbols = 1,
                EnableTileScan = false, TimeoutMilliseconds = TestBudget.Adjust(1500)
            });
        Assert.Equal(expected, Assert.Single(result.Symbols).Text);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(3, false)]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    public void Scan_PerforatedTexture_DoesNotInventRetailPayloadDuringTileRecovery(int quarterTurns, bool inverted) {
        var pixels = ReadFixture("zxing-perforated-negative-13.png", quarterTurns, inverted, out var width, out var height);
        var options = ScanOptions.Robust(TestBudget.Adjust(5000));
        options.MaxSymbols = 32;
        options.Image = new ImageDecodeOptions { MaxDimension = 0 };
        var result = SymbolScanner.Scan(ImageFrame.Packed(pixels, width, height, PixelFormat.Rgba32), options);

        // A different format can exhaust its attempt on a legacy runtime while
        // later formats still run. Keep incompletion distinct from false acceptance.
        Assert.Contains(result.CompletionReason, new[] { ScanCompletionReason.Completed, ScanCompletionReason.RecognitionBudgetExceeded });
        Assert.DoesNotContain(result.Symbols, hit => hit.Format == SymbolFormat.Ean || hit.Format == SymbolFormat.UpcA || hit.Format == SymbolFormat.UpcE);
        // The original corpus image is negative for every format. Inversion is
        // an additional retail regression diagnostic, outside that corpus.
        if (!inverted) Assert.Empty(result.Symbols);

        // Exhaust the typed retail tile paths as well, without a competing QR
        // attempt or a recognition deadline masking their false-acceptance check.
        foreach (var type in new[] { BarcodeType.EAN, BarcodeType.UPCA, BarcodeType.UPCE }) {
            Assert.False(BarcodeDecoder.TryDecodeAll(pixels, width, height, width * 4, PixelFormat.Rgba32,
                out _, type, new BarcodeDecodeOptions { EnableTileScan = true, TileGrid = 2 }));
        }
    }

    private static byte[] ReadFixture(string name, int quarterTurns, bool inverted, out int width, out int height) {
        using var fixture = typeof(RetailFineContrastTests).Assembly.GetManifestResourceStream(
            "CodeGlyphX.Tests.Fixtures.Linear." + name);
        Assert.NotNull(fixture);
        using var buffer = new MemoryStream();
        fixture!.CopyTo(buffer);
        var pixels = ImageReader.DecodeRgba32(buffer.ToArray(), out width, out height);
        for (var turn = 0; turn < quarterTurns; turn++) {
            var rotated = new byte[pixels.Length];
            for (var y = 0; y < height; y++) for (var x = 0; x < width; x++)
                Buffer.BlockCopy(pixels, (y * width + x) * 4, rotated, (x * height + height - 1 - y) * 4, 4);
            pixels = rotated;
            (width, height) = (height, width);
        }
        if (inverted) for (var at = 0; at < pixels.Length; at += 4)
            for (var channel = 0; channel < 3; channel++) pixels[at + channel] = (byte)(255 - pixels[at + channel]);

        return pixels;
    }
}
