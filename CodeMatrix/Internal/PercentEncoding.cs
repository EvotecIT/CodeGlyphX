using System;
using System.Text;

namespace CodeMatrix.Internal;

internal static class PercentEncoding {
    private static readonly UTF8Encoding Utf8Strict = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    public static string Escape(string value) {
        if (value is null) throw new ArgumentNullException(nameof(value));
        if (value.Length == 0) return string.Empty;

        var utf8 = Encoding.UTF8.GetBytes(value);
        var sb = new StringBuilder(utf8.Length);
        AppendEscaped(sb, utf8);
        return sb.ToString();
    }

    public static void AppendEscaped(StringBuilder sb, string value) {
        if (sb is null) throw new ArgumentNullException(nameof(sb));
        if (value is null) throw new ArgumentNullException(nameof(value));
        if (value.Length == 0) return;

        AppendEscaped(sb, Encoding.UTF8.GetBytes(value));
    }

    private static void AppendEscaped(StringBuilder sb, byte[] utf8) {
        for (var i = 0; i < utf8.Length; i++) {
            var b = utf8[i];
            if (IsUnreserved(b)) {
                sb.Append((char)b);
                continue;
            }

            sb.Append('%');
            sb.Append(ToHexUpper(b >> 4));
            sb.Append(ToHexUpper(b & 0x0F));
        }
    }

    private static bool IsUnreserved(byte b) =>
        b is >= (byte)'A' and <= (byte)'Z' or >= (byte)'a' and <= (byte)'z' or >= (byte)'0' and <= (byte)'9'
            or (byte)'-' or (byte)'.' or (byte)'_' or (byte)'~';

    private static char ToHexUpper(int value) => (char)(value < 10 ? '0' + value : 'A' + (value - 10));

    public static string Decode(string value) {
        if (value is null) throw new ArgumentNullException(nameof(value));
        if (value.Length == 0) return string.Empty;
        if (!TryDecode(value, out var decoded)) throw new FormatException("Invalid percent-encoding.");
        return decoded;
    }

    public static bool TryDecode(string value, out string decoded) {
        decoded = string.Empty;
        if (value is null) return false;
        if (value.Length == 0) return true;

        if (value.IndexOf('%') < 0) {
            for (var i = 0; i < value.Length; i++) {
                if (value[i] > 0x7F) return false;
            }
            decoded = value;
            return true;
        }

        var bytes = new byte[value.Length];
        var count = 0;

        for (var i = 0; i < value.Length; i++) {
            var c = value[i];
            if (c == '%') {
                if (i + 2 >= value.Length) return false;
                var hi = FromHex(value[i + 1]);
                var lo = FromHex(value[i + 2]);
                if (hi < 0 || lo < 0) return false;
                bytes[count++] = (byte)((hi << 4) | lo);
                i += 2;
                continue;
            }

            if (c > 0x7F) return false;
            bytes[count++] = (byte)c;
        }

        try {
            decoded = Utf8Strict.GetString(bytes, 0, count);
        } catch (DecoderFallbackException) {
            return false;
        }

        return true;
    }

    private static int FromHex(char c) {
        if (c is >= '0' and <= '9') return c - '0';
        if (c is >= 'A' and <= 'F') return 10 + (c - 'A');
        if (c is >= 'a' and <= 'f') return 10 + (c - 'a');
        return -1;
    }
}
