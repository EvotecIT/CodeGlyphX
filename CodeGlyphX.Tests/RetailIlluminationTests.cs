using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class RetailIlluminationTests {
    [Theory]
    [InlineData(BarcodeType.EAN, "590123412345", "5901234123457", false, false)]
    [InlineData(BarcodeType.UPCA, "03600029145", "036000291452", false, true)]
    [InlineData(BarcodeType.EAN, "1234567", "12345670", true, false)]
    [InlineData(BarcodeType.UPCE, "123456", "01234565", true, true)]
    public void Decode_RetailSymbolAcrossIlluminationGradient_PreservesPayload(
        BarcodeType type, string input, string expected, bool inverted, bool vertical) {
        var pixels = RenderShaded(BarcodeEncoder.Encode(type, input), inverted, vertical, out var width, out var height);
        Assert.True(BarcodeDecoder.TryDecode(pixels, width, height, width * 4, PixelFormat.Rgba32, type, out var decoded));
        Assert.Equal(type, decoded.Type);
        Assert.Equal(expected, decoded.Text);
        Assert.True(BarcodeDecoder.TryDecodeAll(pixels, width, height, width * 4, PixelFormat.Rgba32, out var all, type));
        Assert.Equal(expected, Assert.Single(all).Text);
    }

    [Theory]
    [InlineData("zxing-ean-shaded.png", SymbolFormat.Ean, "5449000039231")]
    [InlineData("zxing-upca-shaded.png", SymbolFormat.UpcA, "854818000116")]
    public void Scan_ShadedIndependentPhotograph_PreservesExactPayload(string name, SymbolFormat format, string expected) {
        using var fixture = typeof(RetailIlluminationTests).Assembly.GetManifestResourceStream("CodeGlyphX.Tests.Fixtures.Linear." + name);
        Assert.NotNull(fixture);
        using var buffer = new MemoryStream();
        fixture!.CopyTo(buffer);
        var options = ScanOptions.Robust(TestBudget.Adjust(5000));
        options.Formats = new[] { format };
        options.MaxSymbols = 1;
        Assert.Equal(expected, Assert.Single(SymbolScanner.Scan(buffer.ToArray(), options).Symbols).Text);
    }

    [Theory]
    [InlineData("zxing-striped-negative-14.png")]
    [InlineData("zxing-striped-negative-15.png")]
    [InlineData("zxing-noise-negative-03.png")]
    public void Scan_Texture_CompletesWithoutInventingRetailPayload(string name) {
        using var fixture = typeof(RetailIlluminationTests).Assembly.GetManifestResourceStream("CodeGlyphX.Tests.Fixtures.Linear." + name);
        Assert.NotNull(fixture);
        using var buffer = new MemoryStream();
        fixture!.CopyTo(buffer);
        var options = ScanOptions.Robust(TestBudget.Adjust(5000));
        options.Formats = new[] { SymbolFormat.Ean, SymbolFormat.UpcA, SymbolFormat.UpcE };
        var result = SymbolScanner.Scan(buffer.ToArray(), options);
        Assert.Equal(ScanCompletionReason.Completed, result.CompletionReason);
        Assert.Empty(result.Symbols);
        var pixels = ImageReader.DecodeRgba32(buffer.ToArray(), out var width, out var height);
        foreach (var type in new[] { BarcodeType.EAN, BarcodeType.UPCA, BarcodeType.UPCE }) {
            Assert.False(BarcodeDecoder.TryDecode(pixels, width, height, width * 4, PixelFormat.Rgba32, type, out _));
            Assert.False(BarcodeDecoder.TryDecodeAll(pixels, width, height, width * 4, PixelFormat.Rgba32, out _, type));
#if NET8_0_OR_GREATER
            Assert.False(BarcodeDecoder.TryDecode(pixels.AsSpan(), width, height, width * 4, PixelFormat.Rgba32, type, out _));
            Assert.False(BarcodeDecoder.TryDecodeAll(pixels.AsSpan(), width, height, width * 4, PixelFormat.Rgba32, out _, type));
#endif
        }
    }

    [Fact]
    public void Decode_ShadedRetailSymbol_StillRejectsInvalidChecksum() {
        var modules = BarcodeEncoder.Encode(BarcodeType.UPCA, "03600029145").Segments
            .SelectMany(segment => Enumerable.Repeat(segment.IsBar, segment.Modules)).ToArray();
        // Replace only the final digit 2 with the valid right-hand pattern for 0.
        // Guards and all digit patterns remain valid, while the checksum is wrong.
        var wrongDigit = "1110010";
        for (var i = 0; i < wrongDigit.Length; i++) modules[85 + i] = wrongDigit[i] == '1';
        var segments = new List<BarSegment>();
        for (var start = 0; start < modules.Length;) {
            var end = start + 1;
            while (end < modules.Length && modules[end] == modules[start]) end++;
            segments.Add(new BarSegment(modules[start], end - start));
            start = end;
        }
        var pixels = RenderShaded(new Barcode1D(segments), false, false, out var width, out var height);
        Assert.False(BarcodeDecoder.TryDecode(pixels, width, height, width * 4, PixelFormat.Rgba32, BarcodeType.UPCA, out _));
        Assert.False(BarcodeDecoder.TryDecodeAll(pixels, width, height, width * 4, PixelFormat.Rgba32, out _, BarcodeType.UPCA));
    }

    private static byte[] RenderShaded(Barcode1D barcode, bool inverted, bool vertical, out int width, out int height) {
        var pixels = BarcodePngRenderer.RenderPixels(barcode, new BarcodePngRenderOptions {
            ModuleSize = 3, QuietZone = 12, HeightModules = 30
        }, out width, out height, out var stride);
        for (var y = 0; y < height; y++) for (var x = 0; x < width; x++) {
            var offset = y * stride + x * 4;
            var intensity = 20 + x * 150 / width + (pixels[offset] == 0 ? 0 : 70);
            var value = (byte)(inverted ? 255 - intensity : intensity);
            pixels[offset] = pixels[offset + 1] = pixels[offset + 2] = value;
        }
        if (!vertical) return pixels;
        var rotated = new byte[pixels.Length];
        for (var y = 0; y < height; y++) for (var x = 0; x < width; x++)
            Buffer.BlockCopy(pixels, (y * width + x) * 4, rotated, (x * height + height - 1 - y) * 4, 4);
        var oldWidth = width;
        width = height;
        height = oldWidth;
        return rotated;
    }
}
