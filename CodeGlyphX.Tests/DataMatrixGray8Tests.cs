using System;
using System.Threading;
using CodeGlyphX.DataMatrix;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class DataMatrixGray8Tests {
    [Theory]
    [InlineData(0, false, false)]
    [InlineData(1, false, false)]
    [InlineData(2, true, false)]
    [InlineData(3, false, true)]
    public void PaddedGrayPixels_PreservePayloadMetadataAcrossPixelApis(int rotation, bool mirrored, bool inverted) {
        const string payload = "010123456789012810GRAY42";
        var modules = DataMatrixEncoder.Encode(payload, new DataMatrixEncodingOptions {
            Rows = 24, Columns = 24, IsGs1 = true
        });
        const int scale = 4;
        const int quiet = 3;
        var width = (modules.Width + quiet * 2) * scale;
        var height = (modules.Height + quiet * 2) * scale;
        var stride = width + 7;
        // Opaque black padding must not contribute to finder bounds. Threshold
        // neighbors 127 and 128 also exercise the exact grayscale intensity rule.
        var pixels = new byte[(height - 1) * stride + width];
        for (var y = 0; y < height; y++) for (var x = 0; x < width; x++) {
            var mx = x / scale - quiet;
            var my = y / scale - quiet;
            var dark = false;
            if ((uint)mx < modules.Width && (uint)my < modules.Height) {
                var sx = rotation == 1 ? my : rotation == 2 ? modules.Width - 1 - mx : rotation == 3 ? modules.Width - 1 - my : mx;
                var sy = rotation == 1 ? modules.Height - 1 - mx : rotation == 2 ? modules.Height - 1 - my : rotation == 3 ? mx : my;
                dark = modules[mirrored ? modules.Width - 1 - sx : sx, sy];
            }
            pixels[y * stride + x] = dark != inverted ? (byte)127 : (byte)128;
        }

        Assert.True(DataMatrixDecoder.TryDecode(pixels, width, height, stride, PixelFormat.Gray8, out var text));
        Assert.Equal(payload, text);
        Assert.True(DataMatrixDecoder.TryDecode(pixels, width, height, stride, PixelFormat.Gray8, out text, out var diagnostics));
        Assert.Equal(payload, text);
        Assert.True(diagnostics.Success);
        Assert.True(DataMatrixDecoder.TryDecodeDetailed(pixels, width, height, stride, PixelFormat.Gray8, out var decoded));
        Assert.Equal(payload, decoded.Text);
        Assert.True(decoded.IsGs1);
        Assert.Equal(24, decoded.Rows);
        Assert.Equal(24, decoded.Columns);
#if NET8_0_OR_GREATER
        Assert.True(DataMatrixDecoder.TryDecode(pixels.AsSpan(), width, height, stride, PixelFormat.Gray8, out text));
        Assert.Equal(payload, text);
        Assert.True(DataMatrixDecoder.TryDecode(pixels.AsSpan(), width, height, stride, PixelFormat.Gray8, out text, out diagnostics));
        Assert.Equal(payload, text);
        Assert.True(diagnostics.Success);
        Assert.True(DataMatrixDecoder.TryDecodeDetailed(pixels.AsSpan(), width, height, stride, PixelFormat.Gray8, out decoded));
        Assert.True(decoded.IsGs1);
        Assert.Equal(payload, decoded.Text);
#endif
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        Assert.False(DataMatrixDecoder.TryDecode(pixels, width, height, stride, PixelFormat.Gray8, cancelled.Token, out _));
        Assert.False(DataMatrixDecoder.TryDecode(pixels, width, height, stride, PixelFormat.Gray8, cancelled.Token, out _, out diagnostics));
        Assert.False(diagnostics.Success);
        Assert.Equal("Cancelled.", diagnostics.Failure);
        Assert.False(DataMatrixDecoder.TryDecodeDetailed(pixels, width, height, stride, PixelFormat.Gray8, cancelled.Token, out _));
    }

    [Theory]
    [InlineData(20, 20, 19, 400, PixelFormat.Gray8)]
    [InlineData(20, 20, 27, 532, PixelFormat.Gray8)]
    [InlineData(20, 20, 79, 1600, PixelFormat.Rgba32)]
    [InlineData(20, 20, 80, 1599, PixelFormat.Bgra32)]
    [InlineData(int.MaxValue, int.MaxValue, int.MaxValue, 0, PixelFormat.Gray8)]
    [InlineData(20, 20, 80, 1600, PixelFormat.Rgb24)]
    public void InvalidPixelLayouts_ReturnFailureBeforeSampling(int width, int height, int stride, int length, PixelFormat format) {
        var pixels = new byte[length];
        Assert.False(DataMatrixDecoder.TryDecode(pixels, width, height, stride, format, out _));
        Assert.False(DataMatrixDecoder.TryDecode(pixels, width, height, stride, format, out _, out var diagnostics));
        Assert.False(diagnostics.Success);
        Assert.False(DataMatrixDecoder.TryDecodeDetailed(pixels, width, height, stride, format, out _));
#if NET8_0_OR_GREATER
        Assert.False(DataMatrixDecoder.TryDecode(pixels.AsSpan(), width, height, stride, format, out _));
        Assert.False(DataMatrixDecoder.TryDecode(pixels.AsSpan(), width, height, stride, format, out _, out diagnostics));
        Assert.False(diagnostics.Success);
        Assert.False(DataMatrixDecoder.TryDecodeDetailed(pixels.AsSpan(), width, height, stride, format, out _));
#endif
    }
}
