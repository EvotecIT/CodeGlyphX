using System;
using System.IO;
using System.Linq;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("ImageScannerSerial")]
public sealed class Code39RunLocalizationTests {
    [Theory]
    [InlineData("zxing-code39-clipped-ascii.png", "Extended !?*#")]
    [InlineData("zxing-code39-clipped-lowercase.png", "12ab")]
    [InlineData("zxing-code39-reversed-photo.png", "404785")]
    public void Decode_IndependentImage_PreservesExactPayload(string name, string expected) {
        using var fixture = typeof(Code39RunLocalizationTests).Assembly.GetManifestResourceStream(
            "CodeGlyphX.Tests.Fixtures.Linear." + name);
        Assert.NotNull(fixture);
        using var buffer = new MemoryStream();
        fixture!.CopyTo(buffer);
        Assert.True(ImageReader.TryDecodeRgba32(buffer.ToArray(), out var pixels, out var width, out var height));
        Assert.True(BarcodeDecoder.TryDecode(pixels, width, height, width * 4, PixelFormat.Rgba32, BarcodeType.Code39, out var decoded));
        Assert.Equal(expected, decoded.Text);
        Assert.Equal(expected, Assert.Single(Scan(pixels, width, height).Symbols).Text);
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
    public void Decode_ClippedQuietMargins_PreservesDirectionPolarityAndChecksum(bool vertical, bool reverse, bool inverted) {
        const string expected = "Ship !?*#";
        var barcode = BarcodeEncoder.EncodeCode39(expected, includeChecksum: true, fullAsciiMode: true);
        // Independent narrow/wide widths prevent the whole-row integer-pitch path
        // from hiding a failure in character-by-character run localization.
        var sampled = new Barcode1D(barcode.Segments.Select((s, i) =>
            new BarSegment(s.IsBar, s.Modules == 1 ? 3 : 8 + i % 2)));
        var original = BarcodePngRenderer.RenderPixels(sampled,
            new BarcodePngRenderOptions { ModuleSize = 1, QuietZone = 2, HeightModules = 48 },
            out var sw, out var sh, out var stride);
        var width = vertical ? sh : sw;
        var height = vertical ? sw : sh;
        var pixels = new byte[width * height * 4];
        for (var y = 0; y < height; y++) {
            for (var x = 0; x < width; x++) {
                var sx = vertical ? y : x;
                var sy = vertical ? x : y;
                if (reverse) sx = sw - 1 - sx;
                var value = original[sy * stride + sx * 4];
                if (inverted) value = (byte)(255 - value);
                var at = (y * width + x) * 4;
                pixels[at] = pixels[at + 1] = pixels[at + 2] = value;
                pixels[at + 3] = 255;
            }
        }
        var options = new BarcodeDecodeOptions { Code39Checksum = Code39ChecksumPolicy.RequireValid };
        Assert.True(BarcodeDecoder.TryDecode(pixels, width, height, width * 4, PixelFormat.Rgba32, BarcodeType.Code39, options, out var decoded));
        Assert.Equal(expected, decoded.Text);
        Assert.Equal(expected, Assert.Single(Scan(pixels, width, height, options).Symbols).Text);
    }

    [Theory]
    [InlineData("missing-stop")]
    [InlineData("interior-short-quiet")]
    [InlineData("invalid-checksum")]
    public void Decode_FramedInvalidSymbol_DoesNotBypassGuardsQuietZonesOrChecksum(string damage) {
        var segments = BarcodeEncoder.EncodeCode39("AB12", includeChecksum: true, fullAsciiMode: false).Segments.ToArray();
        if (damage != "interior-short-quiet") {
            var replacement = BarcodeEncoder.EncodeCode39("Z", includeChecksum: false, fullAsciiMode: false).Segments.Skip(10).Take(9).ToArray();
            Array.Copy(replacement, 0, segments, segments.Length - (damage == "missing-stop" ? 9 : 19), 9);
        }
        var sampled = new Barcode1D(segments.Select((s, i) => new BarSegment(s.IsBar, s.Modules == 1 ? 3 : 8 + i % 2)));
        var pixels = BarcodePngRenderer.RenderPixels(sampled,
            new BarcodePngRenderOptions { ModuleSize = 1, QuietZone = damage == "interior-short-quiet" ? 2 : 24, HeightModules = 48 },
            out var width, out var height, out var stride);
        // Both quiet runs are now interior, so the image-edge exception cannot apply.
        for (var y = 0; y < height; y++) {
            foreach (var x in new[] { 0, width - 1 }) {
                var at = y * stride + x * 4;
                pixels[at] = pixels[at + 1] = pixels[at + 2] = 0;
            }
        }
        var options = new BarcodeDecodeOptions { Code39Checksum = Code39ChecksumPolicy.RequireValid };
        Assert.False(BarcodeDecoder.TryDecode(pixels, width, height, stride, PixelFormat.Rgba32, BarcodeType.Code39, options, out _));
        Assert.Empty(Scan(pixels, width, height, options).Symbols);
    }

    private static ScanResult Scan(byte[] pixels, int width, int height, BarcodeDecodeOptions? barcode = null) => SymbolScanner.Scan(
        ImageFrame.Packed(pixels, width, height, PixelFormat.Rgba32),
        new ScanOptions { Profile = ScanProfile.Robust, Formats = new[] { SymbolFormat.Code39 }, Barcode = barcode,
            TimeoutMilliseconds = TestBudget.Adjust(1500), MaxSymbols = 1 });
}
