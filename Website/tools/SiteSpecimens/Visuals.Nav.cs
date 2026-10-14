// Navigation thumbnails: one small composition per destination in the site.json "main" menu, drawn on the night
// panel colors. The codes inside are CodeGlyphX SVG output nested as-is; the benchmark sparkline is drawn from the
// published benchmark data. nav.json mirrors the menu for pages whose header the API generator writes.
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using CodeGlyphX;
using CodeGlyphX.Rendering;

internal static partial class SiteVisuals {
    const int ThumbW = 160, ThumbH = 100;

    static string Thumb(string inner, string label) =>
        $"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{ThumbW}\" height=\"{ThumbH}\" viewBox=\"0 0 {ThumbW} {ThumbH}\" role=\"img\" aria-label=\"{Esc(label)}\">{inner}</svg>";

    static string Mono(double x, double y, string text, string fill, double size = 7.5, string anchor = "start", string weight = "500") =>
        $"<text x=\"{F(x)}\" y=\"{F(y)}\" font-family=\"JetBrains Mono, Consolas, monospace\" font-size=\"{F(size)}\" font-weight=\"{weight}\" fill=\"{fill}\" text-anchor=\"{anchor}\">{Esc(text)}</text>";

    static Dictionary<string, object> BuildNav(JsonNode site, string latestVersion, double[] speedups) {
        var thumbs = new Dictionary<string, object>();
        var ink = Hex(NightInk);
        const string dim = "#6f6987";

        string Qr(string payload, Rgba32 fg, QrModuleShape? shape = null, QrRenderOptions? o = null) =>
            QrMarkup(payload, o ?? QrNight(fg, shape), new QrEncodingOptions { ErrorCorrectionLevel = QrErrorCorrectionLevel.M });

        var art = new Dictionary<string, string>();

        // Playground: an editor beside the code it renders.
        {
            var sb = new StringBuilder();
            double[] widths = { 46, 34, 52, 28, 40 };
            for (var i = 0; i < widths.Length; i++) {
                sb.Append($"<rect x=\"{F(10 + (i % 2) * 6)}\" y=\"{F(22 + i * 12)}\" width=\"{F(widths[i])}\" height=\"4\" rx=\"2\" fill=\"{(i == 1 ? Hex(Pink) : i == 3 ? Hex(Cyan) : dim)}\"/>");
            }
            sb.Append($"<rect x=\"76\" y=\"14\" width=\"1\" height=\"72\" fill=\"{dim}\" opacity=\".5\"/>");
            var qr = Qr("https://codeglyphx.com/playground/", Violet, QrModuleShape.Rounded, new QrRenderOptions {
                Foreground = Violet, Background = Clear, ModuleSize = 4, QuietZone = 0, ModuleShape = QrModuleShape.Rounded,
                Eyes = new QrEyeOptions { UseFrame = true, OuterShape = QrModuleShape.Rounded, InnerShape = QrModuleShape.Circle, OuterColor = Cyan, InnerColor = Pink }
            });
            sb.Append(Nest(qr, 90, 16, 62, 68));
            art["playground"] = sb.ToString();
        }

        // PowerShell: the command and the file it wrote.
        {
            var sb = new StringBuilder();
            sb.Append(Mono(8, 30, "PS>", "#5391fe", 8, weight: "700"));
            sb.Append(Mono(8, 44, "New-ImageQRCode", ink, 7.2));
            sb.Append(Mono(8, 56, "-Content", dim, 7.2));
            sb.Append(Mono(8, 68, "-FilePath qr.png", dim, 7.2));
            sb.Append(Nest(Qr("https://evotec.xyz", NightInk), 100, 22, 52, 56));
            art["powershell"] = sb.ToString();
        }

        // Showcase: three shapes from the same payload, as the style presets do.
        {
            var sb = new StringBuilder();
            (QrModuleShape Shape, Rgba32 Color)[] looks = { (QrModuleShape.Dot, Violet), (QrModuleShape.Leaf, Pink), (QrModuleShape.Diamond, Cyan) };
            for (var i = 0; i < looks.Length; i++) {
                var o = new QrRenderOptions {
                    Foreground = looks[i].Color, Background = Clear, ModuleSize = 4, QuietZone = 0, ModuleShape = looks[i].Shape,
                    Eyes = new QrEyeOptions { UseFrame = true, OuterShape = QrModuleShape.Rounded, InnerShape = QrModuleShape.Rounded }
                };
                sb.Append(Nest(Qr("https://codeglyphx.com/showcase/", looks[i].Color, o: o), 6 + i * 51, 27, 46, 46));
            }
            art["showcase"] = sb.ToString();
        }

        // Pricing: the license as a Data Matrix, and what it costs.
        {
            var sb = new StringBuilder();
            sb.Append(Nest(DataMatrixCode.Render("APACHE-2.0", OutputFormat.Svg, options: Matrix(NightInk)).GetText(), 12, 20, 60, 60));
            sb.Append($"<text x=\"118\" y=\"60\" font-family=\"Unbounded, Inter, Segoe UI, sans-serif\" font-size=\"30\" font-weight=\"800\" fill=\"{ink}\" text-anchor=\"middle\">$0</text>");
            sb.Append(Mono(118, 76, "Apache 2.0", Hex(Cyan), 7, "middle"));
            art["pricing"] = sb.ToString();
        }

        // Docs: a sheet of the families the guides cover.
        {
            var sb = new StringBuilder();
            sb.Append(Nest(Qr("https://codeglyphx.com/docs/", NightInk), 10, 14, 40, 40));
            sb.Append(Nest(DataMatrixCode.Render("DOCS", OutputFormat.Svg, options: Matrix(Violet)).GetText(), 60, 14, 40, 40));
            sb.Append(Nest(AztecCode.Render("DOCS", OutputFormat.Svg, renderOptions: Matrix(Cyan)).GetText(), 110, 14, 40, 40));
            sb.Append(Nest(Barcode.Render(SymbolFormat.Code128, "CODEGLYPHX-DOCS", OutputFormat.Svg, Bars(NightInk, height: 18)).GetText(), 10, 66, 140, 20));
            art["docs"] = sb.ToString();
        }

        // API: an Aztec bullseye and the call that drew it.
        {
            var sb = new StringBuilder();
            sb.Append(Nest(AztecCode.Render("CodeGlyphX.AztecCode", OutputFormat.Svg, renderOptions: Matrix(Violet)).GetText(), 54, 8, 52, 52));
            sb.Append(Mono(80, 80, "AztecCode.Render()", ink, 7.4, "middle"));
            art["api"] = sb.ToString();
        }

        // Benchmarks: how many times faster than the runner-up, scenario by scenario (log scale).
        {
            var sb = new StringBuilder();
            var n = Math.Max(1, speedups.Length);
            var gap = 3.0;
            var bw = (140 - gap * (n - 1)) / n;
            var max = Math.Log10(Math.Max(2, speedups.DefaultIfEmpty(2).Max()));
            sb.Append($"<rect x=\"10\" y=\"84\" width=\"140\" height=\"1\" fill=\"{dim}\"/>");
            for (var i = 0; i < speedups.Length; i++) {
                var h = Math.Max(3, 64 * Math.Log10(Math.Max(1.05, speedups[i])) / max);
                sb.Append($"<rect x=\"{F(10 + i * (bw + gap))}\" y=\"{F(84 - h)}\" width=\"{F(bw)}\" height=\"{F(h)}\" rx=\"1.5\" fill=\"{(speedups[i] >= 10 ? Hex(Cyan) : Hex(Violet))}\"/>");
            }
            art["benchmarks"] = sb.ToString();
        }

        // Downloads: the latest package name as Code 128.
        {
            var sb = new StringBuilder();
            sb.Append(Nest(Barcode.Render(SymbolFormat.Code128, "CGX-" + latestVersion, OutputFormat.Svg, Bars(NightInk, height: 30)).GetText(), 14, 20, 132, 44));
            sb.Append(Mono(80, 80, "CodeGlyphX." + latestVersion + ".zip", Hex(Cyan), 7.4, "middle"));
            art["downloads"] = sb.ToString();
        }

        // Changelog: a timeline of releases, each a Micro QR of its version.
        {
            var sb = new StringBuilder();
            string[] versions = { "1.0", "1.2", "1.5", "2.0", latestVersion.Length > 3 ? latestVersion[..3] : latestVersion };
            sb.Append($"<rect x=\"14\" y=\"74\" width=\"132\" height=\"1\" fill=\"{dim}\"/>");
            for (var i = 0; i < versions.Length; i++) {
                var x = 14 + i * 30.5;
                var micro = MicroQrCodeEncoder.EncodeAlphanumeric("V" + versions[i], QrErrorCorrectionLevel.L);
                var color = i == versions.Length - 1 ? Cyan : NightInk;
                sb.Append(Nest(MatrixBarcode.Render(micro.Modules, OutputFormat.Svg, Matrix(color)).GetText(), x, 34, 24, 24));
                sb.Append($"<circle cx=\"{F(x + 12)}\" cy=\"74.5\" r=\"2.4\" fill=\"{Hex(color)}\"/>");
                sb.Append(Mono(x + 12, 88, versions[i], i == versions.Length - 1 ? Hex(Cyan) : dim, 6.6, "middle"));
            }
            art["changelog"] = sb.ToString();
        }

        // The menu itself: groups and destinations, read from site.json so the header has one source of truth.
        var groups = new List<object>();
        var main = site["Navigation"]!["Menus"]!.AsArray().First(m => (string?)m!["Name"] == "main")!;
        foreach (var g in main["Items"]!.AsArray()) {
            if (g!["Items"] is null) continue; // Home: the brand link carries it.
            var items = new List<object>();
            foreach (var it in g!["Items"]!.AsArray()) {
                var id = (string)it!["Id"]!;
                if (!art.TryGetValue(id, out var inner)) throw new InvalidOperationException("No navigation thumbnail for " + id);
                var src = WriteVisual($"nav/{id}.svg", Thumb(inner, (string)it["Title"]!));
                thumbs[id] = new { src, w = ThumbW, h = ThumbH };
                items.Add(new { id, title = (string)it["Title"]!, url = (string)it["Url"]!, description = (string?)it["Description"] ?? "", thumb = src });
            }
            groups.Add(new { id = (string)g["Id"]!, title = (string)g["Title"]!, description = (string?)g["Description"] ?? "", items });
        }
        WriteVisual("nav.json", JsonSerializer.Serialize(new { groups }, new JsonSerializerOptions { WriteIndented = false }) + "\n");
        return thumbs;
    }
}
