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
        var logo = LogoBuilder.CreateCirclePng(64, new Rgba32(35, 32, 38), new Rgba32(190, 42, 42), out _, out _);
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
                variants.Add(new { name = preset == QrHeroPreset.Neon ? "Dark design" : dark ? "Dark" : "Light", src = "/assets/hero-presets/" + file, width, height, decoded = payload, budget });
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

    private static string DisplayName(QrHeroPreset preset) => preset switch {
        QrHeroPreset.DotMatrix => "Dot Matrix", QrHeroPreset.MonoClassy => "Mono Classy",
        QrHeroPreset.PaperCutout => "Paper Cutout", QrHeroPreset.RetroPixel => "Retro Pixel",
        QrHeroPreset.MinimalLine => "Minimal Line", _ => preset.ToString()
    };
}
