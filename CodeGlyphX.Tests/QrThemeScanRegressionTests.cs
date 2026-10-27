using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("ImageScannerSerial")]
public sealed class QrThemeScanRegressionTests {
    public static IEnumerable<object[]> SmallDeliveryCases() {
        foreach (var theme in new[] { QrArtTheme.NeonGlow, QrArtTheme.LiquidGlass,
            QrArtTheme.InsetRings, QrArtTheme.StripeEyes, QrArtTheme.PaintSplash }) {
            foreach (var variant in Enum.GetValues<QrArtVariant>()) {
                if (theme == QrArtTheme.LiquidGlass && variant == QrArtVariant.Bold) continue;
                if (theme == QrArtTheme.PaintSplash && variant != QrArtVariant.Conservative) continue;
                foreach (var logo in new[] { false, true }) yield return new object[] { theme, variant, logo };
            }
        }
    }

    [Theory]
    [MemberData(nameof(SmallDeliveryCases))]
    public void Themes_RetainStrictLongUrlScans_AtSmallDeliverySize(QrArtTheme theme, QrArtVariant variant, bool logo) {
        var options = new QrRenderOptions {
            TargetSizePx = 1024, Art = QrArt.Theme(theme, variant),
            LogoPng = logo ? QrArtPremiumScanTests.Logo() : null
        };
        var payload = QrArtPremiumScanTests.LongUrl;
        var png = QR.Render(payload, OutputFormat.Png, options,
            new QrEncodingOptions { ErrorCorrectionLevel = QrErrorCorrectionLevel.H }).ToArray();
        var transform = theme == QrArtTheme.PaintSplash ? "rotate2deg" : "blur121";
        var result = Assert.Single(QrArtWave1ScanTests.ScanImage(png, payload),
            cell => cell.Size == 256 && cell.Transform == transform);
        Assert.True(result.StrictPassed, result.ToString());
        Assert.True(result.LibraryPassed, result.ToString());
    }

    public static IEnumerable<object[]> LongUrlStrictCases() {
        foreach (var logo in new[] { false, true }) {
            yield return new object[] { QrArtTheme.ConnectedSquircleGlow, QrArtVariant.Bold, 256, "blur121", logo };
            foreach (var variant in Enum.GetValues<QrArtVariant>())
                yield return new object[] { QrArtTheme.PaintSplash, variant, 1024, "rotate2deg", logo };
        }
    }

    [Theory]
    [MemberData(nameof(LongUrlStrictCases))]
    public void Themes_RetainStrictLongUrlScans_AtDeliverySizeAndTransform(QrArtTheme theme, QrArtVariant variant, int size,
        string transform, bool logo) {
        var options = new QrRenderOptions {
            TargetSizePx = 1024, Art = QrArt.Theme(theme, variant),
            LogoPng = logo ? QrArtPremiumScanTests.Logo() : null
        };
        var payload = QrArtPremiumScanTests.LongUrl;
        var png = QR.Render(payload, OutputFormat.Png, options,
            new QrEncodingOptions { ErrorCorrectionLevel = QrErrorCorrectionLevel.H }).ToArray();
        var result = Assert.Single(QrArtWave1ScanTests.ScanImage(png, payload),
            cell => cell.Size == size && cell.Transform == transform);
        Assert.True(result.StrictPassed, result.ToString());
        Assert.True(result.LibraryPassed, result.ToString());
    }

    // A palette stop below 4.5:1 contrast makes the guardrails replace the whole palette with neutral ink,
    // so the theme silently renders as plain black modules.
    [Fact]
    public void Themes_KeepTheirPaletteInk() {
        var failures = new List<string>();
        foreach (var theme in Enum.GetValues<QrArtTheme>()) foreach (var variant in Enum.GetValues<QrArtVariant>()) {
            var png = QR.Render(QrArtPremiumScanTests.LongUrl, OutputFormat.Png,
                new QrRenderOptions { TargetSizePx = 512, Art = QrArt.Theme(theme, variant) },
                new QrEncodingOptions { ErrorCorrectionLevel = QrErrorCorrectionLevel.H }).ToArray();
            var rgba = ImageReader.DecodeRgba32(png, out var width, out var height);
            int dark = 0, neutral = 0;
            for (var y = height / 4; y < height * 3 / 4; y++) for (var x = width / 4; x < width * 3 / 4; x++) {
                var p = (y * width + x) * 4;
                int r = rgba[p], g = rgba[p + 1], b = rgba[p + 2];
                if (299 * r + 587 * g + 114 * b >= 100_000) continue;
                dark++;
                if (Math.Max(r, Math.Max(g, b)) - Math.Min(r, Math.Min(g, b)) < 24) neutral++;
            }
            if (dark == 0 || neutral >= dark / 2) failures.Add($"{theme}/{variant}: {neutral} of {dark} dark pixels are neutral");
        }
        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }

    // Theme looks draw format and version bits as whole modules in their own shape family,
    // not as square slabs that read as stray rectangles beside dotted or rounded data.
    [Theory]
    [InlineData(QrModuleShape.Dot)]
    [InlineData(QrModuleShape.ConnectedSquircle)]
    [InlineData(QrModuleShape.Rounded)]
    public void ThemeVersionBits_FollowTheModuleShape(QrModuleShape shape) {
        var qr = QR.Encode(QrArtPremiumScanTests.LongUrl, new QrEncodingOptions { ErrorCorrectionLevel = QrErrorCorrectionLevel.H });
        const int module = 12, quiet = 4;
        var options = new QrPngRenderOptions {
            ModuleSize = module, QuietZone = quiet, ModuleShape = shape, ModuleCornerRadiusPx = 4,
            ProtectFunctionalPatterns = true, PreserveThemeFunctionPatterns = true
        };
        var pixels = QrPngRenderer.RenderPixels(qr.Modules, options, out _, out _, out var stride);
        var size = qr.Modules.Width;
        var bits = 0;
        // Version block beside the top-right finder: three columns by six rows.
        for (var y = 0; y < 6; y++) for (var x = size - 11; x < size - 8; x++) {
            if (!qr.Modules[x, y]) continue;
            var left = (quiet + x) * module;
            var top = (quiet + y) * module;
            Assert.True(pixels[(top + module / 2) * stride + (left + module / 2) * 4] < 64, $"version bit {x},{y} lost its ink");
            Assert.True(pixels[top * stride + left * 4] > 160, $"version bit {x},{y} is still a square slab");
            bits++;
        }
        Assert.True(bits > 0);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(3, false)]
    [InlineData(0, true)]
    [InlineData(3, true)]
    public void PlainCanvas_UsesExplicitPadding_WithoutFrameOrCaption(int padding, bool emptyCaption) {
        var qr = QR.Encode("https://evotec.xyz");
        const int module = 8;
        var options = new QrPngRenderOptions {
            ModuleSize = module, QuietZone = 4,
            Canvas = new QrCanvasOptions {
                PaddingPx = padding,
                Caption = emptyCaption ? new QrCanvasCaptionOptions { Text = string.Empty } : null
            }
        };
        QrPngRenderer.RenderPixels(qr.Modules, options, out var width, out var height, out _);
        var expected = (qr.Size + 8) * module + 2 * padding;
        Assert.Equal(expected, width);
        Assert.Equal(expected, height);
    }
}
