using System;
using System.IO;
using System.Linq;
using CodeGlyphX.Code93;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("ImageScannerSerial")]
public sealed class Code93RunLocalizationTests {
    [Theory]
    [InlineData("single")]
    [InlineData("all")]
    [InlineData("scanner")]
    public void Decode_IndependentFramedDiagram_PreservesExactPayload(string entryPoint) {
        using var fixture = typeof(Code93RunLocalizationTests).Assembly.GetManifestResourceStream(
            "CodeGlyphX.Tests.Fixtures.Linear.zxing-code93-diagram.png");
        Assert.NotNull(fixture);
        using var buffer = new MemoryStream();
        fixture!.CopyTo(buffer);
        Assert.True(ImageReader.TryDecodeRgba32(buffer.ToArray(), out var pixels, out var width, out var height));
        if (entryPoint == "single") {
            Assert.True(BarcodeDecoder.TryDecode(pixels, width, height, width * 4, PixelFormat.Rgba32, BarcodeType.Code93, out var decoded));
            Assert.Equal("CODE 93", decoded.Text);
        } else if (entryPoint == "all") {
            Assert.True(BarcodeDecoder.TryDecodeAll(pixels, width, height, width * 4, PixelFormat.Rgba32, out var decoded, BarcodeType.Code93));
            Assert.Equal("CODE 93", Assert.Single(decoded).Text);
        } else {
            Assert.Equal("CODE 93", Assert.Single(Scan(pixels, width, height).Symbols).Text);
        }
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    [InlineData(true, true, false)]
    [InlineData(false, false, true)]
    [InlineData(false, true, true)]
    [InlineData(true, false, true)]
    [InlineData(true, true, true)]
    public void Decode_FramedSymbol_PreservesAxisDirectionAndPolarity(bool vertical, bool reverse, bool invert) {
        var barcode = Code93Encoder.Encode("FRAME-93");
        var pixels = Frame(barcode, vertical, reverse, invert, out var width, out var height);
        Assert.True(BarcodeDecoder.TryDecode(pixels, width, height, width * 4, PixelFormat.Rgba32, BarcodeType.Code93, out var decoded));
        Assert.Equal("FRAME-93", decoded.Text);
        Assert.Equal("FRAME-93", Assert.Single(Scan(pixels, width, height).Symbols).Text);
    }

    [Theory]
    [InlineData("C")]
    [InlineData("K")]
    [InlineData("termination")]
    public void Decode_FramedDamagedSymbol_DoesNotAdmitUncheckedLocalization(string damage) {
        var valid = Code93Encoder.Encode("FRAME-93");
        var modules = valid.Segments.SelectMany(s => Enumerable.Repeat(s.IsBar, s.Modules)).ToArray();
        if (damage == "termination") {
            modules = modules.Take(modules.Length - 1).ToArray();
        } else {
            // Replace one complete checksum character with a different valid character.
            // Geometry remains valid; the other checksum and all data remain untouched.
            var offset = modules.Length - 1 - (damage == "C" ? 27 : 18);
            var current = 0;
            for (var i = 0; i < 9; i++) current = (current << 1) | (modules[offset + i] ? 1 : 0);
            var replacement = current == 276 ? 328 : 276; // Code93 '0' and '1'.
            for (var i = 0; i < 9; i++) modules[offset + i] = (replacement & (1 << (8 - i))) != 0;
        }
        var damaged = new Barcode1D(modules.Select(bar => new BarSegment(bar, 1)));
        var pixels = Frame(damaged, false, false, false, out var width, out var height);
        Assert.False(BarcodeDecoder.TryDecode(pixels, width, height, width * 4, PixelFormat.Rgba32, BarcodeType.Code93, out _));
        Assert.Empty(Scan(pixels, width, height).Symbols);
    }

    [Fact]
    public void Decode_CompleteChecksumFreeSymbol_RetainsSupportedBehavior() {
        var pixels = BarcodePngRenderer.RenderPixels(Code93Encoder.Encode("ABC", includeChecksum: false),
            new BarcodePngRenderOptions { ModuleSize = 3, QuietZone = 10, HeightModules = 24 },
            out var width, out var height, out var stride);
        Assert.True(BarcodeDecoder.TryDecode(pixels, width, height, stride, PixelFormat.Rgba32, BarcodeType.Code93, out var decoded));
        Assert.Equal("ABC", decoded.Text);
    }

    private static ScanResult Scan(byte[] pixels, int width, int height) => SymbolScanner.Scan(
        ImageFrame.Packed(pixels, width, height, PixelFormat.Rgba32),
        new ScanOptions { Profile = ScanProfile.Robust, Formats = new[] { SymbolFormat.Code93 }, TimeoutMilliseconds = TestBudget.Adjust(1500), MaxSymbols = 1 });

    private static byte[] Frame(Barcode1D barcode, bool vertical, bool reverse, bool invert, out int width, out int height) {
        var original = BarcodePngRenderer.RenderPixels(barcode,
            new BarcodePngRenderOptions { ModuleSize = 3, QuietZone = 10, HeightModules = 24 },
            out var sw, out var sh, out var stride);
        width = vertical ? sh : sw;
        height = vertical ? sw : sh;
        var pixels = new byte[width * height * 4];
        for (var y = 0; y < height; y++) {
            for (var x = 0; x < width; x++) {
                var sx = vertical ? y : x;
                var sy = vertical ? x : y;
                if (reverse) sx = sw - 1 - sx;
                var border = sx == 0 || sx == sw - 1;
                var value = border ? (byte)0 : original[sy * stride + sx * 4];
                if (invert) value = (byte)(255 - value);
                var at = (y * width + x) * 4;
                pixels[at] = pixels[at + 1] = pixels[at + 2] = value;
                pixels[at + 3] = 255;
            }
        }
        return pixels;
    }
}
