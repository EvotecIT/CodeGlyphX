using CodeMatrix.Rendering.Png;
using CodeMatrix.Tests.TestHelpers;
using Xunit;

namespace CodeMatrix.Tests;

public sealed class RoundTripTests {
    [Fact]
    public void Encode_RenderPng_DecodePixels_RoundTrip() {
        var text = "Round-trip test";
        var qr = QrCodeEncoder.EncodeText(text, QrErrorCorrectionLevel.M, 1, 10, null);
        var png = QrPngRenderer.Render(qr.Modules, new QrPngRenderOptions { ModuleSize = 8, QuietZone = 4 });

        var (rgba, width, height, stride) = PngTestDecoder.DecodeRgba32(png);
        Assert.True(QrDecoder.TryDecode(rgba, width, height, stride, PixelFormat.Rgba32, out var decoded));
        Assert.Equal(text, decoded.Text);
    }

    [Fact]
    public void DecodePixels_FinderBased_CanIgnoreNoiseOutsideQr() {
        var text = "Noise outside QR";
        var qr = QrCodeEncoder.EncodeText(text, QrErrorCorrectionLevel.M, 1, 10, null);
        var png = QrPngRenderer.Render(qr.Modules, new QrPngRenderOptions { ModuleSize = 8, QuietZone = 4 });

        var (rgba, width, height, stride) = PngTestDecoder.DecodeRgba32(png);

        var padLeft = 37;
        var padTop = 19;
        var outW = width + padLeft + 11;
        var outH = height + padTop + 23;
        var outStride = outW * 4;
        var canvas = new byte[outStride * outH];

        // Fill white.
        for (var i = 0; i < canvas.Length; i += 4) {
            canvas[i + 0] = 255;
            canvas[i + 1] = 255;
            canvas[i + 2] = 255;
            canvas[i + 3] = 255;
        }

        // Copy QR image into the canvas at an offset.
        for (var y = 0; y < height; y++) {
            var srcRow = y * stride;
            var dstRow = (y + padTop) * outStride + padLeft * 4;
            rgba.AsSpan(srcRow, width * 4).CopyTo(canvas.AsSpan(dstRow, width * 4));
        }

        // Add one stray black pixel outside the QR so simple bounding-box approaches fail.
        canvas[0] = 0;
        canvas[1] = 0;
        canvas[2] = 0;
        canvas[3] = 255;

        Assert.True(QrDecoder.TryDecode(canvas, outW, outH, outStride, PixelFormat.Rgba32, out var decoded));
        Assert.Equal(text, decoded.Text);
    }
}
