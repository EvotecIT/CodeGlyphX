using System;
using System.IO;
using CodeGlyphX.Rendering;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class RetailParityRecoveryTests {
    [Theory]
    [InlineData("zxing-upca-offcentre-photo.png", "070097025088", false, false)]
    [InlineData("zxing-upca-offcentre-photo.png", "070097025088", true, false)]
    [InlineData("zxing-upca-offcentre-column.png", "807648011401", false, false)]
    [InlineData("zxing-upca-offcentre-column.png", "807648011401", true, false)]
    [InlineData("zxing-upca-parity-blur.png", "890444000335", false, false)]
    [InlineData("zxing-upca-parity-blur.png", "890444000335", true, false)]
    [InlineData("zxing-upca-parity-photo.png", "023942431015", false, false)]
    [InlineData("zxing-upca-parity-photo.png", "023942431015", true, false)]
    [InlineData("zxing-upca-blurred-bars.png", "066721017185", false, false)]
    [InlineData("zxing-upca-blurred-bars.png", "066721017185", true, false)]
    [InlineData("zxing-upca-blurred-bars.png", "066721017185", false, true)]
    [InlineData("zxing-upca-blurred-bars.png", "066721017185", true, true)]
    public void Decode_IndependentOffCentrePhotograph_RecoversCorroboratedPayload(string name, string expected, bool vertical, bool inverted) {
        var pixels = ReadFixture(name, out var width, out var height);
        if (vertical) {
            var rotated = new byte[pixels.Length];
            for (var y = 0; y < height; y++) for (var x = 0; x < width; x++)
                Buffer.BlockCopy(pixels, (y * width + x) * 4, rotated, (x * height + height - 1 - y) * 4, 4);
            pixels = rotated;
            (width, height) = (height, width);
        }
        if (inverted) {
            for (var at = 0; at < pixels.Length; at += 4)
                for (var channel = 0; channel < 3; channel++) pixels[at + channel] = (byte)(255 - pixels[at + channel]);
        }
        Assert.True(BarcodeDecoder.TryDecode(pixels, width, height, width * 4, PixelFormat.Rgba32, BarcodeType.UPCA, out var decoded));
        Assert.Equal(expected, decoded.Text);
        Assert.True(BarcodeDecoder.TryDecodeAll(pixels, width, height, width * 4, PixelFormat.Rgba32, out var all, BarcodeType.UPCA));
        Assert.Equal(expected, Assert.Single(all).Text);
        var options = ScanOptions.Robust(0);
        options.Formats = new[] { SymbolFormat.UpcA };
        options.MaxSymbols = 1;
        options.EnableTileScan = false;
        var result = SymbolScanner.Scan(ImageFrame.Packed(pixels, width, height, PixelFormat.Rgba32), options);
        Assert.Equal(expected, Assert.Single(result.Symbols).Text);
#if NET8_0_OR_GREATER
        Assert.True(BarcodeDecoder.TryDecode(pixels.AsSpan(), width, height, width * 4, PixelFormat.Rgba32, BarcodeType.UPCA, out var spanDecoded));
        Assert.Equal(expected, spanDecoded.Text);
        Assert.True(BarcodeDecoder.TryDecodeAll(pixels.AsSpan(), width, height, width * 4, PixelFormat.Rgba32, out var spanAll, BarcodeType.UPCA));
        Assert.Equal(expected, Assert.Single(spanAll).Text);
#endif
    }

    [Theory]
    [InlineData("zxing-upca-parity-blur.png", 116, "890444000335", false, false)]
    [InlineData("zxing-upca-parity-blur.png", 116, "890444000335", true, false)]
    [InlineData("zxing-upca-parity-blur.png", 116, "890444000335", false, true)]
    [InlineData("zxing-upca-parity-blur.png", 116, "890444000335", true, true)]
    [InlineData("zxing-upca-parity-photo.png", 180, "023942431015", false, false)]
    [InlineData("zxing-upca-parity-photo.png", 180, "023942431015", true, false)]
    [InlineData("zxing-upca-parity-photo.png", 180, "023942431015", false, true)]
    [InlineData("zxing-upca-parity-photo.png", 180, "023942431015", true, true)]
    public void Decode_IndependentBlurredRow_UsesValidRetailParity(
        string name, int y, string expected, bool reversed, bool inverted) {
        var image = ReadFixture(name, out var width, out _);
        var row = new byte[width * 4];
        for (var x = 0; x < width; x++) {
            var source = (y * width + (reversed ? width - 1 - x : x)) * 4;
            for (var channel = 0; channel < 3; channel++)
                row[x * 4 + channel] = inverted ? (byte)(255 - image[source + channel]) : image[source + channel];
            row[x * 4 + 3] = 255;
        }

        Assert.True(BarcodeDecoder.TryDecode(row, width, 1, width * 4, PixelFormat.Rgba32, BarcodeType.UPCA, out var decoded));
        Assert.Equal(expected, decoded.Text);
        Assert.Equal(BarcodeType.UPCA, decoded.Type);
        Assert.True(BarcodeDecoder.TryDecodeAll(row, width, 1, width * 4, PixelFormat.Rgba32, out var all, BarcodeType.UPCA));
        Assert.Equal(expected, Assert.Single(all).Text);
#if NET8_0_OR_GREATER
        Assert.True(BarcodeDecoder.TryDecode(row.AsSpan(), width, 1, width * 4, PixelFormat.Rgba32, BarcodeType.UPCA, out var spanDecoded));
        Assert.Equal(expected, spanDecoded.Text);
#endif
    }

    private static byte[] ReadFixture(string name, out int width, out int height) {
        using var fixture = typeof(RetailParityRecoveryTests).Assembly.GetManifestResourceStream(
            "CodeGlyphX.Tests.Fixtures.Linear." + name);
        Assert.NotNull(fixture);
        using var buffer = new MemoryStream();
        fixture!.CopyTo(buffer);
        return ImageReader.DecodeRgba32(buffer.ToArray(), out width, out height);
    }
}
