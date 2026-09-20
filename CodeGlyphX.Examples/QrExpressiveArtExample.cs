using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Art;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Examples;

internal static class QrExpressiveArtExample {
    private static Rgba32 Paper(string scene) => scene switch {
        "botanical" => new Rgba32(234, 229, 204),
        "citrus" => new Rgba32(248, 217, 176),
        "geometric" => new Rgba32(240, 223, 194),
        "landscape" => new Rgba32(246, 229, 209),
        "waves" => new Rgba32(247, 230, 190),
        _ => new Rgba32(235, 245, 251),
    };

    public static void Run(string outputDir) {
        const string payload = "https://example.com/art"; // NOSONAR - reserved example URI.
        var dir = Path.Combine(outputDir, "qr-expressive-art");
        Directory.CreateDirectory(dir);
        var qr = QR.Encode(payload, new QrEasyOptions { ErrorCorrectionLevel = QrErrorCorrectionLevel.H });
        var scenes = new[] {
            ("botanical", QrPngModuleShape.Leaf, new Rgba32(17, 46, 34)),
            ("citrus", QrPngModuleShape.Rounded, new Rgba32(65, 25, 18)),
            ("landscape", QrPngModuleShape.ConnectedRounded, new Rgba32(24, 31, 57)),
            ("waves", QrPngModuleShape.ConnectedSquircle, new Rgba32(8, 41, 51)),
            ("geometric", QrPngModuleShape.Blob, new Rgba32(47, 26, 45)),
            ("earth", QrPngModuleShape.Circle, new Rgba32(12, 31, 49)),
        };
        foreach (var (scene, shape, ink) in scenes) {
            byte[] pixels;
            int width;
            int height;
            if (scene == "earth") {
                var photo = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Assets", "Art", "apollo17-earth.jpg"));
                pixels = ImageReader.DecodeRgba32(photo, out width, out height);
            } else {
                width = height = 768;
                pixels = QrArtIllustrations.Draw(scene, width);
            }
            var options = new QrImageCompositionOptions {
                ModuleSize = 16,
                Strength = 0.92,
                Art = new QrImageArtOptions { Shape = shape, Scale = 0.85, DetailProtection = 0.8, FunctionalForeground = ink, FunctionalBackground = Paper(scene) },
                Canvas = new QrImageCanvasOptions {
                    PaddingModules = 10,
                    PositionX = scene == "earth" || scene == "citrus" ? 0.8 : 0.3,
                    PositionY = scene == "landscape" ? 0.9 : 0.55
                },
            };
            var artwork = QrImageComposer.Render(qr, pixels, width, height, options);
            var png = artwork.ToPng();
            File.WriteAllBytes(Path.Combine(dir, $"{scene}.png"), png);
            var baseline = QrImageComposer.Render(qr, pixels, width, height, new QrImageCompositionOptions { ModuleSize = 16, Strength = 1 });
            baseline.SavePng(Path.Combine(dir, $"{scene}-baseline.png"));
            var report = QrArt.ValidateImage(png, payload, budgetMilliseconds: 3000);
            Console.WriteLine($"{scene}: " + string.Join(", ", report.Checks.Select(c => $"{c.Name}={c.Passed}")));
            if (!report.AllPassed) throw new InvalidOperationException($"Rendered artwork failed validation: {scene}");
        }
    }
}
