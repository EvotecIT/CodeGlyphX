using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
#pragma warning disable CS1591

namespace CodeMatrix.Payloads;

/// <summary>
/// Helpers for building common QR payloads with recommended defaults.
/// </summary>
public static partial class QrPayloads {
    public static QrPayloadData Text(string text) => new(QrPayload.Text(text));

    public static QrPayloadData Url(string url) => new(NormalizeUrl(url));

    public static QrPayloadData Bookmark(string url, string title) {
        var payload = "MEBKM:TITLE:" + EscapeInput(title ?? string.Empty) + ";URL:" + EscapeInput(url ?? string.Empty) + ";;";
        return new QrPayloadData(payload);
    }

    public static QrPayloadData WhatsAppMessage(string message, string? number = null) {
        var sanitized = string.IsNullOrEmpty(number)
            ? string.Empty
            : Regex.Replace(number, "^[0+]+|[ ()-]", string.Empty);
        var payload = "https://wa.me/" + sanitized + "?text=" + Uri.EscapeDataString(message ?? string.Empty);
        return new QrPayloadData(payload);
    }

    public static QrPayloadData Wifi(string ssid, string password, string authType = "WPA", bool hidden = false, bool escapeHexStrings = true) {
        var safeSsid = EscapeInput(ssid ?? string.Empty);
        var safePassword = EscapeInput(password ?? string.Empty);
        if (escapeHexStrings && IsHexStyle(safeSsid)) safeSsid = "\"" + safeSsid + "\"";
        if (escapeHexStrings && IsHexStyle(safePassword)) safePassword = "\"" + safePassword + "\"";
        var payload = "WIFI:T:" + EscapeInput(authType ?? string.Empty) + ";S:" + safeSsid + ";P:" + safePassword + ";" + (hidden ? "H:true" : string.Empty) + ";";
        return new QrPayloadData(payload);
    }

    public static QrPayloadData Email(string address, string? subject = null, string? message = null, QrMailEncoding encoding = QrMailEncoding.Mailto) {
        var payload = encoding switch {
            QrMailEncoding.Mailto => BuildMailto(address, subject, message),
            QrMailEncoding.Matmsg => "MATMSG:TO:" + (address ?? string.Empty) + ";SUB:" + EscapeInput(subject ?? string.Empty) + ";BODY:" + EscapeInput(message ?? string.Empty) + ";;",
            QrMailEncoding.Smtp => "SMTP:" + (address ?? string.Empty) + ":" + EscapeInput(subject ?? string.Empty, simple: true) + ":" + EscapeInput(message ?? string.Empty, simple: true),
            _ => string.Empty
        };
        return new QrPayloadData(payload);
    }

    public static QrPayloadData Mms(string number, string? subject = null, QrMmsEncoding encoding = QrMmsEncoding.Mms) {
        var payload = encoding switch {
            QrMmsEncoding.Mmsto => "mmsto:" + number + (string.IsNullOrEmpty(subject) ? string.Empty : ("?subject=" + Uri.EscapeDataString(subject))),
            QrMmsEncoding.Mms => "mms:" + number + (string.IsNullOrEmpty(subject) ? string.Empty : ("?body=" + Uri.EscapeDataString(subject))),
            _ => string.Empty
        };
        return new QrPayloadData(payload);
    }

    public static QrPayloadData Sms(string number, string? message = null, QrSmsEncoding encoding = QrSmsEncoding.Sms) {
        var payload = encoding switch {
            QrSmsEncoding.Sms => "sms:" + number + (string.IsNullOrEmpty(message) ? string.Empty : ("?body=" + Uri.EscapeDataString(message))),
            QrSmsEncoding.SmsIos => "sms:" + number + (string.IsNullOrEmpty(message) ? string.Empty : (";body=" + Uri.EscapeDataString(message))),
            QrSmsEncoding.Smsto => "SMSTO:" + number + ":" + (message ?? string.Empty),
            _ => string.Empty
        };
        return new QrPayloadData(payload);
    }

    public static QrPayloadData Phone(string number) {
        return new QrPayloadData("tel:" + (number ?? string.Empty));
    }

    public static QrPayloadData Geo(string latitude, string longitude, QrGeolocationEncoding encoding = QrGeolocationEncoding.Geo) {
        var lat = (latitude ?? string.Empty).Replace(",", ".");
        var lon = (longitude ?? string.Empty).Replace(",", ".");
        var payload = encoding == QrGeolocationEncoding.GoogleMaps
            ? "http://maps.google.com/maps?q=" + lat + "," + lon
            : "geo:" + lat + "," + lon;
        return new QrPayloadData(payload);
    }

    public static QrPayloadData CalendarEvent(
        string subject,
        string? description,
        string? location,
        DateTime start,
        DateTime end,
        bool allDayEvent,
        QrCalendarEncoding encoding = QrCalendarEncoding.Universal) {
        var dateFormatStart = allDayEvent ? "yyyyMMdd" : "yyyyMMddTHHmmss";
        var dateFormatEnd = allDayEvent ? "yyyyMMdd" : "yyyyMMddTHHmmss";
        if (!allDayEvent) {
            if (start.Kind == DateTimeKind.Utc) dateFormatStart = "yyyyMMddTHHmmssZ";
            if (end.Kind == DateTimeKind.Utc) dateFormatEnd = "yyyyMMddTHHmmssZ";
        }
        var sb = new StringBuilder();
        sb.Append("BEGIN:VEVENT").Append(Environment.NewLine);
        sb.Append("SUMMARY:").Append(subject ?? string.Empty).Append(Environment.NewLine);
        if (!string.IsNullOrEmpty(description)) sb.Append("DESCRIPTION:").Append(description).Append(Environment.NewLine);
        if (!string.IsNullOrEmpty(location)) sb.Append("LOCATION:").Append(location).Append(Environment.NewLine);
        sb.Append("DTSTART:").Append(start.ToString(dateFormatStart, CultureInfo.InvariantCulture)).Append(Environment.NewLine);
        sb.Append("DTEND:").Append(end.ToString(dateFormatEnd, CultureInfo.InvariantCulture)).Append(Environment.NewLine);
        sb.Append("END:VEVENT");
        if (encoding == QrCalendarEncoding.ICalComplete) {
            sb.Insert(0, "BEGIN:VCALENDAR" + Environment.NewLine + "VERSION:2.0" + Environment.NewLine);
            sb.Append(Environment.NewLine).Append("END:VCALENDAR");
        }
        return new QrPayloadData(sb.ToString());
    }

    public static QrPayloadData Contact(
        QrContactOutputType outputType,
        string firstname,
        string lastname,
        string? nickname = null,
        string? phone = null,
        string? mobilePhone = null,
        string? workPhone = null,
        string? email = null,
        DateTime? birthday = null,
        string? website = null,
        string? street = null,
        string? houseNumber = null,
        string? city = null,
        string? zipCode = null,
        string? country = null,
        string? note = null,
        string? stateRegion = null,
        QrContactAddressOrder addressOrder = QrContactAddressOrder.Default,
        string? org = null,
        string? orgTitle = null,
        QrContactAddressType addressType = QrContactAddressType.HomePreferred) {
        if (outputType == QrContactOutputType.MeCard) {
            var sb = new StringBuilder();
            sb.Append("MECARD+").Append("\r\n");
            if (!string.IsNullOrEmpty(firstname) && !string.IsNullOrEmpty(lastname)) {
                sb.Append("N:").Append(lastname).Append(", ").Append(firstname).Append("\r\n");
            } else if (!string.IsNullOrEmpty(firstname) || !string.IsNullOrEmpty(lastname)) {
                sb.Append("N:").Append(firstname).Append(lastname).Append("\r\n");
            }
            if (!string.IsNullOrEmpty(org)) sb.Append("ORG:").Append(org).Append("\r\n");
            if (!string.IsNullOrEmpty(orgTitle)) sb.Append("TITLE:").Append(orgTitle).Append("\r\n");
            if (!string.IsNullOrEmpty(phone)) sb.Append("TEL:").Append(phone).Append("\r\n");
            if (!string.IsNullOrEmpty(mobilePhone)) sb.Append("TEL:").Append(mobilePhone).Append("\r\n");
            if (!string.IsNullOrEmpty(workPhone)) sb.Append("TEL:").Append(workPhone).Append("\r\n");
            if (!string.IsNullOrEmpty(email)) sb.Append("EMAIL:").Append(email).Append("\r\n");
            if (!string.IsNullOrEmpty(note)) sb.Append("NOTE:").Append(note).Append("\r\n");
            if (birthday.HasValue) sb.Append("BDAY:").Append(birthday.Value.ToString("yyyyMMdd", CultureInfo.InvariantCulture)).Append("\r\n");

            var address = addressOrder == QrContactAddressOrder.Reversed
                ? "ADR:,," + (string.IsNullOrEmpty(houseNumber) ? string.Empty : (houseNumber + " ")) + (string.IsNullOrEmpty(street) ? string.Empty : street) + "," +
                  (string.IsNullOrEmpty(city) ? string.Empty : city) + "," + (string.IsNullOrEmpty(stateRegion) ? string.Empty : stateRegion) + "," +
                  (string.IsNullOrEmpty(zipCode) ? string.Empty : zipCode) + "," + (string.IsNullOrEmpty(country) ? string.Empty : country) + "\r\n"
                : "ADR:,," + (string.IsNullOrEmpty(street) ? string.Empty : (street + " ")) + (string.IsNullOrEmpty(houseNumber) ? string.Empty : houseNumber) + "," +
                  (string.IsNullOrEmpty(city) ? string.Empty : city) + "," + (string.IsNullOrEmpty(stateRegion) ? string.Empty : stateRegion) + "," +
                  (string.IsNullOrEmpty(zipCode) ? string.Empty : zipCode) + "," + (string.IsNullOrEmpty(country) ? string.Empty : country) + "\r\n";
            sb.Append(address);

            if (!string.IsNullOrEmpty(website)) sb.Append("URL:").Append(website).Append("\r\n");
            if (!string.IsNullOrEmpty(nickname)) sb.Append("NICKNAME:").Append(nickname).Append("\r\n");
            return new QrPayloadData(sb.ToString().Trim('\r', '\n'));
        }

        var version = outputType.ToString().Substring(5);
        version = version.Length <= 1 ? (version + ".0") : version.Insert(1, ".");
        var vcard = new StringBuilder();
        vcard.Append("BEGIN:VCARD\r\n");
        vcard.Append("VERSION:").Append(version).Append("\r\n");
        vcard.Append("N:").Append(string.IsNullOrEmpty(lastname) ? string.Empty : lastname).Append(';').Append(string.IsNullOrEmpty(firstname) ? string.Empty : firstname).Append(";;;\r\n");
        vcard.Append("FN:").Append(string.IsNullOrEmpty(firstname) ? string.Empty : (firstname + " ")).Append(string.IsNullOrEmpty(lastname) ? string.Empty : lastname).Append("\r\n");
        if (!string.IsNullOrEmpty(org)) vcard.Append("ORG:").Append(org).Append("\r\n");
        if (!string.IsNullOrEmpty(orgTitle)) vcard.Append("TITLE:").Append(orgTitle).Append("\r\n");

        AppendContactTel(vcard, outputType, phone, "HOME", "VOICE", "home,voice");
        AppendContactTel(vcard, outputType, mobilePhone, "HOME", "CELL", "home,cell");
        AppendContactTel(vcard, outputType, workPhone, "WORK", "VOICE", "work,voice");

        vcard.Append("ADR;");
        vcard.Append(outputType switch {
            QrContactOutputType.VCard21 => GetAddressTypeString21(addressType),
            QrContactOutputType.VCard3 => "TYPE=" + GetAddressTypeString3(addressType),
            _ => "TYPE=" + GetAddressTypeString4(addressType)
        });
        vcard.Append(':');
        var addressV = addressOrder == QrContactAddressOrder.Reversed
            ? ";;" + (string.IsNullOrEmpty(houseNumber) ? string.Empty : (houseNumber + " ")) + (string.IsNullOrEmpty(street) ? string.Empty : street) + ";" +
              (string.IsNullOrEmpty(city) ? string.Empty : city) + ";" + (string.IsNullOrEmpty(stateRegion) ? string.Empty : stateRegion) + ";" +
              (string.IsNullOrEmpty(zipCode) ? string.Empty : zipCode) + ";" + (string.IsNullOrEmpty(country) ? string.Empty : country) + "\r\n"
            : ";;" + (string.IsNullOrEmpty(street) ? string.Empty : (street + " ")) + (string.IsNullOrEmpty(houseNumber) ? string.Empty : houseNumber) + ";" +
              (string.IsNullOrEmpty(city) ? string.Empty : city) + ";" + (string.IsNullOrEmpty(stateRegion) ? string.Empty : stateRegion) + ";" +
              (string.IsNullOrEmpty(zipCode) ? string.Empty : zipCode) + ";" + (string.IsNullOrEmpty(country) ? string.Empty : country) + "\r\n";
        vcard.Append(addressV);

        if (birthday.HasValue) vcard.Append("BDAY:").Append(birthday.Value.ToString("yyyyMMdd", CultureInfo.InvariantCulture)).Append("\r\n");
        if (!string.IsNullOrEmpty(website)) vcard.Append("URL:").Append(website).Append("\r\n");
        if (!string.IsNullOrEmpty(email)) vcard.Append("EMAIL:").Append(email).Append("\r\n");
        if (!string.IsNullOrEmpty(note)) vcard.Append("NOTE:").Append(note).Append("\r\n");
        if (outputType != QrContactOutputType.VCard21 && !string.IsNullOrEmpty(nickname)) {
            vcard.Append("NICKNAME:").Append(nickname).Append("\r\n");
        }
        vcard.Append("END:VCARD");
        return new QrPayloadData(vcard.ToString());
    }

    public static QrPayloadData SkypeCall(string username) {
        return new QrPayloadData("skype:" + (username ?? string.Empty) + "?call");
    }

    public static QrPayloadData AppStoreApple(string appId, string? countryCode = null) {
        return new QrPayloadData(QrPayload.AppStoreApple(appId, countryCode));
    }

    public static QrPayloadData AppStoreGooglePlay(string packageId) {
        return new QrPayloadData(QrPayload.AppStoreGooglePlay(packageId));
    }

    public static QrPayloadData FacebookProfile(string handleOrUrl) {
        return new QrPayloadData(QrPayload.FacebookProfile(handleOrUrl));
    }

    public static QrPayloadData TwitterProfile(string handleOrUrl) {
        return new QrPayloadData(QrPayload.TwitterProfile(handleOrUrl));
    }

    public static QrPayloadData XProfile(string handleOrUrl) {
        return new QrPayloadData(QrPayload.XProfile(handleOrUrl));
    }

    public static QrPayloadData TikTokProfile(string handleOrUrl) {
        return new QrPayloadData(QrPayload.TikTokProfile(handleOrUrl));
    }

    public static QrPayloadData LinkedInProfile(string handleOrUrl) {
        return new QrPayloadData(QrPayload.LinkedInProfile(handleOrUrl));
    }

    public static QrPayloadData LinkedInCompany(string handleOrUrl) {
        return new QrPayloadData(QrPayload.LinkedInCompany(handleOrUrl));
    }

    public static QrPayloadData Upi(
        string vpa,
        string? name = null,
        string? merchantCode = null,
        string? transactionRef = null,
        string? transactionNote = null,
        decimal? amount = null,
        string? currency = "INR") {
        return new QrPayloadData(QrPayload.Upi(vpa, name, merchantCode, transactionRef, transactionNote, amount, currency));
    }

    private static string NormalizeUrl(string url) {
        if (url is null) return string.Empty;
        if (url.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return url;
        return "http://" + url;
    }

    private static void AppendContactTel(StringBuilder sb, QrContactOutputType outputType, string? number, string type21, string type21Suffix, string type4) {
        if (string.IsNullOrEmpty(number)) return;
        sb.Append("TEL;");
        if (outputType == QrContactOutputType.VCard21) {
            sb.Append(type21).Append(';').Append(type21Suffix).Append(':').Append(number);
        } else if (outputType == QrContactOutputType.VCard3) {
            sb.Append("TYPE=").Append(type21).Append(',').Append(type21Suffix).Append(':').Append(number);
        } else {
            sb.Append("TYPE=").Append(type4).Append(";VALUE=uri:tel:").Append(number);
        }
        sb.Append("\r\n");
    }

    private static string GetAddressTypeString21(QrContactAddressType addressType) {
        return addressType switch {
            QrContactAddressType.Home => "HOME",
            QrContactAddressType.Work => "WORK",
            QrContactAddressType.HomePreferred => "HOME;PREF",
            QrContactAddressType.WorkPreferred => "WORK;PREF",
            _ => "HOME;PREF"
        };
    }

    private static string GetAddressTypeString3(QrContactAddressType addressType) {
        return addressType switch {
            QrContactAddressType.Home => "HOME",
            QrContactAddressType.Work => "WORK",
            QrContactAddressType.HomePreferred => "HOME,PREF",
            QrContactAddressType.WorkPreferred => "WORK,PREF",
            _ => "HOME,PREF"
        };
    }

    private static string GetAddressTypeString4(QrContactAddressType addressType) {
        return addressType switch {
            QrContactAddressType.Home => "home",
            QrContactAddressType.Work => "work",
            QrContactAddressType.HomePreferred => "home,pref",
            QrContactAddressType.WorkPreferred => "work,pref",
            _ => "home,pref"
        };
    }

    private static string BuildMailto(string address, string? subject, string? message) {
        var sb = new StringBuilder();
        sb.Append("mailto:");
        sb.Append(address ?? string.Empty);
        var hasQuery = false;
        if (!string.IsNullOrEmpty(subject)) {
            sb.Append(hasQuery ? '&' : '?');
            sb.Append("subject=");
            sb.Append(Uri.EscapeDataString(subject));
            hasQuery = true;
        }
        if (!string.IsNullOrEmpty(message)) {
            sb.Append(hasQuery ? '&' : '?');
            sb.Append("body=");
            sb.Append(Uri.EscapeDataString(message));
        }
        return sb.ToString();
    }

    private static string EscapeInput(string inp, bool simple = false) {
        if (inp is null) return string.Empty;
        char[] chars = simple ? new[] { ':' } : new[] { '\\', ';', ',', ':' };
        foreach (var c in chars) {
            inp = inp.Replace(c.ToString(), "\\" + c);
        }
        return inp;
    }

    private static bool IsHexStyle(string inp) {
        if (string.IsNullOrEmpty(inp)) return false;
        if (!Regex.IsMatch(inp, "\\A\\b[0-9a-fA-F]+\\b\\Z")) {
            return Regex.IsMatch(inp, "\\A\\b(0[xX])?[0-9a-fA-F]+\\b\\Z");
        }
        return true;
    }
}

#pragma warning restore CS1591