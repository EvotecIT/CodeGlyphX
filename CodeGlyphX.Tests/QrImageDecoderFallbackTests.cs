using CodeGlyphX.Rendering.Jpeg;
using CodeGlyphX.Rendering.Png;
using System;
using System.Reflection;
using System.Threading;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("GlobalState")]
public sealed class QrImageDecoderFallbackTests {
    private const string Payload = "LEGACY-FALLBACK-ROUNDTRIP";

    [Fact]
    public void Fallback_Decodes_Large_Png_Render() {
        var png = RenderPng(moduleSize: 18, quietZone: 6);
        WithForcedFallback(() => {
            var ok = QrImageDecoder.TryDecodeImage(png, out var decoded, out var info, new QrPixelDecodeOptions {
                MaxDimension = 2048
            });
            Assert.True(ok);
            Assert.Equal(Payload, decoded.Text);
            Assert.True(info.Dimension >= 21);
        });
    }

    [Fact]
    public void Fallback_Decodes_Large_Jpeg_Render() {
        var jpg = RenderJpeg(moduleSize: 18, quietZone: 6, quality: 100);
        WithForcedFallback(() => {
            var ok = QrImageDecoder.TryDecodeImage(jpg, out var decoded, out var info, new QrPixelDecodeOptions {
                MaxDimension = 2048
            });
            Assert.True(ok);
            Assert.Equal(Payload, decoded.Text);
            Assert.True(info.Dimension >= 21);
        });
    }

    [Fact]
    public void Fallback_Decodes_EqualLuminance_ColorContrast_Png() {
        var qr = QrCodeEncoder.EncodeText(Payload);
        var png = QrPngRenderer.Render(qr.Modules, new QrPngRenderOptions {
            ModuleSize = 18,
            QuietZone = 6,
            Foreground = new Rgba32(255, 0, 0, 255),
            // Similar luminance to red, but strong channel separation.
            Background = new Rgba32(0, 110, 100, 255)
        });

        WithForcedFallback(() => {
            var ok = QrImageDecoder.TryDecodeImage(png, out var decoded, out var info, new QrPixelDecodeOptions {
                MaxDimension = 2048
            });
            Assert.True(ok);
            Assert.Equal(Payload, decoded.Text);
            Assert.True(info.Dimension >= 21);
        });
    }

    [Fact]
    [Trait("Category", "CorpusTiming")]
    public void Fallback_BudgetedTileScan_PreservesFullImageResult() {
        var png = RenderPng(moduleSize: 14, quietZone: 6);

        WithForcedFallback(() => {
            var ok = QrImageDecoder.TryDecodeAllImage(png, imageOptions: null, new QrPixelDecodeOptions {
                EnableTileScan = true,
                TileGrid = 2,
                MaxDimension = 1200,
                BudgetMilliseconds = 1000
            }, out var decoded);

            Assert.True(ok);
            Assert.Contains(decoded, result => result.Text == Payload);
        });
    }

    [Fact]
    [Trait("Category", "CorpusTiming")]
    public void Fallback_ImageRecognitionBudgetReturnsFalseWithoutThrowing() {
        // A large blank raster makes preprocessing exceed the tiny recognition budget.
        var png = QrPngRenderer.Render(QrCodeEncoder.EncodeText(Payload).Modules,
            new QrPngRenderOptions { ModuleSize = 100, Foreground = new Rgba32(255, 255, 255), Background = new Rgba32(255, 255, 255) });
        var imageOptions = new ImageDecodeOptions { RecognitionBudgetMilliseconds = 1 };
        var options = new QrPixelDecodeOptions { MaxDimension = 0 };
        WithForcedFallback(() => {
            Assert.False(QrImageDecoder.TryDecodeImage(png, imageOptions, options, out var single));
            Assert.Null(single);
            Assert.False(QrImageDecoder.TryDecodeAllImage(png, imageOptions, options, out var all));
            Assert.Empty(all);
        });

        // Modern forced fallback uses the single-symbol route; exercise the legacy all-symbol boundary too.
        var arguments = new object?[] { png, imageOptions, options, CancellationToken.None, null };
        Assert.False((bool)GetPrivateMethod("TryDecodeAllImageFallback").Invoke(null, arguments)!);
        Assert.Empty((QrDecoded[])arguments[4]!);
    }

    [Fact]
    public void Fallback_Helper_Preprocessing_Branches_Work() {
        var buildGray = GetPrivateMethod("BuildGrayscale");
        var buildChannel = GetPrivateMethod("BuildChannelGrayscale");
        var tryContrastStretch = GetPrivateMethod("TryContrastStretch");
        var tryLocalNormalize = GetPrivateMethod("TryLocalNormalize");

        var rgba = new byte[] {
            255, 0, 0, 128,
            0, 110, 100, 255
        };

        var gray = (byte[])buildGray.Invoke(null, new object[] { rgba, 2, 1, 8, CancellationToken.None })!;
        Assert.Equal(2, gray.Length);

        var red = (byte[])buildChannel.Invoke(null, new object[] { rgba, 2, 1, 8, 0, CancellationToken.None })!;
        var green = (byte[])buildChannel.Invoke(null, new object[] { rgba, 2, 1, 8, 1, CancellationToken.None })!;
        var blue = (byte[])buildChannel.Invoke(null, new object[] { rgba, 2, 1, 8, 2, CancellationToken.None })!;
        var chroma = (byte[])buildChannel.Invoke(null, new object[] { rgba, 2, 1, 8, 3, CancellationToken.None })!;

        Assert.Equal(255, red[0]);
        Assert.Equal(127, green[0]);
        Assert.Equal(127, blue[0]);
        Assert.True(chroma[0] > 0);
        Assert.True(red[0] > red[1]);
        Assert.True(green[0] > green[1]);
        Assert.True(blue[1] < blue[0]);

        var noStretchArgs = new object?[] { new byte[] { 42, 42, 42 }, null, CancellationToken.None };
        var noStretch = (bool)tryContrastStretch.Invoke(null, noStretchArgs)!;
        Assert.False(noStretch);
        Assert.Empty((byte[])noStretchArgs[1]!);

        var stretchArgs = new object?[] { new byte[] { 10, 200 }, null, CancellationToken.None };
        var stretched = (bool)tryContrastStretch.Invoke(null, stretchArgs)!;
        Assert.True(stretched);
        Assert.Equal(new byte[] { 0, 255 }, (byte[])stretchArgs[1]!);

        var smallNormalizeArgs = new object?[] { new byte[] { 1, 2, 3, 4 }, 2, 2, null, CancellationToken.None };
        var smallNormalize = (bool)tryLocalNormalize.Invoke(null, smallNormalizeArgs)!;
        Assert.False(smallNormalize);
        Assert.Empty((byte[])smallNormalizeArgs[3]!);

        var gradient = new byte[21 * 21];
        for (var i = 0; i < gradient.Length; i++) {
            gradient[i] = (byte)(i % 251);
        }

        var normalizeArgs = new object?[] { gradient, 21, 21, null, CancellationToken.None };
        var normalized = (bool)tryLocalNormalize.Invoke(null, normalizeArgs)!;
        Assert.True(normalized);
        Assert.Equal(gradient.Length, ((byte[])normalizeArgs[3]!).Length);
    }

    [Fact]
    public void Fallback_PreprocessingRejectsCancellationBeforeReadingPixels() {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var token = cancellation.Token;
        var cases = new (string Name, object?[] Arguments)[] {
            ("BuildGrayscale", new object?[] { Array.Empty<byte>(), 1000000, 1, 4000000, token }),
            ("BuildChannelGrayscale", new object?[] { Array.Empty<byte>(), 1000000, 1, 4000000, 0, token }),
            ("ConvertBgraToRgba", new object?[] { Array.Empty<byte>(), 1000000, 1, 4000000, token }),
            ("InvertGrayscale", new object?[] { Array.Empty<byte>(), token }),
            ("ComputeGrayStats", new object?[] { Array.Empty<byte>(), (byte)0, (byte)0, 0, (byte)0, token }),
            ("TryContrastStretch", new object?[] { Array.Empty<byte>(), null, token }),
            ("TryLocalNormalize", new object?[] { Array.Empty<byte>(), 1000000, 100, null, token }),
            ("TryFindDarkBounds", new object?[] { Array.Empty<byte>(), 1000000, 1, (byte)128, 0, 0, 0, 0, token })
        };
        foreach (var item in cases) {
            var exception = Assert.Throws<TargetInvocationException>(() => GetPrivateMethod(item.Name).Invoke(null, item.Arguments));
            Assert.IsType<OperationCanceledException>(exception.InnerException);
        }
    }

    private static byte[] RenderPng(int moduleSize, int quietZone) {
        var qr = QrCodeEncoder.EncodeText(Payload);
        return QrPngRenderer.Render(qr.Modules, new QrPngRenderOptions {
            ModuleSize = moduleSize,
            QuietZone = quietZone
        });
    }

    private static byte[] RenderJpeg(int moduleSize, int quietZone, int quality) {
        var qr = QrCodeEncoder.EncodeText(Payload);
        return QrJpegRenderer.Render(qr.Modules, new QrPngRenderOptions {
            ModuleSize = moduleSize,
            QuietZone = quietZone
        }, quality);
    }

    private static void WithForcedFallback(System.Action action) {
        var previous = CodeGlyphXFeatures.ForceQrFallbackForTests;
        CodeGlyphXFeatures.ForceQrFallbackForTests = true;
        try {
            action();
        } finally {
            CodeGlyphXFeatures.ForceQrFallbackForTests = previous;
        }
    }

    private static MethodInfo GetPrivateMethod(string name) {
        return typeof(QrImageDecoder).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static)
               ?? throw new InvalidOperationException("Expected private method not found: " + name);
    }
}
