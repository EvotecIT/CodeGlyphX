using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;
using CodeGlyphX.Printing;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class QrPremiumGeometryTests {
    [Fact]
    public void SunsetDarkGround_IsBurntEmberWithOrangeAndPinkBlooms() {
        var canvas = QrArt.Hero(QrHeroPreset.Sunset, true).Canvas!;
        Assert.Equal(new Rgba32(42, 18, 6), canvas.BackgroundGradient!.StartColor);
        Assert.Equal(new Rgba32(122, 42, 14), canvas.BackgroundGradient.EndColor);
        Assert.Equal(new Rgba32(255, 111, 24, 150), canvas.PanelGlowColor);
        Assert.Equal(new Rgba32(224, 36, 122, 120), canvas.SecondaryPanelGlowColor);
        var brand = QrArt.Hero(QrHeroPreset.Brand, true).Canvas!.Background;
        var retro = QrArt.Hero(QrHeroPreset.RetroPixel, true).Canvas!.Background;
        Assert.True(brand.R > retro.R && brand.G < retro.G && brand.B < retro.B);
    }
    [Fact]
    public void Neon_HasSeparateVioletBloomOutsideTheMagentaRim_AndALightComposition() {
        var options = QrArt.Hero(QrHeroPreset.Neon, true);
        var encoding = new QrEncodingOptions { ErrorCorrectionLevel = QrErrorCorrectionLevel.H };
        var png = QR.Render(QrArtPremiumScanTests.ShortUrl, OutputFormat.Png, options, encoding).ToArray();
        var light = QrArt.Hero(QrHeroPreset.Neon);
        var lightPng = QR.Render(QrArtPremiumScanTests.ShortUrl, OutputFormat.Png, light, encoding).ToArray();
        Assert.NotEqual(png, lightPng);
        var lightPixels = ImageReader.DecodeRgba32(lightPng, out var lw, out var lh);
        var corner = ((lh / 2) * lw + 8) * 4;
        Assert.True(lightPixels[corner] + lightPixels[corner + 1] + lightPixels[corner + 2] > 690, "Light Neon needs a pale ground.");
        Assert.NotNull(light.Canvas!.PanelGlowColor);
        Assert.NotNull(light.Canvas.SecondaryPanelGlowColor);
        var pixels = ImageReader.DecodeRgba32(png, out var w, out var h);
        var left = Enumerable.Range(0, w / 2).First(x => {
            var p = ((h / 2) * w + x) * 4;
            return pixels[p] == options.Background.R && pixels[p + 1] == options.Background.G && pixels[p + 2] == options.Background.B;
        });
        var control = QrRenderer.CloneOptions(options); control.Canvas!.SecondaryPanelGlowColor = null;
        var baseline = ImageReader.DecodeRgba32(QR.Render(QrArtPremiumScanTests.ShortUrl, OutputFormat.Png, control, encoding).ToArray(), out _, out _);
        var outer = ((h / 2) * w + left - 60) * 4;
        Assert.True(pixels[outer + 2] - baseline[outer + 2] > pixels[outer] - baseline[outer] + 25);
        var rim = ((h / 2) * w + left - 5) * 4;
        Assert.True(pixels[rim] > pixels[rim + 2]);
    }

    public static IEnumerable<object[]> DarkHeroes() => Enum.GetValues<QrHeroPreset>().Select(h => new object[] { h });

    [Theory, MemberData(nameof(DarkHeroes))]
    public void HeroLogoDefaults_UseOnlyTheReviewedTintAndAccentTreatments(QrHeroPreset hero) {
        var options = QrArt.Hero(hero, true);
        var tinted = hero is QrHeroPreset.Ink or QrHeroPreset.DotMatrix or QrHeroPreset.MinimalLine or QrHeroPreset.MonoClassy;
        Assert.Equal(tinted, options.LogoTint.HasValue);
        // Brand's plate matches its panel and needs no outline around the host's logo.
        var ringed = hero is QrHeroPreset.Sunset or QrHeroPreset.Ocean or QrHeroPreset.Neon or QrHeroPreset.Berry;
        Assert.Equal(ringed, options.LogoRingColor.HasValue);
        Assert.Null(options.LogoBackground); // The plate inherits the actual edited panel colour.
    }

    [Fact]
    public void ContinuousTimingHairline_RetainsAlternatingDarkDots() {
        var qr = QR.Encode("TIMING");
        const int m = 40;
        var pixels = QrPngRenderer.RenderPixels(qr.Modules, new QrPngRenderOptions {
            ModuleSize = m, ModuleShape = QrModuleShape.Dot, ModuleScale = .8125,
            Foreground = new Rgba32(31, 31, 31), TimingThicknessModules = .22
        }, out var w, out _, out _);
        byte Red(double x, double y) => pixels[((int)((y + 4) * m) * w + (int)((x + 4) * m)) * 4];
        Assert.True(qr.Modules[8, 6]); Assert.False(qr.Modules[9, 6]);
        Assert.Equal(31, Red(8.5, 6.5));
        Assert.Equal(31, Red(9.5, 6.5));
        Assert.Equal(31, Red(10, 6.5));
        Assert.Equal(31, Red(8.5, 6.25));
        Assert.Equal(255, Red(9.5, 6.25));
        Assert.Equal(255, Red(9.5, 6.1));
    }

    [Fact]
    public void MonoClassyRuns_LeaveTheConcaveLightCornerOpen() {
        var cells = new BitMatrix(5, 5);
        cells[1, 1] = cells[2, 1] = cells[1, 2] = true;
        var pixels = QrPngRenderer.RenderPixels(cells, new QrPngRenderOptions {
            ModuleSize = 40, QuietZone = 1, ModuleShape = QrArt.Hero(QrHeroPreset.MonoClassy).ModuleShape!.Value
        }, out var w, out _, out _);
        Assert.Equal(255, pixels[(121 * w + 121) * 4]);
        Assert.Equal(0, pixels[(100 * w + 100) * 4]);
    }

    [Theory, MemberData(nameof(DarkHeroes))]
    public void DarkHeroes_ReserveDominantGroundAroundTintedScanSafePanels(QrHeroPreset hero) {
        var options = QrArt.Hero(hero, true);
        var image = QR.Render(QrArtPremiumScanTests.ShortUrl, OutputFormat.Png, options,
            new QrEncodingOptions { ErrorCorrectionLevel = QrErrorCorrectionLevel.H }).ToArray();
        var pixels = ImageReader.DecodeRgba32(image, out var w, out var h);
        var panel = options.Background;
        Assert.False(panel.R == 255 && panel.G == 255 && panel.B == 255);
        var left = w; var right = -1;
        for (var y = 0; y < h; y++) for (var x = 0; x < w; x++) {
            var p = (y * w + x) * 4;
            if (pixels[p] != panel.R || pixels[p + 1] != panel.G || pixels[p + 2] != panel.B || pixels[p + 3] != 255) continue;
            left = Math.Min(left, x); right = Math.Max(right, x);
        }
        Assert.True(right > left);
        Assert.True(left >= (right - left + 1) * .18, $"{hero}: dark ground must reserve18% on each side.");
        var ground = ((h / 2) * w + left / 2) * 4;
        Assert.True(pixels[ground] + pixels[ground + 1] + pixels[ground + 2] < 380);
        Assert.NotNull(options.Canvas!.BackgroundGradient);
        Assert.NotEqual(options.Canvas.BackgroundGradient!.StartColor, options.Canvas.BackgroundGradient.EndColor);
    }

    [Fact]
    public void DotAlignment_UsesSeparatePerimeterDotsAndACentreDot() {
        var qr = QR.Encode("DOTS", new QrEncodingOptions { MinVersion = 2, MaxVersion = 2, ErrorCorrectionLevel = QrErrorCorrectionLevel.H });
        const int m = 20;
        var hero = QrArt.Hero(QrHeroPreset.DotMatrix);
        var pixels = QrPngRenderer.RenderPixels(qr.Modules, new QrPngRenderOptions {
            ModuleSize = m, ModuleShape = hero.ModuleShape!.Value, Eyes = hero.Eyes, Foreground = hero.Foreground
        }, out var w, out _, out _);
        byte Red(int x, int y) => pixels[((y + 4 * m) * w + x + 4 * m) * 4];
        Assert.True(Red(16 * m + m / 2, 16 * m + m / 2) < 64);
        Assert.Equal(255, Red(17 * m, 16 * m + m / 2));
        Assert.True(Red(18 * m + m / 2, 18 * m + m / 2) < 64);
    }

    [Fact]
    public void ThinAlignmentRing_HasNoProtrudingDots_AndKeepsEveryPerimeterModuleDark() {
        var qr = QR.Encode("RING", new QrEncodingOptions { MinVersion = 2, MaxVersion = 2, ErrorCorrectionLevel = QrErrorCorrectionLevel.H });
        const int m = 20;
        var hero = QrArt.Hero(QrHeroPreset.MinimalLine);
        var pixels = QrPngRenderer.RenderPixels(qr.Modules, new QrPngRenderOptions {
            ModuleSize = m, ModuleShape = hero.ModuleShape!.Value, ModuleScale = hero.ModuleScale!.Value, Eyes = hero.Eyes, Foreground = hero.Foreground
        }, out var w, out _, out _);
        byte Red(double x, double y) => pixels[((int)((y + 4) * m) * w + (int)((x + 4) * m)) * 4];
        // Every perimeter module centre stays dark: sides on the ring, corners as separate whole dots.
        foreach (var (x, y) in new[] { (16.5, 16.5), (20.5, 16.5), (16.5, 20.5), (20.5, 20.5), (16.5, 18.5), (18.5, 16.5), (16.5, 17.5) }) {
            Assert.True(qr.Modules[(int)x, (int)y]);
            Assert.True(Red(x, y) < 64);
        }
        // A side dot no longer bulges past the ring, and each corner dot stays clear of it.
        Assert.Equal(255, Red(16.28, 17.39));
        Assert.Equal(255, Red(16.77, 16.77));
        Assert.Equal(255, Red(16.05, 16.05));
        Assert.Equal(255, Red(17.5, 18.5));
        Assert.True(Red(18.5, 18.5) < 64);
    }

    [Fact]
    public void RadialGradient_HoldsTheOceanCoreBeforeBlending() {
        var modules = new BitMatrix(21, 21);
        for (var y = 0; y < 21; y++) for (var x = 0; x < 21; x++) modules[x, y] = true;
        var gradient = QrArt.Hero(QrHeroPreset.Ocean).ForegroundGradient!;
        var pixels = QrPngRenderer.RenderPixels(modules, new QrPngRenderOptions { ModuleSize = 10, ForegroundGradient = gradient }, out var w, out _, out _);
        var core = ((4 * 10 + 105) * w + 4 * 10 + 120) * 4;
        Assert.Equal(new byte[] { 11, 46, 107 }, pixels.Skip(core).Take(3));
        var rim = ((4 * 10 + 5) * w + 4 * 10 + 5) * 4;
        Assert.True(pixels[rim + 1] > pixels[core + 1]);
        Assert.Equal(.4, QrRenderer.CloneOptions(QrArt.Hero(QrHeroPreset.Ocean)).ForegroundGradient!.StartOffset);
    }
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
        yield return new object[] { QrHeroPreset.Neon, true };
        yield return new object[] { QrHeroPreset.Neon, false };
        yield return new object[] { QrHeroPreset.PaperCutout, true };
    }
    [Theory, MemberData(nameof(Effects))]
    public void HeroEffects_HaveReservedFiniteExtent_AndNoClippedPixels(QrHeroPreset hero, bool dark) {
        var options = QrArt.Hero(hero, dark);
        var png = QR.Render(QrArtPremiumScanTests.ShortUrl, OutputFormat.Png, options, new QrEncodingOptions { ErrorCorrectionLevel = QrErrorCorrectionLevel.H }).ToArray();
        var rgba = ImageReader.DecodeRgba32(png, out var w, out var h);
        var control = QrRenderer.CloneOptions(options);
        control.Canvas!.PanelGlowColor = control.Canvas.SecondaryPanelGlowColor = null;
        if (hero == QrHeroPreset.PaperCutout) control.Canvas.ShadowColor = Rgba32.Transparent;
        var baseline = ImageReader.DecodeRgba32(QR.Render(QrArtPremiumScanTests.ShortUrl, OutputFormat.Png, control,
            new QrEncodingOptions { ErrorCorrectionLevel = QrErrorCorrectionLevel.H }).ToArray(), out var cw, out var ch);
        Assert.Equal(w, cw); Assert.Equal(h, ch);
        Assert.False(rgba.SequenceEqual(baseline));
        void Edge(int at) {
            for (var c = 0; c < 4; c++) Assert.Equal(baseline[at + c], rgba[at + c]);
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
        var eyes = QrArt.Hero(QrHeroPreset.Botanical).Eyes!;
        Assert.Equal(QrModuleShape.Leaf, eyes.OuterShape);
        Assert.Equal(QrModuleShape.Leaf, eyes.InnerShape);
        var pixels = QrPngRenderer.RenderPixels(qr.Modules, new QrPngRenderOptions {
            ModuleSize = m, ModuleShape = QrModuleShape.Circle,
            Eyes = eyes
        }, out var w, out _, out _);
        foreach (var point in new[] { (.1, .1), (qr.Size - .1, .1), (.1, qr.Size - .1) }) {
            var x = (int)((point.Item1 + 4) * m); var y = (int)((point.Item2 + 4) * m);
            Assert.True(pixels[(y * w + x) * 4] < 32, "Each outer finder corner needs its leaf tip.");
        }
        // The opposite corner is visibly round, so the outward corner is the only tip.
        foreach (var point in new[] { (6.7, 6.7), (qr.Size - 6.7, 6.7), (6.7, qr.Size - 6.7) }) {
            var x = (int)((point.Item1 + 4) * m); var y = (int)((point.Item2 + 4) * m);
            Assert.Equal(255, pixels[(y * w + x) * 4]);
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
        options.Background = new Rgba32(246, 244, 230);
        var output = QR.Render(QrArtPremiumScanTests.LongUrl, OutputFormat.Png, options, new QrEncodingOptions { ErrorCorrectionLevel = QrErrorCorrectionLevel.H }).ToArray();
        Assert.True(QrArt.ValidateImage(output, QrArtPremiumScanTests.LongUrl).AllPassed);
        Assert.Null(options.LogoBackground);
        var pixels = ImageReader.DecodeRgba32(output, out var width, out var height);
        // The supplied E-shaped logo is transparent at its centre, exposing actual plate paint.
        var center = ((height / 2) * width + width / 2) * 4;
        Assert.Equal(options.Background.R, pixels[center]);
        Assert.Equal(options.Background.G, pixels[center + 1]);
        Assert.Equal(options.Background.B, pixels[center + 2]);
    }
}
