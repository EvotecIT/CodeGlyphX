using System;
using System.Text;
using CodeMatrix.Internal;

namespace CodeMatrix.Payloads;

public static class QrPayload {
    public static string Text(string text) => text ?? throw new ArgumentNullException(nameof(text));

    public static string Url(string url) {
        if (url is null) throw new ArgumentNullException(nameof(url));
        return url.Trim();
    }

    public static string Email(string address, string? subject = null, string? body = null) {
        if (address is null) throw new ArgumentNullException(nameof(address));

        var sb = new StringBuilder();
        sb.Append("mailto:");
        sb.Append(address.Trim());

        var hasQuery = false;
        if (!string.IsNullOrEmpty(subject)) {
            sb.Append(hasQuery ? '&' : '?');
            sb.Append("subject=");
            PercentEncoding.AppendEscaped(sb, subject);
            hasQuery = true;
        }
        if (!string.IsNullOrEmpty(body)) {
            sb.Append(hasQuery ? '&' : '?');
            sb.Append("body=");
            PercentEncoding.AppendEscaped(sb, body);
        }

        return sb.ToString();
    }

    public static string Phone(string number) {
        if (number is null) throw new ArgumentNullException(nameof(number));
        return "tel:" + number.Trim();
    }

    public static string Sms(string number, string? body = null) {
        if (number is null) throw new ArgumentNullException(nameof(number));
        var sb = new StringBuilder();
        sb.Append("sms:");
        sb.Append(number.Trim());
        if (!string.IsNullOrEmpty(body)) {
            sb.Append("?body=");
            PercentEncoding.AppendEscaped(sb, body);
        }
        return sb.ToString();
    }

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

        if (!string.IsNullOrWhiteSpace(organization)) {
            sb.Append("ORG:");
            sb.Append(EscapeVCardText(organization));
            sb.Append("\r\n");
        }
        if (!string.IsNullOrWhiteSpace(phone)) {
            sb.Append("TEL;TYPE=CELL:");
            sb.Append(EscapeVCardText(phone));
            sb.Append("\r\n");
        }
        if (!string.IsNullOrWhiteSpace(email)) {
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

