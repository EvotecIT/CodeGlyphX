using System.Text;
using System.Text.Json;
using CodeGlyphX;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Art;

namespace CodeGlyphX.Website.Specimens;

/// <summary>Builds the curated QR board from the same editable presets used by callers.</summary>
internal static class HeroSpecimens {
    internal static void Render(string website) {
        const string payload = "https://codeglyphx.com";
        var directory = Path.Combine(website, "static", "assets", "hero-presets");
        Directory.CreateDirectory(directory);
        var logo = SampleBrandMark(160, new Rgba32(80, 32, 42), new Rgba32(255, 248, 246));
        var presets = new List<object>();
        foreach (var preset in Enum.GetValues<QrHeroPreset>()) {
            var variants = new List<object>();
            foreach (var dark in new[] { false, true }) {
                var appearance = QrArt.Hero(preset, dark);
                var encoding = new QrEncodingOptions { ErrorCorrectionLevel = QrErrorCorrectionLevel.H };
                byte[] png;
                object? budget = null;
                if (preset == QrHeroPreset.Brand) {
                    var composition = QrArt.RenderLogo(payload, logo, new QrLogoRenderOptions { Appearance = appearance }, encoding);
                    png = composition.GetPng();
                    budget = composition.Budget;
                } else {
                    png = QR.Render(payload, OutputFormat.Png, appearance, encoding).ToArray();
                }
                if (!QrArt.ValidateImage(png, payload).AllPassed)
                    throw new InvalidOperationException($"The {preset} {(dark ? "dark" : "light")} specimen failed native image validation.");
                ImageReader.DecodeRgba32(png, out var width, out var height);
                var file = preset.ToString().ToLowerInvariant() + (dark ? "-dark.png" : "-light.png");
                File.WriteAllBytes(Path.Combine(directory, file), png);
                variants.Add(new { name = dark ? "Dark" : "Light", src = "/assets/hero-presets/" + file, width, height, decoded = payload, budget });
            }
            var code = preset == QrHeroPreset.Brand
                ? "var appearance = QrArt.Hero(QrHeroPreset.Brand);\nvar result = QrArt.RenderLogo(\"https://codeglyphx.com\",\n    File.ReadAllBytes(\"logo.png\"),\n    new CodeGlyphX.Rendering.Art.QrLogoRenderOptions { Appearance = appearance });\nFile.WriteAllBytes(\"brand.png\", result.GetPng());"
                : $"var appearance = QrArt.Hero(QrHeroPreset.{preset});\nQR.Save(\"https://codeglyphx.com\", \"qr.png\", appearance,\n    new QrEncodingOptions {{ ErrorCorrectionLevel = QrErrorCorrectionLevel.H }});";
            presets.Add(new { id = preset.ToString().ToLowerInvariant(), name = DisplayName(preset), description = QrArt.DescribeHero(preset), variants, code });
        }
        Directory.CreateDirectory(Path.Combine(website, "data"));
        File.WriteAllText(Path.Combine(website, "data", "hero-presets.json"),
            JsonSerializer.Serialize(new { presets }, new JsonSerializerOptions { WriteIndented = true }) + "\n", new UTF8Encoding(false));
        Console.WriteLine("Rendered and native-validated 24 curated QR specimens.");
    }

    // A neutral sample mark in the Brand eye colour: a rounded tile carrying an open "C" and a detached
    // square terminal. Each pixel averages 4x4 samples so the curves stay smooth after scaling.
    private static byte[] SampleBrandMark(int size, Rgba32 tile, Rgba32 mark) {
        var rgba = new byte[size * size * 4];
        for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++) {
                int tileHits = 0, markHits = 0;
                for (var sy = 0; sy < 4; sy++)
                    for (var sx = 0; sx < 4; sx++) {
                        var u = (x + (sx + .5) / 4) / size - .5;
                        var v = (y + (sy + .5) / 4) / size - .5;
                        if (!InRoundedSquare(u, v, .5, .23)) continue;
                        tileHits++;
                        var r = Math.Sqrt(u * u + v * v);
                        var opening = u > 0 && Math.Abs(v) < u * .9;
                        var arc = r <= .29 && r >= .165 && !opening;
                        var terminal = InRoundedSquare(u - .2275, v, .0625, .018);
                        if (arc || terminal) markHits++;
                    }
                if (tileHits == 0) continue;
                var p = (y * size + x) * 4;
                var t = markHits / (double)tileHits;
                rgba[p] = (byte)Math.Round(tile.R + (mark.R - tile.R) * t);
                rgba[p + 1] = (byte)Math.Round(tile.G + (mark.G - tile.G) * t);
                rgba[p + 2] = (byte)Math.Round(tile.B + (mark.B - tile.B) * t);
                rgba[p + 3] = (byte)((tileHits * 255 + 8) / 16);
            }
        return CodeGlyphX.Rendering.Png.PngImageEncoder.EncodeRgba32(rgba, size, size);

        static bool InRoundedSquare(double u, double v, double half, double radius) {
            var dx = Math.Abs(u) - (half - radius);
            var dy = Math.Abs(v) - (half - radius);
            if (dx <= 0 || dy <= 0) return Math.Abs(u) <= half && Math.Abs(v) <= half;
            return dx * dx + dy * dy <= radius * radius;
        }
    }

    private static string DisplayName(QrHeroPreset preset) => preset switch {
        QrHeroPreset.DotMatrix => "Dot Matrix", QrHeroPreset.MonoClassy => "Mono Classy",
        QrHeroPreset.PaperCutout => "Paper Cutout", QrHeroPreset.RetroPixel => "Retro Pixel",
        QrHeroPreset.MinimalLine => "Minimal Line", _ => preset.ToString()
    };
}
