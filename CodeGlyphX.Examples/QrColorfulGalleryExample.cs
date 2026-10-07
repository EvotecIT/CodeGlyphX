using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Art;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Examples;

/// <summary>Six vivid designs rendered and checked through the public library APIs.</summary>
internal static class QrColorfulGalleryExample {
    private const string Payload = "https://codeglyphx.com";
    private const int ArtworkSize = 640;

    public static void Run(string outputDir) {
        var directory = Path.Combine(outputDir, "qr-colorful-gallery");
        Directory.CreateDirectory(directory);
        var exports = new List<byte[]>();
        var checks = new List<string> { "Design\tFile\tPayload\tOriginal\tHalfSize\tBoxBlur" };

        Save("Prism", "prism", QR.Render(Payload, OutputFormat.Png,
            Sticker(QrModuleShape.ConnectedRounded, QrPaletteMode.Cycle,
                new[] { R(155, 15, 105), R(81, 31, 170), R(0, 112, 138) }, R(255, 89, 168), R(54, 218, 239)), encodingOptions: new QrEncodingOptions { ErrorCorrectionLevel = QrErrorCorrectionLevel.H }, outputOptions: new OutputOptions { PngCompressionLevel = 6 }).ToArray());
        Save("Candy pop", "candy-pop", QR.Render(Payload, OutputFormat.Png,
            Sticker(QrModuleShape.Squircle, QrPaletteMode.Random,
                new[] { R(165, 23, 89), R(109, 31, 160), R(20, 95, 157) }, R(255, 166, 219), R(139, 156, 255)), encodingOptions: new QrEncodingOptions { ErrorCorrectionLevel = QrErrorCorrectionLevel.H }, outputOptions: new OutputOptions { PngCompressionLevel = 6 }).ToArray());
        Save("Neon orbit", "neon-orbit", QR.Render(Payload, OutputFormat.Png,
            Sticker(QrModuleShape.Circle, QrPaletteMode.Rings,
                new[] { R(14, 98, 122), R(37, 40, 153), R(127, 23, 143) }, R(32, 231, 212), R(190, 66, 255)), encodingOptions: new QrEncodingOptions { ErrorCorrectionLevel = QrErrorCorrectionLevel.H }, outputOptions: new OutputOptions { PngCompressionLevel = 6 }).ToArray());

        var qr = QR.Encode(Payload, new QrEncodingOptions { ErrorCorrectionLevel = QrErrorCorrectionLevel.H });
        foreach (var (name, slug, shape) in new[] {
            ("Aurora ribbons", "aurora", QrModuleShape.ConnectedSquircle),
            ("Tropical leaves", "tropical", QrModuleShape.Leaf),
            ("Solar bloom", "solar-bloom", QrModuleShape.Rounded)
        }) {
            var artwork = QrImageComposer.Render(qr, DrawArtwork(slug), ArtworkSize, ArtworkSize,
                new QrImageCompositionOptions {
                    ModuleSize = 16, Strength = 0.95,
                    Canvas = new QrImageCanvasOptions { PaddingModules = 9 },
                    Art = new QrImageArtOptions {
                        Shape = shape, Scale = 0.9, DetailProtection = 0.55,
                        Finders = QrImageFinderStyle.Rounded,
                        FunctionalForeground = R(31, 19, 65),
                        FunctionalBackground = R(255, 247, 253)
                    }
                });
            Save(name, slug, artwork.ToPng());
        }

        File.WriteAllBytes(Path.Combine(directory, "colorful-gallery.png"), ContactSheet(exports));
        File.WriteAllLines(Path.Combine(directory, "validation.tsv"), checks);

        void Save(string name, string slug, byte[] png) {
            var report = QrArt.ValidateImage(png, Payload, budgetMilliseconds: 3000);
            var results = report.Checks.Select(check => check.Passed ? "PASS" : "FAIL").ToArray();
            Console.WriteLine($"{name}: {string.Join(", ", report.Checks.Select(c => $"{c.Name}={c.Passed}"))}");
            // Keep a failed image for diagnosis, but fail the example instead of presenting it as approved.
            File.WriteAllBytes(Path.Combine(directory, slug + ".png"), png);
            if (!report.AllPassed) throw new InvalidOperationException($"Export did not recover the expected payload: {name}");
            exports.Add(png);
            checks.Add($"{name}\t{slug}.png\t{Payload}\t{string.Join("\t", results)}");
        }
    }

    private static QrRenderOptions Sticker(QrModuleShape shape, QrPaletteMode mode,
        Rgba32[] ink, Rgba32 start, Rgba32 end) => new() {
        ModuleSize = 16, QuietZone = 4,
        Foreground = ink[1], Background = Rgba32.White,
        ModuleShape = shape, ModuleScale = 0.94,
        ProtectFunctionalPatterns = true, ProtectQuietZone = true,
        ForegroundPalette = new QrPaletteOptions { Colors = ink, Mode = mode, Seed = 20261003, ApplyToEyes = false },
        Eyes = new QrEyeOptions {
            UseFrame = true, FrameStyle = QrEyeFrameStyle.Single,
            OuterShape = QrModuleShape.Rounded, InnerShape = QrModuleShape.Rounded,
            OuterCornerRadiusPx = 20, InnerCornerRadiusPx = 10,
            OuterColor = ink[1], InnerColor = ink[0]
        },
        Canvas = new QrCanvasOptions {
            PaddingPx = 96, CornerRadiusPx = 0,
            BackgroundGradient = Gradient(start, end),
            Frame = new QrCanvasFrameOptions {
                ThicknessPx = 6, GapPx = 14, RadiusPx = 20, Color = R(255, 255, 255)
            },
            Splash = new QrCanvasSplashOptions {
                Colors = new[] { R(255, 235, 86), R(255, 85, 155), R(62, 235, 221) },
                Count = 18, MinRadiusPx = 8, MaxRadiusPx = 26, SpreadPx = 64,
                Placement = QrCanvasSplashPlacement.CanvasEdges, Seed = 701, DripChance = 0
            }
        }
    };

    // These are deterministic illustrations, not another QR renderer. The library owns all module geometry.
    private static byte[] DrawArtwork(string scene) {
        var pixels = new byte[ArtworkSize * ArtworkSize * 4];
        for (var y = 0; y < ArtworkSize; y++) for (var x = 0; x < ArtworkSize; x++) {
            var u = x / (double)ArtworkSize;
            var v = y / (double)ArtworkSize;
            Rgba32 color;
            if (scene == "aurora") {
                var ribbon = 0.5 + 0.5 * Math.Sin(9 * u + 7 * v + 2 * Math.Sin(8 * v));
                color = Mix(R(36, 13, 114), R(20, 232, 207), ribbon);
                var pink = Math.Pow(0.5 + 0.5 * Math.Sin(12 * v - 6 * u), 6);
                color = Mix(color, R(255, 49, 179), pink * 0.85);
                if ((x * 31 + y * 17) % 1543 < 3) color = R(255, 242, 174);
            } else if (scene == "tropical") {
                color = Mix(R(255, 88, 153), R(255, 207, 66), v);
                for (var leaf = 0; leaf < 8; leaf++) {
                    var angle = leaf * Math.PI / 4;
                    var cx = 0.5 + 0.44 * Math.Cos(angle);
                    var cy = 0.5 + 0.44 * Math.Sin(angle);
                    var dx = u - cx; var dy = v - cy;
                    var a = dx * Math.Cos(angle) + dy * Math.Sin(angle);
                    var b = -dx * Math.Sin(angle) + dy * Math.Cos(angle);
                    if (a * a / 0.038 + b * b / 0.006 < 1) {
                        color = Mix(R(0, 122, 130), R(32, 237, 185), 0.5 + a * 2);
                        if (Math.Abs(b) < 0.003) color = R(248, 236, 113);
                    }
                }
            } else {
                var dx = u - 0.5; var dy = v - 0.5;
                var radius = Math.Sqrt(dx * dx + dy * dy);
                var angle = Math.Atan2(dy, dx);
                var petal = 0.39 + 0.1 * Math.Cos(angle * 9);
                color = Mix(R(88, 30, 175), R(252, 74, 143), v);
                if (radius < petal) color = Mix(R(255, 171, 28), R(255, 239, 92), radius / petal);
                if (radius < 0.18) color = Mix(R(194, 26, 106), R(255, 67, 133), radius / 0.18);
                if (Math.Abs(radius - petal) < 0.009) color = R(255, 230, 141);
                if (radius > 0.57 && Math.Sin(angle * 22 + radius * 35) > 0.8) color = R(29, 227, 209);
            }
            var p = (y * ArtworkSize + x) * 4;
            pixels[p] = color.R; pixels[p + 1] = color.G; pixels[p + 2] = color.B; pixels[p + 3] = 255;
        }
        return pixels;
    }

    private static byte[] ContactSheet(IReadOnlyList<byte[]> images) {
        const int cell = 432, gap = 20, columns = 3;
        var rows = (images.Count + columns - 1) / columns;
        var width = columns * cell + (columns + 1) * gap;
        var height = rows * cell + (rows + 1) * gap;
        var sheet = Enumerable.Repeat((byte)255, width * height * 4).ToArray();
        for (var i = 0; i < images.Count; i++) {
            var pixels = ImageReader.DecodeRgba32(images[i], out var w, out var h);
            var preview = ImageScaler.ResizeToFitBox(pixels, w, h, w * 4, cell, cell, Rgba32.White, preserveAspectRatio: true);
            var left = gap + i % columns * (cell + gap);
            var top = gap + i / columns * (cell + gap);
            for (var y = 0; y < cell; y++) Array.Copy(preview, y * cell * 4, sheet, ((top + y) * width + left) * 4, cell * 4);
        }
        // This sheet is a viewing aid. Validation applies to each full-size export, not its thumbnail.
        return PngImageEncoder.EncodeRgba32(sheet, width, height);
    }

    private static Rgba32 R(byte r, byte g, byte b) => new(r, g, b);
    private static Rgba32 Mix(Rgba32 a, Rgba32 b, double t) {
        t = Math.Clamp(t, 0, 1);
        return R((byte)Math.Round(a.R + (b.R - a.R) * t), (byte)Math.Round(a.G + (b.G - a.G) * t), (byte)Math.Round(a.B + (b.B - a.B) * t));
    }
    private static QrGradientOptions Gradient(Rgba32 a, Rgba32 b) => new() { Type = QrGradientType.DiagonalDown, StartColor = a, EndColor = b };
}
