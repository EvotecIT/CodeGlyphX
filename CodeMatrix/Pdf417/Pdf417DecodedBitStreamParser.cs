using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace CodeMatrix.Pdf417;

internal static class Pdf417DecodedBitStreamParser {
    private enum Mode {
        Alpha,
        Lower,
        Mixed,
        Punct,
        AlphaShift,
        PunctShift
    }

    private const int TextCompactionLatch = 900;
    private const int ByteCompactionLatch = 901;
    private const int NumericCompactionLatch = 902;
    private const int ByteCompactionLatch6 = 924;
    private const int ModeShiftToByte = 913;

    private const int Pl = 25;
    private const int Ll = 27;
    private const int As = 27;
    private const int Ml = 28;
    private const int Al = 28;
    private const int Ps = 29;
    private const int Pal = 29;

    private static readonly char[] PunctChars = ";<>@[\\]_`~!\r\t,:\n-.$/\"|*()?{}'".ToCharArray();
    private static readonly char[] MixedChars = "0123456789&\r\t,:#-.$/+%*=^".ToCharArray();

    private static readonly BigInteger[] Exp900 = CreateExp900();

    public static string? Decode(int[] codewords) {
        if (codewords is null) throw new ArgumentNullException(nameof(codewords));
        var sb = new StringBuilder(codewords.Length * 2);
        var index = 0;
        var mode = TextCompactionLatch;

        while (index < codewords.Length) {
            var code = codewords[index++];
            if (code == TextCompactionLatch) {
                mode = TextCompactionLatch;
                continue;
            }
            if (code == ByteCompactionLatch || code == ByteCompactionLatch6) {
                index = DecodeByteCompaction(code, codewords, index, sb);
                mode = TextCompactionLatch;
                continue;
            }
            if (code == NumericCompactionLatch) {
                index = DecodeNumericCompaction(codewords, index, sb);
                mode = TextCompactionLatch;
                continue;
            }
            if (code == ModeShiftToByte) {
                if (index >= codewords.Length) break;
                sb.Append((char)codewords[index++]);
                continue;
            }

            if (mode == TextCompactionLatch) {
                index--;
                index = DecodeTextCompaction(codewords, index, sb);
                continue;
            }
        }

        return sb.ToString();
    }

    private static int DecodeTextCompaction(int[] codewords, int index, StringBuilder sb) {
        var textData = new List<int>((codewords.Length - index) * 2);
        var byteData = new List<int>((codewords.Length - index) * 2);

        while (index < codewords.Length) {
            var code = codewords[index++];
            if (code < TextCompactionLatch) {
                textData.Add(code / 30);
                textData.Add(code % 30);
                continue;
            }

            if (code == TextCompactionLatch) {
                textData.Add(TextCompactionLatch);
                continue;
            }

            if (code == ModeShiftToByte) {
                if (index >= codewords.Length) break;
                textData.Add(ModeShiftToByte);
                byteData.Add(codewords[index++]);
                continue;
            }

            // mode switch
            index--;
            break;
        }

        DecodeTextCompactionData(textData, byteData, sb);
        return index;
    }

    private static void DecodeTextCompactionData(List<int> textData, List<int> byteData, StringBuilder sb) {
        var subMode = Mode.Alpha;
        var priorToShiftMode = Mode.Alpha;
        var latchedMode = Mode.Alpha;
        var byteIndex = 0;

        for (var i = 0; i < textData.Count; i++) {
            var subModeCh = textData[i];
            char? ch = null;

            switch (subMode) {
                case Mode.Alpha:
                    if (subModeCh < 26) {
                        ch = (char)('A' + subModeCh);
                    } else {
                        switch (subModeCh) {
                            case 26:
                                ch = ' ';
                                break;
                            case Ll:
                                subMode = Mode.Lower;
                                latchedMode = subMode;
                                break;
                            case Ml:
                                subMode = Mode.Mixed;
                                latchedMode = subMode;
                                break;
                            case Ps:
                                priorToShiftMode = subMode;
                                subMode = Mode.PunctShift;
                                break;
                            case ModeShiftToByte:
                                if (byteIndex < byteData.Count) sb.Append((char)byteData[byteIndex++]);
                                break;
                            case TextCompactionLatch:
                                subMode = Mode.Alpha;
                                latchedMode = subMode;
                                break;
                        }
                    }
                    break;
                case Mode.Lower:
                    if (subModeCh < 26) {
                        ch = (char)('a' + subModeCh);
                    } else {
                        switch (subModeCh) {
                            case 26:
                                ch = ' ';
                                break;
                            case As:
                                priorToShiftMode = subMode;
                                subMode = Mode.AlphaShift;
                                break;
                            case Ml:
                                subMode = Mode.Mixed;
                                latchedMode = subMode;
                                break;
                            case Ps:
                                priorToShiftMode = subMode;
                                subMode = Mode.PunctShift;
                                break;
                            case ModeShiftToByte:
                                if (byteIndex < byteData.Count) sb.Append((char)byteData[byteIndex++]);
                                break;
                            case TextCompactionLatch:
                                subMode = Mode.Alpha;
                                latchedMode = subMode;
                                break;
                        }
                    }
                    break;
                case Mode.Mixed:
                    if (subModeCh < Pl) {
                        ch = MixedChars[subModeCh];
                    } else {
                        switch (subModeCh) {
                            case Pl:
                                subMode = Mode.Punct;
                                latchedMode = subMode;
                                break;
                            case 26:
                                ch = ' ';
                                break;
                            case Ll:
                                subMode = Mode.Lower;
                                latchedMode = subMode;
                                break;
                            case Al:
                            case TextCompactionLatch:
                                subMode = Mode.Alpha;
                                latchedMode = subMode;
                                break;
                            case Ps:
                                priorToShiftMode = subMode;
                                subMode = Mode.PunctShift;
                                break;
                            case ModeShiftToByte:
                                if (byteIndex < byteData.Count) sb.Append((char)byteData[byteIndex++]);
                                break;
                        }
                    }
                    break;
                case Mode.Punct:
                    if (subModeCh < Pal) {
                        ch = PunctChars[subModeCh];
                    } else {
                        switch (subModeCh) {
                            case Pal:
                            case TextCompactionLatch:
                                subMode = Mode.Alpha;
                                latchedMode = subMode;
                                break;
                            case ModeShiftToByte:
                                if (byteIndex < byteData.Count) sb.Append((char)byteData[byteIndex++]);
                                break;
                        }
                    }
                    break;
                case Mode.AlphaShift:
                    subMode = priorToShiftMode;
                    if (subModeCh < 26) {
                        ch = (char)('A' + subModeCh);
                    } else {
                        switch (subModeCh) {
                            case 26:
                                ch = ' ';
                                break;
                            case TextCompactionLatch:
                                subMode = Mode.Alpha;
                                latchedMode = subMode;
                                break;
                        }
                    }
                    break;
                case Mode.PunctShift:
                    subMode = priorToShiftMode;
                    if (subModeCh < Pal) {
                        ch = PunctChars[subModeCh];
                    } else {
                        switch (subModeCh) {
                            case Pal:
                            case TextCompactionLatch:
                                subMode = Mode.Alpha;
                                latchedMode = subMode;
                                break;
                            case ModeShiftToByte:
                                if (byteIndex < byteData.Count) sb.Append((char)byteData[byteIndex++]);
                                break;
                        }
                    }
                    break;
            }

            if (ch != null) {
                sb.Append(ch.Value);
            } else if (subMode == latchedMode) {
                // no-op
            }
        }
    }

    private static int DecodeByteCompaction(int mode, int[] codewords, int index, StringBuilder sb) {
        var end = false;

        while (index < codewords.Length && !end) {
            if (index >= codewords.Length || codewords[index] >= TextCompactionLatch) {
                end = true;
            } else {
                var value = 0L;
                var count = 0;
                do {
                    value = 900 * value + codewords[index++];
                    count++;
                } while (count < 5 && index < codewords.Length && codewords[index] < TextCompactionLatch);

                if (count == 5 && (mode == ByteCompactionLatch6 || (index < codewords.Length && codewords[index] < TextCompactionLatch))) {
                    var buffer = new byte[6];
                    for (var i = 0; i < 6; i++) {
                        buffer[i] = (byte)(value >> (8 * (5 - i)));
                    }
                    sb.Append(DecodeBytes(buffer));
                } else {
                    index -= count;
                    var bytes = new List<byte>();
                    while (index < codewords.Length && !end) {
                        var code = codewords[index++];
                        if (code < TextCompactionLatch) {
                            bytes.Add((byte)code);
                        } else {
                            index--;
                            end = true;
                        }
                    }
                    if (bytes.Count > 0) sb.Append(DecodeBytes(bytes.ToArray()));
                }
            }
        }

        return index;
    }

    private static int DecodeNumericCompaction(int[] codewords, int index, StringBuilder sb) {
        var count = 0;
        var end = false;
        var numericCodewords = new int[15];

        while (index < codewords.Length && !end) {
            var code = codewords[index++];
            if (index == codewords.Length) {
                end = true;
            }
            if (code < TextCompactionLatch) {
                numericCodewords[count++] = code;
            } else {
                switch (code) {
                    case TextCompactionLatch:
                    case ByteCompactionLatch:
                    case ByteCompactionLatch6:
                    case NumericCompactionLatch:
                    case ModeShiftToByte:
                        index--;
                        end = true;
                        break;
                }
            }

            if ((count % 15 == 0 || end) && count > 0) {
                var decoded = DecodeBase900ToBase10(numericCodewords, count);
                if (decoded is null) return index;
                sb.Append(decoded);
                count = 0;
            }
        }

        return index;
    }

    private static string? DecodeBase900ToBase10(int[] codewords, int count) {
        var result = BigInteger.Zero;
        for (var i = 0; i < count; i++) {
            result += Exp900[count - i - 1] * new BigInteger(codewords[i]);
        }
        var resultString = result.ToString();
        if (resultString.Length == 0 || resultString[0] != '1') return null;
        return resultString.Substring(1);
    }

    private static string DecodeBytes(byte[] bytes) {
        try {
            var utf8 = new UTF8Encoding(false, true);
            return utf8.GetString(bytes);
        } catch (DecoderFallbackException) {
            return Encoding.Latin1.GetString(bytes);
        }
    }

    private static BigInteger[] CreateExp900() {
        var exp = new BigInteger[16];
        exp[0] = BigInteger.One;
        var nineHundred = new BigInteger(900);
        for (var i = 1; i < exp.Length; i++) {
            exp[i] = exp[i - 1] * nineHundred;
        }
        return exp;
    }
}
