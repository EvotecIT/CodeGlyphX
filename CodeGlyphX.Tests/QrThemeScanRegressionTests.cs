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
