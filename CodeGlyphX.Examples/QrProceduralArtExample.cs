using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Art;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Examples;

/// <summary>Six reproducible, locally drawn QR designs and a compact gallery.</summary>
internal static class QrProceduralArtExample {
    public static void Run(string outputDir) {
        const string payload = "https://codeglyphx.com";
        var directory = Path.Combine(outputDir, "qr-procedural-art");
        Directory.CreateDirectory(directory);
        var designs = new[] {
            ("prismatic-marble", QrArtPattern.Marble, QrImageArtStyle.ModuleShape, QrImageFinderStyle.Rounded,
                new[] { new Rgba32(247, 38, 138), new Rgba32(102, 46, 226), new Rgba32(0, 202, 210), new Rgba32(255, 189, 35) }),
            ("tidal-ribbons", QrArtPattern.Waves, QrImageArtStyle.Ribbons, QrImageFinderStyle.Circular,
                new[] { new Rgba32(16, 58, 190), new Rgba32(0, 174, 202), new Rgba32(19, 217, 164), new Rgba32(252, 218, 77) }),
            ("sunburst-stitch", QrArtPattern.Sunburst, QrImageArtStyle.CrossStitch, QrImageFinderStyle.Chamfered,
                new[] { new Rgba32(255, 193, 30), new Rgba32(255, 72, 69), new Rgba32(213, 25, 143), new Rgba32(85, 37, 172) }),
            ("confetti-tiles", QrArtPattern.Geometric, QrImageArtStyle.ModuleShape, QrImageFinderStyle.Chamfered,
                new[] { new Rgba32(243, 44, 106), new Rgba32(29, 163, 198), new Rgba32(253, 170, 22), new Rgba32(98, 51, 174) }),
            ("botanical-weave", QrArtPattern.Botanical, QrImageArtStyle.Weave, QrImageFinderStyle.Circular,
                new[] { new Rgba32(6, 123, 95), new Rgba32(39, 179, 118), new Rgba32(225, 66, 136), new Rgba32(252, 155, 39) }),
            ("electric-circuit", QrArtPattern.Circuit, QrImageArtStyle.Circuit, QrImageFinderStyle.Rounded,
                new[] { new Rgba32(37, 81, 198), new Rgba32(0, 163, 176), new Rgba32(237, 58, 146), new Rgba32(246, 164, 20) })
        };
        var images = new List<QrImageComposition>();
        for (var i = 0; i < designs.Length; i++) {
            var (name, pattern, style, finders, palette) = designs[i];
            var result = QrArt.ComposePattern(payload,
                new QrArtPatternOptions { Pattern = pattern, Seed = 2026 + i, RotationDegrees = i * 17, Colors = palette },
                new QrImageCompositionOptions {
                    ModuleSize = 18, Strength = 0.96,
                    Canvas = new QrImageCanvasOptions { PaddingModules = 8 },
                    Art = new QrImageArtOptions {
                        Style = style, Shape = QrPngModuleShape.ConnectedRounded, Scale = 0.95,
                        DetailProtection = 0.3, Finders = finders,
                        FunctionalForeground = new Rgba32(21, 26, 47), FunctionalBackground = new Rgba32(255, 249, 239)
                    }
                });
            result.SavePng(Path.Combine(directory, name + ".png"));
            var report = QrArt.ValidateImage(result.ToPng(), payload, 3000);
            Console.WriteLine(name + ": " + string.Join(", ", report.Checks.Select(c => $"{c.Name}={c.Passed}")));
            if (!report.AllPassed) throw new InvalidOperationException("QR artwork failed validation: " + name);
            images.Add(result);
        }
        SaveGallery(images, Path.Combine(directory, "qr-procedural-art-gallery.png"));
    }

    private static void SaveGallery(IReadOnlyList<QrImageComposition> images, string path) {
        const int tile = 420, gap = 24, columns = 3;
        var width = columns * tile + (columns + 1) * gap;
        var height = 2 * tile + 3 * gap;
        var pixels = new byte[width * height * 4];
        for (var p = 0; p < pixels.Length; p += 4) {
            pixels[p] = 241; pixels[p + 1] = 243; pixels[p + 2] = 248; pixels[p + 3] = 255;
        }
        for (var i = 0; i < images.Count; i++) {
            var image = images[i];
            var thumb = ImageScaler.ResizeToFitBox(image.GetPixels(), image.Size, image.Size, image.Size * 4, tile, tile, Rgba32.White, false);
            var left = gap + i % columns * (tile + gap);
            var top = gap + i / columns * (tile + gap);
            for (var y = 0; y < tile; y++) Buffer.BlockCopy(thumb, y * tile * 4, pixels, ((top + y) * width + left) * 4, tile * 4);
        }
        File.WriteAllBytes(path, PngImageEncoder.EncodeRgba32(pixels, width, height));
    }
}
