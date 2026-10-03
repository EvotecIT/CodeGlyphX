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
            ("prismatic-marble", QrArtPattern.Marble), ("tidal-ribbons", QrArtPattern.Waves),
            ("sunburst-stitch", QrArtPattern.Sunburst), ("confetti-tiles", QrArtPattern.Geometric),
            ("botanical-weave", QrArtPattern.Botanical), ("electric-circuit", QrArtPattern.Circuit)
        };
        var images = new List<QrImageComposition>();
        for (var i = 0; i < designs.Length; i++) {
            var (name, pattern) = designs[i];
            var result = QrArt.ComposePattern(payload,
                QrArtPatternPresets.CreatePatternOptions(pattern, seed: 2026 + i),
                QrArtPatternPresets.CreateCompositionOptions(pattern));
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
