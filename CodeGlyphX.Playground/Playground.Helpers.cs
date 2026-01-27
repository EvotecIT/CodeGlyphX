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

    internal QrPngModuleShape ParseModuleShape(string shape)
    {
        return shape switch
        {
            "Dot" => QrPngModuleShape.Dot,
            "DotGrid" => QrPngModuleShape.DotGrid,
            "Diamond" => QrPngModuleShape.Diamond,
            "SoftDiamond" => QrPngModuleShape.SoftDiamond,
            "Squircle" => QrPngModuleShape.Squircle,
            "Leaf" => QrPngModuleShape.Leaf,
            "Wave" => QrPngModuleShape.Wave,
            "Blob" => QrPngModuleShape.Blob,
            "Circle" => QrPngModuleShape.Circle,
            "Rounded" => QrPngModuleShape.Rounded,
            _ => QrPngModuleShape.Square
        };
    }

    internal static QrPngEyeFrameStyle ParseEyeFrameStyle(string style)
    {
        return style switch
        {
            "DoubleRing" => QrPngEyeFrameStyle.DoubleRing,
            "Target" => QrPngEyeFrameStyle.Target,
            "Bracket" => QrPngEyeFrameStyle.Bracket,
            "Badge" => QrPngEyeFrameStyle.Badge,
            _ => QrPngEyeFrameStyle.Single
        };
    }

    internal static QrPngPaletteMode ParsePaletteMode(string mode)
    {
        return mode switch
        {
            "Checker" => QrPngPaletteMode.Checker,
            "Random" => QrPngPaletteMode.Random,
            "Rings" => QrPngPaletteMode.Rings,
            _ => QrPngPaletteMode.Cycle
        };
    }

    internal static QrPngModuleScaleMode ParseScaleMapMode(string mode)
    {
        return mode switch
        {
            "Radial" => QrPngModuleScaleMode.Radial,
            "Random" => QrPngModuleScaleMode.Random,
            "Checker" => QrPngModuleScaleMode.Checker,
            _ => QrPngModuleScaleMode.Rings
        };
    }

    internal static QrPngGradientType ParseGradientType(string mode)
    {
        return mode switch
        {
            "Vertical" => QrPngGradientType.Vertical,
            "DiagonalDown" => QrPngGradientType.DiagonalDown,
            "DiagonalUp" => QrPngGradientType.DiagonalUp,
            "Radial" => QrPngGradientType.Radial,
            _ => QrPngGradientType.Horizontal
        };
    }

    internal static QrPngBackgroundPatternType ParsePatternType(string mode)
    {
        return mode switch
        {
            "Grid" => QrPngBackgroundPatternType.Grid,
            "Checker" => QrPngBackgroundPatternType.Checker,
            "DiagonalStripes" => QrPngBackgroundPatternType.DiagonalStripes,
            "Crosshatch" => QrPngBackgroundPatternType.Crosshatch,
            _ => QrPngBackgroundPatternType.Dots
        };
    }

    internal static Rgba32 ApplyAlpha(Rgba32 color, int alpha)
    {
        if (alpha < 0) alpha = 0;
        if (alpha > 255) alpha = 255;
        return new Rgba32(color.R, color.G, color.B, (byte)alpha);
    }

    internal string GetSafetyStatus()
    {
        if (SafetyReport is null) return string.Empty;
        var score = SafetyReport.Score;
        if (score >= 80) return "Safe";
        if (score >= 60) return "Caution";
        return "Risky";
    }

    internal string GetSafetyColor()
    {
        if (SafetyReport is null) return "#94a3b8";
        var score = SafetyReport.Score;
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
            "GS1128" => "GS1_128",
            "Code32" => "Code32",
            "ITF" => "ITF",
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

        if (SelectedMode == "Decode")
        {
            return "using CodeGlyphX;" + nl
                + "using CodeGlyphX.Rendering;" + nl + nl
                + "var bytes = File.ReadAllBytes(\"image.png\");" + nl
                + "if (QrImageDecoder.TryDecodeImage(bytes, out var qr))" + nl
                + "{" + nl
                + "    Console.WriteLine(qr.Text);" + nl
                + "}" + nl + nl
                + "if (ImageReader.TryDecodeRgba32(bytes, out var rgba, out var w, out var h) &&" + nl
                + "    BarcodeDecoder.TryDecode(rgba, w, h, w * 4, PixelFormat.Rgba32, out var barcode))" + nl
                + "{" + nl
                + "    Console.WriteLine(barcode.Text);" + nl
                + "}";
        }

        if (SelectedCategory == "QR")
        {
            var escapedContent = EscapeString(Content);
            if (ModuleShape != "Square" || CustomEyes || ForegroundColor != "#000000" || TargetSizePx > 0 || BackgroundSupersample > 1)
            {
                var sb = new System.Text.StringBuilder();
                sb.Append("using CodeGlyphX;").Append(nl).Append(nl);
                sb.Append("var options = new QrEasyOptions").Append(nl);
                sb.Append("{").Append(nl);
                sb.Append("    ErrorCorrectionLevel = QrErrorCorrectionLevel.").Append(ErrorCorrection).Append(",").Append(nl);
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
                    sb.Append("    ModuleShape = QrPngModuleShape.").Append(ModuleShape).Append(",").Append(nl);
                }
                if (CustomEyes)
                {
                    sb.Append("    Eyes = new QrPngEyeOptions").Append(nl);
                    sb.Append("    {").Append(nl);
                    sb.Append("        UseFrame = true,").Append(nl);
                    sb.Append("        OuterShape = QrPngModuleShape.").Append(EyeOuterShape).Append(",").Append(nl);
                    sb.Append("        InnerShape = QrPngModuleShape.").Append(EyeInnerShape).Append(nl);
                    sb.Append("    },").Append(nl);
                }
                sb.Append("};").Append(nl).Append(nl);
                sb.Append("QR.Save(\"").Append(escapedContent).Append("\", \"qrcode.png\", options);");
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
                    sb.Append("QR.Save(QrPayloads.VCard(").Append(nl);
                    sb.Append("    firstName: \"").Append(EscapeString(VCardFirstName)).Append("\",").Append(nl);
                    sb.Append("    lastName: \"").Append(EscapeString(VCardLastName)).Append("\",").Append(nl);
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
                    sb.Append("    OtpAuthType.").Append(OtpType).Append(",").Append(nl);
                    sb.Append("    secret: \"").Append(EscapeString(OtpSecret)).Append("\",").Append(nl);
                    sb.Append("    label: \"").Append(EscapeString(OtpLabel)).Append("\",").Append(nl);
                    sb.Append("    issuer: \"").Append(EscapeString(OtpIssuer)).Append("\"").Append(nl);
                    sb.Append("), \"otp.png\");");
                    break;
                case "Girocode":
                    sb.Append("QR.Save(QrPayloads.Girocode(").Append(nl);
                    sb.Append("    iban: \"").Append(EscapeString(GirocodeIban)).Append("\",").Append(nl);
                    sb.Append("    bic: \"").Append(EscapeString(GirocodeBic)).Append("\",").Append(nl);
                    sb.Append("    recipientName: \"").Append(EscapeString(GirocodeRecipient)).Append("\",").Append(nl);
                    sb.Append("    amount: ").Append(GirocodeAmount).Append("m,").Append(nl);
                    sb.Append("    reference: \"").Append(EscapeString(GirocodeReference)).Append("\"").Append(nl);
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
            return "using CodeGlyphX;" + nl + nl + "Barcode.Save(BarcodeType." + GetBarcodeTypeEnumName() + ", \"" + content + "\", \"barcode.png\");";
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

        if (SelectedMode == "Decode")
        {
            return "Imports CodeGlyphX" + nl
                + "Imports CodeGlyphX.Rendering" + nl
                + "Imports CodeGlyphX.Rendering.Png" + nl
                + "Imports System.IO" + nl + nl
                + "Dim bytes = File.ReadAllBytes(\"image.png\")" + nl
                + "Dim qr As QrDecoded" + nl
                + "If QrImageDecoder.TryDecodeImage(bytes, qr) Then" + nl
                + "    Console.WriteLine(qr.Text)" + nl
                + "End If" + nl + nl
                + "Dim rgba As Rgba32" + nl
                + "Dim w As Integer" + nl
                + "Dim h As Integer" + nl
                + "Dim barcode As BarcodeDecoded" + nl
                + "If ImageReader.TryDecodeRgba32(bytes, rgba, w, h) AndAlso" + nl
                + "    BarcodeDecoder.TryDecode(rgba, w, h, w * 4, PixelFormat.Rgba32, barcode) Then" + nl
                + "    Console.WriteLine(barcode.Text)" + nl
                + "End If";
        }

        if (SelectedCategory == "QR")
        {
            var escapedContent = EscapeStringVb(Content);
            if (ModuleShape != "Square" || CustomEyes || ForegroundColor != "#000000" || TargetSizePx > 0 || BackgroundSupersample > 1)
            {
                var lines = new System.Collections.Generic.List<string>
                {
                    "    .ErrorCorrectionLevel = QrErrorCorrectionLevel." + ErrorCorrection
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
                    lines.Add("    .ModuleShape = QrPngModuleShape." + ModuleShape);
                }
                if (CustomEyes)
                {
                    var eyeLines = new System.Collections.Generic.List<string>
                    {
                        "        .UseFrame = True",
                        "        .OuterShape = QrPngModuleShape." + EyeOuterShape,
                        "        .InnerShape = QrPngModuleShape." + EyeInnerShape
                    };
                    var eyeBlock = "    .Eyes = New QrPngEyeOptions With {" + nl
                        + string.Join("," + nl, eyeLines) + nl
                        + "    }";
                    lines.Add(eyeBlock);
                }

                var sb = new System.Text.StringBuilder();
                sb.Append("Imports CodeGlyphX").Append(nl);
                sb.Append("Imports CodeGlyphX.Rendering.Png").Append(nl).Append(nl);
                sb.Append("Dim options = New QrEasyOptions With {").Append(nl);
                sb.Append(string.Join("," + nl, lines)).Append(nl);
                sb.Append("}").Append(nl).Append(nl);
                sb.Append("QR.Save(\"").Append(escapedContent).Append("\", \"qrcode.png\", options)");
                return sb.ToString();
            }
            return "Imports CodeGlyphX" + nl + nl + "QR.Save(\"" + escapedContent + "\", \"qrcode.png\")";
        }
        else if (SelectedCategory == "SpecialQR")
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("Imports CodeGlyphX").Append(nl);
            sb.Append("Imports CodeGlyphX.Payloads").Append(nl).Append(nl);

            switch (SpecialPayloadType)
            {
                case "WiFi":
                    sb.Append("QR.Save(QrPayloads.Wifi(\"").Append(EscapeStringVb(WifiSsid)).Append("\", \"").Append(EscapeStringVb(WifiPassword)).Append("\"), \"wifi.png\")");
                    break;
                case "vCard":
                    sb.Append("QR.Save(QrPayloads.VCard(").Append(nl);
                    sb.Append("    firstName:=\"").Append(EscapeStringVb(VCardFirstName)).Append("\",").Append(nl);
                    sb.Append("    lastName:=\"").Append(EscapeStringVb(VCardLastName)).Append("\",").Append(nl);
                    sb.Append("    email:=\"").Append(EscapeStringVb(VCardEmail)).Append("\",").Append(nl);
                    sb.Append("    phone:=\"").Append(EscapeStringVb(VCardPhone)).Append("\"").Append(nl);
                    sb.Append("), \"contact.png\")");
                    break;
                case "Email":
                    sb.Append("QR.Save(QrPayloads.Email(\"").Append(EscapeStringVb(EmailAddress)).Append("\"), \"email.png\")");
                    break;
                case "Phone":
                    sb.Append("QR.Save(QrPayloads.Phone(\"").Append(EscapeStringVb(PhoneNumber)).Append("\"), \"phone.png\")");
                    break;
                case "SMS":
                    sb.Append("QR.Save(QrPayloads.Sms(\"").Append(EscapeStringVb(SmsNumber)).Append("\", \"").Append(EscapeStringVb(SmsMessage)).Append("\"), \"sms.png\")");
                    break;
                case "OTP":
                    sb.Append("QR.Save(QrPayloads.OneTimePassword(").Append(nl);
                    sb.Append("    OtpAuthType.").Append(OtpType).Append(",").Append(nl);
                    sb.Append("    secret:=\"").Append(EscapeStringVb(OtpSecret)).Append("\",").Append(nl);
                    sb.Append("    label:=\"").Append(EscapeStringVb(OtpLabel)).Append("\",").Append(nl);
                    sb.Append("    issuer:=\"").Append(EscapeStringVb(OtpIssuer)).Append("\"").Append(nl);
                    sb.Append("), \"otp.png\")");
                    break;
                case "Girocode":
                    sb.Append("QR.Save(QrPayloads.Girocode(").Append(nl);
                    sb.Append("    iban:=\"").Append(EscapeStringVb(GirocodeIban)).Append("\",").Append(nl);
                    sb.Append("    bic:=\"").Append(EscapeStringVb(GirocodeBic)).Append("\",").Append(nl);
                    sb.Append("    recipientName:=\"").Append(EscapeStringVb(GirocodeRecipient)).Append("\",").Append(nl);
                    sb.Append("    amount:=").Append(GirocodeAmount).Append("D,").Append(nl);
                    sb.Append("    reference:=\"").Append(EscapeStringVb(GirocodeReference)).Append("\"").Append(nl);
                    sb.Append("), \"sepa.png\")");
                    break;
                default:
                    sb.Clear();
                    sb.Append("Imports CodeGlyphX").Append(nl).Append(nl);
                    sb.Append("QR.Save(\"").Append(EscapeStringVb(UrlContent)).Append("\", \"qrcode.png\")");
                    break;
            }
            return sb.ToString();
        }
        else if (SelectedCategory == "Barcode")
        {
            var content = SelectedBarcodeType switch
            {
                "Code39" or "Code93" => EscapeStringVb(BarcodeContent.ToUpperInvariant()),
                _ => EscapeStringVb(BarcodeContent)
            };
            return "Imports CodeGlyphX" + nl + nl + "Barcode.Save(BarcodeType." + GetBarcodeTypeEnumName() + ", \"" + content + "\", \"barcode.png\")";
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
        return "Imports CodeGlyphX" + nl + nl + "QR.Save(\"https://example.com\", \"qrcode.png\")";
    }

    internal static string EscapeString(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");

    internal static string EscapeStringVb(string s) => s.Replace("\"", "\"\"");

    internal sealed record DecodeResult(string Type, string Text);

    internal enum CodeLanguage
    {
        CSharp,
        Vb
    }
}
