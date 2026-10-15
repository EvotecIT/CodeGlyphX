// Inner-page visuals: navigation thumbnails, documentation figures, benchmark charts and product-page codes.
// Everything is rendered by CodeGlyphX into static/visuals/ and described in data/visuals.json.
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

internal static partial class SiteVisuals {
    public static void Run(string website) {
        root = website;
        staticRoot = Path.Combine(website, "static");

        // Rebuild from scratch so removed visuals do not linger.
        var visuals = Path.Combine(staticRoot, "visuals");
        if (Directory.Exists(visuals)) {
            foreach (var file in Directory.GetFiles(visuals, "*", SearchOption.AllDirectories)) File.Delete(file);
        }

        var site = JsonNode.Parse(File.ReadAllText(Path.Combine(website, "site.json")))!;
        var csproj = File.ReadAllText(Path.Combine(website, "..", "CodeGlyphX", "CodeGlyphX.csproj"));
        var version = Regex.Match(csproj, "<VersionPrefix>([^<]+)</VersionPrefix>").Groups[1].Value;

        var docs = BuildDocs(out var cards);
        var bench = BuildBench(out var recognition);
        var nav = BuildNav(site, version, recognition);
        var pages = BuildPages(version);

        var model = new {
            generatedBy = "Website/tools/SiteSpecimens",
            version,
            nav = new { thumbs = nav },
            docs = new { cards, figures = docs.ToDictionary(kv => kv.Key, kv => (object)new { layout = kv.Value.Layout, items = kv.Value.Items.Select(i => new { src = i.Src, w = i.W, h = i.H, label = i.Label, note = i.Note, decoded = i.Decoded, text = i.Text }), extra = kv.Value.Extra }) },
            bench,
            pages
        };
        var json = new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
        File.WriteAllText(Path.Combine(website, "data", "visuals.json"), JsonSerializer.Serialize(model, json) + "\n", Utf8);

        var files = Directory.GetFiles(visuals, "*", SearchOption.AllDirectories);
        Console.WriteLine($"Visuals: {files.Length} files, {files.Sum(f => new FileInfo(f).Length) / 1024.0:0.0} KB under static/visuals.");
    }
}
