// Documentation figures: wherever a guide describes output, the figure shows that output, rendered here by
// CodeGlyphX. Captions that state a decoded value come from decoding the rendered image with CodeGlyphX.
using System.Text;
using CodeGlyphX;
using CodeGlyphX.Aztec;
using CodeGlyphX.DataMatrix;
using CodeGlyphX.Payloads;
using CodeGlyphX.Pdf417;
using CodeGlyphX.Rendering;

namespace CodeGlyphX.Website.Specimens;

internal static partial class SiteVisuals {
    sealed record FigItem(string Src, int W, int H, string Label, string? Note = null, string? Decoded = null, string? Text = null);

    sealed record Figure(string Layout, IReadOnlyList<FigItem> Items, object? Extra = null);

    static FigItem Item(Asset a, string label, string? note = null, string? decoded = null) => new(a.Src, a.W, a.H, label, note, decoded);

    /// <summary>Records an existing specimen from static/specimens (already rendered by Program.cs).</summary>
    static Asset Specimen(string name) {
        var path = Path.Combine(staticRoot, "specimens", name + ".svg");
        var (w, h) = SvgSize(File.ReadAllText(path));
        return new Asset("/specimens/" + name + ".svg", w, h);
    }

    static Dictionary<string, Figure> BuildDocs(out Dictionary<string, object> cards) {
        var figs = new Dictionary<string, Figure>();
        var plain = new MatrixOptions { ModuleSize = 6, QuietZone = 2 };

        // QR error correction: the same URL at each level grows the symbol.
        {
            var items = new List<FigItem>();
            foreach (var (level, pct) in new[] { (QrErrorCorrectionLevel.L, "7%"), (QrErrorCorrectionLevel.M, "15%"), (QrErrorCorrectionLevel.Q, "25%"), (QrErrorCorrectionLevel.H, "30%") }) {
                var enc = new QrEncodingOptions { ErrorCorrectionLevel = level };
                var code = QR.Encode("https://codeglyphx.com/docs/qr/", enc);
                var a = SaveSvg($"docs/qr-ecc-{level.ToString().ToLowerInvariant()}.svg",
                    code.Render(OutputFormat.Svg, new QrRenderOptions { ModuleSize = 6, QuietZone = 2 }).GetText());
                items.Add(Item(a, level.ToString(), $"recovers ~{pct} · version {code.Version}, {code.Size} × {code.Size}"));
            }
            figs["qr-ecc"] = new Figure("compare", items);
        }

        // Module shapes: one payload, one shape per tile.
        {
            var items = new List<FigItem>();
            foreach (var shape in new[] { QrModuleShape.Square, QrModuleShape.Rounded, QrModuleShape.Circle, QrModuleShape.Dot, QrModuleShape.Squircle, QrModuleShape.Diamond, QrModuleShape.Leaf, QrModuleShape.ConnectedRounded }) {
                var o = new QrRenderOptions { ModuleSize = 8, QuietZone = 2, ModuleShape = shape, Foreground = new Rgba32(22, 19, 31) };
                var a = SaveQr($"docs/qr-shape-{shape.ToString().ToLowerInvariant()}", "https://codeglyphx.com", o);
                items.Add(Item(a, shape.ToString(), ReadsBack("https://codeglyphx.com", o)));
            }
            figs["qr-shapes"] = new Figure("row", items);
        }

        // Eye frames: the finder patterns styled independently of the data modules.
        {
            var items = new List<FigItem>();
            foreach (var frame in new[] { QrEyeFrameStyle.Single, QrEyeFrameStyle.DoubleRing, QrEyeFrameStyle.Target, QrEyeFrameStyle.Bracket, QrEyeFrameStyle.Badge, QrEyeFrameStyle.CutCorner }) {
                var o = new QrRenderOptions {
                    ModuleSize = 8, QuietZone = 2, ModuleShape = QrModuleShape.Rounded, Foreground = new Rgba32(22, 19, 31),
                    Eyes = new QrEyeOptions { UseFrame = true, FrameStyle = frame, OuterShape = QrModuleShape.Rounded, InnerShape = QrModuleShape.Circle, OuterColor = new Rgba32(109, 40, 217), InnerColor = new Rgba32(14, 116, 144) }
                };
                var a = SaveQr($"docs/qr-eye-{frame.ToString().ToLowerInvariant()}", "https://codeglyphx.com", o);
                items.Add(Item(a, frame.ToString(), ReadsBack("https://codeglyphx.com", o)));
            }
            figs["qr-eyes"] = new Figure("row", items);
        }

        // Micro QR: the four sizes, then standard QR version 1 for scale.
        {
            var items = new List<FigItem>();
            var micro = new (int Version, string Text, Func<MicroQrCode> Encode)[] {
                (1, "12345", () => MicroQrCodeEncoder.EncodeNumeric("12345", QrErrorCorrectionLevel.L, 1, 1)),
                (2, "CGX26", () => MicroQrCodeEncoder.EncodeAlphanumeric("CGX26", QrErrorCorrectionLevel.L, 2, 2)),
                (3, "CGX-2026", () => MicroQrCodeEncoder.EncodeAlphanumeric("CGX-2026", QrErrorCorrectionLevel.L, 3, 3)),
                (4, "CODEGLYPHX-2026", () => MicroQrCodeEncoder.EncodeAlphanumeric("CODEGLYPHX-2026", QrErrorCorrectionLevel.L, 4, 4)),
            };
            foreach (var m in micro) {
                var code = m.Encode();
                var a = SaveSvg($"docs/microqr-m{m.Version}.svg", MatrixBarcode.Render(code.Modules, OutputFormat.Svg, plain).GetText());
                items.Add(Item(a, "M" + m.Version, $"{code.Size} × {code.Size}", m.Text));
            }
            var qr = QR.Encode("CODEGLYPHX-2026", new QrEncodingOptions { ErrorCorrectionLevel = QrErrorCorrectionLevel.L });
            var q = SaveSvg("docs/microqr-vs-qr.svg", qr.Render(OutputFormat.Svg, new QrRenderOptions { ModuleSize = 6, QuietZone = 4 }).GetText());
            items.Add(Item(q, "QR version " + qr.Version, $"{qr.Size} × {qr.Size}, wider quiet zone", "CODEGLYPHX-2026"));
            figs["microqr-sizes"] = new Figure("compare", items);
        }

        // Data Matrix: square and rectangular symbols for the same serial.
        {
            var items = new List<FigItem>();
            foreach (var shape in new[] { DataMatrixShape.Square, DataMatrixShape.Rectangular }) {
                var sym = DataMatrixCode.Encode("SERIAL-12345", new DataMatrixEncodingOptions { Shape = shape });
                var a = SaveSvg($"docs/datamatrix-{shape.ToString().ToLowerInvariant()}.svg", sym.Render(OutputFormat.Svg, plain).GetText());
                items.Add(Item(a, shape.ToString(), $"{sym.Rows} × {sym.Columns} modules", "SERIAL-12345"));
            }
            figs["datamatrix-shapes"] = new Figure("compare", items);
        }

        // PDF417: more error correction, more rows.
        {
            var items = new List<FigItem>();
            foreach (var level in new[] { 2, 4, 6 }) {
                var sym = Pdf417Code.Encode("DOCUMENT-2026-0042", new Pdf417EncodeOptions { ErrorCorrectionLevel = level });
                var a = SaveSvg($"docs/pdf417-ecc{level}.svg", sym.Render(OutputFormat.Svg, new MatrixOptions { ModuleSize = 2, QuietZone = 2 }).GetText());
                items.Add(Item(a, "Level " + level, $"{sym.Rows} rows × {sym.Columns} columns"));
            }
            figs["pdf417-ecc"] = new Figure("compare", items);
        }

        // Aztec: compact and full-range symbols, and the error-correction percentage.
        {
            var items = new List<FigItem>();
            foreach (var (pct, label) in new[] { (23, "23% (default)"), (50, "50%") }) {
                var sym = AztecCode.Encode("TICKET-2026-0042", new AztecEncodeOptions { ErrorCorrectionPercent = pct });
                var a = SaveSvg($"docs/aztec-ecc{pct}.svg", sym.Render(OutputFormat.Svg, plain).GetText());
                items.Add(Item(a, label, $"{(sym.Compact ? "compact" : "full")}, {sym.Layers} layers, {sym.Size} × {sym.Size}"));
            }
            figs["aztec-ecc"] = new Figure("compare", items);
        }

        // Linear pairs, drawn at the same module width so density differences are visible.
        Figure Linear(string id, params (SymbolFormat Format, string Value, string Label)[] list) {
            var items = new List<FigItem>();
            for (var n = 0; n < list.Length; n++) {
                var x = list[n];
                var o = new BarcodeOptions { ModuleSize = 2, QuietZone = 10, HeightModules = 36 };
                var svg = Barcode.Render(x.Format, x.Value, OutputFormat.Svg, o).GetText();
                var a = SaveSvg($"docs/{id}-{n + 1}-{x.Format.ToString().ToLowerInvariant()}.svg", svg);
                var png = Barcode.Render(x.Format, x.Value, OutputFormat.Png, new BarcodeOptions { ModuleSize = 3, QuietZone = 12, HeightModules = 40 }).ToArray();
                var decoded = Scan(png, x.Format);
                var mods = Barcode.Encode(x.Format, x.Value).TotalModules;
                var note = $"{mods} modules wide";
                if (decoded is not null && decoded != x.Value && decoded.StartsWith(x.Value, StringComparison.Ordinal)) note += $", check character {decoded[x.Value.Length..]}";
                items.Add(Item(a, x.Label, note, decoded));
            }
            return new Figure("linear", items);
        }

        figs["code128"] = Linear("code128", (SymbolFormat.Code128, "PRODUCT-12345", "Code 128"), (SymbolFormat.Gs1Code128, "(01)09501101530003(17)261231", "GS1-128"));
        figs["code39-93"] = Linear("code39", (SymbolFormat.Code39, "HELLO-123", "Code 39"), (SymbolFormat.Code93, "HELLO-123", "Code 93"));
        figs["ean-upc"] = Linear("retail", (SymbolFormat.Ean, "5901234123457", "EAN-13"), (SymbolFormat.Ean, "96385074", "EAN-8"), (SymbolFormat.UpcA, "036000291452", "UPC-A"), (SymbolFormat.UpcE, "123456", "UPC-E"));

        // Other linear and postal symbologies, from the specimen sheet.
        figs["other-1d"] = new Figure("row", new[] {
            ("itf14", "ITF-14"), ("codabar", "Codabar"), ("telepen", "Telepen"), ("msi", "MSI"), ("pharmacode", "Pharmacode"),
            ("postnet", "POSTNET"), ("royalmail4state", "Royal Mail 4-State"), ("uspsintelligentmail", "USPS Intelligent Mail"),
        }.Select(s => Item(Specimen(s.Item1), s.Item2)).ToList());

        // GS1: one element string in three carriers, each decoded back.
        {
            const string ai = "(01)09520123456788(10)ABC1(17)261231";
            const string link = "https://id.gs1.org/01/09520123456788/10/ABC1?17=261231";
            var items = new List<FigItem>();
            var c128 = Barcode.Render(SymbolFormat.Gs1Code128, ai, OutputFormat.Svg, new BarcodeOptions { ModuleSize = 2, QuietZone = 10, HeightModules = 36 }).GetText();
            items.Add(Item(SaveSvg("docs/gs1-128.svg", c128), "GS1-128", "linear carrier",
                Scan(Barcode.Render(SymbolFormat.Gs1Code128, ai, OutputFormat.Png, new BarcodeOptions { ModuleSize = 3, QuietZone = 12 }).ToArray(), SymbolFormat.Gs1Code128, SymbolFormat.Code128)));
            var dmOptions = new DataMatrixEncodingOptions { IsGs1 = true };
            const string dmText = ai;
            try {
                var dm = DataMatrixCode.Render(dmText, OutputFormat.Svg, dmOptions, plain).GetText();
                items.Add(Item(SaveSvg("docs/gs1-datamatrix.svg", dm), "GS1 DataMatrix", "2D carrier",
                    Scan(DataMatrixCode.Render(dmText, OutputFormat.Png, dmOptions, new MatrixOptions { ModuleSize = 8, QuietZone = 4 }).ToArray(), SymbolFormat.DataMatrix)));
            } catch (Exception ex) {
                Console.WriteLine("GS1 DataMatrix skipped: " + ex.Message);
            }
            var dl = QR.Render(link, OutputFormat.Svg, new QrRenderOptions { ModuleSize = 6, QuietZone = 2 }).GetText();
            items.Add(Item(SaveSvg("docs/gs1-digital-link.svg", dl), "Digital Link QR", "web URI carrier",
                Scan(QR.Render(link, OutputFormat.Png, new QrRenderOptions { ModuleSize = 8 }).ToArray(), SymbolFormat.QrCode)));
            figs["gs1"] = new Figure("compare", items);
        }

        // Payload helpers: each code beside the string a phone reads from it.
        {
            var items = new List<FigItem>();
            var payloads = new (string Id, string Label, object Payload)[] {
                ("wifi", "Wi-Fi network", QrPayloads.Wifi("Evotec-Guest", "correct-horse-battery")),
                ("otp", "Authenticator (TOTP)", QrPayloads.OneTimePassword(OtpAuthType.Totp, "JBSWY3DPEHPK3PXP", label: "ava@example.com", issuer: "CodeGlyphX")),
                ("vcard", "Contact card", QrPayload.VCard(firstName: "Ava", lastName: "Stone", phone: "+14155550198", email: "ava@example.com")),
                ("girocode", "SEPA payment", QrPayloads.Girocode(iban: "DE89370400440532013000", bic: "COBADEFFXXX", name: "Evotec Services", amount: 99.99m)),
            };
            foreach (var p in payloads) {
                var pngOptions = new QrRenderOptions { ModuleSize = 8 };
                var png = p.Payload switch {
                    QrPayloadData data => QR.Render(data, OutputFormat.Png, pngOptions).ToArray(),
                    string text => QR.Render(text, OutputFormat.Png, pngOptions).ToArray(),
                    _ => throw new InvalidOperationException("Unexpected payload type for " + p.Id)
                };
                var decoded = QrImageDecoder.TryDecodeImage(png, out var d) ? d.Text : null;
                items.Add(Item(Specimen("payload-" + p.Id), p.Label, null, decoded));
            }
            figs["payloads"] = new Figure("decoded", items);
        }

        figs["decoding"] = BuildScanFigure();
        figs["renderers"] = BuildFormatsFigure();

        // Quick start: what the four steps write.
        {
            var items = new List<FigItem>();
            var hello = QR.Render("Hello, World!", OutputFormat.Svg, new QrRenderOptions { ModuleSize = 6, QuietZone = 2 }).GetText();
            var helloPng = QR.Render("Hello, World!", OutputFormat.Png, new QrRenderOptions { ModuleSize = 8 }).ToArray();
            items.Add(Item(SaveSvg("docs/quickstart-hello.svg", hello), "hello.png", "QR.Save(\"Hello, World!\", ...)",
                QrImageDecoder.TryDecodeImage(helloPng, out var hd) ? hd.Text : null));
            items.Add(Item(SaveSvg("docs/quickstart-code128.svg", Barcode.Render(SymbolFormat.Code128, "PRODUCT-12345", OutputFormat.Svg, new BarcodeOptions { ModuleSize = 2, QuietZone = 10, HeightModules = 36 }).GetText()), "barcode.png", "Code 128"));
            items.Add(Item(SaveSvg("docs/quickstart-ean.svg", Barcode.Render(SymbolFormat.Ean, "5901234123457", OutputFormat.Svg, new BarcodeOptions { ModuleSize = 2, QuietZone = 10, HeightModules = 36 }).GetText()), "ean.png", "EAN-13"));
            figs["quickstart"] = new Figure("row", items);
        }

        // Documentation map cards: one code per topic.
        var cardMap = new Dictionary<string, object>();
        cards = cardMap;
        void Card(string key, Asset a) => cardMap[key] = new { src = a.Src, w = a.W, h = a.H };
        Card("installation", SaveSvg("docs/card-installation.svg", QR.Render("https://www.nuget.org/packages/CodeGlyphX", OutputFormat.Svg, new QrRenderOptions { ModuleSize = 6, QuietZone = 2 }).GetText()));
        Card("quickstart", Specimen2("/visuals/docs/quickstart-hello.svg"));
        Card("migration-3", SaveSvg("docs/card-migration-3.svg", DataMatrixCode.Render("CODEGLYPHX 3.0", OutputFormat.Svg, options: plain).GetText()));
        Card("qr", Specimen("qrcode"));
        Card("microqr", Specimen("microqrcode"));
        Card("datamatrix", Specimen("datamatrix"));
        Card("pdf417", Specimen("pdf417"));
        Card("aztec", Specimen("aztec"));
        Card("code128", Specimen("code128"));
        Card("code39", Specimen("code39"));
        Card("ean-upc", Specimen("ean"));
        Card("barcode-profiles", SaveSvg("docs/card-barcode-profiles.svg",
            BarcodeProfiles.Render(LinearBarcodeProfile.Isbn, "9781565812314", OutputFormat.Svg).GetText()));
        Card("other-1d", Specimen("royalmail4state"));
        Card("gs1", File.Exists(Path.Combine(staticRoot, "visuals", "docs", "gs1-datamatrix.svg")) ? Specimen2("/visuals/docs/gs1-datamatrix.svg") : Specimen("gs1code128"));
        Card("payloads", Specimen("payload-wifi"));
        Card("decoding", new Asset(figs["decoding"].Items[0].Src, figs["decoding"].Items[0].W, figs["decoding"].Items[0].H));
        Card("renderers", Specimen2("/visuals/docs/formats-svg.svg"));
        Card("styling", Specimen2(figs["qr-eyes"].Items.First(i => i.Label == "Target").Src));
        Card("symbol-capabilities", Specimen("hanxin"));
        return figs;
    }

    static Asset Specimen2(string sitePath) {
        var path = Path.Combine(staticRoot, sitePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (!path.EndsWith(".svg", StringComparison.Ordinal)) {
            ImageReader.DecodeRgba32(File.ReadAllBytes(path), out var pw, out var ph);
            return new Asset(sitePath, pw, ph);
        }
        var (w, h) = SvgSize(File.ReadAllText(path));
        return new Asset(sitePath, w, h);
    }

    /// <summary>States whether CodeGlyphX decodes the styled PNG back to its payload.</summary>
    static string ReadsBack(string payload, QrRenderOptions options) {
        var png = QR.Render(payload, OutputFormat.Png, options).ToArray();
        var ok = QrImageDecoder.TryDecodeImage(png, out var d) && d.Text == payload;
        return ok ? "decodes" : "does not decode";
    }

    /// <summary>
    /// Three inputs, each passed to SymbolScanner.Scan with the default formats: QR artwork from the home page, and
    /// a Data Matrix and a Code 128 label rendered here. The figure lists what the scanner returned for each.
    /// </summary>
    static Figure BuildScanFigure() {
        var inputs = new List<(string Src, byte[] Bytes, string Label)>();
        var art = Path.Combine(staticRoot, "images", "art", "qr-expressive-waves.png");
        inputs.Add(("/images/art/qr-expressive-waves.png", File.ReadAllBytes(art), "qr-expressive-waves.png"));
        var dm = DataMatrixCode.Render("LOT-2026-0042", OutputFormat.Png, options: new MatrixOptions { ModuleSize = 8, QuietZone = 3 }, outputOptions: PngOut).ToArray();
        inputs.Add((SavePng("docs/scan-datamatrix.png", dm).Src, dm, "label-lot.png"));
        var bc = Barcode.Render(SymbolFormat.Code128, "CODEGLYPHX-2026", OutputFormat.Png, new BarcodeOptions { ModuleSize = 2, QuietZone = 12, HeightModules = 40 }, PngOut).ToArray();
        inputs.Add((SavePng("docs/scan-code128.png", bc).Src, bc, "carton.png"));

        var items = new List<FigItem>();
        foreach (var input in inputs) {
            var scan = SymbolScanner.Scan(input.Bytes, ScanOptions.Balanced(6000));
            var first = scan.IsSuccess && scan.Symbols.Count > 0 ? scan.Symbols[0] : null;
            ImageReader.DecodeRgba32(input.Bytes, out var w, out var h);
            Console.WriteLine($"Scan {input.Label}: {(first is null ? "nothing" : first.Format + " " + first.Text)}");
            items.Add(new FigItem(input.Src, w, h, input.Label, first?.Format.ToString(), first?.Text));
        }
        return new Figure("scan", items);
    }
    /// <summary>One QR written to every output format; the figure shows three and lists the size of each file.</summary>
    static Figure BuildFormatsFigure() {
        const string payload = "https://codeglyphx.com";
        var o = new QrRenderOptions { ModuleSize = 6, QuietZone = 2 };
        var sizes = new List<object>();
        foreach (var f in new[] { OutputFormat.Svg, OutputFormat.Svgz, OutputFormat.Png, OutputFormat.Webp, OutputFormat.Gif, OutputFormat.Bmp, OutputFormat.Jpeg, OutputFormat.Tiff, OutputFormat.Pdf, OutputFormat.Eps, OutputFormat.Html, OutputFormat.Ascii }) {
            try {
                var r = QR.Render(payload, f, o);
                sizes.Add(new { format = f.ToString().ToUpperInvariant(), bytes = r.Data.Length, kb = Kb(r.Data.Length), mime = r.MimeType });
            } catch (Exception ex) {
                Console.WriteLine($"Format {f} skipped: {ex.Message}");
            }
        }
        var svg = SaveSvg("docs/formats-svg.svg", QR.Render(payload, OutputFormat.Svg, o).GetText());
        var png = SavePng("docs/formats-png.png", QR.Render(payload, OutputFormat.Png, o).ToArray());
        var ascii = QR.Render(payload, OutputFormat.Ascii, new QrRenderOptions { QuietZone = 1 }).GetText();
        var items = new List<FigItem> {
            Item(svg, "SVG", "vector paths"),
            Item(png, "PNG", "raster, 1 px = 1/6 module"),
            Item(SaveSvg("docs/formats-ascii.svg", AsciiSvg(ascii)), "ASCII", "text for consoles and logs, set in a monospaced font"),
        };
        return new Figure("formats", items, new { payload, sizes });
    }
}
