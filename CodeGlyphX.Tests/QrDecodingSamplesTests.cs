using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class QrDecodingSamplesTests {
    [Theory]
    [InlineData("Assets/DecodingSamples/qr-clean-large.png", 1)]
    [InlineData("Assets/DecodingSamples/qr-clean-small.png", 1)]
    [InlineData("Assets/DecodingSamples/qr-dot-aa.png", 1)]
    [InlineData("Assets/DecodingSamples/qr-dot-aa-soft.png", 1)]
    [InlineData("Assets/DecodingSamples/qr-dot-antialiasing-twitter.png", 1)]
    [InlineData("Assets/DecodingSamples/qr-generator-ui.png", 1)]
    public void QrDecode_SampleImages(string relativePath, int minCount) {
        var bytes = ReadRepoFile(relativePath);
        Assert.True(ImageReader.TryDecodeRgba32(bytes, out var rgba, out var width, out var height));

        var options = new QrPixelDecodeOptions {
            Profile = QrDecodeProfile.Robust,
            MaxDimension = 1600,
            BudgetMilliseconds = 2000,
            EnableTileScan = true
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
    public void QrDecode_MultiCodeComposite_FindsAll() {
        var texts = new[] { "HELLO-ONE", "HELLO-TWO", "HELLO-THREE", "HELLO-FOUR" };
        var qrOptions = new QrPngRenderOptions {
            ModuleSize = 4,
            QuietZone = 4
        };

        var cells = texts.Select(text => {
            var qr = QrCodeEncoder.EncodeText(text);
            var pixels = QrPngRenderer.RenderPixels(qr.Modules, qrOptions, out var w, out var h, out var stride);
            return (text, pixels, w, h, stride);
        }).ToArray();

        var cellW = cells[0].w;
        var cellH = cells[0].h;
        var gap = 16;
        var grid = 2;
        var canvasW = grid * cellW + (grid - 1) * gap;
        var canvasH = grid * cellH + (grid - 1) * gap;
        var canvasStride = canvasW * 4;
        var canvas = new byte[canvasStride * canvasH];

        for (var i = 0; i < canvas.Length; i += 4) {
            canvas[i + 0] = 255;
            canvas[i + 1] = 255;
            canvas[i + 2] = 255;
            canvas[i + 3] = 255;
        }

        for (var index = 0; index < cells.Length; index++) {
            var row = index / grid;
            var col = index % grid;
            var x0 = col * (cellW + gap);
            var y0 = row * (cellH + gap);
            var cell = cells[index];
            for (var y = 0; y < cellH; y++) {
                Buffer.BlockCopy(cell.pixels, y * cell.stride, canvas, (y0 + y) * canvasStride + x0 * 4, cellW * 4);
            }
        }

        var decodeOptions = new QrPixelDecodeOptions {
            Profile = QrDecodeProfile.Robust,
            BudgetMilliseconds = 2000,
            EnableTileScan = true,
            TileGrid = 2
        };

        Assert.True(QrDecoder.TryDecodeAll(canvas, canvasW, canvasH, canvasStride, PixelFormat.Rgba32, out var decoded, decodeOptions));
        var decodedText = decoded.Select(d => d.Text).ToArray();
        foreach (var text in texts) {
            Assert.Contains(text, decodedText);
        }
    }

    [Fact]
    public void QrDecode_IllustrationSample_CompletesUnderBudget() {
        var bytes = ReadRepoFile("Assets/DecodingSamples/qr-illustration-template.jpg");
        Assert.True(ImageReader.TryDecodeRgba32(bytes, out var rgba, out var width, out var height));

        var options = new QrPixelDecodeOptions {
            Profile = QrDecodeProfile.Robust,
            BudgetMilliseconds = 1500,
            EnableTileScan = true
        };

        var sw = Stopwatch.StartNew();
        _ = QrDecoder.TryDecodeAll(rgba, width, height, width * 4, PixelFormat.Rgba32, out _, options);
        sw.Stop();

        Assert.True(sw.ElapsedMilliseconds <= 5000);
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

    [Fact]
    public void QrDecode_Wrapper_RespectsCancellation() {
        var qr = QrCodeEncoder.EncodeText("cancel-wrapper");
        var png = QrPngRenderer.Render(qr.Modules, new QrPngRenderOptions {
            ModuleSize = 4,
            QuietZone = 4
        });

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.False(QR.TryDecodePng(png, options: null, cts.Token, out _));
    }

    [Fact]
    public void QrDecode_WrapperImage_RespectsCancellation() {
        var qr = QrCodeEncoder.EncodeText("cancel-image");
        var png = QrPngRenderer.Render(qr.Modules, new QrPngRenderOptions {
            ModuleSize = 4,
            QuietZone = 4
        });

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.False(QR.TryDecodeImage(png, options: null, cts.Token, out _));
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
