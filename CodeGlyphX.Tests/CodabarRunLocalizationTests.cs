using System;
using System.IO;
using System.Linq;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("ImageScannerSerial")]
public sealed class CodabarRunLocalizationTests {
    [Theory]
    [InlineData("zxing-codabar-eroded.png", "3419500", "single")]
    [InlineData("zxing-codabar-eroded.png", "3419500", "all")]
    [InlineData("zxing-codabar-eroded.png", "3419500", "scanner")]
    [InlineData("zxing-codabar-wide-rounding.png", "123455", "single")]
    [InlineData("zxing-codabar-wide-rounding.png", "123455", "all")]
    [InlineData("zxing-codabar-wide-rounding.png", "123455", "scanner")]
    public void Decode_IndependentImage_PreservesExactPayload(string name, string expected, string entryPoint) {
        using var fixture = typeof(CodabarRunLocalizationTests).Assembly.GetManifestResourceStream(
            "CodeGlyphX.Tests.Fixtures.Linear." + name);
        Assert.NotNull(fixture);
        using var buffer = new MemoryStream();
        fixture!.CopyTo(buffer);
        Assert.True(ImageReader.TryDecodeRgba32(buffer.ToArray(), out var pixels, out var width, out var height));
        if (entryPoint == "single") {
            Assert.True(BarcodeDecoder.TryDecode(pixels, width, height, width * 4, PixelFormat.Rgba32, BarcodeType.Codabar, out var decoded));
            Assert.Equal(expected, decoded.Text);
        } else if (entryPoint == "all") {
            Assert.True(BarcodeDecoder.TryDecodeAll(pixels, width, height, width * 4, PixelFormat.Rgba32, out var decoded, BarcodeType.Codabar));
            Assert.Equal(expected, Assert.Single(decoded).Text);
        } else {
            var decoded = Assert.Single(Scan(pixels, width, height).Symbols);
            Assert.Equal(SymbolFormat.Codabar, decoded.Format);
            Assert.Equal(expected, decoded.Text);
        }
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
    public void Decode_FramedPrintGain_PreservesDirectionPolarityAndPunctuation(bool vertical, bool reverse, bool invert) {
        var barcode = BarcodeEncoder.EncodeCodabar("12.-/$:+90");
        // The same print gain shrinks bars and expands spaces. Punctuation has
        // three wide bars and no wide spaces; minor space variation must not
        // manufacture a different character.
        var sampled = new Barcode1D(barcode.Segments.Select((s, index) => new BarSegment(s.IsBar,
            s.Modules == 1 ? (s.IsBar ? 2 : 4 + (index % 3 == 0 ? 1 : 0)) : (s.IsBar ? 6 : 8))));
        var original = BarcodePngRenderer.RenderPixels(sampled,
            new BarcodePngRenderOptions { ModuleSize = 1, QuietZone = 24, HeightModules = 64 },
            out var sw, out var sh, out var stride);
        var width = vertical ? sh : sw;
        var height = vertical ? sw : sh;
        var pixels = new byte[width * height * 4];
        for (var y = 0; y < height; y++) {
            for (var x = 0; x < width; x++) {
                var sx = vertical ? y : x;
                var sy = vertical ? x : y;
                if (reverse) sx = sw - 1 - sx;
                var value = sx == 0 || sx == sw - 1 ? (byte)0 : original[sy * stride + sx * 4];
                if (invert) value = (byte)(255 - value);
                var at = (y * width + x) * 4;
                pixels[at] = pixels[at + 1] = pixels[at + 2] = value;
                pixels[at + 3] = 255;
            }
        }
        Assert.True(BarcodeDecoder.TryDecode(pixels, width, height, width * 4, PixelFormat.Rgba32, BarcodeType.Codabar, out var decoded));
        Assert.Equal("12.-/$:+90", decoded.Text);
        Assert.Equal("12.-/$:+90", Assert.Single(Scan(pixels, width, height).Symbols).Text);
    }

    [Theory]
    [InlineData("missing-stop")]
    [InlineData("inconsistent-width")]
    public void Decode_DamagedFramedSymbol_DoesNotAcceptOnlyCharacterShapes(string damage) {
        var runs = BarcodeEncoder.EncodeCodabar("1234").Segments.ToArray();
        if (damage == "missing-stop") {
            // Replace the closing guard with an ordinary data character.
            var data = BarcodeEncoder.EncodeCodabar("1").Segments.Skip(8).Take(7).ToArray();
            Array.Copy(data, 0, runs, runs.Length - 7, 7);
        } else {
            var wide = Array.FindIndex(runs, 8, s => s.IsBar && s.Modules == 2);
            runs[wide] = new BarSegment(true, 8);
        }
        var pixels = BarcodePngRenderer.RenderPixels(new Barcode1D(runs),
            new BarcodePngRenderOptions { ModuleSize = 3, QuietZone = 12, HeightModules = 48 },
            out var width, out var height, out var stride);
        // Ink outside the quiet zones makes this a localized candidate, rather
        // than letting the legacy whole-row decoder bypass localization checks.
        for (var y = 0; y < height; y++) {
            foreach (var x in new[] { 0, width - 1 }) {
                var at = y * stride + x * 4;
                pixels[at] = pixels[at + 1] = pixels[at + 2] = 0;
            }
        }
        Assert.False(BarcodeDecoder.TryDecode(pixels, width, height, stride, PixelFormat.Rgba32, BarcodeType.Codabar, out _));
        Assert.Empty(Scan(pixels, width, height).Symbols);
    }

    private static ScanResult Scan(byte[] pixels, int width, int height) => SymbolScanner.Scan(
        ImageFrame.Packed(pixels, width, height, PixelFormat.Rgba32),
        new ScanOptions { Profile = ScanProfile.Robust, Formats = new[] { SymbolFormat.Codabar },
            TimeoutMilliseconds = TestBudget.Adjust(1500), MaxSymbols = 1 });
}
