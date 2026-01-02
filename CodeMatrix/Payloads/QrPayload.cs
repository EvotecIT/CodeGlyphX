using System;
using System.Text;
using CodeMatrix.Internal;

namespace CodeMatrix.Payloads;

/// <summary>
/// Helpers for building common QR payload strings (URLs, Wi‑Fi, vCard, and basic URI schemes).
/// </summary>
/// <remarks>
/// The returned strings can be passed directly to <see cref="QrCodeEncoder.EncodeText(string, QrErrorCorrectionLevel, int, int, int?)"/>.
/// </remarks>
public static class QrPayload {
    /// <summary>
    /// Returns the text as-is (after a null check).
    /// </summary>
    public static string Text(string text) => text ?? throw new ArgumentNullException(nameof(text));

    /// <summary>
    /// Builds a URL payload (trimmed).
    /// </summary>
    public static string Url(string url) {
        if (url is null) throw new ArgumentNullException(nameof(url));
        return url.Trim();
    }

    /// <summary>
    /// Builds a <c>mailto:</c> URI payload.
    /// </summary>
    /// <remarks>
    /// Query parameters are percent-encoded.
    /// </remarks>
    public static string Email(string address, string? subject = null, string? body = null) {
        if (address is null) throw new ArgumentNullException(nameof(address));

        var sb = new StringBuilder();
        sb.Append("mailto:");
        sb.Append(address.Trim());

        var hasQuery = false;
        if (subject is { Length: > 0 }) {
            sb.Append(hasQuery ? '&' : '?');
            sb.Append("subject=");
            PercentEncoding.AppendEscaped(sb, subject);
            hasQuery = true;
        }
        if (body is { Length: > 0 }) {
            sb.Append(hasQuery ? '&' : '?');
            sb.Append("body=");
            PercentEncoding.AppendEscaped(sb, body);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Builds a <c>tel:</c> URI payload.
    /// </summary>
    public static string Phone(string number) {
        if (number is null) throw new ArgumentNullException(nameof(number));
        return "tel:" + number.Trim();
    }

    /// <summary>
    /// Builds an <c>sms:</c> URI payload.
    /// </summary>
    public static string Sms(string number, string? body = null) {
        if (number is null) throw new ArgumentNullException(nameof(number));
        var sb = new StringBuilder();
        sb.Append("sms:");
        sb.Append(number.Trim());
        if (body is { Length: > 0 }) {
            sb.Append("?body=");
            PercentEncoding.AppendEscaped(sb, body);
        }
        return sb.ToString();
    }

    /// <summary>
    /// Builds a Wi‑Fi QR payload (common <c>WIFI:...</c> format).
    /// </summary>
    public static string Wifi(string ssid, string password, string authType = "WPA", bool hidden = false) {
        if (ssid is null) throw new ArgumentNullException(nameof(ssid));
        if (password is null) throw new ArgumentNullException(nameof(password));
        if (authType is null) throw new ArgumentNullException(nameof(authType));

        var sb = new StringBuilder();
        sb.Append("WIFI:");
        sb.Append("T:");
        sb.Append(EscapeWifiValue(authType));
        sb.Append(';');
        sb.Append("S:");
        sb.Append(EscapeWifiValue(ssid));
        sb.Append(';');
        sb.Append("P:");
        sb.Append(EscapeWifiValue(password));
        sb.Append(';');
        if (hidden) {
            sb.Append("H:true;");
        }
        sb.Append(';');
        return sb.ToString();
    }

    /// <summary>
    /// Builds a minimal vCard 3.0 payload (name + optional fields).
    /// </summary>
    public static string VCard(string firstName, string lastName, string? phone = null, string? email = null, string? organization = null) {
        if (firstName is null) throw new ArgumentNullException(nameof(firstName));
        if (lastName is null) throw new ArgumentNullException(nameof(lastName));

        var fn = (firstName + " " + lastName).Trim();
        var sb = new StringBuilder();
        sb.Append("BEGIN:VCARD\r\n");
        sb.Append("VERSION:3.0\r\n");
        sb.Append("N:");
        sb.Append(EscapeVCardText(lastName));
        sb.Append(';');
        sb.Append(EscapeVCardText(firstName));
        sb.Append(";;;\r\n");
        sb.Append("FN:");
        sb.Append(EscapeVCardText(fn));
        sb.Append("\r\n");

        if (organization is not null && HasNonWhitespace(organization)) {
            sb.Append("ORG:");
            sb.Append(EscapeVCardText(organization));
            sb.Append("\r\n");
        }
        if (phone is not null && HasNonWhitespace(phone)) {
            sb.Append("TEL;TYPE=CELL:");
            sb.Append(EscapeVCardText(phone));
            sb.Append("\r\n");
        }
        if (email is not null && HasNonWhitespace(email)) {
            sb.Append("EMAIL:");
            sb.Append(EscapeVCardText(email));
            sb.Append("\r\n");
        }

        sb.Append("END:VCARD");
        return sb.ToString();
    }

    private static string EscapeWifiValue(string value) {
        var sb = new StringBuilder(value.Length);
        for (var i = 0; i < value.Length; i++) {
            var c = value[i];
            if (c is '\\' or ';' or ',' or ':') sb.Append('\\');
            sb.Append(c);
        }
        return sb.ToString();
    }

    private static bool HasNonWhitespace(string value) {
        for (var i = 0; i < value.Length; i++) {
            if (!char.IsWhiteSpace(value[i])) return true;
        }
        return false;
    }

    private static string EscapeVCardText(string value) {
        var sb = new StringBuilder(value.Length);
        for (var i = 0; i < value.Length; i++) {
            var c = value[i];
            switch (c) {
                case '\\':
                    sb.Append(@"\\");
                    break;
                case ';':
                    sb.Append(@"\;");
                    break;
                case ',':
                    sb.Append(@"\,");
                    break;
                case '\r':
                    break;
                case '\n':
                    sb.Append(@"\n");
                    break;
                default:
                    sb.Append(c);
                    break;
            }
        }
        return sb.ToString();
    }
}
