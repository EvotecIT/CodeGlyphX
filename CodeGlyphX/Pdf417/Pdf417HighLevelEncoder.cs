using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

#if NET8_0_OR_GREATER
using ByteSpan = System.ReadOnlySpan<byte>;
#else
using ByteSpan = byte[];
#endif

namespace CodeGlyphX.Pdf417;

internal static class Pdf417HighLevelEncoder {
    private const int TextCompactionLatch = 900;
    private const int ByteCompactionLatch = 901;
    private const int NumericCompactionLatch = 902;

    private const int Pl = 25;
    private const int Ll = 27;
    private const int As = 27;
    private const int Ml = 28;
    private const int Al = 28;
    private const int Ps = 29;

    private static readonly char[] PunctChars = ";<>@[\\]_`~!\r\t,:\n-.$/\"|*()?{}'".ToCharArray();
    private static readonly char[] MixedChars = "0123456789&\r\t,:#-.$/+%*=^".ToCharArray();

    private enum Submode {
        Alpha,
        Lower,
        Mixed
    }

    public static List<int> Encode(string msg, Pdf417Compaction compaction, Encoding? encoding) {
        if (msg is null) throw new ArgumentNullException(nameof(msg));
        encoding ??= Encoding.UTF8;

        return compaction switch {
            Pdf417Compaction.Text => EncodeTextMode(msg),
            Pdf417Compaction.Numeric => EncodeNumericMode(msg),
            Pdf417Compaction.Byte => EncodeByteMode(encoding.GetBytes(msg)),
            _ => EncodeAuto(msg, encoding)
        };
    }

    private static List<int> EncodeAuto(string msg, Encoding encoding) {
        var result = new List<int>(msg.Length);
        var idx = 0;
        var mode = Pdf417Compaction.Text;

        while (idx < msg.Length) {
            var digitCount = CountConsecutiveDigits(msg, idx);
            if (digitCount >= 13) {
                if (mode != Pdf417Compaction.Numeric) {
                    result.Add(NumericCompactionLatch);
                    mode = Pdf417Compaction.Numeric;
                }
                var length = Math.Min(digitCount, 44);
                EncodeNumeric(msg, idx, length, result);
                idx += length;
                continue;
            }

            var textCount = CountConsecutiveText(msg, idx);
            if (textCount >= 5) {
                if (mode != Pdf417Compaction.Text) {
                    result.Add(TextCompactionLatch);
                    mode = Pdf417Compaction.Text;
                }
                EncodeText(msg, idx, textCount, result);
                idx += textCount;
                continue;
            }

            var binCount = CountConsecutiveBinary(msg, idx);
            if (binCount == 0) binCount = 1;
            if (mode != Pdf417Compaction.Byte) {
                result.Add(ByteCompactionLatch);
                mode = Pdf417Compaction.Byte;
            }
            EncodeBytes(encoding.GetBytes(msg.Substring(idx, binCount)), result);
            idx += binCount;
        }

        return result;
    }

    private static List<int> EncodeTextMode(string msg) {
        for (var i = 0; i < msg.Length; i++) {
            if (!IsText(msg[i])) throw new ArgumentException("Text compaction supports only text characters.", nameof(msg));
        }
        var result = new List<int>(msg.Length);
        EncodeText(msg, 0, msg.Length, result);
        return result;
    }

    private static List<int> EncodeNumericMode(string msg) {
        for (var i = 0; i < msg.Length; i++) {
            if (!char.IsDigit(msg[i])) throw new ArgumentException("Numeric compaction supports digits only.", nameof(msg));
        }
        var result = new List<int>(msg.Length);
        result.Add(NumericCompactionLatch);
        EncodeNumeric(msg, 0, msg.Length, result);
        return result;
    }

    private static List<int> EncodeByteMode(byte[] data) {
        var result = new List<int>(data.Length + 2) { ByteCompactionLatch };
        EncodeBytes(data, result);
        return result;
    }

    private static void EncodeText(string msg, int start, int count, List<int> result) {
        var submode = Submode.Alpha;
        var tmp = new List<int>(count * 2);

        for (var i = 0; i < count; i++) {
            var ch = msg[start + i];
            if (!IsText(ch)) {
                continue;
            }

            switch (submode) {
                case Submode.Alpha:
                    if (IsAlphaUpper(ch)) {
                        tmp.Add(ch - 'A');
                    } else if (ch == ' ') {
                        tmp.Add(26);
                    } else if (IsAlphaLower(ch)) {
                        tmp.Add(Ll);
                        submode = Submode.Lower;
                        tmp.Add(ch - 'a');
                    } else if (IsMixed(ch)) {
                        tmp.Add(Ml);
                        submode = Submode.Mixed;
                        tmp.Add(GetMixedValue(ch));
                    } else if (IsPunct(ch)) {
                        tmp.Add(Ps);
                        tmp.Add(GetPunctValue(ch));
                    }
                    break;
                case Submode.Lower:
                    if (IsAlphaLower(ch)) {
                        tmp.Add(ch - 'a');
                    } else if (ch == ' ') {
                        tmp.Add(26);
                    } else if (IsAlphaUpper(ch)) {
                        tmp.Add(As);
                        tmp.Add(ch - 'A');
                    } else if (IsMixed(ch)) {
                        tmp.Add(Ml);
                        submode = Submode.Mixed;
                        tmp.Add(GetMixedValue(ch));
                    } else if (IsPunct(ch)) {
                        tmp.Add(Ps);
                        tmp.Add(GetPunctValue(ch));
                    }
                    break;
                case Submode.Mixed:
                    if (IsMixed(ch)) {
                        tmp.Add(GetMixedValue(ch));
                    } else if (ch == ' ') {
                        tmp.Add(26);
                    } else if (IsAlphaLower(ch)) {
                        tmp.Add(Ll);
                        submode = Submode.Lower;
                        tmp.Add(ch - 'a');
                    } else if (IsAlphaUpper(ch)) {
                        tmp.Add(Al);
                        submode = Submode.Alpha;
                        tmp.Add(ch - 'A');
                    } else if (IsPunct(ch)) {
                        tmp.Add(Ps);
                        tmp.Add(GetPunctValue(ch));
                    }
                    break;
            }
        }

        for (var i = 0; i < tmp.Count; i += 2) {
            if (i + 1 < tmp.Count) {
                result.Add(tmp[i] * 30 + tmp[i + 1]);
            } else {
                result.Add(tmp[i] * 30 + 29);
            }
        }
    }

    private static void EncodeNumeric(string msg, int start, int count, List<int> result) {
        var idx = 0;
        while (idx < count) {
            var length = Math.Min(44, count - idx);
            var part = msg.Substring(start + idx, length);
            var value = BigInteger.Parse("1" + part);
            var tmp = new List<int>();
            while (value > 0) {
                value = BigInteger.DivRem(value, 900, out var rem);
                tmp.Add((int)rem);
            }
            for (var i = tmp.Count - 1; i >= 0; i--) result.Add(tmp[i]);
            idx += length;
        }
    }

    private static void EncodeBytes(ByteSpan data, List<int> result) {
        var idx = 0;
        while (idx + 6 <= data.Length) {
            long t = 0;
            for (var i = 0; i < 6; i++) {
                t = (t << 8) + data[idx + i];
            }
            idx += 6;
            var tmp = new int[5];
            for (var i = 4; i >= 0; i--) {
                tmp[i] = (int)(t % 900);
                t /= 900;
            }
            result.AddRange(tmp);
        }

        for (; idx < data.Length; idx++) {
            result.Add(data[idx]);
        }
    }

    private static int CountConsecutiveDigits(string msg, int start) {
        var count = 0;
        while (start + count < msg.Length && char.IsDigit(msg[start + count])) count++;
        return count;
    }

    private static int CountConsecutiveText(string msg, int start) {
        var count = 0;
        while (start + count < msg.Length && IsText(msg[start + count])) count++;
        return count;
    }

    private static int CountConsecutiveBinary(string msg, int start) {
        var count = 0;
        while (start + count < msg.Length && !IsText(msg[start + count])) count++;
        return count;
    }

    private static bool IsText(char ch) => IsAlphaUpper(ch) || IsAlphaLower(ch) || IsMixed(ch) || IsPunct(ch) || ch == ' ';

    private static bool IsAlphaUpper(char ch) => ch is >= 'A' and <= 'Z';

    private static bool IsAlphaLower(char ch) => ch is >= 'a' and <= 'z';

    private static bool IsMixed(char ch) => Array.IndexOf(MixedChars, ch) >= 0;

    private static bool IsPunct(char ch) => Array.IndexOf(PunctChars, ch) >= 0;

    private static int GetMixedValue(char ch) => Array.IndexOf(MixedChars, ch);

    private static int GetPunctValue(char ch) => Array.IndexOf(PunctChars, ch);
}
