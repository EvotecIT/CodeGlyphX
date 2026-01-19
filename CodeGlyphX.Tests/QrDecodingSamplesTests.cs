using System;
using System.IO;
using System.Threading;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class QrDecodingSamplesTests {
    [Theory]
    [InlineData("Assets/DecodingSamples/qr-clean-large.png", 1)]
    [InlineData("Assets/DecodingSamples/qr-clean-small.png", 1)]
    public void QrDecode_SampleImages(string relativePath, int minCount) {
        var bytes = ReadRepoFile(relativePath);
        Assert.True(ImageReader.TryDecodeRgba32(bytes, out var rgba, out var width, out var height));

        var options = new QrPixelDecodeOptions {
            Profile = QrDecodeProfile.Robust,
            MaxDimension = 1600,
            MaxMilliseconds = 2000
        };

        if (!QrDecoder.TryDecodeAll(rgba, width, height, width * 4, PixelFormat.Rgba32, out var results, options)) {
            if (QrDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, out var decoded, out var info, options)) {
                results = new[] { decoded };
            } else {
                Assert.Fail(info.ToString());
            }
        }

        Assert.True(results.Length >= minCount);
    }

    [Fact]
    public void QrDecode_PixelDecode_RespectsCancellation() {
        var qr = QrCodeEncoder.EncodeText("cancel");
        var png = QrPngRenderer.Render(qr.Modules, new QrPngRenderOptions {
            ModuleSize = 4,
            QuietZone = 4
        });
        Assert.True(ImageReader.TryDecodeRgba32(png, out var rgba, out var width, out var height));

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var ok = QrDecoder.TryDecode(
            rgba,
            width,
            height,
            width * 4,
            PixelFormat.Rgba32,
            out _,
            out var info,
            new QrPixelDecodeOptions { Profile = QrDecodeProfile.Robust },
            cts.Token);

        Assert.False(ok);
        Assert.Equal(QrDecodeFailureReason.Cancelled, info.Module.Failure);
    }

    private static byte[] ReadRepoFile(string relativePath) {
        if (string.IsNullOrWhiteSpace(relativePath)) {
            throw new ArgumentException("Path is required.", nameof(relativePath));
        }

        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 10 && dir is not null; i++) {
            var candidate = Path.Combine(dir.FullName, relativePath);
            if (File.Exists(candidate)) {
                return File.ReadAllBytes(candidate);
            }
            dir = dir.Parent;
        }

        throw new FileNotFoundException($"Could not locate sample file '{relativePath}'.");
    }
}
