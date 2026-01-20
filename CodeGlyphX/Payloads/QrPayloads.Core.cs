using System;
using System.Globalization;
using System.Text;
using CodeGlyphX.Internal;

namespace CodeGlyphX.Payloads;

/// <summary>
/// Helpers for building common QR payloads with recommended defaults.
/// </summary>
/// <example>
/// <code>
/// using CodeGlyphX;
/// using CodeGlyphX.Payloads;
/// QR.Save(QrPayloads.Wifi("MyNetwork", "Password123"), "wifi.png");
/// </code>
/// </example>
public static partial class QrPayloads {
    /// <summary>
    /// Builds a plain text payload.
    /// </summary>
    public static QrPayloadData Text(string text) => new(QrPayload.Text(text));

    /// <summary>
    /// Builds a URL payload.
    /// </summary>
    public static QrPayloadData Url(string url) {
        var normalized = NormalizeUrl(url);
        if (!QrPayloadValidation.IsValidUrl(normalized)) throw new ArgumentException("URL is invalid.", nameof(url));
        return new QrPayloadData(normalized);
    }

    /// <summary>
    /// Builds a bookmark payload (MEBKM).
    /// </summary>
    public static QrPayloadData Bookmark(string url, string title) {
        if (string.IsNullOrWhiteSpace(url)) throw new ArgumentException("URL must not be empty.", nameof(url));
        var normalized = NormalizeUrl(url);
        if (!QrPayloadValidation.IsValidUrl(normalized)) throw new ArgumentException("URL is invalid.", nameof(url));
        var safeUrl = EscapeInput(normalized);
        var safeTitle = EscapeInput(title ?? string.Empty);
        var payload = string.IsNullOrEmpty(safeTitle)
            ? "MEBKM:URL:" + safeUrl + ";;"
            : "MEBKM:TITLE:" + safeTitle + ";URL:" + safeUrl + ";;";
        return new QrPayloadData(payload);
    }

    /// <summary>
    /// Builds a WhatsApp message payload.
    /// </summary>
    public static QrPayloadData WhatsAppMessage(string message, string? number = null) {
        var sanitized = string.IsNullOrEmpty(number)
            ? string.Empty
            : RegexCache.WhatsappSanitize().Replace(number, string.Empty);
        if (!string.IsNullOrEmpty(number) && string.IsNullOrEmpty(sanitized)) {
            throw new ArgumentException("WhatsApp number is invalid.", nameof(number));
        }
        var payload = "https://wa.me/" + sanitized + "?text=" + Uri.EscapeDataString(message ?? string.Empty);
        return new QrPayloadData(payload);
    }

    /// <summary>
    /// Builds a Wi-Fi payload.
    /// </summary>
    public static QrPayloadData Wifi(string ssid, string password, string authType = "WPA", bool hidden = false, bool escapeHexStrings = true) {
        if (string.IsNullOrWhiteSpace(ssid)) throw new ArgumentException("SSID must not be empty.", nameof(ssid));
        if (!QrPayloadValidation.IsValidWifiAuth(authType)) throw new ArgumentException("WiFi auth type is invalid.", nameof(authType));
        var safeSsid = EscapeInput(ssid ?? string.Empty);
        var safePassword = EscapeInput(password ?? string.Empty);
        if (escapeHexStrings && IsHexStyle(safeSsid)) safeSsid = "\"" + safeSsid + "\"";
        if (escapeHexStrings && IsHexStyle(safePassword)) safePassword = "\"" + safePassword + "\"";
        var payload = "WIFI:T:" + EscapeInput(authType ?? string.Empty) + ";S:" + safeSsid + ";P:" + safePassword + ";" + (hidden ? "H:true" : string.Empty) + ";";
        return new QrPayloadData(payload);
    }

    /// <summary>
    /// Builds an email payload.
    /// </summary>
    public static QrPayloadData Email(string address, string? subject = null, string? message = null, QrMailEncoding encoding = QrMailEncoding.Mailto) {
        if (!QrPayloadValidation.IsValidEmail(address)) throw new ArgumentException("Email address is invalid.", nameof(address));
        var payload = encoding switch {
            QrMailEncoding.Mailto => BuildMailto(address, subject, message),
            QrMailEncoding.Matmsg => "MATMSG:TO:" + (address ?? string.Empty) + ";SUB:" + EscapeInput(subject ?? string.Empty) + ";BODY:" + EscapeInput(message ?? string.Empty) + ";;",
            QrMailEncoding.Smtp => "SMTP:" + (address ?? string.Empty) + ":" + EscapeInput(subject ?? string.Empty, simple: true) + ":" + EscapeInput(message ?? string.Empty, simple: true),
            _ => string.Empty
        };
        return new QrPayloadData(payload);
    }

    /// <summary>
    /// Builds an MMS payload.
    /// </summary>
    public static QrPayloadData Mms(string number, string? subject = null, QrMmsEncoding encoding = QrMmsEncoding.Mms) {
        if (!QrPayloadValidation.IsValidPhone(number, 5)) throw new ArgumentException("MMS number is invalid.", nameof(number));
        var payload = encoding switch {
            QrMmsEncoding.Mmsto => "mmsto:" + number + (string.IsNullOrEmpty(subject) ? string.Empty : ("?subject=" + Uri.EscapeDataString(subject))),
            QrMmsEncoding.Mms => "mms:" + number + (string.IsNullOrEmpty(subject) ? string.Empty : ("?body=" + Uri.EscapeDataString(subject))),
            _ => string.Empty
        };
        return new QrPayloadData(payload);
    }

    /// <summary>
    /// Builds an SMS payload.
    /// </summary>
    public static QrPayloadData Sms(string number, string? message = null, QrSmsEncoding encoding = QrSmsEncoding.Sms) {
        if (!QrPayloadValidation.IsValidPhone(number, 5)) throw new ArgumentException("SMS number is invalid.", nameof(number));
        var payload = encoding switch {
            QrSmsEncoding.Sms => "sms:" + number + (string.IsNullOrEmpty(message) ? string.Empty : ("?body=" + Uri.EscapeDataString(message))),
            QrSmsEncoding.SmsIos => "sms:" + number + (string.IsNullOrEmpty(message) ? string.Empty : (";body=" + Uri.EscapeDataString(message))),
            QrSmsEncoding.Smsto => "SMSTO:" + number + ":" + (message ?? string.Empty),
            _ => string.Empty
        };
        return new QrPayloadData(payload);
    }

    /// <summary>
    /// Builds a phone payload (tel:).
    /// </summary>
    public static QrPayloadData Phone(string number) {
        if (!QrPayloadValidation.IsValidPhone(number, 5)) throw new ArgumentException("Phone number is invalid.", nameof(number));
        return new QrPayloadData("tel:" + (number ?? string.Empty));
    }

    /// <summary>
    /// Builds a geolocation payload.
    /// </summary>
    public static QrPayloadData Geo(string latitude, string longitude, QrGeolocationEncoding encoding = QrGeolocationEncoding.Geo) {
        if (!TryParseGeo(latitude, longitude, out var lat, out var lon)) {
            throw new ArgumentException("Latitude/longitude are invalid.");
        }
        var latText = lat.ToString(CultureInfo.InvariantCulture);
        var lonText = lon.ToString(CultureInfo.InvariantCulture);
        var payload = encoding == QrGeolocationEncoding.GoogleMaps
            ? "http://maps.google.com/maps?q=" + latText + "," + lonText
            : "geo:" + latText + "," + lonText;
        return new QrPayloadData(payload);
    }

    /// <summary>
    /// Builds a calendar event payload.
    /// </summary>
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

    /// <summary>
    /// Builds a contact payload (MeCard or vCard).
    /// </summary>
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

    /// <summary>
    /// Builds a Skype call payload.
    /// </summary>
    public static QrPayloadData SkypeCall(string username) {
        return new QrPayloadData("skype:" + (username ?? string.Empty) + "?call");
    }

    /// <summary>
    /// Builds an Apple App Store payload.
    /// </summary>
    public static QrPayloadData AppStoreApple(string appId, string? countryCode = null) {
        return new QrPayloadData(QrPayload.AppStoreApple(appId, countryCode));
    }

    /// <summary>
    /// Builds a Google Play payload.
    /// </summary>
    public static QrPayloadData AppStoreGooglePlay(string packageId) {
        return new QrPayloadData(QrPayload.AppStoreGooglePlay(packageId));
    }

    /// <summary>
    /// Builds a Facebook profile payload.
    /// </summary>
    public static QrPayloadData FacebookProfile(string handleOrUrl) {
        return new QrPayloadData(QrPayload.FacebookProfile(handleOrUrl));
    }

    /// <summary>
    /// Builds a Twitter profile payload.
    /// </summary>
    public static QrPayloadData TwitterProfile(string handleOrUrl) {
        return new QrPayloadData(QrPayload.TwitterProfile(handleOrUrl));
    }

    /// <summary>
    /// Builds an X profile payload.
    /// </summary>
    public static QrPayloadData XProfile(string handleOrUrl) {
        return new QrPayloadData(QrPayload.XProfile(handleOrUrl));
    }

    /// <summary>
    /// Builds a TikTok profile payload.
    /// </summary>
    public static QrPayloadData TikTokProfile(string handleOrUrl) {
        return new QrPayloadData(QrPayload.TikTokProfile(handleOrUrl));
    }

    /// <summary>
    /// Builds a LinkedIn profile payload.
    /// </summary>
    public static QrPayloadData LinkedInProfile(string handleOrUrl) {
        return new QrPayloadData(QrPayload.LinkedInProfile(handleOrUrl));
    }

    /// <summary>
    /// Builds a LinkedIn company payload.
    /// </summary>
    public static QrPayloadData LinkedInCompany(string handleOrUrl) {
        return new QrPayloadData(QrPayload.LinkedInCompany(handleOrUrl));
    }

    /// <summary>
    /// Builds a UPI payment payload.
    /// </summary>
    public static QrPayloadData Upi(
        string vpa,
        string? name = null,
        string? merchantCode = null,
        string? transactionRef = null,
        string? transactionNote = null,
        decimal? amount = null,
        string? currency = "INR") {
        if (!QrPayloadValidation.IsValidUpiVpa(vpa)) throw new ArgumentException("UPI VPA is invalid.", nameof(vpa));
        return new QrPayloadData(QrPayload.Upi(vpa, name, merchantCode, transactionRef, transactionNote, amount, currency));
    }

    private static string NormalizeUrl(string url) {
        if (url is null) return string.Empty;
        var trimmed = url.Trim();
        if (trimmed.Length == 0) return string.Empty;
        if (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) {
            return trimmed;
        }
        return "http://" + trimmed;
    }

    private static bool TryParseGeo(string latitude, string longitude, out double lat, out double lon) {
        lat = 0;
        lon = 0;
        if (!double.TryParse(latitude?.Trim().Replace(",", "."), NumberStyles.Float, CultureInfo.InvariantCulture, out lat)) return false;
        if (!double.TryParse(longitude?.Trim().Replace(",", "."), NumberStyles.Float, CultureInfo.InvariantCulture, out lon)) return false;
        if (lat < -90 || lat > 90) return false;
        if (lon < -180 || lon > 180) return false;
        return true;
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
        if (!RegexCache.HexPlain().IsMatch(inp)) {
            return RegexCache.HexWithPrefix().IsMatch(inp);
        }
        return true;
    }
}
