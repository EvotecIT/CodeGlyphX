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
            "Circle" => QrPngModuleShape.Circle,
            "Rounded" => QrPngModuleShape.Rounded,
            _ => QrPngModuleShape.Square
        };
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
            _ => SelectedBarcodeType
        };
    }

    internal string GetCodeExample()
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
            if (ModuleShape != "Square" || CustomEyes || ForegroundColor != "#000000")
            {
                var sb = new System.Text.StringBuilder();
                sb.Append("using CodeGlyphX;").Append(nl).Append(nl);
                sb.Append("var options = new QrEasyOptions").Append(nl);
                sb.Append("{").Append(nl);
                sb.Append("    ErrorCorrectionLevel = QrErrorCorrectionLevel.").Append(ErrorCorrection).Append(",").Append(nl);
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

    internal static string EscapeString(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");

    internal sealed record DecodeResult(string Type, string Text);
}
