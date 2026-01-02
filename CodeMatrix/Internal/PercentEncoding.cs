using System;
using System.Text;

namespace CodeMatrix.Internal;

internal static class PercentEncoding {
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
}

