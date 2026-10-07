using System.IO;
using CodeGlyphX;
using CodeGlyphX.Payloads;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Examples;

internal static class QrConnectedExample {
    public static void Run(string outputDir) {
        var payload = QrPayload.Url("https://example.com/qr/connected");

        var encoding = new QrEncodingOptions { ErrorCorrectionLevel = QrErrorCorrectionLevel.H };
        var options = new QrRenderOptions {
            ModuleSize = 10,
            QuietZone = 4,
            Foreground = new Rgba32(88, 120, 255),
            Background = new Rgba32(250, 252, 255),
            ModuleShape = QrModuleShape.ConnectedRounded,
            ModuleScale = 0.9,
            ModuleScaleMap = new QrModuleScaleMapOptions {
                Mode = QrModuleScaleMode.Radial,
                MinScale = 0.86,
                MaxScale = 1.0,
                RingSize = 2,
            },
            ForegroundPalette = new QrPaletteOptions {
                Mode = QrPaletteMode.Cycle,
                RingSize = 2,
                ApplyToEyes = false,
                Colors = new[] {
                    new Rgba32(88, 120, 255),
                    new Rgba32(120, 96, 255),
                    new Rgba32(88, 210, 255),
                },
            },
            Eyes = new QrEyeOptions {
                UseFrame = true,
                FrameStyle = QrEyeFrameStyle.Target,
                OuterShape = QrModuleShape.Rounded,
                InnerShape = QrModuleShape.Circle,
                OuterColor = new Rgba32(88, 120, 255),
                InnerColor = new Rgba32(88, 210, 255),
                OuterCornerRadiusPx = 6,
                InnerCornerRadiusPx = 4,
            },
            Canvas = new QrCanvasOptions {
                PaddingPx = 24,
                CornerRadiusPx = 26,
                BackgroundGradient = new QrGradientOptions {
                    Type = QrGradientType.DiagonalDown,
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

        QR.Save(payload, Path.Combine(outputDir, "qr-connected-rounded.png"), options, encoding);
        OutputWriter.Write(
            Path.Combine(outputDir, "qr-connected-rounded.pdf"),
            QR.Render(payload, OutputFormat.Pdf, options, encoding, extras: new OutputOptions { VectorMode = RenderMode.Raster })
        );
        OutputWriter.Write(
            Path.Combine(outputDir, "qr-connected-rounded.eps"),
            QR.Render(payload, OutputFormat.Eps, options, encoding, extras: new OutputOptions { VectorMode = RenderMode.Raster })
        );
    }
}
