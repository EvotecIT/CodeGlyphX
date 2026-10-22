using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using CodeGlyphX.Internal;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("ImageScannerSerial")]
public sealed class QrFallbackDeadlineTests {
    [Theory]
    [InlineData(2)]
    [InlineData(11)]
    public void NearestResizePollsTheInheritedDeadlineDuringFillAndSampling(int abortAt) {
        var source = new byte[4 * 4 * 4];
        var original = (byte[])source.Clone();
        var polls = 0;
        Assert.Throws<OperationCanceledException>(() => ImageScaler.ResizeToFitNearest(
            source, 4, 4, 16, 4, 4, Rgba32.White, false, CancellationToken.None,
            () => {
                if (++polls == abortAt) throw new OperationCanceledException();
            }));
        Assert.Equal(abortAt, polls);
        Assert.Equal(original, source);
    }

    [Fact]
    public void PreprocessingPollsTheDeadlineWithoutWaitingForTimerCancellation() {
        var cases = new (string Name, object?[] Arguments)[] {
            ("BuildGrayscale", new object?[] { Array.Empty<byte>(), 4, 4, 16, CancellationToken.None }),
            ("BuildChannelGrayscale", new object?[] { Array.Empty<byte>(), 4, 4, 16, 0, CancellationToken.None }),
            ("ConvertBgraToRgba", new object?[] { Array.Empty<byte>(), 4, 4, 16, CancellationToken.None }),
            ("InvertGrayscale", new object?[] { Array.Empty<byte>(), CancellationToken.None }),
            ("ComputeGrayStats", new object?[] { Array.Empty<byte>(), (byte)0, (byte)0, 0, (byte)0, CancellationToken.None }),
            ("TryContrastStretch", new object?[] { Array.Empty<byte>(), null, CancellationToken.None }),
            ("TryLocalNormalize", new object?[] { Array.Empty<byte>(), 4, 4, null, CancellationToken.None }),
            ("TryFindDarkBounds", new object?[] { Array.Empty<byte>(), 4, 4, (byte)128, 0, 0, 0, 0, CancellationToken.None }),
            ("CropRgba", new object?[] { Array.Empty<byte>(), 4, 4, 16, 0, 0, 2, 2, CancellationToken.None }),
            ("EstimateDimension", new object?[] { Array.Empty<byte>(), 4, 0, 0, 3, 3, (byte)128, CancellationToken.None }),
            ("EstimateModuleSize", new object?[] { Array.Empty<byte>(), 4, 4, 0, 0, 3, 3, (byte)128, CancellationToken.None })
        };
        using (DecodeBudget.Begin(1)) {
            Assert.True(SpinWait.SpinUntil(() => DecodeBudget.IsExpired, TimeSpan.FromSeconds(1)));
            foreach (var item in cases) {
                var method = typeof(QrImageDecoder).GetMethod(item.Name, BindingFlags.Static | BindingFlags.NonPublic)!;
                var exception = Assert.Throws<TargetInvocationException>(() => method.Invoke(null, item.Arguments));
                Assert.IsType<OperationCanceledException>(exception.InnerException);
            }
        }
    }

    [Theory]
    [InlineData("Raw")]
    [InlineData("RawAll")]
    [InlineData("Image")]
    [InlineData("ImageAll")]
    public void FallbackDoesNotRestartAnExpiredEnclosingDeadline(string route) {
        var qr = QrCodeEncoder.EncodeText("FALLBACK-DEADLINE");
        var options = new QrPngRenderOptions { ModuleSize = 8, QuietZone = 4 };
        var png = QrPngRenderer.Render(qr.Modules, options);
        var pixels = QrPngRenderer.RenderPixels(qr.Modules, options, out var width, out var height, out var stride);
        var name = route switch {
            "Raw" => "TryDecodeFallback",
            "RawAll" => "TryDecodeAllFallback",
            "Image" => "TryDecodeImageFallback",
            _ => "TryDecodeAllImageFallback"
        };
        var method = typeof(QrImageDecoder).GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
            .Single(m => m.Name == name && (route != "Image" || m.GetParameters().Length == 6
                && m.GetParameters()[1].ParameterType == typeof(ImageDecodeOptions)));
        object?[] arguments = route switch {
            "Raw" => new object?[] { pixels, width, height, stride, PixelFormat.Rgba32, null, CancellationToken.None, null, null },
            "RawAll" => new object?[] { pixels, width, height, stride, PixelFormat.Rgba32, null, CancellationToken.None, null, true },
            "Image" => new object?[] { png, null, null, CancellationToken.None, null, null },
            _ => new object?[] { png, null, null, CancellationToken.None, null }
        };
        // This private boundary selects the legacy implementation on every target framework.
        Assert.True((bool)method.Invoke(null, arguments)!);
        using (DecodeBudget.Begin(1)) {
            Assert.True(SpinWait.SpinUntil(() => DecodeBudget.IsExpired, TimeSpan.FromSeconds(1)));
            Assert.False((bool)method.Invoke(null, arguments)!);
        }
    }
}
