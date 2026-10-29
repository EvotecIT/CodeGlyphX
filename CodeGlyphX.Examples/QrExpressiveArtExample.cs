using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Art;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Examples;

internal static class QrExpressiveArtExample {
    internal const string Payload = "https://example.com/art"; // NOSONAR - reserved example URI.
    internal static readonly string[] Scenes = { "botanical", "citrus", "landscape", "waves", "geometric", "earth" };

    private static Rgba32 Paper(string scene) => scene switch {
        "botanical" => new Rgba32(234, 229, 204),
        "citrus" => new Rgba32(248, 217, 176),
        _ => new Rgba32(247, 230, 190),
    };

    private static (QrModuleShape Shape, Rgba32 Ink) Classic(string scene) => scene switch {
        "botanical" => (QrModuleShape.Leaf, new Rgba32(17, 46, 34)),
        "citrus" => (QrModuleShape.Rounded, new Rgba32(65, 25, 18)),
        _ => (QrModuleShape.ConnectedSquircle, new Rgba32(8, 41, 51)),
    };

    /// <summary>Loads the source art: the NASA photograph for Earth, a locally drawn illustration otherwise.</summary>
    internal static byte[] Source(string scene, string earthPhotoPath, out int width, out int height) {
        if (scene == "earth") return ImageReader.DecodeRgba32(File.ReadAllBytes(earthPhotoPath), out width, out height);
        width = height = 768;
        return QrArtIllustrations.Draw(scene, width);
    }

    /// <summary>Composes one scene with the options the published artwork uses.</summary>
    internal static QrImageComposition Compose(string scene, byte[] pixels, int width, int height) {
        var qr = QR.Encode(Payload, new QrEncodingOptions { ErrorCorrectionLevel = QrErrorCorrectionLevel.H });
        return QrImageComposer.Render(qr, pixels, width, height, Options(scene));
    }

    private static QrImageCompositionOptions Options(string scene) {
        if (scene is "earth" or "landscape" or "geometric") {
            // Busy or multi-coloured art reads better when the whole symbol takes the art's own tones: light modules
            // and the quiet zone become a high key of the picture, and dark modules (finders included) its deep tones.
            // The code is centred on the subject, so the tinted panel sits inside it instead of cutting across an edge.
            return new QrImageCompositionOptions {
                ModuleSize = 16,
                Strength = 0.95,
                ImageZoom = scene == "earth" ? 1.15 : 1,
                Art = new QrImageArtOptions {
                    Shape = scene == "earth" ? QrModuleShape.Circle : QrModuleShape.Rounded,
                    Scale = 0.92,
                    DetailProtection = 0.3,
                    Finders = scene == "earth" ? QrImageFinderStyle.Circular : QrImageFinderStyle.Rounded,
                    FunctionalTint = 1,
                    // The photograph's deep tones are close to its mid tones; half a deep-ocean ink keeps strict readers sure.
                    DarkModuleArt = scene == "earth" ? 0.5 : 1,
                    FunctionalForeground = scene == "earth" ? new Rgba32(10, 28, 52) : Rgba32.Black
                },
                // A soft edge melts the panel into the photograph; flat illustrations keep a crisp, frosted-glass panel.
                Canvas = new QrImageCanvasOptions { PaddingModules = 10, FadeModules = scene == "earth" ? 6 : 0 },
            };
        }
        var (shape, ink) = Classic(scene);
        return new QrImageCompositionOptions {
            ModuleSize = 16,
            Strength = 0.92,
            Art = new QrImageArtOptions { Shape = shape, Scale = 0.85, DetailProtection = 0.8, FunctionalForeground = ink, FunctionalBackground = Paper(scene) },
            Canvas = new QrImageCanvasOptions { PaddingModules = 10, PositionX = scene == "citrus" ? 0.8 : 0.3, PositionY = 0.55 },
        };
    }

    public static void Run(string outputDir) {
        var dir = Path.Combine(outputDir, "qr-expressive-art");
        Directory.CreateDirectory(dir);
        var qr = QR.Encode(Payload, new QrEncodingOptions { ErrorCorrectionLevel = QrErrorCorrectionLevel.H });
        var photo = Path.Combine(AppContext.BaseDirectory, "Assets", "Art", "apollo17-earth.jpg");
        foreach (var scene in Scenes) {
            var pixels = Source(scene, photo, out var width, out var height);
            var png = Compose(scene, pixels, width, height).ToPng();
            File.WriteAllBytes(Path.Combine(dir, $"{scene}.png"), png);
            var baseline = QrImageComposer.Render(qr, pixels, width, height, new QrImageCompositionOptions { ModuleSize = 16, Strength = 1 });
            baseline.SavePng(Path.Combine(dir, $"{scene}-baseline.png"));
            var report = QrArt.ValidateImage(png, Payload, budgetMilliseconds: 3000);
            Console.WriteLine($"{scene}: " + string.Join(", ", report.Checks.Select(c => $"{c.Name}={c.Passed}")));
            if (!report.AllPassed) throw new InvalidOperationException($"Rendered artwork failed validation: {scene}");
        }
    }
}
