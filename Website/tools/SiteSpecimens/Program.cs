// Renders the codeglyphx.com specimen sheet and payload codes, and decodes the hero artwork, with CodeGlyphX itself.
// Usage: dotnet run --project Website/tools/SiteSpecimens -c Release -- <Website directory>
using System.Text;
using System.Text.Json;
using CodeGlyphX;
using CodeGlyphX.Payloads;
using CodeGlyphX.Website.Specimens;
using CodeGlyphX.Rendering;

var website = Path.GetFullPath(args.Length > 0 ? args[0] : "Website");
var output = Path.Combine(website, "static", "specimens");
Directory.CreateDirectory(output);
var utf8 = new UTF8Encoding(false);

// One valid sample per format supported by the generic renderers. MaxiCode keeps its specialized hexagonal geometry and has no generic SVG renderer.
var samples = new (SymbolFormat Format, string Name, string Family, string Value)[] {
    (SymbolFormat.QrCode, "QR Code", "2D", "https://codeglyphx.com"),
    (SymbolFormat.MicroQrCode, "Micro QR", "2D", "CGX2026"),
    (SymbolFormat.RmQrCode, "rMQR", "2D", "CODEGLYPHX"),
    (SymbolFormat.DataMatrix, "Data Matrix", "2D", "LOT-2026-0042"),
    (SymbolFormat.Aztec, "Aztec", "2D", "TICKET-2026-0042"),
    (SymbolFormat.Pdf417, "PDF417", "2D", "DOCUMENT-2026-0042"),
    (SymbolFormat.MicroPdf417, "MicroPDF417", "2D", "INV-0042"),
    (SymbolFormat.DotCode, "DotCode", "2D", "CGX-0042"),
    (SymbolFormat.HanXin, "Han Xin", "2D", "CodeGlyphX"),
    (SymbolFormat.Code128, "Code 128", "Linear", "CODEGLYPHX-2026"),
    (SymbolFormat.Gs1Code128, "GS1-128", "Linear", "(01)09501101530003"),
    (SymbolFormat.Code39, "Code 39", "Linear", "CGX-2026"),
    (SymbolFormat.Code93, "Code 93", "Linear", "CGX-2026"),
    (SymbolFormat.Code11, "Code 11", "Linear", "123-45"),
    (SymbolFormat.Codabar, "Codabar", "Linear", "A40156B"),
    (SymbolFormat.Msi, "MSI", "Linear", "1234567"),
    (SymbolFormat.Plessey, "Plessey", "Linear", "1234ABCD"),
    (SymbolFormat.Telepen, "Telepen", "Linear", "CGX"),
    (SymbolFormat.Code32, "Code 32", "Linear", "12345678"),
    (SymbolFormat.Pharmacode, "Pharmacode", "Linear", "1234"),
    (SymbolFormat.Ean, "EAN-13", "Retail", "5901234123457"),
    (SymbolFormat.UpcA, "UPC-A", "Retail", "036000291452"),
    (SymbolFormat.UpcE, "UPC-E", "Retail", "123456"),
    (SymbolFormat.Itf14, "ITF-14", "Retail", "15400141288763"),
    (SymbolFormat.Itf, "Interleaved 2 of 5", "Linear", "12345678"),
    (SymbolFormat.Industrial2Of5, "Industrial 2 of 5", "Linear", "12345"),
    (SymbolFormat.Matrix2Of5, "Matrix 2 of 5", "Linear", "12345"),
    (SymbolFormat.Iata2Of5, "IATA 2 of 5", "Linear", "12345"),
    (SymbolFormat.Gs1DataBarOmnidirectional, "GS1 DataBar", "GS1", "0950110153000"),
    (SymbolFormat.Gs1DataBarLimited, "GS1 DataBar Limited", "GS1", "(01)09501101530003"),
    (SymbolFormat.Gs1DataBarExpanded, "GS1 DataBar Expanded", "GS1", "(01)09501101530003(17)261231"),
    (SymbolFormat.Gs1DataBarStacked, "GS1 DataBar Stacked", "GS1", "0950110153000"),
    (SymbolFormat.Postnet, "POSTNET", "Postal", "12345"),
    (SymbolFormat.Planet, "PLANET", "Postal", "12345678901"),
    (SymbolFormat.KixCode, "KIX", "Postal", "1231FZ13XHS"),
    (SymbolFormat.RoyalMail4State, "Royal Mail 4-State", "Postal", "LU178XE1A"),
    (SymbolFormat.UspsIntelligentMail, "USPS Intelligent Mail", "Postal", "01234567094987654321"),
};

var rendered = new List<object>();
foreach (var sample in samples) {
    var svg = TryRender(sample.Format, sample.Value, out var error);
    if (string.IsNullOrWhiteSpace(svg))
        throw new InvalidOperationException($"Failed to render required {sample.Name} specimen: {error ?? "renderer returned no SVG artifact"}");

    var file = sample.Format.ToString().ToLowerInvariant() + ".svg";
    File.WriteAllText(Path.Combine(output, file), svg, utf8);
    rendered.Add(new { name = sample.Name, family = sample.Family, value = sample.Value, file = "/specimens/" + file });
}

// Payload helpers rendered as QR codes.
var payloads = new (string Id, string Name, string Code, Func<object> Create)[] {
    ("wifi", "Wi-Fi network", "QrPayloads.Wifi(\"Evotec-Guest\",\n    \"correct-horse-battery\")", () => QrPayloads.Wifi("Evotec-Guest", "correct-horse-battery")),
    ("otp", "Authenticator (TOTP)", "QrPayloads.OneTimePassword(\n    OtpAuthType.Totp,\n    \"JBSWY3DPEHPK3PXP\",\n    label: \"ava@example.com\",\n    issuer: \"CodeGlyphX\")",
        () => QrPayloads.OneTimePassword(OtpAuthType.Totp, "JBSWY3DPEHPK3PXP", label: "ava@example.com", issuer: "CodeGlyphX")),
    ("vcard", "Contact card", "QrPayload.VCard(\n    firstName: \"Ava\",\n    lastName: \"Stone\",\n    phone: \"+14155550198\",\n    email: \"ava@example.com\")",
        () => QrPayload.VCard(firstName: "Ava", lastName: "Stone", phone: "+14155550198", email: "ava@example.com")),
    ("girocode", "SEPA payment", "QrPayloads.Girocode(\n    iban: \"DE89370400440532013000\",\n    bic: \"COBADEFFXXX\",\n    name: \"Evotec Services\",\n    amount: 99.99m)",
        () => QrPayloads.Girocode(iban: "DE89370400440532013000", bic: "COBADEFFXXX", name: "Evotec Services", amount: 99.99m)),
};
var payloadResults = new List<object>();
foreach (var payload in payloads) {
    var path = Path.Combine(output, "payload-" + payload.Id + ".svg");
    switch (payload.Create()) {
        case QrPayloadData data: QR.Save(data, path); break;
        case string text: QR.Save(text, path); break;
        default: throw new InvalidOperationException("Unexpected payload type for " + payload.Id);
    }
    payloadResults.Add(new { id = payload.Id, name = payload.Name, code = payload.Code, file = "/specimens/payload-" + payload.Id + ".svg" });
}

// Decode the hero artwork so the page only claims what CodeGlyphX actually reads back.
var decoded = new List<object>();
var artRoot = Path.Combine(website, "static", "images", "art");
if (Directory.Exists(artRoot)) {
    foreach (var image in Directory.GetFiles(artRoot, "*.png").OrderBy(p => p, StringComparer.Ordinal)) {
        var ok = QrImageDecoder.TryDecodeImage(File.ReadAllBytes(image), QrPixelDecodeOptions.Screen(budgetMilliseconds: 4000, maxDimension: 1600), out var result);
        decoded.Add(new { file = "/images/art/" + Path.GetFileName(image), decoded = ok, text = ok ? result.Text : null });
        Console.WriteLine($"{Path.GetFileName(image)}: {(ok ? result.Text : "not decoded")}");
    }
}

var json = new JsonSerializerOptions { WriteIndented = true };
File.WriteAllText(Path.Combine(website, "data", "specimens.json"),
    JsonSerializer.Serialize(new { generatedBy = "Website/tools/SiteSpecimens", count = rendered.Count, specimens = rendered, payloads = payloadResults, decoded }, json) + "\n", utf8);
Console.WriteLine($"Rendered {rendered.Count} specimens and {payloadResults.Count} payloads.");

// Inner pages: navigation thumbnails, docs figures, benchmark charts and product-page codes (Visuals*.cs).
SiteVisuals.Run(website);

static string? TryRender(SymbolFormat format, string value, out string? error) {
    error = null;
    try {
        return format == SymbolFormat.QrCode
            ? QR.Render(value, OutputFormat.Svg).GetText()
            : Barcode.Render(format, value, OutputFormat.Svg).GetText();
    } catch (Exception linear) {
        try {
            return MatrixBarcode.Render(format, value, OutputFormat.Svg).GetText();
        } catch (Exception matrix) {
            error = linear.GetType().Name + " / " + matrix.Message;
            return null;
        }
    }
}
