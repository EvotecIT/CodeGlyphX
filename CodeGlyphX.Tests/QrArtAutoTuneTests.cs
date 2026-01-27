using System;
using System.Reflection;
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
        var render = BuildRender(options, payload, qr);

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
        var render = BuildRender(options, payload, qr);

        Assert.Equal(RenderDefaults.QrForeground, render.Foreground);
        Assert.Equal(RenderDefaults.QrBackground, render.Background);
        Assert.Null(render.ForegroundGradient);
        Assert.Null(render.ForegroundPalette);
        Assert.Null(render.ForegroundPaletteZones);
    }

    private static QrPngRenderOptions BuildRender(QrEasyOptions options, string payload, QrCode qr) {
        var method = typeof(QrEasy).GetMethod(
            "BuildPngOptions",
            BindingFlags.NonPublic | BindingFlags.Static,
            binder: null,
            types: new[] { typeof(QrEasyOptions), typeof(string), typeof(QrCode) },
            modifiers: null);

        Assert.NotNull(method);

        var render = method!.Invoke(null, new object[] { options, payload, qr });
        return Assert.IsType<QrPngRenderOptions>(render);
    }
}

