using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using CodeGlyphX.Internal;

namespace CodeGlyphX.Payloads;

/// <summary>
/// Parses QR payload strings into structured data when possible.
/// </summary>
public static class QrPayloadParser {
    /// <summary>
    /// Attempts to parse the payload into a structured representation.
    /// </summary>
    public static bool TryParse(string payload, out QrParsedPayload parsed) {
        parsed = null!;
        if (payload is null) return false;
        var raw = payload.Trim();
        if (raw.Length == 0) return false;

        if (TryParseWifi(raw, out var wifi)) {
            parsed = new QrParsedPayload(QrPayloadType.Wifi, raw, wifi);
            return true;
        }

        if (TryParseMailto(raw, out var email)) {
            parsed = new QrParsedPayload(QrPayloadType.Email, raw, email);
            return true;
        }

        if (TryParseSms(raw, out var sms)) {
            parsed = new QrParsedPayload(QrPayloadType.Sms, raw, sms);
            return true;
        }

        if (TryParseTel(raw, out var phone)) {
            parsed = new QrParsedPayload(QrPayloadType.Phone, raw, phone);
            return true;
        }

        if (TryParseUpi(raw, out var upi)) {
            parsed = new QrParsedPayload(QrPayloadType.Upi, raw, upi);
            return true;
        }

        if (TryParseGeo(raw, out var geo)) {
            parsed = new QrParsedPayload(QrPayloadType.Geo, raw, geo);
            return true;
        }

        if (TryParseMeCard(raw, out var mecard)) {
            parsed = new QrParsedPayload(QrPayloadType.MeCard, raw, mecard);
            return true;
        }

        if (TryParseVCard(raw, out var vcard)) {
            parsed = new QrParsedPayload(QrPayloadType.VCard, raw, vcard);
            return true;
        }

        if (TryParseCalendar(raw, out var calendar)) {
            parsed = new QrParsedPayload(QrPayloadType.Calendar, raw, calendar);
            return true;
        }

        if (TryParseOtp(raw, out var otp)) {
            parsed = new QrParsedPayload(QrPayloadType.Otp, raw, otp);
            return true;
        }

        if (TryParseAppStore(raw, out var app)) {
            parsed = new QrParsedPayload(QrPayloadType.AppStore, raw, app);
            return true;
        }

        if (TryParseSocial(raw, out var social)) {
            parsed = new QrParsedPayload(QrPayloadType.Social, raw, social);
            return true;
        }

        if (TryParseBookmark(raw, out var bookmark)) {
            parsed = new QrParsedPayload(QrPayloadType.Bookmark, raw, bookmark);
            return true;
        }

        if (TryParsePayPal(raw, out var paypal)) {
            parsed = new QrParsedPayload(QrPayloadType.PayPal, raw, paypal);
            return true;
        }

        if (LooksLikeUrl(raw)) {
            parsed = new QrParsedPayload(QrPayloadType.Url, raw, raw);
            return true;
        }

        parsed = new QrParsedPayload(QrPayloadType.Text, raw, raw);
        return true;
    }

    /// <summary>
    /// Attempts to parse the payload with optional strict validation.
    /// </summary>
    public static bool TryParse(string payload, QrPayloadParseOptions? options, out QrParsedPayload parsed, out QrPayloadValidationResult validation) {
        validation = new QrPayloadValidationResult();
        if (!TryParse(payload, out parsed)) return false;
        if (options?.Strict == true) {
            ValidateParsed(parsed, options, validation);
            return validation.IsValid;
        }
        return true;
    }

    /// <summary>
    /// Validates a parsed payload against strict schema rules.
    /// </summary>
    public static QrPayloadValidationResult Validate(QrParsedPayload parsed, QrPayloadParseOptions? options = null) {
        var result = new QrPayloadValidationResult();
        ValidateParsed(parsed, options ?? new QrPayloadParseOptions { Strict = true }, result);
        return result;
    }

    /// <summary>
    /// Parses payload, returning raw text when structure is not recognized.
    /// </summary>
    public static QrParsedPayload Parse(string payload) {
        if (!TryParse(payload, out var parsed)) {
            throw new FormatException("Invalid QR payload.");
        }
        return parsed;
    }

    private static bool TryParseWifi(string raw, out QrParsedData.Wifi wifi) {
        wifi = null!;
        if (!raw.StartsWith("WIFI:", StringComparison.OrdinalIgnoreCase)) return false;

        return TryParseWifi(raw.AsSpan(5), out wifi);
    }

    private static bool TryParseBookmark(string raw, out QrParsedData.Bookmark bookmark) {
        bookmark = null!;
        if (!raw.StartsWith("MEBKM:", StringComparison.OrdinalIgnoreCase)) return false;
        return TryParseBookmark(raw.AsSpan(6), out bookmark);
    }

    private static bool TryParseWifi(ReadOnlySpan<char> body, out QrParsedData.Wifi wifi) {
        wifi = null!;
        if (!TryReadFieldValue(body, "S", out var ssid) || ssid.Length == 0) return false;
        TryReadFieldValue(body, "P", out var password);
        TryReadFieldValue(body, "T", out var auth);
        TryReadFieldValue(body, "H", out var hiddenText);

        var hidden = hiddenText.Length > 0 && hiddenText.Equals("true".AsSpan(), StringComparison.OrdinalIgnoreCase);
        wifi = new QrParsedData.Wifi(
            UnescapeField(ssid),
            password.Length == 0 ? null : UnescapeField(password),
            auth.Length == 0 ? string.Empty : UnescapeField(auth),
            hidden);
        return true;
    }

    private static bool TryParseBookmark(ReadOnlySpan<char> body, out QrParsedData.Bookmark bookmark) {
        bookmark = null!;
        if (!TryReadFieldValue(body, "URL", out var url) || url.Length == 0) return false;
        TryReadFieldValue(body, "TITLE", out var title);
        var urlText = UnescapeField(url);
        var titleText = title.Length == 0 ? null : UnescapeField(title);
        bookmark = new QrParsedData.Bookmark(urlText, titleText);
        return true;
    }

    private static bool TryParsePayPal(string raw, out QrParsedData.PayPal paypal) {
        paypal = null!;
        if (!TryNormalizePayPalUrl(raw, out var url, out var handle, out var amount, out var currency)) return false;
        paypal = new QrParsedData.PayPal(handle, amount, currency, url);
        return true;
    }

    private static bool TryReadFieldValue(ReadOnlySpan<char> body, string key, out ReadOnlySpan<char> value) {
        value = default;
        var search = key.AsSpan();
        var i = 0;
        while (i < body.Length) {
            var fieldStart = i;
            var escape = false;
            var sepIndex = -1;
            while (i < body.Length) {
                var ch = body[i];
                if (escape) {
                    escape = false;
                    i++;
                    continue;
                }
                if (ch == '\\') {
                    escape = true;
                    i++;
                    continue;
                }
                if (ch == ':') { sepIndex = i; i++; break; }
                if (ch == ';') { break; }
                i++;
            }

            if (sepIndex >= 0) {
                var fieldKey = body.Slice(fieldStart, sepIndex - fieldStart);
                if (fieldKey.Equals(search, StringComparison.OrdinalIgnoreCase)) {
                    var valueStart = i;
                    escape = false;
                    while (i < body.Length) {
                        var ch = body[i];
                        if (escape) { escape = false; i++; continue; }
                        if (ch == '\\') { escape = true; i++; continue; }
                        if (ch == ';') break;
                        i++;
                    }
                    value = body.Slice(valueStart, i - valueStart);
                    return true;
                }
            }

            while (i < body.Length && body[i] != ';') i++;
            if (i < body.Length && body[i] == ';') i++;
        }
        return false;
    }

    private static string UnescapeField(ReadOnlySpan<char> value) {
        if (value.Length == 0) return string.Empty;
        var hasEscape = false;
        for (var i = 0; i < value.Length; i++) {
            if (value[i] == '\\') { hasEscape = true; break; }
        }
        if (!hasEscape) return value.ToString();

        var sb = new StringBuilder(value.Length);
        var escape = false;
        for (var i = 0; i < value.Length; i++) {
            var ch = value[i];
            if (escape) {
                sb.Append(ch);
                escape = false;
                continue;
            }
            if (ch == '\\') {
                escape = true;
                continue;
            }
            sb.Append(ch);
        }
        return sb.ToString();
    }

    private static bool TryParseMailto(string raw, out QrParsedData.Email email) {
        email = null!;
        if (!raw.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase)) return false;
        var rest = raw.Substring(7);
        SplitOnce(rest, '?', out var address, out var query);
        string? subject = null;
        string? body = null;
        if (!string.IsNullOrEmpty(query)) {
            ParseQuery(query!, out subject, out body);
        }
        email = new QrParsedData.Email(address, subject, body);
        return true;
    }

    private static void ParseQuery(string query, out string? subject, out string? body) {
        subject = null;
        body = null;
        var pairs = query.Split('&');
        for (var i = 0; i < pairs.Length; i++) {
            var pair = pairs[i];
            var idx = pair.IndexOf('=');
            if (idx <= 0) continue;
            var key = pair.Substring(0, idx);
            var value = pair.Substring(idx + 1);
            if (!PercentEncoding.TryDecode(value, out var decoded)) continue;
            if (key.Equals("subject", StringComparison.OrdinalIgnoreCase)) subject = decoded;
            if (key.Equals("body", StringComparison.OrdinalIgnoreCase)) body = decoded;
        }
    }

    private static bool TryParseSms(string raw, out QrParsedData.Sms sms) {
        sms = null!;
        if (raw.StartsWith("SMSTO:", StringComparison.OrdinalIgnoreCase)) {
            var smstoRest = raw.Substring(6);
            SplitOnce(smstoRest, ':', out var smstoNumber, out var smstoBody);
            sms = new QrParsedData.Sms(smstoNumber, smstoBody);
            return true;
        }
        if (!raw.StartsWith("sms:", StringComparison.OrdinalIgnoreCase)) return false;
        var smsRest = raw.Substring(4);
        SplitOnce(smsRest, '?', out var number, out var smsQuery);
        string? body = null;
        if (!string.IsNullOrEmpty(smsQuery)) {
            ParseQuery(smsQuery!, out _, out body);
        }
        sms = new QrParsedData.Sms(number, body);
        return true;
    }

    private static bool TryParseTel(string raw, out QrParsedData.Phone phone) {
        phone = null!;
        if (!raw.StartsWith("tel:", StringComparison.OrdinalIgnoreCase)) return false;
        phone = new QrParsedData.Phone(raw.Substring(4));
        return true;
    }

    private static bool TryParseUpi(string raw, out QrParsedData.Upi upi) {
        upi = null!;
        if (!raw.StartsWith("upi://", StringComparison.OrdinalIgnoreCase)) return false;
        var idx = raw.IndexOf('?');
        if (idx < 0) return false;
        var query = raw.Substring(idx + 1);
        string? vpa = null;
        string? name = null;
        string? mc = null;
        string? tr = null;
        string? tn = null;
        decimal? amount = null;
        string? currency = null;

        var pairs = query.Split('&');
        for (var i = 0; i < pairs.Length; i++) {
            var pair = pairs[i];
            var eq = pair.IndexOf('=');
            if (eq <= 0) continue;
            var key = pair.Substring(0, eq);
            var value = pair.Substring(eq + 1);
            if (!PercentEncoding.TryDecode(value, out var decoded)) decoded = value;

            if (key.Equals("pa", StringComparison.OrdinalIgnoreCase)) vpa = decoded;
            else if (key.Equals("pn", StringComparison.OrdinalIgnoreCase)) name = decoded;
            else if (key.Equals("mc", StringComparison.OrdinalIgnoreCase)) mc = decoded;
            else if (key.Equals("tr", StringComparison.OrdinalIgnoreCase)) tr = decoded;
            else if (key.Equals("tn", StringComparison.OrdinalIgnoreCase)) tn = decoded;
            else if (key.Equals("am", StringComparison.OrdinalIgnoreCase) && decimal.TryParse(decoded, NumberStyles.Any, CultureInfo.InvariantCulture, out var v)) amount = v;
            else if (key.Equals("cu", StringComparison.OrdinalIgnoreCase)) currency = decoded;
        }

        if (string.IsNullOrEmpty(vpa)) return false;
        var vpaValue = vpa ?? string.Empty;
        upi = new QrParsedData.Upi(vpaValue, name, mc, tr, tn, amount, currency);
        return true;
    }

    private static bool TryParseGeo(string raw, out QrParsedData.Geo geo) {
        geo = null!;
        if (!raw.StartsWith("geo:", StringComparison.OrdinalIgnoreCase)) return false;
        var coords = raw.Substring(4).Split(',');
        if (coords.Length < 2) return false;
        if (!double.TryParse(coords[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var lat)) return false;
        if (!double.TryParse(coords[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var lon)) return false;
        double? alt = null;
        if (coords.Length > 2 && double.TryParse(coords[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var a)) alt = a;
        geo = new QrParsedData.Geo(lat, lon, alt);
        return true;
    }

    private static bool TryParseMeCard(string raw, out QrParsedData.MeCard mecard) {
        mecard = null!;
        if (!raw.StartsWith("MECARD:", StringComparison.OrdinalIgnoreCase)) return false;
        var body = raw.Substring(7);
        var fields = SplitMecardFields(body);
        if (!fields.TryGetValue("N", out var name) || string.IsNullOrEmpty(name)) return false;
        var parts = name.Split(',');
        var last = parts.Length > 0 ? parts[0] : string.Empty;
        var first = parts.Length > 1 ? parts[1] : string.Empty;
        fields.TryGetValue("TEL", out var phone);
        fields.TryGetValue("EMAIL", out var email);
        fields.TryGetValue("URL", out var url);
        fields.TryGetValue("ADR", out var address);
        fields.TryGetValue("NOTE", out var note);
        fields.TryGetValue("ORG", out var org);
        mecard = new QrParsedData.MeCard(first, last, phone, email, url, address, note, org);
        return true;
    }

    private static Dictionary<string, string> SplitMecardFields(string body) {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var sb = new StringBuilder();
        string? key = null;
        var escape = false;

        for (var i = 0; i < body.Length; i++) {
            var ch = body[i];
            if (escape) {
                sb.Append(ch);
                escape = false;
                continue;
            }
            if (ch == '\\') {
                escape = true;
                continue;
            }
            if (key is null) {
                if (ch == ':') {
                    key = sb.ToString();
                    sb.Clear();
                    continue;
                }
            } else if (ch == ';') {
                dict[key] = sb.ToString();
                key = null;
                sb.Clear();
                continue;
            }
            sb.Append(ch);
        }
        if (key is not null) dict[key] = sb.ToString();
        return dict;
    }

    private static bool TryParseVCard(string raw, out QrParsedData.VCard vcard) {
        vcard = null!;
        if (!raw.StartsWith("BEGIN:VCARD", StringComparison.OrdinalIgnoreCase)) return false;
        string? fn = null;
        string? org = null;
        string? email = null;
        string? tel = null;

        var lines = raw.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        for (var i = 0; i < lines.Length; i++) {
            var line = lines[i];
            if (line.StartsWith("FN:", StringComparison.OrdinalIgnoreCase)) fn = line.Substring(3);
            else if (line.StartsWith("ORG:", StringComparison.OrdinalIgnoreCase)) org = line.Substring(4);
            else if (line.StartsWith("EMAIL", StringComparison.OrdinalIgnoreCase)) {
                var idx = line.IndexOf(':');
                if (idx >= 0) email = line.Substring(idx + 1);
            } else if (line.StartsWith("TEL", StringComparison.OrdinalIgnoreCase)) {
                var idx = line.IndexOf(':');
                if (idx >= 0) tel = line.Substring(idx + 1);
            }
        }

        vcard = new QrParsedData.VCard(raw, fn, org, email, tel);
        return true;
    }

    private static bool TryParseCalendar(string raw, out QrParsedData.Calendar calendar) {
        calendar = null!;
        if (raw.IndexOf("BEGIN:VEVENT", StringComparison.OrdinalIgnoreCase) < 0) return false;
        string? summary = null;
        DateTime? start = null;
        DateTime? end = null;

        var lines = raw.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        for (var i = 0; i < lines.Length; i++) {
            var line = lines[i];
            if (line.StartsWith("SUMMARY:", StringComparison.OrdinalIgnoreCase)) summary = line.Substring(8);
            else if (line.StartsWith("DTSTART", StringComparison.OrdinalIgnoreCase)) {
                var idx = line.IndexOf(':');
                if (idx >= 0 && TryParseIcalDate(line.Substring(idx + 1), out var dt)) start = dt;
            } else if (line.StartsWith("DTEND", StringComparison.OrdinalIgnoreCase)) {
                var idx = line.IndexOf(':');
                if (idx >= 0 && TryParseIcalDate(line.Substring(idx + 1), out var dt)) end = dt;
            }
        }

        calendar = new QrParsedData.Calendar(raw, summary, start, end);
        return true;
    }

    private static bool TryParseOtp(string raw, out QrParsedData.Otp otp) {
        otp = null!;
        if (!raw.StartsWith("otpauth://", StringComparison.OrdinalIgnoreCase)) return false;
        if (!OtpAuthParser.TryParse(raw, out var payload)) return false;
        otp = new QrParsedData.Otp(payload);
        return true;
    }

    private static bool TryParseIcalDate(string value, out DateTime dateTime) {
        dateTime = default;
        if (string.IsNullOrEmpty(value)) return false;

        if (DateTime.TryParseExact(value, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var date)) {
            dateTime = date;
            return true;
        }
        if (DateTime.TryParseExact(value, "yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dtz)) {
            dateTime = dtz;
            return true;
        }
        if (DateTime.TryParseExact(value, "yyyyMMdd'T'HHmmss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dtn)) {
            dateTime = dtn;
            return true;
        }
        return false;
    }

    private static void SplitOnce(string value, char separator, out string left, out string? right) {
        var idx = value.IndexOf(separator);
        if (idx < 0) {
            left = value;
            right = null;
            return;
        }
        left = idx == 0 ? string.Empty : value.Substring(0, idx);
        right = idx + 1 >= value.Length ? string.Empty : value.Substring(idx + 1);
    }

    private static bool TryParseAppStore(string raw, out QrParsedData.AppStore app) {
        app = null!;
        if (!LooksLikeUrl(raw)) return false;
        if (raw.IndexOf("apps.apple.com", StringComparison.OrdinalIgnoreCase) >= 0) {
            var id = ExtractAppStoreId(raw);
            app = new QrParsedData.AppStore(raw, "apple", id);
            return true;
        }
        if (raw.IndexOf("play.google.com", StringComparison.OrdinalIgnoreCase) >= 0) {
            var id = ExtractQueryParam(raw, "id");
            app = new QrParsedData.AppStore(raw, "google", id);
            return true;
        }
        return false;
    }

    private static string? ExtractAppStoreId(string raw) {
        var idx = raw.IndexOf("/id", StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return null;
        idx += 3;
        var end = idx;
        while (end < raw.Length && char.IsDigit(raw[end])) end++;
        return end > idx ? raw.Substring(idx, end - idx) : null;
    }

    private static bool TryParseSocial(string raw, out QrParsedData.Social social) {
        social = null!;
        if (!LooksLikeUrl(raw)) return false;
        var lower = raw.ToLowerInvariant();
        if (lower.Contains("facebook.com/")) {
            social = new QrParsedData.Social(raw, "facebook", ExtractHandle(raw, "facebook.com/"));
            return true;
        }
        if (lower.Contains("twitter.com/") || lower.Contains("x.com/")) {
            social = new QrParsedData.Social(raw, "twitter", ExtractHandle(raw, "twitter.com/") ?? ExtractHandle(raw, "x.com/"));
            return true;
        }
        if (lower.Contains("tiktok.com/")) {
            social = new QrParsedData.Social(raw, "tiktok", ExtractHandle(raw, "tiktok.com/"));
            return true;
        }
        if (lower.Contains("linkedin.com/in/")) {
            social = new QrParsedData.Social(raw, "linkedin", ExtractHandle(raw, "linkedin.com/in/"));
            return true;
        }
        if (lower.Contains("linkedin.com/company/")) {
            social = new QrParsedData.Social(raw, "linkedin", ExtractHandle(raw, "linkedin.com/company/"));
            return true;
        }
        return false;
    }

    private static string? ExtractHandle(string raw, string marker) {
        var idx = raw.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return null;
        idx += marker.Length;
        while (idx < raw.Length && raw[idx] == '/') idx++;
        var end = idx;
        while (end < raw.Length && raw[end] != '/' && raw[end] != '?' && raw[end] != '#') end++;
        return end > idx ? raw.Substring(idx, end - idx) : null;
    }

    private static string? ExtractQueryParam(string raw, string key) {
        var idx = raw.IndexOf('?');
        if (idx < 0) return null;
        var query = raw.Substring(idx + 1);
        var pairs = query.Split('&');
        for (var i = 0; i < pairs.Length; i++) {
            var pair = pairs[i];
            var eq = pair.IndexOf('=');
            if (eq <= 0) continue;
            var k = pair.Substring(0, eq);
            var value = pair.Substring(eq + 1);
            if (!k.Equals(key, StringComparison.OrdinalIgnoreCase)) continue;
            if (PercentEncoding.TryDecode(value, out var decoded)) return decoded;
            return value;
        }
        return null;
    }

    private static bool LooksLikeUrl(string raw) {
        if (raw.StartsWith("http://", StringComparison.OrdinalIgnoreCase)) return true;
        if (raw.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    private static bool TryNormalizePayPalUrl(string raw, out string url, out string handle, out decimal? amount, out string? currency) {
        url = string.Empty;
        handle = string.Empty;
        amount = null;
        currency = null;

        var candidate = raw;
        if (!candidate.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !candidate.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) {
            if (!candidate.StartsWith("paypal.me/", StringComparison.OrdinalIgnoreCase) &&
                !candidate.StartsWith("www.paypal.me/", StringComparison.OrdinalIgnoreCase)) {
                return false;
            }
            candidate = "https://" + candidate;
        }

        if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri)) return false;
        if (!IsPayPalMeHost(uri.Host)) return false;

        var segments = uri.AbsolutePath.Trim('/').Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0) return false;
        handle = segments[0];
        if (string.IsNullOrEmpty(handle)) return false;

        if (segments.Length >= 2) {
            var amountText = segments[1];
            ParsePayPalAmount(amountText, out amount, out currency);
        }

        url = uri.GetLeftPart(UriPartial.Path);
        return true;
    }

    private static void ParsePayPalAmount(string amountText, out decimal? amount, out string? currency) {
        amount = null;
        currency = null;
        if (string.IsNullOrWhiteSpace(amountText)) return;

        var end = amountText.Length - 1;
        while (end >= 0 && char.IsLetter(amountText[end])) end--;
        var numberPart = amountText.Substring(0, end + 1);
        var currencyPart = amountText.Substring(end + 1);

        if (decimal.TryParse(numberPart, NumberStyles.Number, CultureInfo.InvariantCulture, out var value)) {
            amount = value;
        }
        if (!string.IsNullOrEmpty(currencyPart)) {
            currency = currencyPart.ToUpperInvariant();
        }
    }

    private static bool IsPayPalMeHost(string host) {
        if (string.IsNullOrEmpty(host)) return false;
        if (host.Equals("paypal.me", StringComparison.OrdinalIgnoreCase)) return true;
        if (host.Equals("www.paypal.me", StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    private static void ValidateParsed(QrParsedPayload parsed, QrPayloadParseOptions? options, QrPayloadValidationResult validation) {
        switch (parsed.Type) {
            case QrPayloadType.Wifi:
                if (!parsed.TryGet<QrParsedData.Wifi>(out var wifi)) break;
                if (string.IsNullOrWhiteSpace(wifi.Ssid)) validation.Add("WiFi SSID is empty.");
                if (!QrPayloadValidation.IsValidWifiAuth(wifi.AuthType) && options?.AllowUnknownWifiAuth != true) {
                    validation.Add("WiFi auth type is not recognized.");
                }
                break;
            case QrPayloadType.Email:
                if (!parsed.TryGet<QrParsedData.Email>(out var email)) break;
                if (!QrPayloadValidation.IsValidEmail(email.Address)) validation.Add("Email address is invalid.");
                break;
            case QrPayloadType.Phone:
                if (!parsed.TryGet<QrParsedData.Phone>(out var phone)) break;
                if (!QrPayloadValidation.IsValidPhone(phone.Number, 5)) validation.Add("Phone number is invalid.");
                break;
            case QrPayloadType.Sms:
                if (!parsed.TryGet<QrParsedData.Sms>(out var sms)) break;
                if (!QrPayloadValidation.IsValidPhone(sms.Number, 5)) validation.Add("SMS number is invalid.");
                break;
            case QrPayloadType.Upi:
                if (!parsed.TryGet<QrParsedData.Upi>(out var upi)) break;
                if (!QrPayloadValidation.IsValidUpiVpa(upi.Vpa)) validation.Add("UPI VPA is invalid.");
                if (!string.IsNullOrEmpty(upi.Currency) && !QrPayloadValidation.IsValidCurrency(upi.Currency)) {
                    validation.Add("UPI currency is invalid.");
                }
                if (upi.Amount.HasValue && upi.Amount.Value <= 0m) validation.Add("UPI amount must be positive.");
                break;
            case QrPayloadType.Geo:
                if (!parsed.TryGet<QrParsedData.Geo>(out var geo)) break;
                if (geo.Latitude < -90 || geo.Latitude > 90) validation.Add("Geo latitude is out of range.");
                if (geo.Longitude < -180 || geo.Longitude > 180) validation.Add("Geo longitude is out of range.");
                break;
            case QrPayloadType.MeCard:
                if (!parsed.TryGet<QrParsedData.MeCard>(out var mecard)) break;
                if (string.IsNullOrWhiteSpace(mecard.FirstName) && string.IsNullOrWhiteSpace(mecard.LastName)) {
                    validation.Add("MeCard name is missing.");
                }
                break;
            case QrPayloadType.VCard:
                if (!parsed.TryGet<QrParsedData.VCard>(out var vcard)) break;
                if (string.IsNullOrWhiteSpace(vcard.FullName)
                    && string.IsNullOrWhiteSpace(vcard.Organization)
                    && string.IsNullOrWhiteSpace(vcard.Email)
                    && string.IsNullOrWhiteSpace(vcard.Phone)) {
                    validation.Add("vCard lacks identifying fields.");
                }
                break;
            case QrPayloadType.Calendar:
                if (!parsed.TryGet<QrParsedData.Calendar>(out var cal)) break;
                if (cal.Summary is null && cal.Start is null && cal.End is null) {
                    validation.Add("Calendar event is missing summary/date fields.");
                }
                break;
            case QrPayloadType.AppStore:
                if (!parsed.TryGet<QrParsedData.AppStore>(out var app)) break;
                if (string.IsNullOrEmpty(app.Platform)) validation.Add("App Store platform is unknown.");
                break;
            case QrPayloadType.Social:
                if (!parsed.TryGet<QrParsedData.Social>(out var social)) break;
                if (string.IsNullOrEmpty(social.Network)) validation.Add("Social network is unknown.");
                break;
            case QrPayloadType.Bookmark:
                if (!parsed.TryGet<QrParsedData.Bookmark>(out var bm)) break;
                if (!QrPayloadValidation.IsValidUrl(bm.Url)) validation.Add("Bookmark URL is invalid.");
                break;
            case QrPayloadType.PayPal:
                if (!parsed.TryGet<QrParsedData.PayPal>(out var paypal)) break;
                if (string.IsNullOrWhiteSpace(paypal.Handle)) validation.Add("PayPal handle is missing.");
                if (paypal.Amount.HasValue && paypal.Amount.Value <= 0m) validation.Add("PayPal amount must be positive.");
                if (!string.IsNullOrEmpty(paypal.Currency) && !QrPayloadValidation.IsValidCurrency(paypal.Currency)) {
                    validation.Add("PayPal currency is invalid.");
                }
                if (!QrPayloadValidation.IsValidUrl(paypal.Url)) validation.Add("PayPal URL is invalid.");
                break;
            case QrPayloadType.Url:
                if (!QrPayloadValidation.IsValidUrl(parsed.Raw)) validation.Add("URL is invalid.");
                break;
        }
    }
}
