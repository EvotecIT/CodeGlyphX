using System;
using CodeGlyphX.Rendering;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class QrMultiDecodeTests {
    [Fact]
    public void DecodeAll_FindsTwoQrCodes() {
        var left = QrEasy.RenderPixels("LEFT-QR", out var w1, out var h1, out var s1);
        var right = QrEasy.RenderPixels("RIGHT-QR", out var w2, out var h2, out var s2);

        var pad = 12;
        var width = w1 + w2 + pad * 3;
        var height = Math.Max(h1, h2) + pad * 2;
        var stride = width * 4;
        var canvas = new byte[height * stride];

        for (var i = 0; i < canvas.Length; i += 4) {
            canvas[i] = 255;
            canvas[i + 1] = 255;
            canvas[i + 2] = 255;
            canvas[i + 3] = 255;
        }

        Blit(left, w1, h1, s1, canvas, width, height, stride, pad, pad);
        Blit(right, w2, h2, s2, canvas, width, height, stride, pad * 2 + w1, pad);

        Assert.True(QrDecoder.TryDecodeAll(canvas, width, height, stride, PixelFormat.Rgba32, out var results));
        Assert.Contains(results, r => r.Text == "LEFT-QR");
        Assert.Contains(results, r => r.Text == "RIGHT-QR");
    }

    private static void Blit(byte[] src, int srcW, int srcH, int srcStride, byte[] dest, int destW, int destH, int destStride, int offsetX, int offsetY) {
        for (var y = 0; y < srcH; y++) {
            var dy = offsetY + y;
            if ((uint)dy >= (uint)destH) break;
            Buffer.BlockCopy(src, y * srcStride, dest, dy * destStride + offsetX * 4, srcW * 4);
        }
    }
}
