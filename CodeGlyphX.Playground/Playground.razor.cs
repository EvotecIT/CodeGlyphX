using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CodeGlyphX;
using CodeGlyphX.Aztec;
using CodeGlyphX.DataMatrix;
using CodeGlyphX.Pdf417;
using CodeGlyphX.Payloads;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace CodeGlyphX.Playground;

public partial class Playground {
    // Category selection
    internal string SelectedCategory { get; set; } = "QR";
    internal string SelectedMode { get; set; } = "Generate";

    // QR Code options
    internal string Content { get; set; } = "https://github.com/EvotecIT/CodeGlyphX";
    internal string ErrorCorrection { get; set; } = "M";

    // QR Styling
    internal string ModuleShape { get; set; } = "Square";
    internal int CornerRadius { get; set; } = 3;
    internal string ForegroundColor { get; set; } = "#000000";
    internal string BackgroundColor { get; set; } = "#FFFFFF";
    internal bool CustomEyes { get; set; } = false;
    internal string EyeOuterShape { get; set; } = "Square";
    internal string EyeInnerShape { get; set; } = "Square";
    internal string EyeOuterColor { get; set; } = "#8b5cf6";
    internal string EyeInnerColor { get; set; } = "#06b6d4";

    // Special QR Payloads
    internal string SpecialPayloadType { get; set; } = "WiFi";

    // WiFi
    internal string WifiSsid { get; set; } = "MyNetwork";
    internal string WifiPassword { get; set; } = "Password123";
    internal string WifiSecurity { get; set; } = "WPA";
    internal bool WifiHidden { get; set; } = false;

    // vCard
    internal string VCardFirstName { get; set; } = "John";
    internal string VCardLastName { get; set; } = "Doe";
    internal string VCardEmail { get; set; } = "john@example.com";
    internal string VCardPhone { get; set; } = "+1234567890";
    internal string VCardOrganization { get; set; } = "";
    internal string VCardWebsite { get; set; } = "";

    // Email
    internal string EmailAddress { get; set; } = "contact@example.com";
    internal string EmailSubject { get; set; } = "";
    internal string EmailBody { get; set; } = "";

    // Phone
    internal string PhoneNumber { get; set; } = "+1234567890";

    // SMS
    internal string SmsNumber { get; set; } = "+1234567890";
    internal string SmsMessage { get; set; } = "Hello!";

    // URL
    internal string UrlContent { get; set; } = "https://evotec.xyz";

    // OTP
    internal string OtpSecret { get; set; } = "JBSWY3DPEHPK3PXP";
    internal string OtpLabel { get; set; } = "user@example.com";
    internal string OtpIssuer { get; set; } = "MyApp";
    internal string OtpType { get; set; } = "TOTP";

    // Girocode
    internal string GirocodeIban { get; set; } = "DE89370400440532013000";
    internal string GirocodeBic { get; set; } = "COBADEFFXXX";
    internal string GirocodeRecipient { get; set; } = "Acme Corp";
    internal decimal GirocodeAmount { get; set; } = 99.99m;
    internal string GirocodeReference { get; set; } = "Invoice-2024-001";

    // Barcode options
    internal string SelectedBarcodeType { get; set; } = "Code128";
    internal string BarcodeContent { get; set; } = "PRODUCT-12345";
    internal string BarcodeHint { get; set; } = "";

    // Matrix code options
    internal string SelectedMatrixType { get; set; } = "DataMatrix";
    internal string MatrixContent { get; set; } = "Serial: ABC123-XYZ789";
    internal string SelectedDataMatrixMode { get; set; } = "Auto";
    internal int Pdf417EccLevel { get; set; } = -1;
    internal bool Pdf417Compact { get; set; } = false;
    internal bool AztecAutoEcc { get; set; } = true;
    internal int AztecEccPercent { get; set; } = 23;
    internal int AztecLayers { get; set; } = 0;

    // Component keys for forcing re-render
    internal int _exampleKey = 0;

    // Output
    internal string? ImageDataUri { get; set; }
    internal string? SvgDataUri { get; set; }
    internal string? ErrorMessage { get; set; }
    internal string? DecodeImageDataUri { get; set; }
    internal string? DecodeError { get; set; }
    internal readonly List<DecodeResult> DecodeResults = new();
    internal bool DecodeQr { get; set; } = true;
    internal bool DecodeBarcode { get; set; } = true;
    internal bool DecodeMatrix { get; set; } = true;
    internal bool DecodeDownscale { get; set; } = true;
    internal bool DecodeStopAfterFirst { get; set; } = true;
    internal int DecodeMaxDimension { get; set; } = 1024;
    internal int DecodeMaxMilliseconds { get; set; } = 600;
    internal bool IsDecoding { get; set; }
    internal string DecodeStatus { get; set; } = string.Empty;
    internal CancellationTokenSource? _decodeCts;
    internal bool IsDropActive { get; set; } = false;

    protected override void OnInitialized()
    {
        GenerateCode();
    }

    internal void OnCategoryChanged()
    {
        ResetOutputs();
        if (SelectedMode == "Generate")
        {
            GenerateCode();
        }
        _exampleKey++;
        StateHasChanged();
    }

    internal void OnSpecialPayloadTypeChanged()
    {
        ResetOutputs();
        if (SelectedMode == "Generate")
        {
            GenerateCode();
        }
        _exampleKey++;
        StateHasChanged();
    }

    internal void OnBarcodeTypeChanged()
    {
        ResetOutputs();

        // Update hint and content based on barcode type
        switch (SelectedBarcodeType)
        {
            case "Code39":
                BarcodeHint = "Only uppercase A-Z, digits 0-9, and -. $/+% space allowed";
                BarcodeContent = "HELLO-123";
                break;
            case "Code93":
                BarcodeHint = "Uppercase A-Z, digits 0-9, and -. $/+% space (full ASCII requires options)";
                BarcodeContent = "HELLO-123";
                break;
            case "Code11":
                BarcodeHint = "Digits 0-9 and '-' only";
                BarcodeContent = "123-45";
                break;
            case "Codabar":
                BarcodeHint = "Digits and - $ . / : +, with optional start/stop A-D";
                BarcodeContent = "A123456B";
                break;
            case "MSI":
                BarcodeHint = "Digits only";
                BarcodeContent = "1234567";
                break;
            case "Plessey":
                BarcodeHint = "Hex digits only (0-9, A-F)";
                BarcodeContent = "A1B2C3";
                break;
            case "EAN":
                BarcodeHint = "Enter 8 or 13 digits";
                BarcodeContent = "5901234123457";
                break;
            case "UPCA":
                BarcodeHint = "Enter 12 digits";
                BarcodeContent = "036000291452";
                break;
            case "UPCE":
                BarcodeHint = "Enter 6-8 digits";
                BarcodeContent = "042526";
                break;
            case "ITF14":
                BarcodeHint = "Enter 14 digits";
                BarcodeContent = "10012345678902";
                break;
            case "ITF":
                BarcodeHint = "Even number of digits only";
                BarcodeContent = "123456";
                break;
            case "Telepen":
                BarcodeHint = "Full ASCII support";
                BarcodeContent = "Hello-123";
                break;
            case "Code32":
                BarcodeHint = "9 digits for Italian pharmaceutical codes";
                BarcodeContent = "123456789";
                break;
            case "GS1128":
                BarcodeHint = "GS1 AI format, e.g. (01)09506000134352(10)ABC123";
                BarcodeContent = "(01)09506000134352(10)ABC123";
                break;
            default:
                BarcodeHint = "Full ASCII (A-Z, a-z, 0-9, symbols)";
                BarcodeContent = "PRODUCT-12345";
                break;
        }

        if (SelectedMode == "Generate")
        {
            GenerateCode();
        }

        _exampleKey++;
        StateHasChanged();
    }

    internal void OnModeChanged()
    {
        ResetOutputs();
        if (SelectedMode == "Generate")
        {
            GenerateCode();
        }
        _exampleKey++;
        StateHasChanged();
    }

    internal void ApplyPreset(string preset)
    {
        SelectedMode = "Generate";

        switch (preset)
        {
            case "QrUrl":
                SelectedCategory = "QR";
                Content = "https://evotec.xyz";
                ErrorCorrection = "M";
                break;
            case "QrWifi":
                SelectedCategory = "SpecialQR";
                SpecialPayloadType = "WiFi";
                WifiSsid = "Office-WiFi";
                WifiPassword = "Password123";
                WifiSecurity = "WPA";
                WifiHidden = false;
                break;
            case "QrOtp":
                SelectedCategory = "SpecialQR";
                SpecialPayloadType = "OTP";
                OtpType = "TOTP";
                OtpLabel = "user@evotec.xyz";
                OtpIssuer = "CodeGlyphX";
                OtpSecret = "JBSWY3DPEHPK3PXP";
                break;
            case "BarcodeEan":
                SelectedCategory = "Barcode";
                SelectedBarcodeType = "EAN";
                BarcodeContent = "5901234123457";
                OnBarcodeTypeChanged();
                break;
            case "BarcodeCode128":
                SelectedCategory = "Barcode";
                SelectedBarcodeType = "Code128";
                BarcodeContent = "CODE128-1234";
                OnBarcodeTypeChanged();
                break;
            case "MatrixPdf417":
                SelectedCategory = "Matrix";
                SelectedMatrixType = "Pdf417";
                MatrixContent = "Document ID: 98765";
                Pdf417EccLevel = -1;
                Pdf417Compact = false;
                break;
            case "MatrixDataMatrix":
                SelectedCategory = "Matrix";
                SelectedMatrixType = "DataMatrix";
                MatrixContent = "Serial: ABC123-XYZ789";
                SelectedDataMatrixMode = "Auto";
                break;
            case "MatrixAztec":
                SelectedCategory = "Matrix";
                SelectedMatrixType = "Aztec";
                MatrixContent = "Ticket: CONF-2024";
                AztecAutoEcc = true;
                AztecLayers = 0;
                break;
        }

        GenerateCode();
        _exampleKey++;
        StateHasChanged();
    }

    internal bool IsValidCode39Content(string content)
    {
        if (string.IsNullOrEmpty(content)) return false;
        foreach (char c in content.ToUpperInvariant())
        {
            if (!char.IsLetterOrDigit(c) && c != '-' && c != '.' && c != ' ' && c != '$' && c != '/' && c != '+' && c != '%')
                return false;
            if (char.IsLetter(c) && char.IsLower(c))
                return false;
        }
        return true;
    }

    internal static bool IsValidCode11Content(string content)
    {
        if (string.IsNullOrEmpty(content)) return false;
        foreach (var ch in content)
        {
            if (!char.IsDigit(ch) && ch != '-') return false;
        }
        return true;
    }

    internal static bool IsValidHexContent(string content)
    {
        if (string.IsNullOrEmpty(content)) return false;
        foreach (var ch in content)
        {
            if (!(ch >= '0' && ch <= '9') && !(ch >= 'A' && ch <= 'F') && !(ch >= 'a' && ch <= 'f'))
                return false;
        }
        return true;
    }

    internal static string NormalizeDigits(string content)
        => new string(content.Where(char.IsDigit).ToArray());

}
