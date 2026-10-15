using System;
using System.Collections.Generic;
using CodeGlyphX.Code128;

namespace CodeGlyphX;

/// <summary>Prepares USPS Intelligent Mail package data and encodes it through the GS1-128 carrier.</summary>
/// <remarks>Supports the commercial, online, retail and legacy PIC structures in USPS2000508 Rev E. It validates structure and checksums; USPS identifier assignment, manifests and service eligibility remain separate.</remarks>
public static class IntelligentMailPackage {
    /// <summary>Validates a complete PIC or computes its missing final Mod 10 digit, then optionally adds a five- or nine-digit routing ZIP.</summary>
    public static string Prepare(string trackingNumber, string? routingZip = null) {
        if (trackingNumber is null) throw new ArgumentNullException(nameof(trackingNumber));
        var pic = trackingNumber.Replace(" ", string.Empty);
        IdentifierChecks.RequireDigits(pic);
        if (pic.Length == 21 || pic.Length == 25) pic += IdentifierChecks.Gs1(pic);
        if (!TryParsePic(pic)) throw new ArgumentException("Invalid USPS package identification code or check digit.", nameof(trackingNumber));
        if (string.IsNullOrEmpty(routingZip)) return pic;
        IdentifierChecks.RequireDigits(routingZip!);
        if ((routingZip!.Length != 5 && routingZip.Length != 9) || pic.Length + routingZip.Length + 3 > 34)
            throw new ArgumentException("Routing requires a five- or nine-digit ZIP and a complete payload of at most 34 digits.", nameof(routingZip));
        return "420" + routingZip + Gs1.GroupSeparator + pic;
    }

    /// <summary>Builds commercial AI 92 or 93 data from its three-digit service, assigned mailer ID and serial number.</summary>
    public static string CreateCommercial(string serviceType, string mailerId, string serialNumber, string? routingZip = null) {
        RequireField(serviceType, 3, nameof(serviceType));
        RequireMailer(mailerId);
        if (serialNumber is null) throw new ArgumentNullException(nameof(serialNumber));
        var shortLength = mailerId.Length == 9 ? 7 : 10;
        if (serialNumber.Length != shortLength && serialNumber.Length != shortLength + 4)
            throw new ArgumentException("The serial length must be 7 or 11 for a nine-digit MID, or 10 or 14 for a six-digit MID.", nameof(serialNumber));
        IdentifierChecks.RequireDigits(serialNumber);
        return Prepare((mailerId.Length == 9 ? "92" : "93") + serviceType + mailerId + serialNumber, routingZip);
    }

    /// <summary>Builds online AI 94 data with a two-digit source identifier and a five- or eight-digit serial.</summary>
    public static string CreateOnline(string serviceType, string sourceIdentifier, string mailerId, string serialNumber, string? routingZip = null) {
        RequireField(serviceType, 3, nameof(serviceType));
        RequireField(sourceIdentifier, 2, nameof(sourceIdentifier));
        RequireMailer(mailerId);
        RequireField(serialNumber, mailerId.Length == 9 ? 5 : 8, nameof(serialNumber));
        return Prepare("94" + serviceType + sourceIdentifier + mailerId + serialNumber, routingZip);
    }

    /// <summary>Builds retail AI 95 data with a one-digit channel, six-digit device, four-digit YDDD date and five-digit serial.</summary>
    public static string CreateRetail(string serviceType, string channel, string deviceId, string julianDate, string serialNumber, string? routingZip = null) {
        RequireField(serviceType, 3, nameof(serviceType));
        RequireField(channel, 1, nameof(channel));
        RequireField(deviceId, 6, nameof(deviceId));
        RequireField(julianDate, 4, nameof(julianDate));
        RequireField(serialNumber, 5, nameof(serialNumber));
        return Prepare("95" + serviceType + channel + deviceId + julianDate + serialNumber, routingZip);
    }

    /// <summary>Encodes the checked PIC and optional routing field, including the routing FNC1 separator.</summary>
    public static Barcode1D Encode(string trackingNumber, string? routingZip = null) => Code128Encoder.EncodeGs1(Prepare(trackingNumber, routingZip));

    /// <summary>Decodes a known GS1-128 carrier and validates the USPS application payload.</summary>
    public static bool TryDecode(bool[] modules, out IntelligentMailPackageData data) {
        data = null!;
        return modules != null && BarcodeDecoder.TryDecode(modules, BarcodeType.GS1_128, out var decoded) && TryParse(decoded.Text, out data);
    }

    /// <summary>Parses a decoded element string, keeping the routing field separate from the printable PIC.</summary>
    public static bool TryParse(string value, out IntelligentMailPackageData data) {
        data = null!;
        if (value is null) return false;
        var zip = string.Empty;
        var pic = value;
        if (value.StartsWith("420", StringComparison.Ordinal)) {
            var separator = value.IndexOf(Gs1.GroupSeparator);
            if (separator != 8 && separator != 12) return false;
            zip = value.Substring(3, separator - 3);
            pic = value.Substring(separator + 1);
            if (!IsDigits(zip) || zip.Length + 3 + pic.Length > 34) return false;
        }
        if (!TryParsePic(pic)) return false;
        data = new IntelligentMailPackageData(pic, zip, value);
        return true;
    }

    private static bool TryParsePic(string pic) {
        if ((pic.Length != 22 && pic.Length != 26) || !IsDigits(pic)
            || pic[pic.Length - 1] != IdentifierChecks.Gs1(pic.Substring(0, pic.Length - 1))) return false;
        if (pic[0] != '9' || pic[1] < '1' || pic[1] > '5') return false;
        switch (pic[1]) {
            case '1': return pic.Length == 22;
            case '2': return pic[5] == '9';
            case '3': return pic[5] != '9';
            case '4': return pic.Length == 22;
            case '5':
                var day = (pic[13] - '0') * 100 + (pic[14] - '0') * 10 + pic[15] - '0';
                return pic.Length == 22 && day >= 1 && day <= 366;
            default: return false;
        }
    }

    private static void RequireMailer(string value) {
        if (value is null) throw new ArgumentNullException(nameof(value));
        if ((value.Length != 6 && value.Length != 9) || !IsDigits(value) || (value[0] == '9') != (value.Length == 9))
            throw new ArgumentException("A MID requires nine digits starting with 9, or six digits starting with 0 through 8.", nameof(value));
    }

    private static void RequireField(string value, int length, string name) {
        if (value is null) throw new ArgumentNullException(name);
        if (value.Length != length || !IsDigits(value)) throw new ArgumentException($"The field requires {length} digits.", name);
    }

    private static bool IsDigits(string value) {
        foreach (var c in value) if (c < '0' || c > '9') return false;
        return true;
    }
}

/// <summary>A validated USPS package identifier and optional postal routing information.</summary>
public sealed class IntelligentMailPackageData {
    /// <summary>Gets the complete checked package identification code.</summary>
    public string TrackingNumber { get; }
    /// <summary>Gets the routing ZIP, or an empty string when omitted.</summary>
    public string RoutingZip { get; }
    /// <summary>Gets the complete decoded GS1-128 element string.</summary>
    public string ElementString { get; }
    /// <summary>Gets the two-digit channel application identifier.</summary>
    public string ChannelApplicationIdentifier => TrackingNumber.Substring(0, 2);
    /// <summary>Gets the service type code; legacy AI 91 carries two service digits.</summary>
    public string ServiceType => TrackingNumber.Substring(2, TrackingNumber[1] == '1' ? 2 : 3);
    /// <summary>Gets the PIC grouped in fours for printing, excluding routing data.</summary>
    public string HumanReadable {
        get {
            var groups = new List<string>();
            for (var i = 0; i < TrackingNumber.Length; i += 4) groups.Add(TrackingNumber.Substring(i, Math.Min(4, TrackingNumber.Length - i)));
            return string.Join(" ", groups);
        }
    }
    internal IntelligentMailPackageData(string pic, string zip, string value) { TrackingNumber = pic; RoutingZip = zip; ElementString = value; }
}
