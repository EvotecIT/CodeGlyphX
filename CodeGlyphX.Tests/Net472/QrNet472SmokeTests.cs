using System;
using CodeGlyphX.Rendering.Ascii;
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

    [Fact]
    public void Net472_QrImageDecoder_DecodeImageResult_Succeeds() {
        var qr = QrCodeEncoder.EncodeText(Payload);
        var png = QrPngRenderer.Render(qr.Modules, new QrPngRenderOptions {
            ModuleSize = 22,
            QuietZone = 8
        });

        var imageOptions = ImageDecodeOptions.Screen(maxMilliseconds: 700, maxDimension: 1200);
        var result = QrImageDecoder.DecodeImageResult(png, imageOptions, new QrPixelDecodeOptions {
            MaxDimension = 2500
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(DecodeFailureReason.None, result.Failure);
        Assert.Equal(Payload, result.Value?.Text);
    }

    [Fact]
    public void Net472_QrImageDecoder_DecodeImageResult_UnsupportedFormat() {
        var bad = new byte[] { 1, 2, 3, 4, 5 };
        var result = QrImageDecoder.DecodeImageResult(bad);

        Assert.False(result.IsSuccess);
        Assert.Equal(DecodeFailureReason.UnsupportedFormat, result.Failure);
    }

    [Fact]
    public void Net472_FeatureFlags_ReportFallback() {
        Assert.False(CodeGlyphXFeatures.SupportsQrPixelDecode);
        Assert.True(CodeGlyphXFeatures.SupportsQrPixelDecodeFallback);
        Assert.False(CodeGlyphXFeatures.SupportsQrPixelDebug);
        Assert.False(CodeGlyphXFeatures.SupportsSpanPixelPipeline);
        Assert.Equal("net472", CodeGlyphXFeatures.TargetFramework);
    }

    [Fact]
    public void Net472_QrImageDecoder_TileScan_Succeeds_On_Single() {
        var qr = QrCodeEncoder.EncodeText(Payload);
        var png = QrPngRenderer.Render(qr.Modules, new QrPngRenderOptions {
            ModuleSize = 14,
            QuietZone = 6
        });

        var opts = new QrPixelDecodeOptions {
            EnableTileScan = true,
            TileGrid = 2,
            MaxDimension = 1200
        };

        var ok = QrImageDecoder.TryDecodeAllImage(png, imageOptions: null, opts, out var decoded);
        Assert.True(ok);
        Assert.Contains(decoded, d => d.Text == Payload);
    }

    [Fact]
    public void Net472_QrImageDecoder_Decodes_Inverted_Colors() {
        var qr = QrCodeEncoder.EncodeText(Payload);
        var png = QrPngRenderer.Render(qr.Modules, new QrPngRenderOptions {
            ModuleSize = 12,
            QuietZone = 6,
            Foreground = new CodeGlyphX.Rendering.Png.Rgba32(255, 255, 255, 255),
            Background = new CodeGlyphX.Rendering.Png.Rgba32(0, 0, 0, 255)
        });

        var ok = QrImageDecoder.TryDecodeImage(png, out var decoded);
        Assert.True(ok);
        Assert.Equal(Payload, decoded.Text);
    }

    [Fact]
    public void Net472_QrImageDecoder_Decodes_Rotated90_Pixels() {
        var qr = QrCodeEncoder.EncodeText(Payload);
        var pixels = QrPngRenderer.RenderPixels(qr.Modules, new QrPngRenderOptions {
            ModuleSize = 10,
            QuietZone = 6
        }, out var width, out var height, out var stride);

        var rotated = Rotate90(pixels, width, height, stride, out var rw, out var rh, out var rstride);
        var ok = QrImageDecoder.TryDecode(rotated, rw, rh, rstride, PixelFormat.Rgba32, out var decoded);
        Assert.True(ok);
        Assert.Equal(Payload, decoded.Text);
    }

    [Fact]
    public void Net472_AsciiConsole_Render_Succeeds() {
        var qr = QrCodeEncoder.EncodeText("NET472-ASCII");
        var options = new AsciiConsoleOptions {
            UseHalfBlocks = true,
            UseUnicodeBlocks = true,
            UseAnsiColors = true,
            PreferScanReliability = true,
            TargetWidth = 28,
            TargetHeight = 14,
            DarkGradient = new AsciiGradientOptions {
                Type = AsciiGradientType.Horizontal,
                StartColor = new Rgba32(0, 0, 0),
                EndColor = new Rgba32(40, 40, 40)
            }
        };

        var fit = AsciiConsole.Fit(qr.Modules, options);
        var ascii = MatrixAsciiRenderer.Render(qr.Modules, fit);
        Assert.False(string.IsNullOrWhiteSpace(ascii));
        Assert.Contains("\u001b[", ascii, StringComparison.Ordinal);
    }

    [Fact]
    public void Net472_QrImageDecoder_DisableTransforms_Fails_On_Rotation() {
        var qr = QrCodeEncoder.EncodeText(Payload);
        var pixels = QrPngRenderer.RenderPixels(qr.Modules, new QrPngRenderOptions {
            ModuleSize = 10,
            QuietZone = 6
        }, out var width, out var height, out var stride);

        var rotated = Rotate90(pixels, width, height, stride, out var rw, out var rh, out var rstride);
        var ok = QrImageDecoder.TryDecode(rotated, rw, rh, rstride, PixelFormat.Rgba32, new QrPixelDecodeOptions {
            DisableTransforms = true
        }, out _);
        Assert.False(ok);
    }

    [Fact]
    public void Net472_QrImageDecoder_Decodes_Mirrored_Pixels() {
        var qr = QrCodeEncoder.EncodeText(Payload);
        var pixels = QrPngRenderer.RenderPixels(qr.Modules, new QrPngRenderOptions {
            ModuleSize = 10,
            QuietZone = 6
        }, out var width, out var height, out var stride);

        var mirrored = MirrorX(pixels, width, height, stride, out var mw, out var mh, out var mstride);
        var ok = QrImageDecoder.TryDecode(mirrored, mw, mh, mstride, PixelFormat.Rgba32, out var decoded);
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

    private static byte[] Rotate90(byte[] pixels, int width, int height, int stride, out int outWidth, out int outHeight, out int outStride) {
        outWidth = height;
        outHeight = width;
        outStride = outWidth * 4;
        var rotated = new byte[outHeight * outStride];

        for (var y = 0; y < height; y++) {
            var row = y * stride;
            for (var x = 0; x < width; x++) {
                var src = row + x * 4;
                var nx = height - 1 - y;
                var ny = x;
                var dst = ny * outStride + nx * 4;
                rotated[dst + 0] = pixels[src + 0];
                rotated[dst + 1] = pixels[src + 1];
                rotated[dst + 2] = pixels[src + 2];
                rotated[dst + 3] = pixels[src + 3];
            }
        }

        return rotated;
    }

    private static byte[] MirrorX(byte[] pixels, int width, int height, int stride, out int outWidth, out int outHeight, out int outStride) {
        outWidth = width;
        outHeight = height;
        outStride = outWidth * 4;
        var mirrored = new byte[outHeight * outStride];

        for (var y = 0; y < height; y++) {
            var row = y * stride;
            var outRow = y * outStride;
            for (var x = 0; x < width; x++) {
                var src = row + x * 4;
                var nx = width - 1 - x;
                var dst = outRow + nx * 4;
                mirrored[dst + 0] = pixels[src + 0];
                mirrored[dst + 1] = pixels[src + 1];
                mirrored[dst + 2] = pixels[src + 2];
                mirrored[dst + 3] = pixels[src + 3];
            }
        }

        return mirrored;
    }

}
