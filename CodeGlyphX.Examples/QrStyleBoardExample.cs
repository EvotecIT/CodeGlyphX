using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CodeGlyphX;
using CodeGlyphX.Rendering;

namespace CodeGlyphX.Examples;

/// <summary>
/// Renders the website style board from <see cref="QrStyleBoardLooks"/>: one PNG per look, the
/// <c>style-board.json</c> manifest read by the site, and a local HTML preview. Copy the PNGs to
/// <c>Website/static/assets/style-board/</c> and the manifest to <c>Website/static/data/</c>.
/// </summary>
internal static partial class QrStyleBoardExample {
    public static void Run(string outputDir) {
        var dir = Path.Combine(outputDir, "qr-style-board");
        Directory.CreateDirectory(dir);

        var entries = new List<StyleBoardEntry>(QrStyleBoardLooks.All.Count);
        foreach (var look in QrStyleBoardLooks.All) {
            var png = QrStyleBoardLooks.RenderPng(look);
            if (!QrArt.ValidateImage(png, QrStyleBoardLooks.Payload).AllPassed) {
                throw new InvalidOperationException($"The {look.Name} style board look failed native image validation.");
            }
            ImageReader.DecodeRgba32(png, out var width, out var height);
            var fileName = look.Slug + ".png";
            File.WriteAllBytes(Path.Combine(dir, fileName), png);
            entries.Add(new StyleBoardEntry(look.Name, fileName, $"{look.Name} QR style: {look.Summary}",
                QrStyleBoardLooks.Payload, width, height));
        }

        var manifestJson = JsonSerializer.Serialize(entries, typeof(List<StyleBoardEntry>), StyleBoardJsonContext.Default);
        (manifestJson + "\n").WriteText(Path.Combine(dir, "style-board.json"));
        WriteHtmlIndex(dir, entries);
    }

    private readonly record struct StyleBoardEntry(string Name, string File, string Alt, string Payload, int Width, int Height);

    [JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    [JsonSerializable(typeof(List<StyleBoardEntry>))]
    private sealed partial class StyleBoardJsonContext : JsonSerializerContext;

    private static void WriteHtmlIndex(string dir, List<StyleBoardEntry> entries) {
        var sb = new StringBuilder();
        sb.AppendLine("<!doctype html>");
        sb.AppendLine("<html lang=\"en\"><head><meta charset=\"utf-8\"/>");
        sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"/>");
        sb.AppendLine("<title>QR Style Board</title>");
        sb.AppendLine("<style>");
        sb.AppendLine("body{font-family:Segoe UI,Arial,sans-serif;background:#0e0e14;color:#e6e6e6;margin:0;padding:24px}");
        sb.AppendLine(".grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(220px,1fr));gap:18px}");
        sb.AppendLine(".card{background:#141521;border-radius:16px;padding:14px;text-align:center}");
        sb.AppendLine(".card img{width:100%;height:auto}");
        sb.AppendLine(".label{margin-top:10px;font-weight:600;font-size:14px}");
        sb.AppendLine("</style></head><body>");
        sb.AppendLine("<h1>QR Style Board</h1>");
        sb.AppendLine("<div class=\"grid\">");
        foreach (var entry in entries) {
            sb.Append("<div class=\"card\"><img src=\"").Append(entry.File).Append("\" alt=\"").Append(WebUtility.HtmlEncode(entry.Alt)).Append("\"/>")
                .Append("<div class=\"label\">").Append(WebUtility.HtmlEncode(entry.Name)).Append("</div></div>");
        }
        sb.AppendLine("</div></body></html>");
        sb.ToString().WriteText(Path.Combine(dir, "style-board.html"));
    }
}
