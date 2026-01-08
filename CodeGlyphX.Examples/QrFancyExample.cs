using System.IO;
using CodeGlyphX;
using CodeGlyphX.Payloads;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Examples;

internal static class QrFancyExample {
    public static void Run(string outputDir) {
        var payload = QrPayload.Url("https://example.com/qr/fancy");
        var logoPng = LogoBuilder.CreateCirclePng(48, new Rgba32(12, 44, 96), new Rgba32(235, 192, 92), out _, out _);

        QR.Create(payload)
            .WithOptions(o => {
                o.ErrorCorrectionLevel = QrErrorCorrectionLevel.H;
                o.ModuleSize = 10;
                o.QuietZone = 4;
                o.Background = new Rgba32(250, 252, 255);
                o.Foreground = new Rgba32(18, 44, 78);
                o.ModuleShape = QrPngModuleShape.Rounded;
                o.ModuleScale = 0.85;
                o.ModuleCornerRadiusPx = 3;
                o.ForegroundGradient = new QrPngGradientOptions {
                    Type = QrPngGradientType.DiagonalDown,
                    StartColor = new Rgba32(11, 91, 158),
                    EndColor = new Rgba32(27, 168, 132),
                };
                o.Eyes = new QrPngEyeOptions {
                    UseFrame = true,
                    OuterShape = QrPngModuleShape.Rounded,
                    InnerShape = QrPngModuleShape.Circle,
                    OuterScale = 1.0,
                    InnerScale = 0.92,
                    OuterCornerRadiusPx = 5,
                    InnerCornerRadiusPx = 4,
                    OuterGradient = new QrPngGradientOptions {
                        Type = QrPngGradientType.Radial,
                        StartColor = new Rgba32(9, 48, 112),
                        EndColor = new Rgba32(32, 146, 196),
                        CenterX = 0.35,
                        CenterY = 0.35,
                    },
                    InnerColor = new Rgba32(18, 44, 78),
                };
                o.LogoPng = logoPng;
                o.LogoScale = 0.22;
                o.LogoPaddingPx = 6;
                o.LogoDrawBackground = true;
                o.LogoBackground = new Rgba32(255, 255, 255);
                o.LogoCornerRadiusPx = 10;
            })
            .SavePng(Path.Combine(outputDir, "qr-fancy.png"));
    }
}
