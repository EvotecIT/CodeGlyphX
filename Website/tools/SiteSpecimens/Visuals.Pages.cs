// Visuals for the product pages: showcase, pricing, downloads, changelog, not-found and the API index.
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using CodeGlyphX;
using CodeGlyphX.Payloads;
using CodeGlyphX.Rendering;

internal static partial class SiteVisuals {
    static readonly Rgba32 Ink = new(22, 19, 31);
    static readonly Rgba32 DeepViolet = new(91, 33, 182);

    static QrRenderOptions Plate(QrModuleShape shape = QrModuleShape.Square, Rgba32? eyes = null) => new() {
        ModuleSize = 6, QuietZone = 2, Foreground = Ink, ModuleShape = shape == QrModuleShape.Square ? null : shape,
        Eyes = eyes is null ? null : new QrEyeOptions { UseFrame = true, OuterShape = QrModuleShape.Rounded, InnerShape = QrModuleShape.Rounded, OuterColor = eyes, InnerColor = eyes }
    };

    /// <summary>A QR saved as SVG, with the text CodeGlyphX decodes from its PNG rendering.</summary>
    static object CodeCard(string file, string payload, string label, QrRenderOptions? options = null, string? href = null) {
        var o = options ?? Plate();
        var a = SaveQr(file.Replace(".svg", ""), payload, o);
        var png = QR.Render(payload, OutputFormat.Png, new QrRenderOptions { ModuleSize = 8 }).ToArray();
        var decoded = QrImageDecoder.TryDecodeImage(png, out var d) ? d.Text : null;
        return new { src = a.Src, w = a.W, h = a.H, label, decoded, href };
    }

    static object Obj(Asset a) => new { src = a.Src, w = a.W, h = a.H };

    static object BuildPages(string version) {
        // Showcase: the codes each application works with.
        var showcase = new Dictionary<string, object> {
            ["informationbox"] = CodeCard("pages/showcase-otp.svg",
                "otpauth://totp/Contoso:ava@contoso.com?secret=JBSWY3DPEHPK3PXP&issuer=Contoso&digits=6&period=30",
                "Enrollment code for an authenticator", Plate(QrModuleShape.Rounded, DeepViolet)),
        };
        // Pricing: scan to sponsor, and the license.
        var pricing = new {
            sponsor = CodeCard("pages/pricing-sponsor.svg", "https://github.com/sponsors/PrzemyslawKlys", "GitHub Sponsors", Plate(QrModuleShape.Rounded, DeepViolet), "https://github.com/sponsors/PrzemyslawKlys"),
            paypal = CodeCard("pages/pricing-paypal.svg", "https://paypal.me/PrzemyslawKlys", "PayPal", Plate(QrModuleShape.Rounded), "https://paypal.me/PrzemyslawKlys"),
            license = Obj(SaveSvg("pages/pricing-license.svg", DataMatrixCode.Render("Apache-2.0", OutputFormat.Svg, options: new MatrixOptions { ModuleSize = 8, QuietZone = 1, Foreground = Ink }).GetText())),
        };

        // Downloads: one code per way to get CodeGlyphX.
        var zip = $"CodeGlyphX.{version}.zip";
        var downloads = new {
            version,
            nuget = CodeCard("pages/download-nuget.svg", "https://www.nuget.org/packages/CodeGlyphX", "nuget.org/packages/CodeGlyphX", Plate(QrModuleShape.Rounded, DeepViolet), "https://www.nuget.org/packages/CodeGlyphX"),
            gallery = CodeCard("pages/download-psgallery.svg", "https://www.powershellgallery.com/packages/ImagePlayground", "PowerShell Gallery", Plate(QrModuleShape.Rounded, new Rgba32(37, 99, 235)), "https://www.powershellgallery.com/packages/ImagePlayground"),
            releases = CodeCard("pages/download-releases.svg", "https://github.com/EvotecIT/CodeGlyphX/releases", "GitHub Releases", Plate(), "https://github.com/EvotecIT/CodeGlyphX/releases"),
            zip,
            zipCode = Obj(SaveSvg("pages/download-zip.svg", Barcode.Render(SymbolFormat.Code128, zip, OutputFormat.Svg, new BarcodeOptions { ModuleSize = 2, QuietZone = 0, HeightModules = 30, Foreground = Ink, Background = Clear }).GetText())),
        };

        return new { showcase, pricing, downloads, changelog = BuildChangelog(), notFound = BuildNotFound(), api = BuildApi() };
    }

    /// <summary>
    /// Release highlights from data/release-highlights.json (hand-maintained: which release added which visible
    /// capability). Each highlight is drawn here with the API it names.
    /// </summary>
    static object BuildChangelog() {
        var path = Path.Combine(root, "data", "release-highlights.json");
        var list = JsonNode.Parse(File.ReadAllText(path))!["highlights"]!.AsArray();
        var output = new List<object>();
        foreach (var h in list) {
            var key = (string)h!["visual"]!;
            var file = $"changelog/{key}.svg";
            if (key == "scanner") {
                // The decoding guide's scanned desk: four families found in one image.
                var desk = Path.Combine(staticRoot, "images", "art", "qr-expressive-waves.png");
                ImageReader.DecodeRgba32(File.ReadAllBytes(desk), out var dw, out var dh);
                output.Add(new { tag = (string)h["tag"]!, version = (string)h["version"]!, title = (string)h["title"]!, summary = (string)h["summary"]!, api = (string?)h["api"], src = "/images/art/qr-expressive-waves.png", w = dw, h = dh });
                continue;
            }
            var styled = key switch {
                "qr-art" => new QrRenderOptions {
                    ModuleSize = 8, QuietZone = 2, Foreground = DeepViolet, ModuleShape = QrModuleShape.Dot,
                    Eyes = new QrEyeOptions { UseFrame = true, FrameStyle = QrEyeFrameStyle.Target, OuterShape = QrModuleShape.Rounded, InnerShape = QrModuleShape.Circle, OuterColor = new Rgba32(190, 24, 93), InnerColor = new Rgba32(14, 116, 144) }
                },
                "connected" => new QrRenderOptions { ModuleSize = 8, QuietZone = 2, Foreground = DeepViolet, ModuleShape = QrModuleShape.ConnectedSquircle },
                "transparent" => new QrRenderOptions { ModuleSize = 8, QuietZone = 2, Foreground = DeepViolet, Background = Clear, ModuleShape = QrModuleShape.Rounded },
                _ => null
            };
            if (styled is not null) {
                var s = SaveQr($"changelog/{key}", "https://codeglyphx.com", styled);
                output.Add(new { tag = (string)h["tag"]!, version = (string)h["version"]!, title = (string)h["title"]!, summary = (string)h["summary"]!, api = (string?)h["api"], src = s.Src, w = s.W, h = s.H });
                continue;
            }
            string svg = key switch {
                "microqr" => MatrixBarcode.Render(MicroQrCodeEncoder.EncodeAlphanumeric("CGX-2026", QrErrorCorrectionLevel.L).Modules, OutputFormat.Svg, new MatrixOptions { ModuleSize = 6, QuietZone = 2 }).GetText(),
                "industrial" => Barcode.Render(SymbolFormat.Itf14, "15400141288763", OutputFormat.Svg, new BarcodeOptions { ModuleSize = 2, QuietZone = 8, HeightModules = 36 }).GetText(),
                "databar-expanded" => File.ReadAllText(Path.Combine(staticRoot, "specimens", "gs1databarexpanded.svg")),
                "code39" => Barcode.Render(SymbolFormat.Code39, "CGX-2.1", OutputFormat.Svg, new BarcodeOptions { ModuleSize = 2, QuietZone = 8, HeightModules = 36 }).GetText(),
                "datamatrix" => DataMatrixCode.Render("LOT-2026-0042", OutputFormat.Svg, options: new MatrixOptions { ModuleSize = 6, QuietZone = 2 }).GetText(),

                "ascii" => AsciiSvg(QR.Render("CGX", OutputFormat.Ascii, new QrRenderOptions { QuietZone = 1 }).GetText()),
                "first" => QR.Render("https://codeglyphx.com", OutputFormat.Svg, new QrRenderOptions { ModuleSize = 6, QuietZone = 2 }).GetText(),
                _ => throw new InvalidOperationException("Unknown release highlight visual " + key)
            };
            var a = SaveSvg(file, svg);
            output.Add(new {
                tag = (string)h["tag"]!,
                version = (string)h["version"]!,
                title = (string)h["title"]!,
                summary = (string)h["summary"]!,
                api = (string?)h["api"],
                src = a.Src, w = a.W, h = a.H
            });
        }
        return output;
    }

    /// <summary>ASCII output drawn as monospaced text so it can sit in an image frame.</summary>
    static string AsciiSvg(string ascii) {
        var lines = ascii.Replace("\r", "").TrimEnd('\n').Split('\n');
        var cols = lines.Max(l => l.Length);
        const double cw = 6.0;
        // Keep the symbol square: a row is as tall as one module is wide.
        var lh = Math.Clamp(cw * cols / Math.Max(1, lines.Length), 7.2, 14.0);
        var fs = Math.Min(lh * 0.95, cw * 1.66);
        var w = cols * cw + 8;
        var h = lines.Length * lh + 8;
        var sb = new System.Text.StringBuilder();
        sb.Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{F(w)}\" height=\"{F(h)}\" viewBox=\"0 0 {F(w)} {F(h)}\"><rect width=\"100%\" height=\"100%\" fill=\"#FFFFFF\"/>");
        sb.Append($"<g font-family=\"Consolas, 'JetBrains Mono', monospace\" font-size=\"{F(fs)}\" fill=\"#16131F\" xml:space=\"preserve\">");
        for (var i = 0; i < lines.Length; i++) sb.Append($"<text x=\"4\" y=\"{F(4 + (i + 1) * lh - 1.5)}\" textLength=\"{F(lines[i].Length * cw)}\">{Esc(lines[i])}</text>");
        sb.Append("</g></svg>");
        return sb.ToString();
    }

    /// <summary>
    /// The not-found page suggests the closest page to the address that failed. Every page gets a QR here that decodes
    /// to its URL, so the suggestion can be scanned too.
    /// </summary>
    static object BuildNotFound() {
        var pages = new List<object>();
        void Add(string url, string title, string section) {
            var slug = url.Trim('/').Replace('/', '-');
            if (slug.Length == 0) slug = "home";
            var full = "https://codeglyphx.com" + url;
            var a = SaveSvg($"notfound/{slug}.svg", QR.Render(full, OutputFormat.Svg, new QrRenderOptions { ModuleSize = 6, QuietZone = 2, Foreground = Ink }).GetText());
            pages.Add(new { url, title, section, src = a.Src });
        }

        Add("/", "Home", "CodeGlyphX");
        foreach (var dir in new[] { ("pages", ""), ("docs", "/docs") }) {
            foreach (var md in Directory.GetFiles(Path.Combine(root, "content", dir.Item1), "*.md").OrderBy(p => p, StringComparer.Ordinal)) {
                var text = File.ReadAllText(md);
                var slug = Regex.Match(text, @"^slug:\s*(\S+)", RegexOptions.Multiline).Groups[1].Value;
                if (slug is "" or "404" or "index" && dir.Item1 == "pages") continue;
                var title = Regex.Match(text, @"^title:\s*(.+)$", RegexOptions.Multiline).Groups[1].Value.Trim().Trim('"');
                title = Regex.Replace(title, @"\s*[-|]\s*CodeGlyphX.*$", "");
                var url = slug == "index" ? dir.Item2 + "/" : dir.Item2 + "/" + slug + "/";
                Add(url, title, dir.Item1 == "docs" ? "Docs" : "Pages");
            }
        }
        Add("/api/", "API reference", "Reference");
        Add("/powershell/commands/", "ImagePlayground commands", "Reference");
        WriteVisual("notfound/index.json", System.Text.Json.JsonSerializer.Serialize(new { pages }) + "\n");
        return new { count = pages.Count, index = "/visuals/notfound/index.json" };
    }

    /// <summary>Entry points of the API, each with the code it produces.</summary>
    static object BuildApi() {
        var list = new (string Type, string Url, string Summary, string Visual)[] {
            ("QR", "/api/codeglyphx.qr/", "Encode, render and save QR codes.", "/specimens/qrcode.svg"),
            ("Barcode", "/api/codeglyphx.barcode/", "Linear symbologies from Code 128 to postal.", "/specimens/code128.svg"),
            ("DataMatrixCode", "/api/codeglyphx.datamatrixcode/", "Data Matrix, square and rectangular.", "/specimens/datamatrix.svg"),
            ("Pdf417Code", "/api/codeglyphx.pdf417code/", "PDF417 and MicroPDF417.", "/specimens/pdf417.svg"),
            ("AztecCode", "/api/codeglyphx.azteccode/", "Aztec, compact and full range.", "/specimens/aztec.svg"),
            ("QrPayloads", "/api/codeglyphx.payloads.qrpayloads/", "Wi-Fi, contacts, OTP, payments.", "/specimens/payload-wifi.svg"),
            ("SymbolScanner", "/api/codeglyphx.symbolscanner/", "Find and decode codes in images.", "/images/art/qr-expressive-waves.png"),
        };
        return list.Select(x => new { type = x.Type, url = x.Url, summary = x.Summary, src = x.Visual }).ToList();
    }
}
