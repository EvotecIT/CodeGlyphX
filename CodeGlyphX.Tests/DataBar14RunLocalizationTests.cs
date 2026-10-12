using System;
using System.IO;
using System.Collections.Generic;
using CodeGlyphX.DataBar;
using CodeGlyphX.Rendering;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("ImageScannerSerial")]
public sealed class DataBar14RunLocalizationTests {
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void Decode_IndependentTiltedPhotograph_UsesCompleteCharacterFits(bool vertical, bool inverted) {
        using var fixture = typeof(DataBar14RunLocalizationTests).Assembly.GetManifestResourceStream(
            "CodeGlyphX.Tests.Fixtures.Linear.zxing-databar14-character-pitch.png");
        Assert.NotNull(fixture);
        using var buffer = new MemoryStream(); fixture!.CopyTo(buffer);
        var pixels = ImageReader.DecodeRgba32(buffer.ToArray(), out var width, out var height);
        Transform(ref pixels, ref width, ref height, vertical, inverted);
        Assert.True(BarcodeDecoder.TryDecode(pixels, width, height, width * 4, PixelFormat.Rgba32,
            BarcodeType.GS1DataBarOmni, out var decoded));
        Assert.Equal("0200123456789", decoded.Text);
        Assert.Equal(BarcodeType.GS1DataBarOmni, decoded.Type);
        Assert.True(BarcodeDecoder.TryDecodeAll(pixels, width, height, width * 4, PixelFormat.Rgba32,
            out var all, BarcodeType.GS1DataBarOmni));
        Assert.Equal("0200123456789", Assert.Single(all).Text);
        var scan = SymbolScanner.Scan(ImageFrame.Packed(pixels, width, height, PixelFormat.Rgba32), new ScanOptions {
            Formats = new[] { SymbolFormat.Gs1DataBarOmnidirectional },
            TimeoutMilliseconds = TestBudget.Adjust(5000)
        });
        Assert.Empty(scan.UnsupportedFormats);
        var symbol = Assert.Single(scan.Symbols);
        Assert.Equal("0200123456789", symbol.Text);
        Assert.Equal(SymbolFormat.Gs1DataBarOmnidirectional, symbol.Format);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void Decode_StretchedEnclosedEndGuard_RemainsInvalid(bool vertical, bool inverted) {
        var encoded = DataBar14Encoder.EncodeOmnidirectional("0200123456789");
        var segments = new List<BarSegment>(encoded.Segments);
        var gap = segments.Count - 2;
        Assert.False(segments[gap].IsBar);
        segments[gap] = new BarSegment(false, 3);
        var invalid = new Barcode1D(segments);
        Assert.False(DataBar14Decoder.TryDecodeOmnidirectional(invalid, out _));
        var pixels = Rendering.Png.BarcodePngRenderer.RenderPixels(invalid,
            new Rendering.Png.BarcodePngRenderOptions { ModuleSize = 3, QuietZone = 10, HeightModules = 40 },
            out var width, out var height, out _);
        Transform(ref pixels, ref width, ref height, vertical, inverted);
        Assert.False(BarcodeDecoder.TryDecode(pixels, width, height, width * 4, PixelFormat.Rgba32,
            BarcodeType.GS1DataBarOmni, out _));
        Assert.False(BarcodeDecoder.TryDecodeAll(pixels, width, height, width * 4, PixelFormat.Rgba32,
            out _, BarcodeType.GS1DataBarOmni));
        Assert.Empty(SymbolScanner.Scan(ImageFrame.Packed(pixels, width, height, PixelFormat.Rgba32), new ScanOptions {
            Formats = new[] { SymbolFormat.Gs1DataBarOmnidirectional },
            TimeoutMilliseconds = TestBudget.Adjust(5000)
        }).Symbols);
    }

    private static void Transform(ref byte[] pixels, ref int width, ref int height, bool vertical, bool inverted) {
        if (vertical) {
            var rotated = new byte[pixels.Length];
            for (var y = 0; y < height; y++) for (var x = 0; x < width; x++)
                Buffer.BlockCopy(pixels, (y * width + x) * 4, rotated, (x * height + height - 1 - y) * 4, 4);
            pixels = rotated; (width, height) = (height, width);
        }
        if (inverted) for (var at = 0; at < pixels.Length; at += 4)
            for (var channel = 0; channel < 3; channel++) pixels[at + channel] = (byte)(255 - pixels[at + channel]);
    }
}
