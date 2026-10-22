using CodeGlyphX.Printing;
using CodeGlyphX.Rendering;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using CodeGlyphX.DataMatrix;
using CodeGlyphX.Pdf417;

namespace CodeGlyphX.Playground;

public partial class BarcodePrintStudio : ComponentBase {
    private static readonly SymbolFormat[] HibcCarriers = { SymbolFormat.Code39, SymbolFormat.Code128, SymbolFormat.QrCode, SymbolFormat.Aztec,
        SymbolFormat.DataMatrix, SymbolFormat.Pdf417, SymbolFormat.MicroPdf417, SymbolFormat.CodablockF };
    private SymbolFormat HibcCarrier { get; set; } = SymbolFormat.Code39;
    private bool HibcChecked { get; set; }
    private int? DotWidth { get; set; }
    private int? DotHeight { get; set; }
    private int? HibcQrVersion { get; set; }
    private QrErrorCorrectionLevel HibcQrEc { get; set; } = QrErrorCorrectionLevel.M;
    private int? HibcColumns { get; set; }
    private bool HibcRectangle { get; set; }
    private string SelectedFormat { get; set; } = "Ean";
    private string Content { get; set; } = "5901234123457";
    private string LinearContent { get; set; } = "(01)09506000134352";
    private CodeOneVersion CodeOneSize { get; set; }
    private UltracodeErrorCorrectionLevel UltraEc { get; set; } = UltracodeErrorCorrectionLevel.EC2;
    private Gs1CompositeCarrier Carrier { get; set; }
    private Gs1CompositeComponent Component { get; set; } = Gs1CompositeComponent.CcA;
    private int Columns { get; set; } = 4;
    private string CheckMode { get; set; } = "";
    private bool PrintChecks { get; set; } = true;
    private bool FullAscii { get; set; }
    private MsiChecksumType MsiCheck { get; set; } = MsiChecksumType.Mod10;
    private bool IncludeText { get; set; }
    private string ReadableText { get; set; } = "";
    private string Caption { get; set; } = "";
    private int Dpi { get; set; } = 300;
    private double FontPoints { get; set; } = 8;
    private double? XDimension { get; set; }
    private double Rotation { get; set; }
    private string FontChoice { get; set; } = "None";
    private BarcodeFont? UploadedFont { get; set; }
    private BarcodeFont? Font => FontChoice switch {
        "OcrA" => BarcodeFont.OcrA, "OcrB" => BarcodeFont.OcrB, "Uploaded" => UploadedFont, _ => null
    };
    private string? FontName => Font?.Name;
    private bool Busy { get; set; }
    private string? Error { get; set; }
    private BarcodePrintLayout? Layout { get; set; }
    private IReadOnlyList<BarcodePrintFinding> Findings { get; set; } = Array.Empty<BarcodePrintFinding>();
    private string? PngUri { get; set; }
    private string? SvgUri { get; set; }
    private string? PdfUri { get; set; }
    private string Dimensions { get; set; } = "";
    private bool SupportsOptionalCheck => SelectedFormat is "Code39" or "Code11" or "Codabar" or "Msi" or "Plessey" or "Itf" or "Bc412"
        or "Industrial2Of5" or "Matrix2Of5" or "Iata2Of5" or "Coop2Of5" or "Datalogic2Of5";

    private void ClearOutput() {
        Layout = null; Findings = Array.Empty<BarcodePrintFinding>(); PngUri = SvgUri = PdfUri = null; Error = null;
    }

    private void ChangeFont() {
        ClearOutput();
        if (Font is not null) IncludeText = true;
    }

    private void ChangeFormat() {
        ClearOutput(); CheckMode = ""; FullAscii = false; MsiCheck = MsiChecksumType.Mod10;
        ReadableText = ""; Caption = SelectedFormat == "Package" ? "USPS TRACKING #" : "";
        Content = SelectedFormat switch {
            "Profile:Isbn" => "978-0-306-40615-7", "Profile:Ismn" => "979-0-2306-7118-7", "Profile:Issn" => "0317-8471 12",
            "Profile:Ean14" => "0950600013435", "Profile:Sscc18" => "00340123450000000", "Profile:Pzn7" => "123456", "Profile:Pzn8" => "1234567",
            "Profile:Identcode" => "01234567890", "Profile:Leitcode" => "0123456789012", "Profile:MarksAndSpencer" => "0642118",
            "Hibc" => "A999BJC5D6E71", "Package" => "(420)12345(92)12391234567812345670", "Coupon" => "(8110)012345612345611110123",
            "Ean" => "5901234123457", "UpcA" => "036000291452", "UpcE" => "042526", "Itf14" => "10012345678902",
            "Gs1Composite" or "CompositeComponent" => "(10)ABC123", "Gs1Code128" => "(01)09506000134352(10)ABC123",
            "AztecRune" => "42", "Raw" => "11223344", "CodeOne" => "Code One label",
            "Msi" or "Plessey" or "Code11" or "Itf" or "ChannelCode" => "123456",
            _ => "HELLO-123"
        };
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "S5693:Make sure that this file upload is safe", Justification = "This browser-local font stream has a fixed 16 MiB limit and the managed parser independently bounds font tables, shaping work and outlines; it is not an HTTP request to a server.")]
    private async Task LoadFont(InputFileChangeEventArgs args) {
        ClearOutput(); UploadedFont = null; FontChoice = "None"; Busy = true;
        try {
            using var input = args.File.OpenReadStream(16 * 1024 * 1024);
            using var bytes = new MemoryStream();
            await input.CopyToAsync(bytes);
            UploadedFont = BarcodeFont.FromBytes(bytes.ToArray());
            FontChoice = "Uploaded";
            IncludeText = true;
        } catch (Exception exception) when (exception is ArgumentException or IOException or InvalidOperationException) {
            Error = "The font could not be loaded. Choose an uncompressed TTF, OTF or TTC file with supported outlines, up to 16 MiB.";
        } finally { Busy = false; }
    }

    private void Generate() {
        if (Busy) return;
        ClearOutput();
        try {
            if (Content.Length > 2048 || LinearContent.Length > 2048 || ReadableText.Length > 2048 || Caption.Length > 2048)
                throw new ArgumentException("The print studio accepts at most 2048 characters per field.");
            var options = new BarcodePrintOptions {
                Dpi = Dpi, Font = Font, FontSizePoints = FontPoints, CaptionSizePoints = FontPoints,
                MinimumCaptionSizePoints = Math.Min(9, FontPoints),
                IncludeHumanReadable = IncludeText, HumanReadableText = string.IsNullOrEmpty(ReadableText) ? null : ReadableText,
                Caption = string.IsNullOrEmpty(Caption) ? null : Caption, XDimensionMm = XDimension
            };
            var layout = BuildLayout(options);
            var placement = new BarcodePrintPlacement { RotationDegrees = Rotation };
            Findings = layout.Preflight(placement, OutputFormat.Pdf).Concat(layout.Preflight(placement, OutputFormat.Png))
                .GroupBy(finding => finding.Code).Select(group => group.First()).ToArray();
            Layout = layout;
            var bounds = placement.Measure(layout.Bounds);
            Dimensions = $"{bounds.Width:0.##} × {bounds.Height:0.##} mm";
            if (Findings.Any(finding => finding.IsError)) return;
            PngUri = DataUri(layout.Render(OutputFormat.Png, placement));
            SvgUri = DataUri(layout.Render(OutputFormat.Svg, placement));
            PdfUri = DataUri(layout.Render(OutputFormat.Pdf, placement));
        } catch (Exception exception) when (exception is ArgumentException or FormatException or InvalidOperationException or NotSupportedException or OverflowException) {
            PngUri = SvgUri = PdfUri = null;
            Error = exception.Message;
        }
    }

    private BarcodePrintLayout BuildLayout(BarcodePrintOptions options) {
        if (SelectedFormat.StartsWith("Profile:", StringComparison.Ordinal))
            return BarcodePrint.Layout(Enum.Parse<LinearBarcodeProfile>(SelectedFormat.Substring(8)), Content, options);
        if (SelectedFormat == "Hibc") {
            var encoding = new HibcEncodingOptions { InputMode = HibcChecked ? HibcInputMode.CheckedPayload : HibcInputMode.Data, ValidateStructure = true };
            if (HibcCarrier == SymbolFormat.QrCode) encoding.Qr = new QrEncodingOptions { MinVersion = HibcQrVersion ?? 1, MaxVersion = HibcQrVersion ?? 40, ErrorCorrectionLevel = HibcQrEc };
            if (HibcCarrier == SymbolFormat.DataMatrix) encoding.DataMatrix = new DataMatrixEncodingOptions { Shape = HibcRectangle ? DataMatrixShape.Rectangular : DataMatrixShape.Square };
            if (HibcCarrier == SymbolFormat.Pdf417 && HibcColumns.HasValue) encoding.Pdf417 = new Pdf417EncodeOptions { MinColumns = HibcColumns.Value, MaxColumns = HibcColumns.Value };
            if (HibcCarrier == SymbolFormat.MicroPdf417) encoding.MicroPdf417 = new MicroPdf417EncodeOptions { Columns = HibcColumns };
            if (HibcCarrier == SymbolFormat.CodablockF) encoding.CodablockFColumns = HibcColumns;
            return BarcodePrint.LayoutHibc(HibcCarrier, Content, encoding, options);
        }
        if (SelectedFormat == "Package") {
            if (!IntelligentMailPackage.TryParse(Gs1.ElementString(Content), out var package)) throw new ArgumentException("Enter a complete checked USPS package identifier, with optional (420) routing ZIP.");
            return BarcodePrint.Layout(package, string.IsNullOrWhiteSpace(Caption) ? "USPS TRACKING #" : Caption, options);
        }
        if (SelectedFormat == "Coupon") {
            if (!NorthAmericanCoupon.TryParse(Content, out var coupon)) throw new ArgumentException("Enter one complete checked (8110) coupon element.");
            return BarcodePrint.Layout(coupon, options: options);
        }
        if (SelectedFormat == "Raw") return BarcodePrint.LayoutRaw(RawLinear.Encode(Content), options);
        if (SelectedFormat == "CompositeComponent") return BarcodePrint.Layout(Gs1CompositeComponents.Encode(Content, Component, Columns), options);
        var format = Enum.Parse<SymbolFormat>(SelectedFormat);
        if (format == SymbolFormat.Gs1Composite) return BarcodePrint.Layout(Gs1CompositeEncoder.Encode(LinearContent, Content,
            new Gs1CompositeEncodingOptions { Carrier = Carrier, Component = Component }), options);
        if (format == SymbolFormat.Ultracode) return BarcodePrint.Layout(UltracodeEncoder.EncodeText(Content, UltraEc), options);
        if (format == SymbolFormat.CodeOne) return BarcodePrint.Layout(CodeOneEncoder.EncodeText(Content, new CodeOneEncodingOptions { Version = CodeOneSize }), options);
        if (format == SymbolFormat.DotCode) return BarcodePrint.Layout(DotCodeCode.Encode(Content, new DotCodeEncodingOptions { Width = DotWidth, Height = DotHeight }), options);
        if (format == SymbolFormat.QrCode) return BarcodePrint.Layout(QR.Encode(Content), options);
        if (format == SymbolFormat.MaxiCode) return BarcodePrint.Layout(MaxiCodeCode.Encode(Content), options);
        if (format == SymbolFormat.D3Aqr) return BarcodePrint.Layout(D3Aqr.D3AqrEncoder.EncodeText(Content, 2842), options);
        if (SymbolCapabilities.Get(format).Family == SymbolFamily.Linear) {
            var encoding = new BarcodeEncodingOptions {
                CheckMode = string.IsNullOrEmpty(CheckMode) ? null : Enum.Parse<BarcodeCheckMode>(CheckMode),
                PrintCheckCharacters = PrintChecks, Code39FullAscii = FullAscii, MsiChecksum = format == SymbolFormat.Msi ? MsiCheck : MsiChecksumType.Mod10
            };
            return BarcodePrint.Layout(Barcode.Encode(format, Content, encoding), options);
        }
        return BarcodePrint.Layout(MatrixBarcodeEncoder.EncodeSymbol(format, Content), options);
    }

    private static string DataUri(RenderedOutput output) => $"data:{output.MimeType};base64,{Convert.ToBase64String(output.ToArray())}";
}
