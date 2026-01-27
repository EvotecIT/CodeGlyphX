using System;
using System.Reflection;
using CodeGlyphX;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class QrArtAutoTuneTests {
    [Fact]
    public void Art_AutoTune_Enforces_Core_Guardrails_And_Connections() {
        var payload = "https://example.com/auto-tune-core";
        var options = new QrEasyOptions {
            Art = QrArt.Theme(QrArtTheme.NeonGlow, QrArtVariant.Bold, intensity: 95, safetyMode: QrArtSafetyMode.Bold),
            ArtAutoTune = true,
            QuietZone = 1,
            ProtectFunctionalPatterns = false,
            ProtectQuietZone = false,
            ModuleShape = QrPngModuleShape.Rounded,
            ModuleScale = 0.7,
        };

        var qr = QrEasy.Encode(payload, options);
        var render = BuildRender(options, qr);

        Assert.True(render.QuietZone >= 4, $"QuietZone should be at least 4 (was {render.QuietZone}).");
        Assert.True(render.ProtectFunctionalPatterns, "ProtectFunctionalPatterns should be enforced by auto-tune.");
        Assert.True(render.ProtectQuietZone, "ProtectQuietZone should be enforced by auto-tune.");
        Assert.Equal(QrPngModuleShape.ConnectedRounded, render.ModuleShape);
        Assert.True(render.ModuleScale >= 0.86, $"ModuleScale should be clamped for art safety (was {render.ModuleScale:0.00}).");
    }

    [Fact]
    public void Art_AutoTune_Falls_Back_On_Low_Contrast() {
        var payload = "https://example.com/auto-tune-contrast";
        var options = new QrEasyOptions {
            Art = QrArt.Theme(QrArtTheme.StripeEyes, QrArtVariant.Safe, intensity: 60),
            ArtAutoTune = true,
            Foreground = new Rgba32(220, 220, 220),
            Background = new Rgba32(255, 255, 255),
            ForegroundGradient = new QrPngGradientOptions {
                Type = QrPngGradientType.DiagonalDown,
                StartColor = new Rgba32(230, 230, 230),
                EndColor = new Rgba32(245, 245, 245),
            },
            ForegroundPalette = new QrPngPaletteOptions {
                Mode = QrPngPaletteMode.Cycle,
                Colors = new[] {
                    new Rgba32(235, 235, 235),
                    new Rgba32(245, 245, 245),
                },
            },
        };

        var qr = QrEasy.Encode(payload, options);
        var render = BuildRender(options, qr);

        Assert.Equal(RenderDefaults.QrForeground, render.Foreground);
        Assert.Equal(RenderDefaults.QrBackground, render.Background);
        Assert.Null(render.ForegroundGradient);
        Assert.Null(render.ForegroundPalette);
        Assert.Null(render.ForegroundPaletteZones);
    }

    [Fact]
    public void Art_AutoTune_Does_Not_Mutate_User_Options() {
        var payload = "https://example.com/auto-tune-no-mutation";
        var options = new QrEasyOptions {
            Art = QrArt.Theme(QrArtTheme.PaintSplash, QrArtVariant.Bold, intensity: 95, safetyMode: QrArtSafetyMode.Safe),
            ArtAutoTune = true,
            ModuleScaleMap = new QrPngModuleScaleMapOptions {
                Mode = QrPngModuleScaleMode.Random,
                MinScale = 0.5,
                MaxScale = 0.6,
                RingSize = 1,
                Seed = 123,
                ApplyToEyes = true,
            },
            ForegroundPattern = new QrPngForegroundPatternOptions {
                Type = QrPngForegroundPatternType.SpeckleDots,
                Color = new Rgba32(20, 40, 120, 180),
                SizePx = 8,
                ThicknessPx = 3,
                Seed = 7,
                Variation = 0.9,
                Density = 0.95,
                SnapToModuleSize = true,
                ModuleStep = 1,
                ApplyToModules = true,
                ApplyToEyes = true,
            },
            Canvas = new QrPngCanvasOptions {
                PaddingPx = 30,
                Splash = new QrPngCanvasSplashOptions {
                    Color = new Rgba32(0, 120, 220, 180),
                    Count = 40,
                    MinRadiusPx = 18,
                    MaxRadiusPx = 60,
                    SpreadPx = 28,
                    Placement = QrPngCanvasSplashPlacement.CanvasEdges,
                    EdgeBandPx = 110,
                    DripChance = 1.0,
                    DripLengthPx = 52,
                    DripWidthPx = 12,
                    Seed = 4242,
                    ProtectQrArea = false,
                    QrAreaAlphaMax = 200,
                },
            },
            Eyes = new QrPngEyeOptions {
                UseFrame = true,
                FrameStyle = QrPngEyeFrameStyle.Target,
                OuterColor = new Rgba32(10, 30, 90),
                InnerColor = new Rgba32(255, 255, 255),
                SparkleCount = 60,
                AccentRingCount = 12,
                AccentRayCount = 60,
                AccentStripeCount = 60,
                AccentStripeColor = new Rgba32(255, 255, 255, 190),
            },
        };

        var originalMinScale = options.ModuleScaleMap!.MinScale;
        var originalMaxScale = options.ModuleScaleMap.MaxScale;
        var originalThickness = options.ForegroundPattern!.ThicknessPx;
        var originalSplashCount = options.Canvas!.Splash!.Count;
        var originalSplashDripChance = options.Canvas.Splash.DripChance;
        var originalSparkleCount = options.Eyes!.SparkleCount;
        var originalAccentRayCount = options.Eyes.AccentRayCount;
        var originalAccentStripeCount = options.Eyes.AccentStripeCount;

        _ = QR.Png(payload, options);

        Assert.Equal(originalMinScale, options.ModuleScaleMap.MinScale);
        Assert.Equal(originalMaxScale, options.ModuleScaleMap.MaxScale);
        Assert.Equal(originalThickness, options.ForegroundPattern.ThicknessPx);
        Assert.Equal(originalSplashCount, options.Canvas.Splash.Count);
        Assert.Equal(originalSplashDripChance, options.Canvas.Splash.DripChance);
        Assert.Equal(originalSparkleCount, options.Eyes.SparkleCount);
        Assert.Equal(originalAccentRayCount, options.Eyes.AccentRayCount);
        Assert.Equal(originalAccentStripeCount, options.Eyes.AccentStripeCount);
    }

    [Fact]
    public void Art_AutoTune_Engages_Strong_Clamp_When_MinScore_Is_High() {
        var payload = "https://example.com/auto-tune-strong-clamp";
        var options = new QrEasyOptions {
            Art = QrArt.Theme(QrArtTheme.PaintSplash, QrArtVariant.Bold, intensity: 98, safetyMode: QrArtSafetyMode.Safe),
            ArtAutoTune = true,
            ArtAutoTuneMinScore = 99,
            ModuleSize = 1,
            QuietZone = 1,
            ProtectFunctionalPatterns = false,
            ProtectQuietZone = false,
            ModuleShape = QrPngModuleShape.Rounded,
            ModuleScale = 0.78,
            ModuleScaleMap = new QrPngModuleScaleMapOptions {
                Mode = QrPngModuleScaleMode.Random,
                MinScale = 0.62,
                MaxScale = 0.72,
                Seed = 2026,
                ApplyToEyes = true,
            },
            ForegroundPattern = new QrPngForegroundPatternOptions {
                Type = QrPngForegroundPatternType.SpeckleDots,
                Color = new Rgba32(15, 60, 140, 220),
                SizePx = 10,
                ThicknessPx = 4,
                Seed = 19,
                Variation = 0.9,
                Density = 0.95,
                SnapToModuleSize = true,
                ModuleStep = 1,
                ApplyToModules = true,
                ApplyToEyes = true,
            },
            Canvas = new QrPngCanvasOptions {
                PaddingPx = 36,
                Splash = new QrPngCanvasSplashOptions {
                    Color = new Rgba32(20, 120, 220, 210),
                    Count = 64,
                    MinRadiusPx = 16,
                    MaxRadiusPx = 66,
                    SpreadPx = 40,
                    Placement = QrPngCanvasSplashPlacement.CanvasEdges,
                    EdgeBandPx = 120,
                    DripChance = 1.0,
                    DripLengthPx = 60,
                    DripWidthPx = 14,
                    Seed = 2601,
                    ProtectQrArea = false,
                    QrAreaAlphaMax = 220,
                },
            },
            Eyes = new QrPngEyeOptions {
                UseFrame = true,
                SparkleCount = 80,
                AccentRingCount = 16,
                AccentRayCount = 80,
                AccentStripeCount = 80,
                SparkleColor = new Rgba32(255, 255, 255, 220),
                AccentRingColor = new Rgba32(255, 255, 255, 220),
                AccentRayColor = new Rgba32(255, 255, 255, 220),
                AccentStripeColor = new Rgba32(255, 255, 255, 220),
            },
        };

        var qr = QrEasy.Encode(payload, options);
        var render = BuildRender(options, qr);

        Assert.True(render.ModuleScale >= 0.97, $"Strong clamp should raise ModuleScale (was {render.ModuleScale:0.00}).");
        Assert.NotNull(render.ModuleScaleMap);
        Assert.True(render.ModuleScaleMap!.MinScale >= 0.97, $"Strong clamp should raise MinScale (was {render.ModuleScaleMap.MinScale:0.00}).");

        Assert.NotNull(render.ForegroundPattern);
        Assert.Equal(1, render.ForegroundPattern!.ThicknessPx);
        Assert.True(render.ForegroundPattern.Color.A <= 88, $"Strong clamp should cap pattern alpha (was {render.ForegroundPattern.Color.A}).");

        Assert.NotNull(render.Canvas?.Splash);
        Assert.True(render.Canvas!.Splash!.Count <= 16, $"Strong clamp should cap splash count (was {render.Canvas.Splash.Count}).");

        Assert.NotNull(render.Eyes);
        Assert.True(render.Eyes!.SparkleCount <= 22, $"Strong clamp should cap sparkle count (was {render.Eyes.SparkleCount}).");
        Assert.True(render.Eyes.AccentRingCount <= 4, $"Strong clamp should cap accent ring count (was {render.Eyes.AccentRingCount}).");
        Assert.True(render.Eyes.AccentRayCount <= 14, $"Strong clamp should cap accent ray count (was {render.Eyes.AccentRayCount}).");
        Assert.True(render.Eyes.AccentStripeCount <= 18, $"Strong clamp should cap accent stripe count (was {render.Eyes.AccentStripeCount}).");
    }

    private static QrPngRenderOptions BuildRender(QrEasyOptions options, QrCode qr) {
        var cloneMethod = typeof(QrEasy).GetMethod(
            "CloneOptions",
            BindingFlags.NonPublic | BindingFlags.Static,
            binder: null,
            types: new[] { typeof(QrEasyOptions) },
            modifiers: null);
        Assert.NotNull(cloneMethod);
        var cloned = cloneMethod!.Invoke(null, new object[] { options });
        var safeOptions = Assert.IsType<QrEasyOptions>(cloned);

        var method = typeof(QrEasy).GetMethod(
            "BuildPngOptions",
            BindingFlags.NonPublic | BindingFlags.Static,
            binder: null,
            types: new[] { typeof(QrEasyOptions), typeof(QrCode) },
            modifiers: null);

        Assert.NotNull(method);

        var render = method!.Invoke(null, new object[] { safeOptions, qr });
        return Assert.IsType<QrPngRenderOptions>(render);
    }
}
