using System;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CodeGlyphX;
using CodeGlyphX.Aztec;
using CodeGlyphX.DataMatrix;
using CodeGlyphX.Pdf417;
using CodeGlyphX.Payloads;
using CodeGlyphX.Rendering;
using BackgroundPatternType = CodeGlyphX.Rendering.QrBackgroundPatternType;
using CodeGlyphX.Rendering.Png;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace CodeGlyphX.Playground;

public partial class Playground {
    internal QrPayloadData? GetSpecialPayloadData()
    {
        return SpecialPayloadType switch
        {
            "WiFi" => QrPayloads.Wifi(WifiSsid, WifiSecurity == "None" ? "" : WifiPassword, WifiSecurity, WifiHidden),
            "vCard" => QrPayloads.Contact(
                QrContactOutputType.VCard3,
                VCardFirstName,
                VCardLastName,
                phone: VCardPhone,
                email: VCardEmail,
                website: string.IsNullOrEmpty(VCardWebsite) ? null : VCardWebsite,
                org: string.IsNullOrEmpty(VCardOrganization) ? null : VCardOrganization),
            "Email" => QrPayloads.Email(EmailAddress,
                string.IsNullOrEmpty(EmailSubject) ? null : EmailSubject,
                string.IsNullOrEmpty(EmailBody) ? null : EmailBody),
            "Phone" => QrPayloads.Phone(PhoneNumber),
            "SMS" => QrPayloads.Sms(SmsNumber, SmsMessage),
            "URL" => new QrPayloadData(UrlContent),
            "OTP" => QrPayloads.OneTimePassword(
                OtpType == "TOTP" ? OtpAuthType.Totp : OtpAuthType.Hotp,
                OtpSecret, OtpLabel, OtpIssuer),
            "Girocode" => QrPayloads.Girocode(GirocodeIban, GirocodeBic, GirocodeRecipient, GirocodeAmount, GirocodeReference),
            _ => null
        };
    }

    internal Rgba32 ParseColor(string hex)
    {
        hex = hex.TrimStart('#');
        if (hex.Length == 6)
        {
            return new Rgba32(
                Convert.ToByte(hex.Substring(0, 2), 16),
                Convert.ToByte(hex.Substring(2, 2), 16),
                Convert.ToByte(hex.Substring(4, 2), 16)
            );
        }
        return new Rgba32(0, 0, 0);
    }

    internal QrModuleShape ParseModuleShape(string shape)
    {
        return shape switch
        {
            "Dot" => QrModuleShape.Dot,
            "DotGrid" => QrModuleShape.DotGrid,
            "Diamond" => QrModuleShape.Diamond,
            "SoftDiamond" => QrModuleShape.SoftDiamond,
            "Squircle" => QrModuleShape.Squircle,
            "Leaf" => QrModuleShape.Leaf,
            "Wave" => QrModuleShape.Wave,
            "Blob" => QrModuleShape.Blob,
            "Circle" => QrModuleShape.Circle,
            "Rounded" => QrModuleShape.Rounded,
            _ => QrModuleShape.Square
        };
    }

    internal static QrEyeFrameStyle ParseEyeFrameStyle(string style)
    {
        return style switch
        {
            "DoubleRing" => QrEyeFrameStyle.DoubleRing,
            "Target" => QrEyeFrameStyle.Target,
            "Bracket" => QrEyeFrameStyle.Bracket,
            "Badge" => QrEyeFrameStyle.Badge,
            _ => QrEyeFrameStyle.Single
        };
    }

    internal static QrPaletteMode ParsePaletteMode(string mode)
    {
        return mode switch
        {
            "Checker" => QrPaletteMode.Checker,
            "Random" => QrPaletteMode.Random,
            "Rings" => QrPaletteMode.Rings,
            _ => QrPaletteMode.Cycle
        };
    }

    internal static QrModuleScaleMode ParseScaleMapMode(string mode)
    {
        return mode switch
        {
            "Radial" => QrModuleScaleMode.Radial,
            "Random" => QrModuleScaleMode.Random,
            "Checker" => QrModuleScaleMode.Checker,
            _ => QrModuleScaleMode.Rings
        };
    }

    internal static QrGradientType ParseGradientType(string mode)
    {
        return mode switch
        {
            "Vertical" => QrGradientType.Vertical,
            "DiagonalDown" => QrGradientType.DiagonalDown,
            "DiagonalUp" => QrGradientType.DiagonalUp,
            "Radial" => QrGradientType.Radial,
            _ => QrGradientType.Horizontal
        };
    }

    internal static BackgroundPatternType ParsePatternType(string mode)
    {
        return mode switch
        {
            "Grid" => BackgroundPatternType.Grid,
            "Checker" => BackgroundPatternType.Checker,
            "DiagonalStripes" => BackgroundPatternType.DiagonalStripes,
            "Crosshatch" => BackgroundPatternType.Crosshatch,
            _ => BackgroundPatternType.Dots
        };
    }

    internal static Rgba32 ApplyAlpha(Rgba32 color, int alpha)
    {
        if (alpha < 0) alpha = 0;
        if (alpha > 255) alpha = 255;
        return new Rgba32(color.R, color.G, color.B, (byte)alpha);
    }

    internal string GetHeuristicStatus()
    {
        if (HeuristicReport is null) return string.Empty;
        var score = HeuristicReport.Score;
        if (score >= 80) return "Strong";
        if (score >= 60) return "Review";
        return "Weak";
    }

    internal string GetHeuristicColor()
    {
        if (HeuristicReport is null) return "#94a3b8";
        var score = HeuristicReport.Score;
        if (score >= 80) return "#22c55e";
        if (score >= 60) return "#f59e0b";
        return "#ef4444";
    }

    internal static DataMatrixEncodingMode ParseDataMatrixMode(string mode)
    {
        return mode switch
        {
            "Ascii" => DataMatrixEncodingMode.Ascii,
            "C40" => DataMatrixEncodingMode.C40,
            "Text" => DataMatrixEncodingMode.Text,
            "X12" => DataMatrixEncodingMode.X12,
            "Edifact" => DataMatrixEncodingMode.Edifact,
            "Base256" => DataMatrixEncodingMode.Base256,
            _ => DataMatrixEncodingMode.Auto
        };
    }

    internal string GetDownloadFilename(string extension)
    {
        string baseName = SelectedCategory switch
        {
            "QR" => "qrcode",
            "SpecialQR" => $"qr-{SpecialPayloadType.ToLowerInvariant()}",
            "Barcode" => SelectedBarcodeType.ToLowerInvariant(),
            "Matrix" => SelectedMatrixType.ToLowerInvariant(),
            _ => "code"
        };
        return $"{baseName}.{extension}";
    }

    internal string GetBarcodeTypeEnumName()
    {
        return SelectedBarcodeType switch
        {
            "GS1128" => "Gs1Code128",
            "EAN" => "Ean",
            "UPCA" => "UpcA",
            "UPCE" => "UpcE",
            "ITF14" => "Itf14",
            "MSI" => "Msi",
            "Code32" => "Code32",
            "ITF" => "Itf",
            "Telepen" => "Telepen",
            _ => SelectedBarcodeType
        };
    }

    internal string GetCodeExample(CodeLanguage language)
    {
        return language == CodeLanguage.Vb ? GetCodeExampleVb() : GetCodeExampleCSharp();
    }

    private string GetCodeExampleCSharp()
    {
        var nl = "\n";

        if (SelectedMode == "Decode") {
            return "using CodeGlyphX;" + nl + nl
                + "var scan = SymbolScanner.ScanFile(\"image.png\", ScanOptions.Balanced(2000));" + nl
                + "foreach (var symbol in scan.Symbols)" + nl
                + "{" + nl
                + "    Console.WriteLine($\"{symbol.Format}: {symbol.Text}\");" + nl
                + "}" + nl
                + "Console.WriteLine(scan.CompletionReason);";
        }

        if (SelectedCategory == "QR")
        {
            var escapedContent = EscapeString(Content);
            if (ErrorCorrection != "M" || ModuleShape != "Square" || CustomEyes || ForegroundColor != "#000000" || BackgroundColor != "#FFFFFF" || TargetSizePx > 0 || BackgroundSupersample > 1)
            {
                var sb = new System.Text.StringBuilder();
                sb.Append("using CodeGlyphX;").Append(nl).Append(nl);
                sb.Append("using CodeGlyphX.Rendering;").Append(nl);
                sb.Append("using CodeGlyphX.Rendering.Png;").Append(nl).Append(nl);
                sb.Append("var options = new QrRenderOptions").Append(nl);
                sb.Append("{").Append(nl);
                sb.Append("    ArtGuardrailsEnabled = false,").Append(nl);
                var foreground = ParseColor(ForegroundColor);
                var background = ParseColor(BackgroundColor);
                sb.Append("    Foreground = new Rgba32(").Append(foreground.R).Append(", ").Append(foreground.G).Append(", ").Append(foreground.B).Append("),").Append(nl);
                sb.Append("    Background = new Rgba32(").Append(background.R).Append(", ").Append(background.G).Append(", ").Append(background.B).Append("),").Append(nl);
                if (TargetSizePx > 0)
                {
                    sb.Append("    TargetSizePx = ").Append(TargetSizePx).Append(",").Append(nl);
                    if (TargetSizeIncludesQuietZone)
                    {
                        sb.Append("    TargetSizeIncludesQuietZone = true,").Append(nl);
                    }
                }
                if (BackgroundSupersample > 1)
                {
                    sb.Append("    BackgroundSupersample = ").Append(BackgroundSupersample).Append(",").Append(nl);
                }
                if (ModuleShape != "Square")
                {
                    sb.Append("    ModuleShape = QrModuleShape.").Append(ModuleShape).Append(",").Append(nl);
                }
                if (CustomEyes)
                {
                    sb.Append("    Eyes = new QrEyeOptions").Append(nl);
                    sb.Append("    {").Append(nl);
                    sb.Append("        UseFrame = true,").Append(nl);
                    sb.Append("        OuterShape = QrModuleShape.").Append(EyeOuterShape).Append(",").Append(nl);
                    sb.Append("        InnerShape = QrModuleShape.").Append(EyeInnerShape).Append(nl);
                    sb.Append("    },").Append(nl);
                }
                sb.Append("};").Append(nl).Append(nl);
                sb.Append("QR.Save(\"").Append(escapedContent).Append("\", \"qrcode.png\", options, new QrEncodingOptions { ErrorCorrectionLevel = QrErrorCorrectionLevel.").Append(ErrorCorrection).Append(" });");
                return sb.ToString();
            }
            return "using CodeGlyphX;" + nl + nl + "QR.Save(\"" + escapedContent + "\", \"qrcode.png\");";
        }
        else if (SelectedCategory == "SpecialQR")
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("using CodeGlyphX;").Append(nl);
            sb.Append("using CodeGlyphX.Payloads;").Append(nl).Append(nl);

            switch (SpecialPayloadType)
            {
                case "WiFi":
                    sb.Append("QR.Save(QrPayloads.Wifi(\"").Append(EscapeString(WifiSsid)).Append("\", \"").Append(EscapeString(WifiPassword)).Append("\"), \"wifi.png\");");
                    break;
                case "vCard":
                    sb.Append("QR.Save(QrPayloads.Contact(").Append(nl);
                    sb.Append("    QrContactOutputType.VCard3,").Append(nl);
                    sb.Append("    firstname: \"").Append(EscapeString(VCardFirstName)).Append("\",").Append(nl);
                    sb.Append("    lastname: \"").Append(EscapeString(VCardLastName)).Append("\",").Append(nl);
                    sb.Append("    email: \"").Append(EscapeString(VCardEmail)).Append("\",").Append(nl);
                    sb.Append("    phone: \"").Append(EscapeString(VCardPhone)).Append("\"").Append(nl);
                    sb.Append("), \"contact.png\");");
                    break;
                case "Email":
                    sb.Append("QR.Save(QrPayloads.Email(\"").Append(EscapeString(EmailAddress)).Append("\"), \"email.png\");");
                    break;
                case "Phone":
                    sb.Append("QR.Save(QrPayloads.Phone(\"").Append(EscapeString(PhoneNumber)).Append("\"), \"phone.png\");");
                    break;
                case "SMS":
                    sb.Append("QR.Save(QrPayloads.Sms(\"").Append(EscapeString(SmsNumber)).Append("\", \"").Append(EscapeString(SmsMessage)).Append("\"), \"sms.png\");");
                    break;
                case "OTP":
                    sb.Append("QR.Save(QrPayloads.OneTimePassword(").Append(nl);
                    sb.Append("    OtpAuthType.").Append(OtpType == "HOTP" ? "Hotp" : "Totp").Append(",").Append(nl);
                    sb.Append("    secretBase32: \"").Append(EscapeString(OtpSecret)).Append("\",").Append(nl);
                    sb.Append("    label: \"").Append(EscapeString(OtpLabel)).Append("\",").Append(nl);
                    sb.Append("    issuer: \"").Append(EscapeString(OtpIssuer)).Append("\"").Append(nl);
                    sb.Append("), \"otp.png\");");
                    break;
                case "Girocode":
                    sb.Append("QR.Save(QrPayloads.Girocode(").Append(nl);
                    sb.Append("    iban: \"").Append(EscapeString(GirocodeIban)).Append("\",").Append(nl);
                    sb.Append("    bic: \"").Append(EscapeString(GirocodeBic)).Append("\",").Append(nl);
                    sb.Append("    name: \"").Append(EscapeString(GirocodeRecipient)).Append("\",").Append(nl);
                    sb.Append("    amount: ").Append(GirocodeAmount.ToString(CultureInfo.InvariantCulture)).Append("m,").Append(nl);
                    sb.Append("    remittanceInformation: \"").Append(EscapeString(GirocodeReference)).Append("\"").Append(nl);
                    sb.Append("), \"sepa.png\");");
                    break;
                default:
                    sb.Clear();
                    sb.Append("using CodeGlyphX;").Append(nl).Append(nl);
                    sb.Append("QR.Save(\"").Append(EscapeString(UrlContent)).Append("\", \"qrcode.png\");");
                    break;
            }
            return sb.ToString();
        }
        else if (SelectedCategory == "Barcode")
        {
            var content = SelectedBarcodeType switch
            {
                "Code39" or "Code93" => EscapeString(BarcodeContent.ToUpperInvariant()),
                _ => EscapeString(BarcodeContent)
            };
            return "using CodeGlyphX;" + nl + nl + "Barcode.Save(SymbolFormat." + GetBarcodeTypeEnumName() + ", \"" + content + "\", \"barcode.png\");";
        }
        else if (SelectedCategory == "Matrix")
        {
            var content = EscapeString(MatrixContent);
            var method = SelectedMatrixType switch
            {
                "Pdf417" => "Pdf417Code",
                "Aztec" => "AztecCode",
                _ => "DataMatrixCode"
            };
            return "using CodeGlyphX;" + nl + nl + method + ".Save(\"" + content + "\", \"" + SelectedMatrixType.ToLowerInvariant() + ".png\");";
        }
        return "using CodeGlyphX;" + nl + nl + "QR.Save(\"https://example.com\", \"qrcode.png\");";
    }

    private string GetCodeExampleVb()
    {
        var nl = "\n";

        if (SelectedMode == "Decode") {
            return "Imports CodeGlyphX" + nl + nl
                + "Dim scan = SymbolScanner.ScanFile(\"image.png\", ScanOptions.Balanced(2000))" + nl
                + "For Each symbol In scan.Symbols" + nl
                + "    Console.WriteLine($\"{symbol.Format}: {symbol.Text}\")" + nl
                + "Next" + nl
                + "Console.WriteLine(scan.CompletionReason)";
        }

        if (SelectedCategory == "QR")
        {
            var escapedContent = EscapeStringVb(Content);
            if (ErrorCorrection != "M" || ModuleShape != "Square" || CustomEyes || ForegroundColor != "#000000" || BackgroundColor != "#FFFFFF" || TargetSizePx > 0 || BackgroundSupersample > 1)
            {
                var foreground = ParseColor(ForegroundColor);
                var background = ParseColor(BackgroundColor);
                var lines = new System.Collections.Generic.List<string>
                {
                    "    .ArtGuardrailsEnabled = False",
                    "    .Foreground = New Rgba32(" + foreground.R + ", " + foreground.G + ", " + foreground.B + ")",
                    "    .Background = New Rgba32(" + background.R + ", " + background.G + ", " + background.B + ")"
                };
                if (TargetSizePx > 0)
                {
                    lines.Add("    .TargetSizePx = " + TargetSizePx);
                    if (TargetSizeIncludesQuietZone)
                    {
                        lines.Add("    .TargetSizeIncludesQuietZone = True");
                    }
                }
                if (BackgroundSupersample > 1)
                {
                    lines.Add("    .BackgroundSupersample = " + BackgroundSupersample);
                }
                if (ModuleShape != "Square")
                {
                    lines.Add("    .ModuleShape = QrModuleShape." + ModuleShape);
                }
                if (CustomEyes)
                {
                    var eyeLines = new System.Collections.Generic.List<string>
                    {
                        "        .UseFrame = True",
                        "        .OuterShape = QrModuleShape." + EyeOuterShape,
                        "        .InnerShape = QrModuleShape." + EyeInnerShape
                    };
                    var eyeBlock = "    .Eyes = New QrEyeOptions With {" + nl
                        + string.Join("," + nl, eyeLines) + nl
                        + "    }";
                    lines.Add(eyeBlock);
                }

                var sb = new System.Text.StringBuilder();
                sb.Append("Imports CodeGlyphX").Append(nl);
                sb.Append("Imports CodeGlyphX.Rendering").Append(nl);
                sb.Append("Imports CodeGlyphX.Rendering.Png").Append(nl).Append(nl);
                sb.Append("Dim options = New QrRenderOptions With {").Append(nl);
                sb.Append(string.Join("," + nl, lines)).Append(nl);
                sb.Append("}").Append(nl).Append(nl);
                sb.Append("QrCodeEncoder.EncodeText(\"").Append(escapedContent).Append("\", New QrEncodingOptions With {.ErrorCorrectionLevel = QrErrorCorrectionLevel.").Append(ErrorCorrection).Append("}).Save(\"qrcode.png\", options)");
                return sb.ToString();
            }
            return "Imports CodeGlyphX" + nl + nl + "QrCodeEncoder.EncodeText(\"" + escapedContent + "\").Save(\"qrcode.png\")";
        }
        else if (SelectedCategory == "SpecialQR")
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("Imports CodeGlyphX").Append(nl);
            sb.Append("Imports CodeGlyphX.Payloads").Append(nl).Append(nl);

            switch (SpecialPayloadType)
            {
                case "WiFi":
                    sb.Append("Dim payload = QrPayloads.Wifi(\"").Append(EscapeStringVb(WifiSsid)).Append("\", \"").Append(EscapeStringVb(WifiPassword)).Append("\")");
                    break;
                case "vCard":
                    sb.Append("Dim payload = QrPayloads.Contact(").Append(nl);
                    sb.Append("    QrContactOutputType.VCard3,").Append(nl);
                    sb.Append("    firstname:=\"").Append(EscapeStringVb(VCardFirstName)).Append("\",").Append(nl);
                    sb.Append("    lastname:=\"").Append(EscapeStringVb(VCardLastName)).Append("\",").Append(nl);
                    sb.Append("    email:=\"").Append(EscapeStringVb(VCardEmail)).Append("\",").Append(nl);
                    sb.Append("    phone:=\"").Append(EscapeStringVb(VCardPhone)).Append("\"").Append(nl);
                    sb.Append(")");
                    break;
                case "Email":
                    sb.Append("Dim payload = QrPayloads.Email(\"").Append(EscapeStringVb(EmailAddress)).Append("\")");
                    break;
                case "Phone":
                    sb.Append("Dim payload = QrPayloads.Phone(\"").Append(EscapeStringVb(PhoneNumber)).Append("\")");
                    break;
                case "SMS":
                    sb.Append("Dim payload = QrPayloads.Sms(\"").Append(EscapeStringVb(SmsNumber)).Append("\", \"").Append(EscapeStringVb(SmsMessage)).Append("\")");
                    break;
                case "OTP":
                    sb.Append("Dim payload = QrPayloads.OneTimePassword(").Append(nl);
                    sb.Append("    OtpAuthType.").Append(OtpType == "HOTP" ? "Hotp" : "Totp").Append(",").Append(nl);
                    sb.Append("    secretBase32:=\"").Append(EscapeStringVb(OtpSecret)).Append("\",").Append(nl);
                    sb.Append("    label:=\"").Append(EscapeStringVb(OtpLabel)).Append("\",").Append(nl);
                    sb.Append("    issuer:=\"").Append(EscapeStringVb(OtpIssuer)).Append("\"").Append(nl);
                    sb.Append(")");
                    break;
                case "Girocode":
                    sb.Append("Dim payload = QrPayloads.Girocode(").Append(nl);
                    sb.Append("    iban:=\"").Append(EscapeStringVb(GirocodeIban)).Append("\",").Append(nl);
                    sb.Append("    bic:=\"").Append(EscapeStringVb(GirocodeBic)).Append("\",").Append(nl);
                    sb.Append("    name:=\"").Append(EscapeStringVb(GirocodeRecipient)).Append("\",").Append(nl);
                    sb.Append("    amount:=").Append(GirocodeAmount.ToString(CultureInfo.InvariantCulture)).Append("D,").Append(nl);
                    sb.Append("    remittanceInformation:=\"").Append(EscapeStringVb(GirocodeReference)).Append("\"").Append(nl);
                    sb.Append(")");
                    break;
                default:
                    sb.Clear();
                    sb.Append("Imports CodeGlyphX").Append(nl).Append(nl);
                    sb.Append("QrCodeEncoder.EncodeText(\"").Append(EscapeStringVb(UrlContent)).Append("\").Save(\"qrcode.png\")");
                    return sb.ToString();
            }
            var file = SpecialPayloadType switch {
                "WiFi" => "wifi.png", "vCard" => "contact.png", "Email" => "email.png",
                "Phone" => "phone.png", "SMS" => "sms.png", "OTP" => "otp.png", _ => "sepa.png"
            };
            // VB is case insensitive: QR and the implementation namespace Qr collide.
            // The public encoder and symbol Save API preserve typed payload recommendations.
            sb.Append(nl).Append("Dim encoding = New QrEncodingOptions With {").Append(nl)
                .Append("    .ErrorCorrectionLevel = If(payload.ErrorCorrectionLevel, QrErrorCorrectionLevel.M),").Append(nl)
                .Append("    .TextEncoding = If(payload.TextEncoding, QrTextEncoding.Utf8),").Append(nl)
                .Append("    .MinVersion = If(payload.MinVersion, 1),").Append(nl)
                .Append("    .MaxVersion = If(payload.MaxVersion, 40)").Append(nl)
                .Append("}").Append(nl)
                .Append("QrCodeEncoder.EncodeText(payload.Text, encoding).Save(\"").Append(file).Append("\")");
            return sb.ToString();
        }
        else if (SelectedCategory == "Barcode")
        {
            var content = SelectedBarcodeType switch
            {
                "Code39" or "Code93" => EscapeStringVb(BarcodeContent.ToUpperInvariant()),
                _ => EscapeStringVb(BarcodeContent)
            };
            return "Imports CodeGlyphX" + nl + nl + "Barcode.Save(SymbolFormat." + GetBarcodeTypeEnumName() + ", \"" + content + "\", \"barcode.png\")";
        }
        else if (SelectedCategory == "Matrix")
        {
            var content = EscapeStringVb(MatrixContent);
            var method = SelectedMatrixType switch
            {
                "Pdf417" => "Pdf417Code",
                "Aztec" => "AztecCode",
                _ => "DataMatrixCode"
            };
            return "Imports CodeGlyphX" + nl + nl + method + ".Save(\"" + content + "\", \"" + SelectedMatrixType.ToLowerInvariant() + ".png\")";
        }
        return "Imports CodeGlyphX" + nl + nl + "QrCodeEncoder.EncodeText(\"https://example.com\").Save(\"qrcode.png\")";
    }

    internal static string EscapeString(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"")
        .Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t").Replace("\0", "\\0");

    internal static string EscapeStringVb(string s) => s.Replace("\"", "\"\"");

    internal sealed record DecodeResult(string Type, string Text);

    internal enum CodeLanguage
    {
        CSharp,
        Vb
    }
}
