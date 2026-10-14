using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace CodeGlyphX.Playground;

// PowerShell examples use the ImagePlayground module (3.2.7 command surface), which is built on CodeGlyphX.
public partial class PlaygroundExample {
    private const string PowerShellRoadmapUrl = "https://codeglyphx.com/powershell/#roadmap";
    private const int PowerShellMaxLineLength = 88;

    private static readonly (string Value, string Label)[] Platforms = {
        ("Console", "Console"),
        ("AspNet", "ASP.NET"),
        ("Blazor", "Blazor"),
        ("Maui", "MAUI"),
        ("PowerShell", "PowerShell")
    };

    // Tab choices live on the host because the example is re-created after each generation.
    private string SelectedPlatform {
        get => Host?.ExamplePlatform ?? "Console";
        set { if (Host is not null) Host.ExamplePlatform = value; }
    }

    private Playground.CodeLanguage SelectedLanguage {
        get => Host?.ExampleLanguage ?? Playground.CodeLanguage.CSharp;
        set { if (Host is not null) Host.ExampleLanguage = value; }
    }

    private bool IsPowerShell => SelectedPlatform == "PowerShell";

    private string GetExampleFileName() => GetLanguageCssClass() switch {
        "language-powershell" => "Playground.ps1",
        "language-vbnet" => "Program.vb",
        _ => SelectedPlatform == "Blazor" ? "Example.razor" : "Program.cs"
    };

    private string GetPowerShellExample() {
        if (Host is not { } host) return string.Empty;

        var script = new StringBuilder();
        script.Append("# Install-Module ImagePlayground -Scope CurrentUser\n");

        if (host.SelectedMode == "Decode") {
            AppendPowerShellDecode(host, script);
        } else if (host.SelectedCategory == "Barcode") {
            AppendPowerShellBarcode(host, script);
        } else if (host.SelectedCategory == "Matrix") {
            AppendPowerShellMatrix(host, script);
        } else {
            AppendPowerShellQr(host, script);
        }

        return script.ToString().TrimEnd('\n');
    }

    private static void AppendPowerShellQr(Playground host, StringBuilder script) {
        var (command, file, parameters) = host.SelectedCategory == "SpecialQR"
            ? GetSpecialPayloadCommand(host)
            : ("New-ImageQRCode", "qrcode.png", new List<(string, string)> { ("Content", Quote(host.Content)) });

        parameters.Add(("FilePath", Quote(file)));
        if (!string.Equals(host.ForegroundColor, "#000000", StringComparison.OrdinalIgnoreCase))
            parameters.Add(("ForegroundColor", Quote(host.ForegroundColor)));
        if (!string.Equals(host.BackgroundColor, "#FFFFFF", StringComparison.OrdinalIgnoreCase))
            parameters.Add(("BackgroundColor", Quote(host.BackgroundColor)));

        AppendNotYetAvailable(script, GetUnsupportedQrOptions(host));
        AppendCommand(script, command, parameters);
        script.Append("(Get-ImageQRCode -FilePath ").Append(Quote(file)).Append(").Text\n");
    }

    private static (string Command, string File, List<(string, string)> Parameters) GetSpecialPayloadCommand(Playground host) {
        var p = new List<(string, string)>();
        switch (host.SpecialPayloadType) {
            case "WiFi":
                p.Add(("SSID", Quote(host.WifiSsid)));
                p.Add(("Password", Quote(host.WifiPassword)));
                return ("New-ImageQRCodeWiFi", "wifi.png", p);
            case "vCard":
                p.Add(("OutputType", "'VCard3'"));
                AddIfSet(p, "Firstname", host.VCardFirstName);
                AddIfSet(p, "Lastname", host.VCardLastName);
                AddIfSet(p, "Phone", host.VCardPhone);
                AddIfSet(p, "Email", host.VCardEmail);
                AddIfSet(p, "Website", host.VCardWebsite);
                AddIfSet(p, "Org", host.VCardOrganization);
                return ("New-ImageQRContact", "contact.png", p);
            case "Email":
                p.Add(("Email", Quote(host.EmailAddress)));
                AddIfSet(p, "Subject", host.EmailSubject);
                AddIfSet(p, "Message", host.EmailBody);
                return ("New-ImageQRCodeEmail", "email.png", p);
            case "Phone":
                p.Add(("Number", Quote(host.PhoneNumber)));
                return ("New-ImageQRCodePhoneNumber", "phone.png", p);
            case "SMS":
                p.Add(("Number", Quote(host.SmsNumber)));
                AddIfSet(p, "Message", host.SmsMessage);
                return ("New-ImageQRCodeSms", "sms.png", p);
            case "OTP":
                p.Add(("Type", host.OtpType == "HOTP" ? "'Hotp'" : "'Totp'"));
                p.Add(("SecretBase32", Quote(host.OtpSecret)));
                AddIfSet(p, "Label", host.OtpLabel);
                AddIfSet(p, "Issuer", host.OtpIssuer);
                return ("New-ImageQRCodeOtp", "otp.png", p);
            case "Girocode":
                p.Add(("Iban", Quote(host.GirocodeIban)));
                p.Add(("Bic", Quote(host.GirocodeBic)));
                p.Add(("Name", Quote(host.GirocodeRecipient)));
                p.Add(("Amount", host.GirocodeAmount.ToString(CultureInfo.InvariantCulture)));
                AddIfSet(p, "RemittanceInformation", host.GirocodeReference);
                return ("New-ImageQRCodeGirocode", "sepa.png", p);
            default:
                p.Add(("Content", Quote(host.UrlContent)));
                return ("New-ImageQRCode", "url.png", p);
        }
    }

    private static IEnumerable<string> GetUnsupportedQrOptions(Playground host) {
        if (host.SelectedCategory == "SpecialQR" && host.SpecialPayloadType == "WiFi" && (host.WifiSecurity != "WPA" || host.WifiHidden))
            yield return "WEP, open and hidden networks";
        if (host.ErrorCorrection != "M") yield return "error correction level";
        if (host.ModuleShape != "Square" || Math.Abs(host.ModuleScale - 1.0) > 0.001) yield return "module shapes";
        if (host.UseForegroundGradient || host.UseBackgroundGradient) yield return "gradients";
        if (host.EnablePalette || host.EnableZonePalettes) yield return "palettes";
        if (host.EnableScaleMap) yield return "module scale maps";
        if (host.EnableQrBackgroundPattern) yield return "background patterns";
        if (host.CustomEyes) yield return "custom eyes";
        if (host.EnableCanvas) yield return "canvas";
        if (host.TargetSizePx > 0) yield return "output size";
        if (host.BackgroundSupersample > 1) yield return "background supersampling";
    }

    private static void AppendPowerShellBarcode(Playground host, StringBuilder script) {
        var type = host.SelectedBarcodeType switch {
            "GS1128" => "GS1_128",
            "Code128" or "Code39" or "Code93" or "Code11" or "Codabar" or "MSI" or "Plessey" or "Telepen"
                or "EAN" or "UPCA" or "UPCE" or "ITF14" or "ITF" or "Code32" => host.SelectedBarcodeType,
            _ => null
        };
        if (type is null) {
            AppendNotYetAvailable(script, new[] { host.SelectedBarcodeType });
            return;
        }

        var value = host.SelectedBarcodeType is "Code39" or "Code93" ? host.BarcodeContent.ToUpperInvariant() : host.BarcodeContent;
        AppendCommand(script, "New-ImageBarCode", new List<(string, string)> {
            ("Type", Quote(type)), ("Value", Quote(value)), ("FilePath", "'barcode.png'")
        });
        script.Append("(Get-ImageBarCode -FilePath 'barcode.png').Text\n");
    }

    private static void AppendPowerShellMatrix(Playground host, StringBuilder script) {
        var (type, file) = host.SelectedMatrixType switch {
            "Pdf417" => ("PDF417", "pdf417.png"),
            "Aztec" => (null, "aztec.png"),
            _ => ((string?)"DataMatrix", "datamatrix.png")
        };
        if (type is null) {
            AppendNotYetAvailable(script, new[] { "Aztec" });
            return;
        }

        var unsupported = new List<string>();
        if (host.SelectedMatrixType == "DataMatrix" && host.SelectedDataMatrixMode != "Auto") unsupported.Add("Data Matrix encoding modes");
        if (host.SelectedMatrixType == "Pdf417" && (host.Pdf417EccLevel >= 0 || host.Pdf417Compact)) unsupported.Add("PDF417 error correction and compact mode");

        AppendNotYetAvailable(script, unsupported);
        AppendCommand(script, "New-ImageBarCode", new List<(string, string)> {
            ("Type", Quote(type)), ("Value", Quote(host.MatrixContent)), ("FilePath", Quote(file))
        });
        script.Append("(Get-ImageBarCode -FilePath ").Append(Quote(file)).Append(").Text\n");
    }

    private static void AppendPowerShellDecode(Playground host, StringBuilder script) {
        if (host.DecodeQualityPreset != "Balanced" || host.DecodeIncludeAmbiguousFormats)
            AppendNotYetAvailable(script, new[] { "decode presets and format filters" });

        if (host.DecodeQr && !host.DecodeBarcode && !host.DecodeMatrix) {
            script.Append("(Get-ImageQRCode -FilePath 'image.png').Text\n");
        } else {
            script.Append("Get-ImageBarCode -FilePath 'image.png' | Select-Object Kind, Text\n");
        }
    }

    private static void AppendNotYetAvailable(StringBuilder script, IEnumerable<string> features) {
        var list = features.ToList();
        if (list.Count == 0) return;
        script.Append("# Not yet available from PowerShell: ").Append(string.Join(", ", list))
            .Append(". See ").Append(PowerShellRoadmapUrl).Append('\n');
    }

    // One line when it reads comfortably; otherwise splat the parameters.
    private static void AppendCommand(StringBuilder script, string command, List<(string Name, string Value)> parameters) {
        var line = command + string.Concat(parameters.Select(p => " -" + p.Name + " " + p.Value));
        if (line.Length <= PowerShellMaxLineLength && !line.Contains('\n')) {
            script.Append(line).Append('\n');
            return;
        }

        var width = parameters.Max(p => p.Name.Length);
        script.Append("$params = @{\n");
        foreach (var (name, value) in parameters) {
            script.Append("    ").Append(name.PadRight(width)).Append(" = ").Append(value).Append('\n');
        }
        script.Append("}\n").Append(command).Append(" @params\n");
    }

    private static void AddIfSet(List<(string, string)> parameters, string name, string? value) {
        if (!string.IsNullOrEmpty(value)) parameters.Add((name, Quote(value)));
    }

    // Single-quoted PowerShell strings are verbatim; PowerShell also treats typographic single quotes as quotes.
    private static string Quote(string? value) {
        var text = value ?? string.Empty;
        var sb = new StringBuilder(text.Length + 2).Append('\'');
        foreach (var ch in text) {
            sb.Append(ch);
            if (ch is '\'' or '‘' or '’' or '‚' or '‛') sb.Append(ch);
        }
        return sb.Append('\'').ToString();
    }
}
