using CodeMatrix;
using CodeMatrix.Payloads;
using CodeMatrix.Rendering.Html;
using CodeMatrix.Rendering.Png;
using CodeMatrix.Rendering.Svg;

namespace CodeMatrix.Examples;

internal static class EvotecExamples {
    public static void Run(string outputDir) {
        var url = QrPayload.Url("https://evotec.xyz");
        var qr = QrCodeEncoder.EncodeText(url, QrErrorCorrectionLevel.H, 1, 10, null);

        var plain = QrPngRenderer.Render(qr.Modules, new QrPngRenderOptions {
            ModuleSize = 8,
            QuietZone = 4,
            Foreground = new Rgba32(18, 24, 40),
            Background = new Rgba32(255, 255, 255),
        });
        ExampleHelpers.WriteBinary(outputDir, "qr-evotec.png", plain);

        var logoPath = "Assets/Logo/Logo-evotec.png";
        if (!ExampleHelpers.TryReadRepoFile(logoPath, out var logoBytes, out _)) {
            ExampleHelpers.WriteText(outputDir, "qr-evotec-logo-missing.txt", "Missing Assets/Logo/Logo-evotec.png");
            return;
        }

        var logo = QrPngLogoOptions.FromPng(logoBytes);
        logo.Scale = 0.22;
        logo.PaddingPx = 6;
        logo.DrawBackground = true;
        logo.Background = new Rgba32(255, 255, 255);
        logo.CornerRadiusPx = 8;

        var withLogo = new QrPngRenderOptions {
            ModuleSize = 8,
            QuietZone = 4,
            Foreground = new Rgba32(18, 24, 40),
            Background = new Rgba32(255, 255, 255),
            Logo = logo,
        };

        var pngLogo = QrPngRenderer.Render(qr.Modules, withLogo);
        ExampleHelpers.WriteBinary(outputDir, "qr-evotec-logo.png", pngLogo);

        var invertedLogo = new QrPngLogoOptions(logo.Rgba, logo.Width, logo.Height) {
            Scale = 0.20,
            PaddingPx = 6,
            DrawBackground = true,
            Background = new Rgba32(255, 255, 255),
            CornerRadiusPx = 8,
        };
        var inverted = new QrPngRenderOptions {
            ModuleSize = 8,
            QuietZone = 4,
            Foreground = new Rgba32(255, 255, 255),
            Background = new Rgba32(10, 18, 32),
            Logo = invertedLogo,
        };
        var pngInverted = QrPngRenderer.Render(qr.Modules, inverted);
        ExampleHelpers.WriteBinary(outputDir, "qr-evotec-logo-inverted.png", pngInverted);

        var minimalLogo = new QrPngLogoOptions(logo.Rgba, logo.Width, logo.Height) {
            Scale = 0.18,
            PaddingPx = 4,
            DrawBackground = true,
            Background = new Rgba32(255, 255, 255),
            CornerRadiusPx = 6,
        };
        var minimal = new QrPngRenderOptions {
            ModuleSize = 6,
            QuietZone = 4,
            Foreground = new Rgba32(18, 24, 40),
            Background = new Rgba32(255, 255, 255),
            Logo = minimalLogo,
        };
        var pngMinimal = QrPngRenderer.Render(qr.Modules, minimal);
        ExampleHelpers.WriteBinary(outputDir, "qr-evotec-logo-minimal.png", pngMinimal);

        var fancy = new QrPngRenderOptions {
            ModuleSize = 10,
            QuietZone = 4,
            Background = new Rgba32(245, 248, 252),
            Foreground = new Rgba32(17, 34, 68),
            ModuleShape = QrPngModuleShape.Rounded,
            ModuleScale = 0.88,
            ModuleCornerRadiusPx = 3,
            ForegroundGradient = new QrPngGradientOptions {
                Type = QrPngGradientType.DiagonalDown,
                StartColor = new Rgba32(18, 88, 156),
                EndColor = new Rgba32(20, 156, 120),
            },
            Eyes = new QrPngEyeOptions {
                UseFrame = true,
                OuterShape = QrPngModuleShape.Rounded,
                InnerShape = QrPngModuleShape.Circle,
                OuterCornerRadiusPx = 5,
                InnerCornerRadiusPx = 4,
                OuterGradient = new QrPngGradientOptions {
                    Type = QrPngGradientType.Radial,
                    StartColor = new Rgba32(12, 48, 104),
                    EndColor = new Rgba32(26, 128, 192),
                    CenterX = 0.35,
                    CenterY = 0.35,
                },
                InnerColor = new Rgba32(17, 34, 68),
            },
            Logo = logo,
        };

        var pngFancy = QrPngRenderer.Render(qr.Modules, fancy);
        ExampleHelpers.WriteBinary(outputDir, "qr-evotec-logo-fancy.png", pngFancy);

        var svg = SvgQrRenderer.Render(qr.Modules, new QrSvgRenderOptions {
            ModuleSize = 4,
            QuietZone = 4,
            DarkColor = "#121828",
            LightColor = "#ffffff",
        });
        var svgWithLogo = ExampleHelpers.ComposeSvgWithLogo(
            svg,
            logoBytes,
            qrModules: qr.Modules.Width,
            moduleSize: 4,
            quietZone: 4,
            logoScale: 0.22,
            paddingPx: 6,
            cornerRadiusPx: 8,
            backgroundColor: "#ffffff");
        ExampleHelpers.WriteText(outputDir, "qr-evotec-logo.svg", svgWithLogo);

        var html = HtmlQrRenderer.Render(qr.Modules, new QrHtmlRenderOptions {
            ModuleSize = 4,
            QuietZone = 4,
            DarkColor = "#121828",
            LightColor = "#ffffff",
        });
        var htmlWithLogo = ExampleHelpers.ComposeHtmlWithLogo(
            html,
            logoBytes,
            qrModules: qr.Modules.Width,
            moduleSize: 4,
            quietZone: 4,
            logoScale: 0.22,
            paddingPx: 6,
            cornerRadiusPx: 8,
            backgroundColor: "#ffffff");
        ExampleHelpers.WriteText(outputDir, "qr-evotec-logo.html", ExampleHelpers.WrapHtml("Evotec QR", htmlWithLogo));

        var widePath = "Assets/Logo/Logo-evotec-wide.png";
        if (ExampleHelpers.TryReadRepoFile(widePath, out var wideBytes, out _)) {
            var logoWide = QrPngLogoOptions.FromPng(wideBytes);
            logoWide.Scale = 0.28;
            logoWide.PaddingPx = 6;
            logoWide.DrawBackground = true;
            logoWide.Background = new Rgba32(255, 255, 255);
            logoWide.CornerRadiusPx = 10;

            var wideOpts = new QrPngRenderOptions {
                ModuleSize = 8,
                QuietZone = 4,
                Foreground = new Rgba32(18, 24, 40),
                Background = new Rgba32(255, 255, 255),
                Logo = logoWide,
            };
            var pngWide = QrPngRenderer.Render(qr.Modules, wideOpts);
            ExampleHelpers.WriteBinary(outputDir, "qr-evotec-logo-wide.png", pngWide);
        }
    }
}
