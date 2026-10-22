using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using CodeGlyphX.Internal;

namespace CodeGlyphX.CodeOne;

internal static class CodeOnePayloadDecoder {
    internal sealed class Payload {
        internal string Text { get; }
        internal byte[] Bytes { get; }
        internal bool IsGs1 { get; }
        internal int[] Assignments { get; }
        internal Payload(string text, byte[] bytes, bool gs1, int[] assignments) {
            Text = text; Bytes = bytes; IsGs1 = gs1; Assignments = assignments;
        }
    }

    internal static bool TryDecode(int[] words, int count, out Payload payload) {
        payload = null!;
        var bytes = new List<byte>(count);
        var position = 0;
        var escaped = count >= 2 && words[0] == 129 && words[1] == 93;
        if (escaped) position = 2;
        var gs1 = position == 0 && count > 0 && (words[0] == 232 || words[0] == 236);
        var upper = false;
        while (position < count) {
            var value = words[position++];
            if (value is >= 1 and <= 128) {
                bytes.Add((byte)(value - 1 + (upper ? 128 : 0))); upper = false;
            } else if (upper) return false;
            else if (value == 129) {
                for (; position < count; position++) if (words[position] != 129) return false;
                break;
            } else if (value is >= 130 and <= 229) {
                bytes.Add((byte)('0' + (value - 130) / 10)); bytes.Add((byte)('0' + (value - 130) % 10));
            } else if (value is 230 or 239 or 238) {
                if (!ReadTriplets(words, count, ref position, value, bytes)) return false;
            } else if (value == 231) {
                if (position >= count) return false;
                var length = words[position++];
                if (length >= 250) {
                    if (position >= count) return false;
                    length = (length - 249) * 250 + words[position++];
                } else if (length == 0) length = count - position;
                if (length > count - position) return false;
                for (var i = 0; i < length; i++) bytes.Add((byte)words[position++]);
            } else if (value == 232) {
                if (position != 1) bytes.Add((byte)Gs1.GroupSeparator);
            } else if (value == 235) upper = true;
            else if (value == 236 || value is >= 240 and <= 254) {
                if (value == 236 && position != 1) bytes.Add((byte)Gs1.GroupSeparator);
                var bit = value == 236 ? position * 8 : (position - 1) * 8 + 4;
                if (!ReadDecimal(words, count, ref bit, bytes)) return false;
                position = (bit + 7) / 8;
            } else if (value != 255 || position != count) return false;
        }
        if (upper || bytes.Count == 0) return false;
        try { return Interpret(bytes.ToArray(), escaped, gs1, out payload); }
        catch (DecoderFallbackException) { return false; }
    }

    private static bool ReadTriplets(int[] words, int count, ref int position, int mode, List<byte> bytes) {
        var shift = 0;
        var upper = false;
        const string punctuation = "!\"#$%&'()*+,-./:;<=>?@[\\]^_";
        while (position < count) {
            if (words[position] == 255) { position++; return !upper && shift == 0; }
            if (position == count - 1) return !upper && shift == 0; // One terminal ASCII codeword needs no unlatch.
            var packed = words[position] * 256 + words[position + 1] - 1;
            position += 2;
            if (packed < 0 || packed >= 64000) return false;
            var triplet = new[] { packed / 1600, packed / 40 % 40, packed % 40 };
            foreach (var item in triplet) {
                var character = -1;
                if (mode == 238) character = item < 4 ? "\r*> "[item] : item < 14 ? '0' + item - 4 : 'A' + item - 14;
                else if (shift == 0) {
                    if (item < 3) { shift = item + 1; continue; }
                    character = item == 3 ? ' ' : item < 14 ? '0' + item - 4 : (mode == 230 ? 'A' : 'a') + item - 14;
                } else {
                    if (shift == 1) character = item < 32 ? item : -1;
                    else if (shift == 2) {
                        if (item < punctuation.Length) character = punctuation[item];
                        else if (item == 27) character = Gs1.GroupSeparator;
                        else if (item == 30) { upper = true; shift = 0; continue; }
                    } else {
                        if (item < 32) character = mode == 230 || item == 0 || item > 26 ? 96 + item : 'A' + item - 1;
                    }
                    shift = 0;
                }
                if (character < 0 || character + (upper ? 128 : 0) > 255) return false;
                bytes.Add((byte)(character + (upper ? 128 : 0))); upper = false;
            }
        }
        return !upper; // Any shift value may pad the actual final two C40/Text values.
    }

    private static bool ReadDecimal(int[] words, int count, ref int bit, List<byte> bytes) {
        while (bit < count * 8) {
            var remaining = count * 8 - bit;
            if (remaining >= 6 && Peek(words, bit, 6) == 63) {
                bit += 6;
                return FinishDecimal(words, ref bit, bytes);
            }
            if (remaining <= 8) {
                if (remaining == 8) return true; // Final ASCII codeword.
                return FinishDecimal(words, ref bit, bytes);
            }
            if (remaining < 10) return false;
            var number = Peek(words, bit, 10); bit += 10;
            if (number < 1 || number > 1000) return false;
            bytes.AddRange(Encoding.ASCII.GetBytes((number - 1).ToString("D3", CultureInfo.InvariantCulture)));
        }
        return true;
    }

    private static bool FinishDecimal(int[] words, ref int bit, List<byte> bytes) {
        var remaining = (8 - bit % 8) % 8;
        if (remaining >= 4) {
            var digit = Peek(words, bit, 4); bit += 4; remaining -= 4;
            if (digit >= 1 && digit <= 10) bytes.Add((byte)('0' + digit - 1));
            else if (digit != 15) return false;
        }
        if (remaining == 2) {
            if (Peek(words, bit, 2) != 1) return false;
            bit += 2;
        }
        return remaining is 0 or 2;
    }

    private static int Peek(int[] words, int bit, int count) {
        var value = 0;
        for (var i = 0; i < count; i++, bit++) value = value * 2 + ((words[bit / 8] >> (7 - bit % 8)) & 1);
        return value;
    }

    private static bool Interpret(byte[] source, bool escaped, bool gs1, out Payload payload) {
        payload = null!;
        if (!escaped) {
            payload = new Payload(EncodingUtils.Latin1.GetString(source), source, gs1, Array.Empty<int>());
            return true;
        }
        var bytes = new List<byte>(source.Length);
        var pending = new List<byte>();
        var text = new StringBuilder();
        var encoding = EncodingUtils.Latin1;
        var assignments = new List<int>();
        for (var i = 0; i < source.Length; i++) {
            var value = source[i];
            if (value == '\\') {
                if (++i >= source.Length) return false;
                if (source[i] != '\\') {
                    if (source.Length - i < 6) return false;
                    var assignment = 0;
                    for (var j = 0; j < 6; j++) {
                        if (source[i + j] < '0' || source[i + j] > '9') return false;
                        assignment = assignment * 10 + source[i + j] - '0';
                    }
                    text.Append(encoding.GetString(pending.ToArray())); pending.Clear();
                    assignments.Add(assignment);
                    if (EncodingUtils.TryGetEncoding(assignment, out var selected)) encoding = selected;
                    i += 5;
                    continue;
                }
            }
            bytes.Add(value); pending.Add(value);
        }
        text.Append(encoding.GetString(pending.ToArray()));
        payload = new Payload(text.ToString(), bytes.ToArray(), gs1, assignments.ToArray());
        return true;
    }
}
