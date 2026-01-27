using System;
using CodeGlyphX.Rendering.Jpeg;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests.Net472;

// Keep this suite small and net472-safe: it should run fast on Windows CI.
public sealed class QrNet472SmokeTests {
    private const string Payload = "NET472-CI-SMOKE";

    [Fact]
    public void Net472_QrImageDecoder_Decodes_RenderedPng() {
        var qr = QrCodeEncoder.EncodeText(Payload);
        var png = QrPngRenderer.Render(qr.Modules, new QrPngRenderOptions {
            ModuleSize = 14,
            QuietZone = 6
        });

        var ok = QrImageDecoder.TryDecodeImage(png, out var decoded);
        Assert.True(ok);
        Assert.Equal(Payload, decoded.Text);
    }

    [Fact]
    public void Net472_QrImageDecoder_Decodes_RenderedJpeg() {
        var qr = QrCodeEncoder.EncodeText(Payload);
        var jpg = QrJpegRenderer.Render(qr.Modules, new QrPngRenderOptions {
            ModuleSize = 14,
            QuietZone = 6
        }, quality: 100);

        var ok = QrImageDecoder.TryDecodeImage(jpg, out var decoded);
        Assert.True(ok);
        Assert.Equal(Payload, decoded.Text);
    }

    [Fact]
    public void Net472_QrDecoder_Decodes_Modules() {
        var qr = QrCodeEncoder.EncodeText(Payload);
        var ok = QrDecoder.TryDecode(qr.Modules, out var decoded);
        Assert.True(ok);
        Assert.Equal(Payload, decoded.Text);
    }

    [Fact]
    public void Net472_QrImageDecoder_ImageOptions_ByteArray_Overload_Works() {
        var qr = QrCodeEncoder.EncodeText(Payload);
        var png = QrPngRenderer.Render(qr.Modules, new QrPngRenderOptions {
            ModuleSize = 24,
            QuietZone = 8
        });

        var imageOptions = ImageDecodeOptions.Screen(maxMilliseconds: 600, maxDimension: 900);
        var ok = QrImageDecoder.TryDecodeImage(png, imageOptions, options: null, out var decoded);
        Assert.True(ok);
        Assert.Equal(Payload, decoded.Text);
    }

    [Fact]
    public void Net472_QrImageDecoder_ImageOptions_Stream_Overload_Works() {
        var qr = QrCodeEncoder.EncodeText(Payload);
        var png = QrPngRenderer.Render(qr.Modules, new QrPngRenderOptions {
            ModuleSize = 20,
            QuietZone = 6
        });

        using var stream = new System.IO.MemoryStream(png);
        var imageOptions = ImageDecodeOptions.Screen(maxMilliseconds: 600, maxDimension: 1000);
        var ok = QrImageDecoder.TryDecodeImage(stream, imageOptions, options: null, out var decoded);
        Assert.True(ok);
        Assert.Equal(Payload, decoded.Text);
    }

    [Fact]
    public void Net472_QrImageDecoder_Downscale_Still_Decodes() {
        var qr = QrCodeEncoder.EncodeText(Payload);
        var opts = new QrPngRenderOptions {
            ModuleSize = 64,
            QuietZone = 12
        };
        var png = QrPngRenderer.Render(qr.Modules, opts);

        // Force a heavy downscale via ImageDecodeOptions.
        var imageOptions = ImageDecodeOptions.Screen(maxMilliseconds: 800, maxDimension: 700);
        var ok = QrImageDecoder.TryDecodeImage(png, imageOptions, options: new QrPixelDecodeOptions {
            MaxDimension = 4000
        }, out var decoded);

        Assert.True(ok);
        Assert.Equal(Payload, decoded.Text);
    }

    [Fact]
    public void Net472_QrImageDecoder_LightNoise_RoundTrip_Works() {
        var qr = QrCodeEncoder.EncodeText(Payload);
        var pixels = QrPngRenderer.RenderPixels(qr.Modules, new QrPngRenderOptions {
            ModuleSize = 18,
            QuietZone = 6
        }, out var width, out var height, out var stride);

        AddLightNoise(pixels, width, height, stride);
        var png = EncodePng(pixels, width, height, stride);

        var ok = QrImageDecoder.TryDecodeImage(png, out var decoded);
        Assert.True(ok);
        Assert.Equal(Payload, decoded.Text);
    }

    private static void AddLightNoise(byte[] rgba, int width, int height, int stride) {
        var rng = new Random(12345);
        for (var y = 0; y < height; y++) {
            var row = y * stride;
            for (var x = 0; x < width; x++) {
                if (rng.NextDouble() > 0.02) continue; // 2% pixels
                var i = row + (x * 4);
                for (var c = 0; c < 3; c++) {
                    var delta = rng.Next(-16, 17);
                    var value = rgba[i + c] + delta;
                    if (value < 0) value = 0;
                    if (value > 255) value = 255;
                    rgba[i + c] = (byte)value;
                }
                rgba[i + 3] = 255;
            }
        }
    }

    private static byte[] EncodePng(byte[] rgba, int width, int height, int stride) {
        var rowLength = stride + 1;
        var scanlines = new byte[height * rowLength];
        for (var y = 0; y < height; y++) {
            var rowStart = y * rowLength;
            scanlines[rowStart] = 0;
            Buffer.BlockCopy(rgba, y * stride, scanlines, rowStart + 1, stride);
        }
        return PngWriter.WriteRgba8(width, height, scanlines, scanlines.Length);
    }
}
