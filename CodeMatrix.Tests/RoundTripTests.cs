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

    [Fact]
    public void DecodePixels_CanDecodeUiScaledBilinearQr() {
        var text = "UI-scaled QR test";
        var qr = QrCodeEncoder.EncodeText(text, QrErrorCorrectionLevel.M, 1, 10, null);
        var png = QrPngRenderer.Render(qr.Modules, new QrPngRenderOptions { ModuleSize = 8, QuietZone = 4 });

        var (rgba, width, height, stride) = PngTestDecoder.DecodeRgba32(png);

        // Simulate UI scaling that introduces non-integer module sizes and anti-aliasing.
        var dstW = Math.Max(32, width - 23);
        var dstH = Math.Max(32, height - 23);
        var scaled = ResizeBilinearRgba32(rgba, width, height, stride, dstW, dstH, out var dstStride);

        Assert.True(QrDecoder.TryDecode(scaled, dstW, dstH, dstStride, PixelFormat.Rgba32, out var decoded));
        Assert.Equal(text, decoded.Text);
    }

    private static byte[] ResizeBilinearRgba32(byte[] src, int srcW, int srcH, int srcStride, int dstW, int dstH, out int dstStride) {
        dstStride = dstW * 4;
        var dst = new byte[dstStride * dstH];

        for (var y = 0; y < dstH; y++) {
            var sy = (y + 0.5) * srcH / dstH - 0.5;
            var y0 = (int)Math.Floor(sy);
            var y1 = y0 + 1;
            var ty = sy - y0;
            if (y0 < 0) { y0 = 0; ty = 0; }
            if (y1 >= srcH) y1 = srcH - 1;

            var row0 = y0 * srcStride;
            var row1 = y1 * srcStride;
            var outRow = y * dstStride;

            for (var x = 0; x < dstW; x++) {
                var sx = (x + 0.5) * srcW / dstW - 0.5;
                var x0 = (int)Math.Floor(sx);
                var x1 = x0 + 1;
                var tx = sx - x0;
                if (x0 < 0) { x0 = 0; tx = 0; }
                if (x1 >= srcW) x1 = srcW - 1;

                var p00 = row0 + x0 * 4;
                var p10 = row0 + x1 * 4;
                var p01 = row1 + x0 * 4;
                var p11 = row1 + x1 * 4;

                for (var c = 0; c < 4; c++) {
                    var v00 = src[p00 + c];
                    var v10 = src[p10 + c];
                    var v01 = src[p01 + c];
                    var v11 = src[p11 + c];

                    var v0 = v00 + (v10 - v00) * tx;
                    var v1 = v01 + (v11 - v01) * tx;
                    var v = v0 + (v1 - v0) * ty;

                    dst[outRow + x * 4 + c] = (byte)Math.Clamp((int)Math.Round(v), 0, 255);
                }
            }
        }

        return dst;
    }
}
