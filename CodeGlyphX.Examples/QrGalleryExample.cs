using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using CodeGlyphX;
using CodeGlyphX.Rendering;

namespace CodeGlyphX.Examples;

/// <summary>
/// Renders the website QR gallery from <see cref="QrGalleryLooks"/> straight into the repository's
/// <c>Website</c> folder: one PNG per variant under <c>static/assets/gallery/</c> and the page data in
/// <c>data/gallery.json</c> (looks, published C# recipes and the mockup scenes). Logo looks also write
/// their downloadable sample marks. Stale gallery PNGs are removed. The scan badges come from
/// <c>data/gallery-scan.json</c>, which QrGalleryScanTests writes and checks.
/// Run with <c>CODEGLYPHX_GALLERY=1</c>.
/// </summary>
internal static class QrGalleryExample {
    private sealed record Mockup(string Id, string Kind, string Title, string Description, string Variant, IReadOnlyDictionary<string, string> Copy);

    // Each scene is drawn by the gallery page in HTML and CSS around the variant's generated PNG.
    private static readonly Mockup[] Mockups = {
        new("poster", "poster", "Event poster", "Neon on a night-market poster: the glowing panel carries the code at reading distance.", "neon-dark",
            new Dictionary<string, string> {
                ["eyebrow"] = "Fri 14 Nov · 18:00 till late", ["title"] = "Night Market", ["body"] = "Street food, vinyl and late-night design stalls on the old tram depot.",
                ["cta"] = "Scan for the line-up", ["footer"] = "Depot Hall · Free entry",
            }),
        new("business-card", "card", "Business card", "Minimal Line on the back of a card, beside the details it links to.", "minimal-line",
            new Dictionary<string, string> {
                ["name"] = "Ava Stone", ["role"] = "Design Director", ["company"] = "Northwind Studio",
                ["email"] = "ava@example.com", ["phone"] = "+1 415 555 0198", ["cta"] = "See my work",
            }),
        new("package", "package", "Product package", "Botanical on a tea box: leaf modules that belong to the product instead of a sticker.", "botanical",
            new Dictionary<string, string> {
                ["brand"] = "Fernleaf", ["product"] = "Sencha", ["kind"] = "Japanese green tea", ["weight"] = "100 g · 50 cups",
                ["cta"] = "Scan to trace the harvest",
            }),
        new("table-tent", "tent", "Table tent", "Sunset on a restaurant table tent, where people scan from their seat.", "sunset",
            new Dictionary<string, string> {
                ["brand"] = "Saffron & Salt", ["title"] = "Today's menu", ["cta"] = "Scan to order from your table", ["table"] = "Table 12",
            }),
    };

    public static void Run(string outputDir) {
        var website = FindWebsite();
        var imageDir = Path.Combine(website, "static", "assets", "gallery");
        Directory.CreateDirectory(imageDir);
        var written = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var variantSources = new Dictionary<string, (string Src, int Width, int Height, string Name, string LookId)>(StringComparer.Ordinal);

        using var stream = new MemoryStream();
        using (var json = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping })) {
            json.WriteStartObject();
            json.WriteString("generatedBy", "CODEGLYPHX_GALLERY=1 dotnet run --project CodeGlyphX.Examples -c Release");
            json.WriteString("payload", QrGalleryLooks.ShortPayload);
            json.WriteNumber("sizePx", QrGalleryLooks.GallerySizePx);
            json.WriteStartArray("looks");
            foreach (var look in QrGalleryLooks.All) {
                json.WriteStartObject();
                json.WriteString("id", look.Id);
                json.WriteString("name", look.Name);
                json.WriteString("summary", look.Summary);
                json.WriteString("bestFor", look.BestFor);
                json.WriteString("family", look.Family);
                json.WriteStartArray("variants");
                foreach (var variant in look.Variants) {
                    var png = QrGalleryLooks.RenderGalleryPng(variant);
                    if (!QrArt.ValidateImage(png, QrGalleryLooks.ShortPayload).AllPassed) {
                        throw new InvalidOperationException($"The {variant.Id} gallery image failed native image validation.");
                    }
                    ImageReader.DecodeRgba32(png, out var width, out var height);
                    var file = variant.Id + ".png";
                    File.WriteAllBytes(Path.Combine(imageDir, file), png);
                    written.Add(file);
                    var src = "/assets/gallery/" + file;
                    variantSources[variant.Id] = (src, width, height, look.Name, look.Id);
                    json.WriteStartObject();
                    json.WriteString("id", variant.Id);
                    json.WriteString("label", variant.Label);
                    json.WriteString("src", src);
                    json.WriteNumber("width", width);
                    json.WriteNumber("height", height);
                    json.WriteNumber("bytes", png.Length);
                    json.WriteBoolean("logo", variant.ShowsLogo);
                    if (variant.ShowsLogo) {
                        var logoFile = variant.SampleLogoFile ?? throw new InvalidOperationException($"No sample logo file for {variant.Id}.");
                        if (written.Add(logoFile)) File.WriteAllBytes(Path.Combine(imageDir, logoFile), QrGalleryLooks.Logo(variant, variant.Create()));
                        json.WriteString("sampleLogoSrc", "/assets/gallery/" + logoFile);
                    }
                    var label = variant.Label.Length == 0 ? "" : " (" + variant.Label.ToLowerInvariant() + ")";
                    json.WriteString("alt", $"{look.Name}{label} QR code for {QrGalleryLooks.ShortPayload}: {look.Summary}");
                    json.WriteString("code", QrGalleryLooks.Snippet(variant));
                    json.WriteEndObject();
                }
                json.WriteEndArray();
                json.WriteEndObject();
            }
            json.WriteEndArray();

            json.WriteStartArray("mockups");
            foreach (var mockup in Mockups) {
                if (!variantSources.TryGetValue(mockup.Variant, out var source)) {
                    throw new InvalidOperationException($"The {mockup.Id} mockup uses {mockup.Variant}, which is not in the gallery.");
                }
                json.WriteStartObject();
                json.WriteString("id", mockup.Id);
                json.WriteString("kind", mockup.Kind);
                json.WriteString("title", mockup.Title);
                json.WriteString("description", mockup.Description);
                json.WriteString("variant", mockup.Variant);
                json.WriteString("look", source.Name);
                json.WriteString("lookId", source.LookId);
                json.WriteString("src", source.Src);
                json.WriteNumber("width", source.Width);
                json.WriteNumber("height", source.Height);
                json.WriteStartObject("copy");
                foreach (var pair in mockup.Copy) json.WriteString(pair.Key, pair.Value);
                json.WriteEndObject();
                json.WriteEndObject();
            }
            json.WriteEndArray();
            json.WriteEndObject();
        }

        var data = Encoding.UTF8.GetString(stream.ToArray()).Replace("\r\n", "\n") + "\n";
        File.WriteAllText(Path.Combine(website, "data", "gallery.json"), data, new UTF8Encoding(false));

        foreach (var stale in Directory.GetFiles(imageDir, "*.png").Where(f => !written.Contains(Path.GetFileName(f)))) {
            File.Delete(stale);
        }
        Console.WriteLine($"Rendered and native-validated {variantSources.Count} gallery QR images for {QrGalleryLooks.All.Count} looks into {imageDir}.");
    }

    private static string FindWebsite() {
        foreach (var start in new[] { Environment.CurrentDirectory, AppContext.BaseDirectory }) {
            var dir = new DirectoryInfo(start);
            while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "CodeGlyphX.sln"))) dir = dir.Parent;
            if (dir is not null) return Path.Combine(dir.FullName, "Website");
        }
        throw new DirectoryNotFoundException("Run the gallery generator from inside the CodeGlyphX repository.");
    }
}
