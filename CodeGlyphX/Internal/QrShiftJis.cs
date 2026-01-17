using System;
using System.Collections.Generic;
using System.Text;

namespace CodeGlyphX.Internal;

internal static class QrShiftJis {
    public static bool CanEncode(string text) {
        if (text is null) return false;

        for (var i = 0; i < text.Length; i++) {
            var c = text[i];
            if (c <= 0x7F) continue;
            if (c is >= '\uFF61' and <= '\uFF9F') continue; // halfwidth katakana
            if (QrKanjiTable.TryGetValue(c, out _)) continue;
            return false;
        }

        return true;
    }

    public static byte[] Encode(string text) {
        if (text is null) throw new ArgumentNullException(nameof(text));
        if (text.Length == 0) return Array.Empty<byte>();

        var buffer = new byte[text.Length * 2];
        var count = 0;
        for (var i = 0; i < text.Length; i++) {
            var c = text[i];
            if (c <= 0x7F) {
                buffer[count++] = (byte)c;
                continue;
            }

            if (c is >= '\uFF61' and <= '\uFF9F') {
                buffer[count++] = (byte)(0xA1 + (c - '\uFF61'));
                continue;
            }

            if (QrKanjiTable.TryGetValue(c, out var value)) {
                var assembled = ((value / 0xC0) << 8) | (value % 0xC0);
                var sjis = assembled < 0x1F00 ? assembled + 0x8140 : assembled + 0xC140;
                buffer[count++] = (byte)(sjis >> 8);
                buffer[count++] = (byte)sjis;
                continue;
            }

            buffer[count++] = (byte)'?';
        }

        if (count == buffer.Length) return buffer;
        var result = new byte[count];
        Array.Copy(buffer, 0, result, 0, count);
        return result;
    }

    public static string Decode(byte[] bytes) {
        if (bytes is null) throw new ArgumentNullException(nameof(bytes));
        return Decode(bytes, 0, bytes.Length);
    }

    public static string Decode(byte[] bytes, int offset, int length) {
        if (bytes is null) throw new ArgumentNullException(nameof(bytes));
        if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));
        if (length < 0) throw new ArgumentOutOfRangeException(nameof(length));
        if (offset + length > bytes.Length) throw new ArgumentOutOfRangeException(nameof(length));
        if (length == 0) return string.Empty;

        var sb = new StringBuilder(length);
        var end = offset + length;
        for (var i = offset; i < end; i++) {
            var b = bytes[i];
            if (b <= 0x7F) {
                sb.Append((char)b);
                continue;
            }

            if (b is >= 0xA1 and <= 0xDF) {
                sb.Append((char)('\uFF61' + (b - 0xA1)));
                continue;
            }

            if (IsLeadByte(b) && i + 1 < end) {
                var b2 = bytes[++i];
                if (!IsTrailByte(b2)) {
                    sb.Append('?');
                    continue;
                }

                var sjis = (b << 8) | b2;
                var subtracted = sjis switch {
                    >= 0x8140 and <= 0x9FFC => sjis - 0x8140,
                    >= 0xE040 and <= 0xEBBF => sjis - 0xC140,
                    _ => -1
                };

                if (subtracted >= 0) {
                    var value = ((subtracted >> 8) * 0xC0) + (subtracted & 0xFF);
                    if (value >= 0 && value < 0x2000 && QrKanjiTable.TryGetUnicode((ushort)value, out var ch)) {
                        sb.Append(ch);
                        continue;
                    }
                }

                sb.Append('?');
                continue;
            }

            sb.Append('?');
        }

        return sb.ToString();
    }

    private static bool IsLeadByte(byte b) => (b >= 0x81 && b <= 0x9F) || (b >= 0xE0 && b <= 0xFC);

    private static bool IsTrailByte(byte b) => (b >= 0x40 && b <= 0x7E) || (b >= 0x80 && b <= 0xFC);
}
