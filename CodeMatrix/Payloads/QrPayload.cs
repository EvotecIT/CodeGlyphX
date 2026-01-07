using System;
using System.Collections.Generic;
using System.Globalization;
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
    /// Builds a Geo URI payload (<c>geo:lat,lon</c> or <c>geo:lat,lon,alt</c>).
    /// </summary>
    public static string Geo(double latitude, double longitude, double? altitude = null) {
        var sb = new StringBuilder();
        sb.Append("geo:");
        sb.Append(FormatGeo(latitude));
        sb.Append(',');
        sb.Append(FormatGeo(longitude));
        if (altitude.HasValue) {
            sb.Append(',');
            sb.Append(FormatGeo(altitude.Value));
        }
        return sb.ToString();
    }

    /// <summary>
    /// Alias for <see cref="Geo"/>.
    /// </summary>
    public static string Location(double latitude, double longitude, double? altitude = null) {
        return Geo(latitude, longitude, altitude);
    }

    /// <summary>
    /// Builds a MECARD payload (compact contact format).
    /// </summary>
    public static string MeCard(
        string firstName,
        string lastName,
        string? phone = null,
        string? email = null,
        string? url = null,
        string? address = null,
        string? note = null,
        string? organization = null) {
        if (firstName is null) throw new ArgumentNullException(nameof(firstName));
        if (lastName is null) throw new ArgumentNullException(nameof(lastName));

        var sb = new StringBuilder();
        sb.Append("MECARD:");
        sb.Append("N:");
        sb.Append(EscapeMecardValue(lastName));
        if (HasNonWhitespace(firstName)) {
            sb.Append(',');
            sb.Append(EscapeMecardValue(firstName));
        }
        sb.Append(';');

        AppendMecardField(sb, "TEL", phone);
        AppendMecardField(sb, "EMAIL", email);
        AppendMecardField(sb, "URL", url);
        AppendMecardField(sb, "ADR", address);
        AppendMecardField(sb, "NOTE", note);
        AppendMecardField(sb, "ORG", organization);

        sb.Append(';');
        return sb.ToString();
    }

    /// <summary>
    /// Builds a minimal iCalendar event payload (VEVENT).
    /// </summary>
    public static string CalendarEvent(
        string summary,
        DateTime start,
        DateTime? end = null,
        string? location = null,
        string? description = null,
        string? organizer = null,
        string? uid = null,
        bool allDay = false,
        string? timeZoneId = null,
        int? alarmMinutesBefore = null,
        string? alarmDescription = null) {
        if (summary is null) throw new ArgumentNullException(nameof(summary));
        if (alarmMinutesBefore is < 0) throw new ArgumentOutOfRangeException(nameof(alarmMinutesBefore));

        var useTimeZone = !allDay && timeZoneId is not null && HasNonWhitespace(timeZoneId);
        var sb = new StringBuilder();
        AppendICalLine(sb, "BEGIN:VCALENDAR");
        AppendICalLine(sb, "VERSION:2.0");
        AppendICalLine(sb, "BEGIN:VEVENT");

        if (uid is not null && HasNonWhitespace(uid)) {
            AppendICalLine(sb, "UID:" + EscapeICalText(uid));
        }

        AppendICalLine(sb, "SUMMARY:" + EscapeICalText(summary));
        AppendICalLine(sb, BuildICalDateLine("DTSTART", start, allDay, useTimeZone, timeZoneId));

        if (end.HasValue) {
            AppendICalLine(sb, BuildICalDateLine("DTEND", end.Value, allDay, useTimeZone, timeZoneId));
        }

        if (location is not null && HasNonWhitespace(location)) {
            AppendICalLine(sb, "LOCATION:" + EscapeICalText(location));
        }

        if (description is not null && HasNonWhitespace(description)) {
            AppendICalLine(sb, "DESCRIPTION:" + EscapeICalText(description));
        }

        if (organizer is not null && HasNonWhitespace(organizer)) {
            AppendICalLine(sb, "ORGANIZER:" + EscapeICalText(organizer));
        }

        if (alarmMinutesBefore.HasValue) {
            AppendICalLine(sb, "BEGIN:VALARM");
            AppendICalLine(sb, "TRIGGER:-PT" + alarmMinutesBefore.Value + "M");
            AppendICalLine(sb, "ACTION:DISPLAY");
            var alarmText = alarmDescription is null || !HasNonWhitespace(alarmDescription)
                ? "Reminder"
                : alarmDescription;
            AppendICalLine(sb, "DESCRIPTION:" + EscapeICalText(alarmText));
            AppendICalLine(sb, "END:VALARM");
        }

        AppendICalLine(sb, "END:VEVENT");
        AppendICalLine(sb, "END:VCALENDAR");

        if (sb.Length >= 2) sb.Length -= 2;

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

    /// <summary>
    /// Builds a vCard 4.0 payload with optional multi-value fields.
    /// </summary>
    public static string VCard4(
        string firstName,
        string lastName,
        IEnumerable<string>? phones = null,
        IEnumerable<string>? emails = null,
        string? organization = null,
        string? title = null,
        string? url = null,
        string? address = null,
        string? note = null,
        string? birthday = null,
        string? photoUri = null,
        string? logoUri = null) {
        if (firstName is null) throw new ArgumentNullException(nameof(firstName));
        if (lastName is null) throw new ArgumentNullException(nameof(lastName));

        var fn = (firstName + " " + lastName).Trim();
        var sb = new StringBuilder();
        sb.Append("BEGIN:VCARD\r\n");
        sb.Append("VERSION:4.0\r\n");
        sb.Append("N:");
        sb.Append(EscapeVCardText(lastName));
        sb.Append(';');
        sb.Append(EscapeVCardText(firstName));
        sb.Append(";;;\r\n");
        sb.Append("FN:");
        sb.Append(EscapeVCardText(fn));
        sb.Append("\r\n");

        AppendVCardFields(sb, "TEL", phones);
        AppendVCardFields(sb, "EMAIL", emails);

        if (organization is not null && HasNonWhitespace(organization)) {
            sb.Append("ORG:");
            sb.Append(EscapeVCardText(organization));
            sb.Append("\r\n");
        }
        if (title is not null && HasNonWhitespace(title)) {
            sb.Append("TITLE:");
            sb.Append(EscapeVCardText(title));
            sb.Append("\r\n");
        }
        if (url is not null && HasNonWhitespace(url)) {
            sb.Append("URL:");
            sb.Append(EscapeVCardText(url));
            sb.Append("\r\n");
        }
        if (address is not null && HasNonWhitespace(address)) {
            sb.Append("ADR:");
            sb.Append(EscapeVCardAdrValue(address));
            sb.Append("\r\n");
        }
        if (note is not null && HasNonWhitespace(note)) {
            sb.Append("NOTE:");
            sb.Append(EscapeVCardText(note));
            sb.Append("\r\n");
        }
        if (birthday is not null && HasNonWhitespace(birthday)) {
            sb.Append("BDAY:");
            sb.Append(EscapeVCardText(birthday));
            sb.Append("\r\n");
        }
        if (photoUri is not null && HasNonWhitespace(photoUri)) {
            sb.Append("PHOTO:");
            sb.Append(EscapeVCardText(photoUri));
            sb.Append("\r\n");
        }
        if (logoUri is not null && HasNonWhitespace(logoUri)) {
            sb.Append("LOGO:");
            sb.Append(EscapeVCardText(logoUri));
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

    private static string FormatGeo(double value) {
        return value.ToString("0.##########", CultureInfo.InvariantCulture);
    }

    private static void AppendMecardField(StringBuilder sb, string key, string? value) {
        if (value is null || !HasNonWhitespace(value)) return;
        sb.Append(key);
        sb.Append(':');
        sb.Append(EscapeMecardValue(value));
        sb.Append(';');
    }

    private static string EscapeMecardValue(string value) {
        var sb = new StringBuilder(value.Length);
        for (var i = 0; i < value.Length; i++) {
            var c = value[i];
            if (c is '\\' or ';' or ',' or ':') sb.Append('\\');
            sb.Append(c);
        }
        return sb.ToString();
    }

    private static void AppendVCardFields(StringBuilder sb, string key, IEnumerable<string>? values) {
        if (values is null) return;
        foreach (var value in values) {
            if (value is null || !HasNonWhitespace(value)) continue;
            sb.Append(key);
            sb.Append(':');
            sb.Append(EscapeVCardText(value));
            sb.Append("\r\n");
        }
    }

    private static string EscapeVCardAdrValue(string value) {
        var sb = new StringBuilder(value.Length);
        for (var i = 0; i < value.Length; i++) {
            var c = value[i];
            switch (c) {
                case '\\':
                    sb.Append(@"\\");
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

    private static string FormatICalDate(DateTime value, bool allDay, bool allowUtcSuffix) {
        if (allDay) {
            return value.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        }

        var suffix = allowUtcSuffix && value.Kind == DateTimeKind.Utc ? "Z" : string.Empty;
        return value.ToString("yyyyMMdd'T'HHmmss", CultureInfo.InvariantCulture) + suffix;
    }

    private static string EscapeICalText(string value) {
        var sb = new StringBuilder(value.Length);
        for (var i = 0; i < value.Length; i++) {
            var c = value[i];
            switch (c) {
                case '\\':
                    sb.Append("\\\\");
                    break;
                case ';':
                    sb.Append("\\;");
                    break;
                case ',':
                    sb.Append("\\,");
                    break;
                case '\r':
                    break;
                case '\n':
                    sb.Append("\\n");
                    break;
                default:
                    sb.Append(c);
                    break;
            }
        }
        return sb.ToString();
    }

    private static string BuildICalDateLine(
        string key,
        DateTime value,
        bool allDay,
        bool useTimeZone,
        string? timeZoneId) {
        if (useTimeZone) {
            return key + ";TZID=" + EscapeICalParamValue(timeZoneId!) + ":" + FormatICalDate(value, allDay: false, allowUtcSuffix: false);
        }
        return key + ":" + FormatICalDate(value, allDay, allowUtcSuffix: true);
    }

    private static string EscapeICalParamValue(string value) {
        var sb = new StringBuilder(value.Length);
        for (var i = 0; i < value.Length; i++) {
            var c = value[i];
            if (c is '\\' or ';' or ',') sb.Append('\\');
            sb.Append(c);
        }
        return sb.ToString();
    }

    private static void AppendICalLine(StringBuilder sb, string line) {
        const int maxLen = 75;
        if (line.Length <= maxLen) {
            sb.Append(line);
            sb.Append("\r\n");
            return;
        }

        var index = 0;
        while (index < line.Length) {
            var take = Math.Min(maxLen, line.Length - index);
            sb.Append(line, index, take);
            index += take;
            if (index < line.Length) {
                sb.Append("\r\n ");
            } else {
                sb.Append("\r\n");
            }
        }
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
