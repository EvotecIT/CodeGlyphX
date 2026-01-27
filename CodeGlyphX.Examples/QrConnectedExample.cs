using System.IO;
using CodeGlyphX;
using CodeGlyphX.Payloads;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Examples;

internal static class QrConnectedExample {
    public static void Run(string outputDir) {
        var payload = QrPayload.Url("https://example.com/qr/connected");

        var options = new QrEasyOptions {
            ErrorCorrectionLevel = QrErrorCorrectionLevel.H,
            ModuleSize = 10,
            QuietZone = 4,
            Foreground = new Rgba32(88, 120, 255),
            Background = new Rgba32(250, 252, 255),
            ModuleShape = QrPngModuleShape.ConnectedRounded,
            ModuleScale = 0.9,
            ModuleScaleMap = new QrPngModuleScaleMapOptions {
                Mode = QrPngModuleScaleMode.Radial,
                MinScale = 0.86,
                MaxScale = 1.0,
                RingSize = 2,
            },
            ForegroundPalette = new QrPngPaletteOptions {
                Mode = QrPngPaletteMode.Cycle,
                RingSize = 2,
                ApplyToEyes = false,
                Colors = new[] {
                    new Rgba32(88, 120, 255),
                    new Rgba32(120, 96, 255),
                    new Rgba32(88, 210, 255),
                },
            },
            Eyes = new QrPngEyeOptions {
                UseFrame = true,
                FrameStyle = QrPngEyeFrameStyle.Target,
                OuterShape = QrPngModuleShape.Rounded,
                InnerShape = QrPngModuleShape.Circle,
                OuterColor = new Rgba32(88, 120, 255),
                InnerColor = new Rgba32(88, 210, 255),
                OuterCornerRadiusPx = 6,
                InnerCornerRadiusPx = 4,
            },
            Canvas = new QrPngCanvasOptions {
                PaddingPx = 24,
                CornerRadiusPx = 26,
                BackgroundGradient = new QrPngGradientOptions {
                    Type = QrPngGradientType.DiagonalDown,
                    StartColor = new Rgba32(14, 18, 42),
                    EndColor = new Rgba32(28, 20, 76),
                },
                BorderPx = 2,
                BorderColor = new Rgba32(255, 255, 255, 48),
                ShadowOffsetX = 6,
                ShadowOffsetY = 8,
                ShadowColor = new Rgba32(0, 0, 0, 60),
            },
        };

        QR.Save(payload, Path.Combine(outputDir, "qr-connected-rounded.png"), options);
        QR.SavePdf(payload, Path.Combine(outputDir, "qr-connected-rounded.pdf"), options, RenderMode.Raster);
        QR.SaveEps(payload, Path.Combine(outputDir, "qr-connected-rounded.eps"), options, RenderMode.Raster);
    }
}

