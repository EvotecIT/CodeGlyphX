using CodeMatrix;
using CodeMatrix.Payloads;
using CodeMatrix.Rendering.Png;

namespace CodeMatrix.Examples;

internal static class QrFancyExample {
    public static void Run(string outputDir) {
        var payload = QrPayload.Url("https://example.com/qr/fancy");
        var qr = QrCodeEncoder.EncodeText(payload, QrErrorCorrectionLevel.H, 1, 10, null);

        var logoRgba = ExampleHelpers.CreateLogoRgba(48, new Rgba32(12, 44, 96), new Rgba32(235, 192, 92), out var logoW, out var logoH);
        var logo = new QrPngLogoOptions(logoRgba, logoW, logoH) {
            Scale = 0.22,
            PaddingPx = 6,
            DrawBackground = true,
            Background = new Rgba32(255, 255, 255),
            CornerRadiusPx = 10,
        };

        var opts = new QrPngRenderOptions {
            ModuleSize = 10,
            QuietZone = 4,
            Background = new Rgba32(250, 252, 255),
            Foreground = new Rgba32(18, 44, 78),
            ModuleShape = QrPngModuleShape.Rounded,
            ModuleScale = 0.85,
            ModuleCornerRadiusPx = 3,
            ForegroundGradient = new QrPngGradientOptions {
                Type = QrPngGradientType.DiagonalDown,
                StartColor = new Rgba32(11, 91, 158),
                EndColor = new Rgba32(27, 168, 132),
            },
            Eyes = new QrPngEyeOptions {
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
            },
            Logo = logo,
        };

        var png = QrPngRenderer.Render(qr.Modules, opts);
        ExampleHelpers.WriteBinary(outputDir, "qr-fancy.png", png);
    }
}
