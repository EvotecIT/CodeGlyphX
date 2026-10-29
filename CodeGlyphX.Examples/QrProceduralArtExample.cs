using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Art;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Examples;

/// <summary>Six reproducible, locally drawn QR designs and a compact gallery.</summary>
internal static class QrProceduralArtExample {
    internal const string Payload = "https://codeglyphx.com";
    internal const int GalleryTile = 420;
    internal static readonly (string Name, QrArtPattern Pattern)[] Designs = {
        ("veined-marble", QrArtPattern.Marble), ("layered-waves", QrArtPattern.Waves),
        ("sunburst-rays", QrArtPattern.Sunburst), ("bauhaus-tiles", QrArtPattern.Geometric),
        ("botanical-leaves", QrArtPattern.Botanical), ("circuit-traces", QrArtPattern.Circuit)
    };

    /// <summary>Composes one gallery design with the options the published gallery uses.</summary>
    internal static QrImageComposition Compose(int index) {
        var pattern = Designs[index].Pattern;
        // A tighter margin and a crisp, frosted panel keep the code large and clearly separated in a gallery tile.
        var composition = QrArtPatternPresets.CreateCompositionOptions(pattern);
        composition.Canvas!.PaddingModules = 5;
        composition.Canvas.FadeModules = 0;
        return QrArt.ComposePattern(Payload, QrArtPatternPresets.CreatePatternOptions(pattern, seed: 2026 + index), composition);
    }

    public static void Run(string outputDir) {
        var directory = Path.Combine(outputDir, "qr-procedural-art");
        Directory.CreateDirectory(directory);
        var images = new List<QrImageComposition>();
        for (var i = 0; i < Designs.Length; i++) {
            var name = Designs[i].Name;
            var result = Compose(i);
            result.SavePng(Path.Combine(directory, name + ".png"));
            var report = QrArt.ValidateImage(result.ToPng(), Payload, 3000);
            Console.WriteLine(name + ": " + string.Join(", ", report.Checks.Select(c => $"{c.Name}={c.Passed}")));
            if (!report.AllPassed) throw new InvalidOperationException("QR artwork failed validation: " + name);
            images.Add(result);
        }
        SaveGallery(images, Path.Combine(directory, "qr-procedural-art-gallery.png"));
    }

    private static void SaveGallery(IReadOnlyList<QrImageComposition> images, string path) {
        const int tile = GalleryTile, gap = 24, columns = 3;
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
