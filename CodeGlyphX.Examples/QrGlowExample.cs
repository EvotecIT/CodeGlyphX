using System.IO;
using CodeGlyphX;
using CodeGlyphX.Payloads;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Examples;

internal static class QrGlowExample {
    public static void Run(string outputDir) {
        var payload = QrPayload.Url("https://example.com/qr/glow");

        var options = new QrEasyOptions {
            ErrorCorrectionLevel = QrErrorCorrectionLevel.H,
            ModuleSize = 10,
            QuietZone = 4,
            Foreground = new Rgba32(0, 255, 240),
            Background = new Rgba32(255, 255, 255),
            ModuleShape = QrPngModuleShape.Dot,
            ModuleScale = 0.9,
            ModuleScaleMap = new QrPngModuleScaleMapOptions {
                Mode = QrPngModuleScaleMode.Radial,
                MinScale = 0.82,
                MaxScale = 1.0,
                RingSize = 2,
            },
            ForegroundPalette = new QrPngPaletteOptions {
                Mode = QrPngPaletteMode.Cycle,
                RingSize = 2,
                ApplyToEyes = false,
                Colors = new[] {
                    new Rgba32(0, 255, 240),
                    new Rgba32(0, 170, 255),
                    new Rgba32(255, 92, 255),
                },
            },
            Eyes = new QrPngEyeOptions {
                UseFrame = true,
                FrameStyle = QrPngEyeFrameStyle.Glow,
                OuterShape = QrPngModuleShape.Rounded,
                InnerShape = QrPngModuleShape.Circle,
                OuterColor = new Rgba32(0, 255, 240),
                InnerColor = new Rgba32(255, 92, 255),
                OuterCornerRadiusPx = 6,
                InnerCornerRadiusPx = 4,
                GlowRadiusPx = 30,
                GlowAlpha = 130,
                GlowColor = new Rgba32(0, 200, 255, 200),
            },
            Canvas = new QrPngCanvasOptions {
                PaddingPx = 24,
                CornerRadiusPx = 26,
                BackgroundGradient = new QrPngGradientOptions {
                    Type = QrPngGradientType.DiagonalDown,
                    StartColor = new Rgba32(8, 10, 28),
                    EndColor = new Rgba32(28, 18, 64),
                },
                BorderPx = 2,
                BorderColor = new Rgba32(255, 255, 255, 48),
                ShadowOffsetX = 6,
                ShadowOffsetY = 8,
                ShadowColor = new Rgba32(0, 0, 0, 60),
            },
        };

        QR.Save(payload, Path.Combine(outputDir, "qr-glow.png"), options);
        QR.SavePdf(payload, Path.Combine(outputDir, "qr-glow.pdf"), options, RenderMode.Raster);
        QR.SaveEps(payload, Path.Combine(outputDir, "qr-glow.eps"), options, RenderMode.Raster);
    }
}

