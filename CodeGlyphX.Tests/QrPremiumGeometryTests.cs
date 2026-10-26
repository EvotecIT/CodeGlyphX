using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;
using CodeGlyphX.Printing;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class QrPremiumGeometryTests {
    [Fact]
    public void FormatAndVersionInk_UsesTheBodyCoverageAndGradient() {
        var qr = QR.Encode(QrArtPremiumScanTests.ShortUrl, new QrEncodingOptions { MinVersion = 7, MaxVersion = 7, ErrorCorrectionLevel = QrErrorCorrectionLevel.H });
        const int m = 20;
        var pixels = QrPngRenderer.RenderPixels(qr.Modules, new QrPngRenderOptions {
            ModuleSize = m, ModuleShape = QrModuleShape.Circle, ProtectFunctionalPatterns = true,
            ForegroundGradient = new QrGradientOptions { StartColor = new Rgba32(20, 50, 110), EndColor = new Rgba32(100, 20, 90) }
        }, out var w, out _, out _);
        var count = 0;
        for (var y = 0; y < qr.Size; y++) for (var x = 0; x < qr.Size; x++) {
            var format = x == 8 && y < 6 || y == 8 && x < 6;
            var version = x >= qr.Size - 11 && x < qr.Size - 8 && y < 6 || y >= qr.Size - 11 && y < qr.Size - 8 && x < 6;
            if ((!format && !version) || !qr.Modules[x, y]) continue;
            var corner = (((y + 4) * m + 1) * w + (x + 4) * m + 1) * 4;
            var center = (((y + 4) * m + 10) * w + (x + 4) * m + 10) * 4;
            Assert.Equal(255, pixels[corner]); Assert.Equal(255, pixels[corner + 1]);
            Assert.InRange(pixels[center + 2], 50, 150); Assert.True(pixels[center] > 0);
            count++;
        }
        Assert.True(count > 10);
    }

    [Fact]
    public void LiquidConcaveJoins_AreContinuous_AndDoNotFillDiagonalOnlyContacts() {
        var cells = new BitMatrix(5, 5);
        cells[1, 1] = cells[2, 1] = cells[1, 2] = true;
        cells[3, 3] = cells[4, 4] = true;
        var rgba = QrPngRenderer.RenderPixels(cells, new QrPngRenderOptions { ModuleSize = 40, QuietZone = 1, ModuleShape = QrModuleShape.Liquid }, out var w, out _, out _);
        byte Red(int x, int y) => rgba[(y * w + x) * 4];
        Assert.True(Red(121, 121) < 32, "The light cell's concave corner must receive a fillet.");
        Assert.Equal(255, Red(159, 159));
        Assert.Equal(255, Red(199, 199));
    }

    public static IEnumerable<object[]> Effects() {
        yield return new object[] { QrHeroPreset.Neon };
        yield return new object[] { QrHeroPreset.PaperCutout };
    }
    [Theory, MemberData(nameof(Effects))]
    public void HeroEffects_HaveReservedFiniteExtent_AndNoClippedPixels(QrHeroPreset hero) {
        var options = QrArt.Hero(hero, true);
        var png = QR.Render(QrArtPremiumScanTests.ShortUrl, OutputFormat.Png, options, new QrEncodingOptions { ErrorCorrectionLevel = QrErrorCorrectionLevel.H }).ToArray();
        var rgba = ImageReader.DecodeRgba32(png, out var w, out var h);
        void Edge(int at) {
            if (hero == QrHeroPreset.PaperCutout) Assert.Equal(0, rgba[at + 3]);
            else if (rgba[at + 3] != 0) {
                Assert.Equal(options.Canvas!.Background.R, rgba[at]);
                Assert.Equal(options.Canvas.Background.G, rgba[at + 1]);
                Assert.Equal(options.Canvas.Background.B, rgba[at + 2]);
            }
        }
        for (var x = 0; x < w; x++) { Edge(x * 4); Edge(((h - 1) * w + x) * 4); }
        for (var y = 0; y < h; y++) { Edge((y * w) * 4); Edge((y * w + w - 1) * 4); }
    }

    [Fact]
    public void ThinFinderRing_KeepsItsCentreline_AndTheThreeModuleBall() {
        var qr = QR.Encode(QrArtPremiumScanTests.ShortUrl);
        const int m = 40;
        var rgba = QrPngRenderer.RenderPixels(qr.Modules, new QrPngRenderOptions {
            ModuleSize = m, ModuleShape = QrModuleShape.Dot,
            Eyes = new QrEyeOptions { OuterShape = QrModuleShape.Circle, InnerShape = QrModuleShape.Circle, RingThicknessModules = .5 }
        }, out var w, out _, out _);
        byte Red(double y) => rgba[((int)((4 + y) * m) * w + (int)(7.5 * m)) * 4];
        Assert.Equal(255, Red(.1));
        Assert.True(Red(.5) < 16);
        Assert.True(Red(.65) < 16);
        Assert.Equal(255, Red(.9));
        Assert.Equal(255, Red(1.5));
        Assert.True(Red(2.1) < 16);
    }

    [Fact]
    public void MirroredLeafFinders_PointTowardsEachOutsideCorner() {
        var qr = QR.Encode(QrArtPremiumScanTests.ShortUrl);
        const int m = 20;
        var pixels = QrPngRenderer.RenderPixels(qr.Modules, new QrPngRenderOptions {
            ModuleSize = m, ModuleShape = QrModuleShape.Circle,
            Eyes = new QrEyeOptions { OuterShape = QrModuleShape.Leaf, InnerShape = QrModuleShape.Leaf, MirrorOutward = true }
        }, out var w, out _, out _);
        foreach (var point in new[] { (.1, .1), (qr.Size - .1, .1), (.1, qr.Size - .1) }) {
            var x = (int)((point.Item1 + 4) * m); var y = (int)((point.Item2 + 4) * m);
            Assert.True(pixels[(y * w + x) * 4] < 32, "Each outer finder corner needs its leaf tip.");
        }
    }

    [Fact]
    public void GradientStops_AreExact_AndPerceptualMidpointPreservesVividColour() {
        var a = new Rgba32(255, 122, 26); var b = new Rgba32(224, 36, 122);
        Assert.Equal(a, QrColorInterpolation.Interpolate(a, b, 0, QrGradientInterpolation.Oklch));
        Assert.Equal(b, QrColorInterpolation.Interpolate(a, b, 1, QrGradientInterpolation.Oklch));
        var mid = QrColorInterpolation.Interpolate(a, b, .5, QrGradientInterpolation.Oklch);
        Assert.InRange(mid.R, 240, 255); Assert.InRange(mid.B, 45, 95);
        Assert.NotEqual(mid, QrColorInterpolation.Interpolate(a, b, .5, QrGradientInterpolation.Srgb));
    }

    [Fact]
    public void CuratedHeroes_SpanSixBodies_WithNoMoreThanTwoPerBody() {
        var bodies = Enum.GetValues<QrHeroPreset>().Select(h => QrArt.Hero(h).ModuleShape).GroupBy(s => s).ToArray();
        Assert.True(bodies.Length >= 6);
        Assert.All(bodies, g => Assert.InRange(g.Count(), 1, 2));
    }

    [Theory]
    [InlineData(QrLogoPlateShape.Circle)]
    [InlineData(QrLogoPlateShape.Squircle)]
    [InlineData(QrLogoPlateShape.RoundedSquare)]
    public void LogoPlates_UseBackgroundAndTint_WithinTheOriginalBudget(QrLogoPlateShape shape) {
        var options = QrArt.Hero(QrHeroPreset.Brand, true);
        options.LogoPng = QrArtPremiumScanTests.Logo(); options.LogoPlateShape = shape;
        options.LogoRingColor = new Rgba32(174, 49, 33); options.LogoRingWidthPx = 2;
        var copy = QrRenderer.CloneOptions(options);
        Assert.Equal(shape, copy.LogoPlateShape); Assert.Equal(options.LogoTint, copy.LogoTint);
        var output = QR.Render(QrArtPremiumScanTests.LongUrl, OutputFormat.Png, options, new QrEncodingOptions { ErrorCorrectionLevel = QrErrorCorrectionLevel.H }).ToArray();
        Assert.True(QrArt.ValidateImage(output, QrArtPremiumScanTests.LongUrl).AllPassed);
        Assert.Equal(options.Background, options.LogoBackground);
    }
}
