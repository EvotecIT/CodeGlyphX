using System;
using System.IO;
using System.Linq;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("ImageScannerSerial")]
public sealed class ItfRunLocalizationTests {
    [Theory]
    [InlineData("zxing-itf-clipped.png", "00012345678905")]
    [InlineData("zxing-itf-label-photo.png", "070429")]
    public void Decode_IndependentImage_PreservesCompletePayload(string name, string expected) {
        using var fixture = typeof(ItfRunLocalizationTests).Assembly.GetManifestResourceStream(
            "CodeGlyphX.Tests.Fixtures.Linear." + name);
        Assert.NotNull(fixture);
        using var buffer = new MemoryStream(); fixture!.CopyTo(buffer);
        var png = buffer.ToArray();
        Assert.True(ImageReader.TryDecodeRgba32(png, out var pixels, out var width, out var height));
        Assert.True(BarcodeDecoder.TryDecode(pixels, width, height, width * 4, PixelFormat.Rgba32, BarcodeType.ITF, out var decoded));
        Assert.Equal(expected, decoded.Text);
        Assert.True(BarcodeDecoder.TryDecodeAll(pixels, width, height, width * 4, PixelFormat.Rgba32, out var all, BarcodeType.ITF));
        Assert.Equal(expected, Assert.Single(all).Text);
        Assert.Equal(expected, Assert.Single(Scan(pixels, width, height, SymbolFormat.Itf).Symbols).Text);
        Assert.Equal(expected, Assert.Single(SymbolScanner.Scan(png, new ScanOptions {
            Profile = ScanProfile.Robust, Formats = new[] { SymbolFormat.Itf }, MaxSymbols = 1,
            TimeoutMilliseconds = TestBudget.Adjust(1500)
        }).Symbols).Text);
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, true)]
    [InlineData(false, true, true)]
    [InlineData(true, false, false)]
    [InlineData(true, true, false)]
    [InlineData(true, false, true)]
    [InlineData(true, true, true)]
    public void Decode_FramedPrintGain_PreservesDirectionAxisAndPolarity(bool vertical, bool reverse, bool inverted) {
        const string expected = "070429";
        var original = Render(BarcodeEncoder.Encode(BarcodeType.ITF, expected), 70, out var sw, out var sh);
        var width = vertical ? sh : sw; var height = vertical ? sw : sh;
        var pixels = new byte[width * height * 4];
        for (var y = 0; y < height; y++) {
            for (var x = 0; x < width; x++) {
                var sx = vertical ? y : x; var sy = vertical ? x : y;
                if (reverse) sx = sw - 1 - sx;
                var value = original[(sy * sw + sx) * 4];
                if (inverted) value = (byte)(255 - value);
                var at = (y * width + x) * 4;
                pixels[at] = pixels[at + 1] = pixels[at + 2] = value; pixels[at + 3] = 255;
            }
        }
        Assert.True(BarcodeDecoder.TryDecode(pixels, width, height, width * 4, PixelFormat.Rgba32, BarcodeType.ITF, out var decoded));
        Assert.Equal(expected, decoded.Text);
        Assert.Equal(expected, Assert.Single(Scan(pixels, width, height, SymbolFormat.Itf).Symbols).Text);
    }

    [Theory]
    [InlineData(BarcodeType.ITF, "12", "12", SymbolFormat.Itf)]
    [InlineData(BarcodeType.ITF14, "1001234500001", "10012345000017", SymbolFormat.Itf14)]
    public void Decode_FramedPrintGain_PreservesShortItfAndCheckedItf14(BarcodeType type, string input, string expected, SymbolFormat format) {
        var pixels = Render(BarcodeEncoder.Encode(type, input), 70, out var width, out var height);
        Assert.True(BarcodeDecoder.TryDecode(pixels, width, height, width * 4, PixelFormat.Rgba32, type, out var decoded));
        Assert.Equal(type, decoded.Type);
        Assert.Equal(expected, decoded.Text);
        Assert.Equal(expected, Assert.Single(Scan(pixels, width, height, format).Symbols).Text);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Decode_FramedDamage_DoesNotBypassStopOrInteriorQuietZone(bool missingStop) {
        var segments = BarcodeEncoder.Encode(BarcodeType.ITF, "070429").Segments.ToArray();
        if (missingStop) segments[segments.Length - 3] = new BarSegment(true, 1);
        var pixels = Render(new Barcode1D(segments), missingStop ? 70 : 2, out var width, out var height);
        Assert.False(BarcodeDecoder.TryDecode(pixels, width, height, width * 4, PixelFormat.Rgba32, BarcodeType.ITF, out _));
        Assert.Empty(Scan(pixels, width, height, SymbolFormat.Itf).Symbols);
    }

    [Fact]
    public void Decode_FramedInvalidItf14Checksum_RemainsGenericItfOnly() {
        const string invalidGtin = "10012345000018";
        var pixels = Render(BarcodeEncoder.Encode(BarcodeType.ITF, invalidGtin), 70, out var width, out var height);
        Assert.False(BarcodeDecoder.TryDecode(pixels, width, height, width * 4, PixelFormat.Rgba32, BarcodeType.ITF14, out _));
        Assert.Empty(Scan(pixels, width, height, SymbolFormat.Itf14).Symbols);
        Assert.True(BarcodeDecoder.TryDecode(pixels, width, height, width * 4, PixelFormat.Rgba32, BarcodeType.ITF, out var generic));
        Assert.Equal(invalidGtin, generic.Text);
    }

    private static byte[] Render(Barcode1D barcode, int quiet, out int width, out int height) {
        // Narrow spaces and bars have distinct physical widths, as in the
        // independent diagram. Clutter prevents a whole-row fit from hiding a
        // failure to locate and classify the complete symbol's own runs.
        var sampled = new Barcode1D(barcode.Segments.Select(s => new BarSegment(s.IsBar,
            s.Modules == 1 ? (s.IsBar ? 3 : 7) : (s.IsBar ? 10 : 15))));
        var pixels = BarcodePngRenderer.RenderPixels(sampled,
            new BarcodePngRenderOptions { ModuleSize = 1, QuietZone = quiet, HeightModules = 48 },
            out width, out height, out var stride);
        for (var y = 0; y < height; y++) {
            foreach (var x in new[] { 0, width - 1 }) {
                var at = y * stride + x * 4; pixels[at] = pixels[at + 1] = pixels[at + 2] = 0;
            }
        }
        return pixels;
    }

    private static ScanResult Scan(byte[] pixels, int width, int height, SymbolFormat format) => SymbolScanner.Scan(
        ImageFrame.Packed(pixels, width, height, PixelFormat.Rgba32),
        new ScanOptions { Profile = ScanProfile.Robust, Formats = new[] { format }, MaxSymbols = 1,
            TimeoutMilliseconds = TestBudget.Adjust(1500) });
}
