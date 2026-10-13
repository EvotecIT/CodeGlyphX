#if NET8_0_OR_GREATER
using System;
using System.Linq;
using System.Threading;
using CodeGlyphX.Qr;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("ImageScannerSerial")]
public sealed class QrFinderGroupTests {
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void OrdinaryCollection_PreservesIndependentRotatedSymbolsAndRepeatedPayloads(bool repeatedPayload, bool mixedPolarity) {
        const int size = 520;
        var pixels = Enumerable.Repeat((byte)255, size * size * 4).ToArray();
        var expected = new string[4];
        for (var index = 0; index < expected.Length; index++) {
            expected[index] = repeatedPayload ? "SAME" : "GROUP-" + index;
            var version = index * 2 + 1;
            var modules = QrCodeEncoder.EncodeText(expected[index], new QrEncodingOptions {
                MinVersion = version, MaxVersion = version
            }).Modules;
            for (var turn = 0; turn < index; turn++) modules = CodeGlyphX.Internal.BitMatrixTransforms.Rotate90(modules);
            var image = QrPngRenderer.RenderPixels(modules, new QrPngRenderOptions { ModuleSize = 4, QuietZone = 4 },
                out var width, out var height, out var stride);
            // Mixed polarity exercises separate finder passes on the same canvas.
            if (mixedPolarity && (index & 1) != 0) {
                for (var at = 0; at < image.Length; at += 4) {
                    image[at] ^= 255;
                    image[at + 1] ^= 255;
                    image[at + 2] ^= 255;
                }
            }
            var left = 16 + (index % 2) * 260;
            var top = 16 + (index / 2) * 260;
            for (var row = 0; row < height; row++) {
                Buffer.BlockCopy(image, row * stride, pixels, ((top + row) * size + left) * 4, width * 4);
            }
        }

        var options = QrPixelDecodeOptions.Robust();
        options.BudgetMilliseconds = TestBudget.Adjust(6000);
        options.MaxScale = 1;
        options.EnableTileScan = false;
        Assert.True(QrPixelDecoder.TryDecodeAll(pixels, size, size, size * 4, PixelFormat.Rgba32,
            options, accept: null, CancellationToken.None, deduplicate: !repeatedPayload, out var decoded));
        if (repeatedPayload) {
            Assert.True(decoded.Length >= expected.Length, "Independent symbols with the same payload must remain discoverable.");
            Assert.All(decoded, value => Assert.Equal("SAME", value.Text));
        } else {
            Assert.Equal(expected.OrderBy(value => value), decoded.Select(value => value.Text).Distinct().OrderBy(value => value));
        }
    }
}
#endif
