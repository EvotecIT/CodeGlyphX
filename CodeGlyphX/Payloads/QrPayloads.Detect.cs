using System;

namespace CodeGlyphX.Payloads;

public static partial class QrPayloads {
    /// <summary>
    /// Detects the most likely payload type from a raw input string.
    /// </summary>
    public static QrPayloadData Detect(string input, QrPayloadDetectOptions? options = null) {
        if (input is null) throw new ArgumentNullException(nameof(input));
        var raw = input.Trim();
        if (raw.Length == 0) return Text(string.Empty);

        var opts = options ?? new QrPayloadDetectOptions();

        if (QrPayloadParser.TryParse(raw, out var parsed) && parsed.Type != QrPayloadType.Text) {
            return new QrPayloadData(parsed.Raw);
        }

        if (opts.AllowBareEmail && LooksLikeEmail(raw)) {
            return Email(raw);
        }

        if (opts.AllowBarePhone && LooksLikePhone(raw)) {
            return Phone(raw);
        }

        if (opts.PreferTextWhenAmbiguous && HasWhitespace(raw)) {
            return Text(raw);
        }

        if (opts.AllowBareUrl && LooksLikeUrl(raw)) {
            return Url(raw);
        }

        return Text(raw);
    }

    private static bool LooksLikeEmail(string text) {
        if (string.IsNullOrEmpty(text)) return false;
        var at = text.IndexOf('@');
        if (at <= 0 || at >= text.Length - 3) return false;
        if (HasWhitespace(text)) return false;
        var dot = text.IndexOf('.', at + 2);
        return dot > at + 1 && dot < text.Length - 1;
    }

    private static bool LooksLikePhone(string text) {
        if (string.IsNullOrEmpty(text)) return false;
        var digits = 0;
        for (var i = 0; i < text.Length; i++) {
            var ch = text[i];
            if (ch >= '0' && ch <= '9') {
                digits++;
                continue;
            }
            if (ch == '+' || ch == ' ' || ch == '-' || ch == '(' || ch == ')' || ch == '.' || ch == '/') {
                continue;
            }
            return false;
        }
        return digits >= 5;
    }

    private static bool LooksLikeUrl(string text) {
        if (string.IsNullOrEmpty(text)) return false;
        if (text.StartsWith("http://", StringComparison.OrdinalIgnoreCase)) return true;
        if (text.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) return true;
        if (text.StartsWith("www.", StringComparison.OrdinalIgnoreCase)) return true;
        if (HasWhitespace(text) || text.IndexOf('@') >= 0) return false;

        var host = text;
        var slash = host.IndexOfAny(new[] { '/', '?', '#' });
        if (slash >= 0) host = host.Substring(0, slash);
        if (host.Length < 4) return false;
        if (host[0] == '.' || host[host.Length - 1] == '.') return false;
        return host.IndexOf('.') > 0;
    }

    private static bool HasWhitespace(string text) {
        for (var i = 0; i < text.Length; i++) {
            if (char.IsWhiteSpace(text[i])) return true;
        }
        return false;
    }
}
