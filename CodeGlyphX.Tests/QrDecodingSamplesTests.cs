using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using CodeGlyphX;
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
    [InlineData("Assets/DecodingSamples/qr-art-dots-variants.png", 1)]
    [InlineData("Assets/DecodingSamples/qr-art-jess3-characters-grid.png", 1)]
    [InlineData("Assets/DecodingSamples/qr-art-jess3-characters-splash.png", 1)]
    [InlineData("Assets/DecodingSamples/qr-art-jess3-characters-splash-variant.png", 1)]
    public void QrDecode_SampleImages(string relativePath, int minCount) {
        var bytes = ReadRepoFile(relativePath);
        Assert.True(ImageReader.TryDecodeRgba32(bytes, out var rgba, out var width, out var height));

        var options = new QrPixelDecodeOptions {
            Profile = QrDecodeProfile.Robust,
            MaxDimension = 2200,
            BudgetMilliseconds = TestBudget.Adjust(2000),
            AggressiveSampling = true,
            EnableTileScan = true
        };

        var fallbackOptions = new QrPixelDecodeOptions {
            Profile = QrDecodeProfile.Robust,
            MaxDimension = 2000,
            MaxScale = 3,
            MaxMilliseconds = TestBudget.Adjust(2000),
            BudgetMilliseconds = TestBudget.Adjust(6000),
            AutoCrop = true,
            AggressiveSampling = true,
            StylizedSampling = false,
            EnableTileScan = true,
            TileGrid = 4
        };

        var fallbackStylized = new QrPixelDecodeOptions {
            Profile = QrDecodeProfile.Robust,
            MaxDimension = 2400,
            MaxScale = 4,
            MaxMilliseconds = TestBudget.Adjust(3000),
            BudgetMilliseconds = TestBudget.Adjust(9000),
            AutoCrop = true,
            AggressiveSampling = true,
            StylizedSampling = true,
            EnableTileScan = true,
            TileGrid = 4
        };

        var fallbackHeavy = new QrPixelDecodeOptions {
            Profile = QrDecodeProfile.Robust,
            MaxDimension = 3200,
            MaxScale = 6,
            MaxMilliseconds = TestBudget.Adjust(5000),
            BudgetMilliseconds = TestBudget.Adjust(15000),
            AutoCrop = true,
            AggressiveSampling = true,
            StylizedSampling = true,
            EnableTileScan = true,
            TileGrid = 6
        };

        if (!TryDecodeResults(rgba, width, height, width * 4, out var results, out var diagnostics, options, fallbackOptions, fallbackStylized, fallbackHeavy)) {
            Assert.Fail(diagnostics);
        }

        Assert.True(results.Length >= minCount);
    }

    [Theory]
    [InlineData("Assets/DecodingSamples/qr-clean-small.png", "otpauth://totp/Evotec+Services+sp.+z+o.o.%3aprzemyslaw.klys%40evotec.pl?secret=jnll6mrqknd57pmn&issuer=Microsoft")]
    [InlineData("Assets/DecodingSamples/qr-clean-large.png", "This is a quick test! 123#?")]
    [InlineData("Assets/DecodingSamples/qr-dot-aa.png", "DOT-AA")]
    [InlineData("Assets/DecodingSamples/qr-dot-aa-soft.png", "DOT-AA-SOFT")]
    [InlineData("Assets/DecodingSamples/qr-dot-antialiasing-twitter.png", "This is a quick test! 123#?")]
    [InlineData("Assets/DecodingSamples/qr-generator-ui.png", "https://qrstud.io/qrmnky")]
    [InlineData("Assets/DecodingSamples/qr-noisy-ui.png", "otpauth://totp/Evotec+Services+sp.+z+o.o.%3aprzemyslaw.klys%40evotec.pl?secret=pqhjwcgzncvzykhd&issuer=Microsoft")]
    [InlineData("Assets/DecodingSamples/qr-art-dots-variants.png", "http://jess3.com")]
    [InlineData("Assets/DecodingSamples/qr-art-jess3-characters-grid.png", "http://jess3.com")]
    [InlineData("Assets/DecodingSamples/qr-art-jess3-characters-splash.png", "http://jess3.com")]
    [InlineData("Assets/DecodingSamples/qr-art-jess3-characters-splash-variant.png", "http://jess3.com")]
    public void QrDecode_SampleImages_ReturnExpectedText(string relativePath, string expectedText) {
        var bytes = ReadRepoFile(relativePath);
        Assert.True(ImageReader.TryDecodeRgba32(bytes, out var rgba, out var width, out var height));

        var options = new QrPixelDecodeOptions {
            Profile = QrDecodeProfile.Robust,
            MaxDimension = 1600,
            MaxMilliseconds = TestBudget.Adjust(800),
            BudgetMilliseconds = TestBudget.Adjust(2000),
            AggressiveSampling = true,
            EnableTileScan = true
        };

        var fallbackOptions = new QrPixelDecodeOptions {
            Profile = QrDecodeProfile.Robust,
            MaxDimension = 2000,
            MaxScale = 3,
            MaxMilliseconds = TestBudget.Adjust(2000),
            BudgetMilliseconds = TestBudget.Adjust(6000),
            AutoCrop = true,
            AggressiveSampling = true,
            StylizedSampling = false,
            EnableTileScan = true,
            TileGrid = 4
        };

        var fallbackStylized = new QrPixelDecodeOptions {
            Profile = QrDecodeProfile.Robust,
            MaxDimension = 2400,
            MaxScale = 4,
            MaxMilliseconds = TestBudget.Adjust(3000),
            BudgetMilliseconds = TestBudget.Adjust(9000),
            AutoCrop = true,
            AggressiveSampling = true,
            StylizedSampling = true,
            EnableTileScan = true,
            TileGrid = 4
        };

        var fallbackHeavy = new QrPixelDecodeOptions {
            Profile = QrDecodeProfile.Robust,
            MaxDimension = 3200,
            MaxScale = 6,
            MaxMilliseconds = TestBudget.Adjust(5000),
            BudgetMilliseconds = TestBudget.Adjust(15000),
            AutoCrop = true,
            AggressiveSampling = true,
            StylizedSampling = true,
            EnableTileScan = true,
            TileGrid = 6
        };

        var texts = DecodeTexts(rgba, width, height, width * 4, options, fallbackOptions, fallbackStylized, fallbackHeavy);
        Assert.Contains(expectedText, texts);
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
            BudgetMilliseconds = TestBudget.Adjust(2000),
            EnableTileScan = true,
            TileGrid = 2
        };

        Assert.True(QrDecoder.TryDecodeAll(canvas, canvasW, canvasH, canvasStride, PixelFormat.Rgba32, out var decoded, decodeOptions));
        var decodedText = decoded.Select(d => d.Text).ToArray();
        foreach (var text in texts) {
            Assert.Contains(text, decodedText);
        }
    }

    [Theory(Skip = "Pending stylized finder/decoder improvements for heavy illustration samples.")]
    [InlineData("Assets/DecodingSamples/qr-art-facebook-splash-grid.png", 1)]
    [InlineData("Assets/DecodingSamples/qr-art-montage-grid.png", 1)]
    [InlineData("Assets/DecodingSamples/qr-art-stripe-eye-grid.png", 1)]
    [InlineData("Assets/DecodingSamples/qr-art-drip-variants.png", 1)]
    [InlineData("Assets/DecodingSamples/qr-art-solid-bg-grid.png", 1)]
    [InlineData("Assets/DecodingSamples/qr-art-gear-illustration-grid.png", 1)]
    public void QrDecode_StylizedIllustrationSamples_Pending(string relativePath, int minCount) {
        var bytes = ReadRepoFile(relativePath);
        Assert.True(ImageReader.TryDecodeRgba32(bytes, out var rgba, out var width, out var height));

        var options = new QrPixelDecodeOptions {
            Profile = QrDecodeProfile.Robust,
            MaxDimension = 2200,
            BudgetMilliseconds = 2500,
            AggressiveSampling = true,
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
    public void QrDecode_NoisyUiSample_DecodesUnderBudget() {
        var bytes = ReadRepoFile("Assets/DecodingSamples/qr-noisy-ui.png");
        Assert.True(ImageReader.TryDecodeRgba32(bytes, out var rgba, out var width, out var height));

        var options = new QrPixelDecodeOptions {
            Profile = QrDecodeProfile.Robust,
            MaxMilliseconds = TestBudget.Adjust(2000),
            BudgetMilliseconds = TestBudget.Adjust(2000),
            MaxDimension = 1600,
            AggressiveSampling = true
        };

        Assert.True(QrDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, out var decoded, options));
        Assert.Contains("otpauth://", decoded.Text, StringComparison.OrdinalIgnoreCase);
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

    private static string[] DecodeTexts(byte[] rgba, int width, int height, int stride, params QrPixelDecodeOptions[] options) {
        if (!TryDecodeResults(rgba, width, height, stride, out var results, out var diagnostics, options)) {
            Assert.Fail(diagnostics);
        }

        return results
            .Select(result => result.Text)
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private static bool TryDecodeResults(
        byte[] rgba,
        int width,
        int height,
        int stride,
        out QrDecoded[] results,
        out string diagnostics,
        params QrPixelDecodeOptions[] options) {
        results = Array.Empty<QrDecoded>();
        diagnostics = "No decode options provided.";

        if (options is null || options.Length == 0) return false;

        for (var i = 0; i < options.Length; i++) {
            var option = options[i];
            QrPixelDecodeInfo info = default;
            if (QrDecoder.TryDecode(rgba, width, height, stride, PixelFormat.Rgba32, out var decoded, out info, option)) {
                results = new[] { decoded };
                diagnostics = info.ToString();
                return true;
            }

            diagnostics = info.ToString();

            if (i == 0 && QrDecoder.TryDecodeAll(rgba, width, height, stride, PixelFormat.Rgba32, out var decodedList, option)) {
                if (decodedList.Length > 0) {
                    results = decodedList;
                    diagnostics = string.Empty;
                    return true;
                }

                diagnostics = "Decode produced no results.";
            }
        }

        return false;
    }
}
