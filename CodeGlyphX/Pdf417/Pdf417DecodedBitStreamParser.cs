using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using CodeGlyphX.Internal;

namespace CodeGlyphX.Pdf417;

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
    private const int MacroPdf417ControlBlock = 928;
    private const int MacroPdf417Terminator = 922;
    private const int MacroPdf417OptionalField = 923;
    private const int CharsetEci = 927;
    private const int GeneralPurposeEci = 926;
    private const int UserDefinedEci = 925;

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
        return Decode(codewords, out _);
    }

    public static string? Decode(int[] codewords, out Pdf417MacroMetadata? macro) {
        if (codewords is null) throw new ArgumentNullException(nameof(codewords));
        try {
            return DecodeCore(codewords, out macro);
        } catch (DecoderFallbackException) {
            macro = null;
            return null;
        }
    }

    private static string? DecodeCore(int[] codewords, out Pdf417MacroMetadata? macro) {
        macro = null;
        var result = new DecodedText(codewords.Length * 2);
        var index = 0;
        var mode = TextCompactionLatch;
        var textMode = Mode.Alpha;

        while (index < codewords.Length) {
            var code = codewords[index++];
            if (code == CharsetEci) {
                if (!DecodeCharsetEci(codewords, ref index, result)) return null;
                continue;
            }
            if (code == GeneralPurposeEci || code == UserDefinedEci) return null;
            if (code == TextCompactionLatch) {
                mode = TextCompactionLatch;
                textMode = Mode.Alpha;
                continue;
            }
            if (code == ByteCompactionLatch || code == ByteCompactionLatch6) {
                index = DecodeByteCompaction(code, codewords, index, result);
                if (index < 0) return null;
                mode = TextCompactionLatch;
                textMode = Mode.Alpha;
                continue;
            }
            if (code == NumericCompactionLatch) {
                index = DecodeNumericCompaction(codewords, index, result);
                if (index < 0) return null;
                mode = TextCompactionLatch;
                textMode = Mode.Alpha;
                continue;
            }
            if (code == MacroPdf417ControlBlock) {
                result.FlushBytes();
                if (!DecodeMacroBlock(codewords, ref index, ref macro)) return null;
                mode = TextCompactionLatch;
                textMode = Mode.Alpha;
                continue;
            }
            if (code == ModeShiftToByte) {
                if (index >= codewords.Length) return null;
                var value = codewords[index++];
                if (value < 0 || value > 255) return null;
                result.AppendByte((byte)value);
                continue;
            }

            if (code < 0 || code >= TextCompactionLatch) return null;
            if (mode == TextCompactionLatch) {
                index--;
                index = DecodeTextCompaction(codewords, index, result, ref textMode);
                if (index < 0) return null;
                continue;
            }
        }

        return result.ToString();
    }

    private static bool DecodeCharsetEci(int[] codewords, ref int index, DecodedText result) {
        if (index >= codewords.Length || codewords[index] < 0 || codewords[index] >= 900
            || !EncodingUtils.TryGetEncoding(codewords[index++], out var encoding)) return false;
        result.SetEncoding(encoding);
        return true;
    }

    private static bool DecodeMacroBlock(int[] codewords, ref int index, ref Pdf417MacroMetadata? macro) {
        if (index + 1 >= codewords.Length) return false;

        var segmentCodewords = new[] { codewords[index], codewords[index + 1] };
        index += 2;

        var segmentIndexText = DecodeBase900ToBase10(segmentCodewords, 2);
        if (segmentIndexText is null || !int.TryParse(segmentIndexText, out var segmentIndex)) return false;

        var fileId = new StringBuilder();
        while (index < codewords.Length) {
            var code = codewords[index];
            if (code == MacroPdf417Terminator || code == MacroPdf417OptionalField) break;
            if (code < 0) return false;
            if (code >= TextCompactionLatch) break;
            fileId.Append(code.ToString("000", System.Globalization.CultureInfo.InvariantCulture));
            index++;
        }
        if (fileId.Length == 0) return false;

        var isLastSegment = false;
        int? segmentCount = null;
        string? fileName = null;
        string? sender = null;
        string? addressee = null;
        long? timestamp = null;
        long? fileSize = null;
        int? checksum = null;

        while (index < codewords.Length) {
            var code = codewords[index];
            if (code == MacroPdf417Terminator) {
                isLastSegment = true;
                index++;
                break;
            }
            if (code != MacroPdf417OptionalField) break;
            if (index + 1 >= codewords.Length) return false;
            index++; // skip 923
            var field = codewords[index++];
            switch (field) {
                case 0: {
                    var text = new StringBuilder();
                    index = DecodeMacroText(codewords, index, text);
                    if (index < 0) return false;
                    fileName = text.ToString();
                    break;
                }
                case 1: {
                    var numeric = new StringBuilder();
                    index = DecodeMacroNumeric(codewords, index, numeric);
                    if (index < 0) return false;
                    if (!int.TryParse(numeric.ToString(), out var value)) return false;
                    segmentCount = value;
                    break;
                }
                case 2: {
                    var numeric = new StringBuilder();
                    index = DecodeMacroNumeric(codewords, index, numeric);
                    if (index < 0) return false;
                    if (!long.TryParse(numeric.ToString(), out var value)) return false;
                    timestamp = value;
                    break;
                }
                case 3: {
                    var text = new StringBuilder();
                    index = DecodeMacroText(codewords, index, text);
                    if (index < 0) return false;
                    sender = text.ToString();
                    break;
                }
                case 4: {
                    var text = new StringBuilder();
                    index = DecodeMacroText(codewords, index, text);
                    if (index < 0) return false;
                    addressee = text.ToString();
                    break;
                }
                case 5: {
                    var numeric = new StringBuilder();
                    index = DecodeMacroNumeric(codewords, index, numeric);
                    if (index < 0) return false;
                    if (!long.TryParse(numeric.ToString(), out var value)) return false;
                    fileSize = value;
                    break;
                }
                case 6: {
                    var numeric = new StringBuilder();
                    index = DecodeMacroNumeric(codewords, index, numeric);
                    if (index < 0) return false;
                    if (!int.TryParse(numeric.ToString(), out var value)) return false;
                    checksum = value;
                    break;
                }
                default:
                    return false;
            }
        }

        macro = new Pdf417MacroMetadata(segmentIndex, fileId.ToString(), isLastSegment, segmentCount, fileName, timestamp, sender, addressee, fileSize, checksum);
        return true;
    }

    private static int DecodeMacroText(int[] codewords, int index, StringBuilder sb) {
        // Each metadata field owns its charset and pending bytes independently of the payload and other fields.
        var result = new DecodedText(codewords.Length - index);
        var textMode = Mode.Alpha;
        while (index < codewords.Length) {
            var code = codewords[index];
            if (code == CharsetEci) {
                index++;
                if (!DecodeCharsetEci(codewords, ref index, result)) return -1;
                continue;
            }
            if (code >= TextCompactionLatch && code != TextCompactionLatch && code != ModeShiftToByte) break;
            index = DecodeTextCompaction(codewords, index, result, ref textMode);
            if (index < 0) return -1;
        }
        sb.Append(result.ToString());
        return index;
    }

    private static int DecodeMacroNumeric(int[] codewords, int index, StringBuilder sb) {
        var count = 0;
        var numericCodewords = new int[15];
        while (index < codewords.Length) {
            var code = codewords[index];
            if (code == MacroPdf417OptionalField || code == MacroPdf417Terminator || code == MacroPdf417ControlBlock) break;
            if (code >= TextCompactionLatch) break;
            numericCodewords[count++] = code;
            index++;

            if (count == 15) {
                var decoded = DecodeBase900ToBase10(numericCodewords, count);
                if (decoded is null) return -1;
                sb.Append(decoded);
                count = 0;
            }
        }

        if (count > 0) {
            var decoded = DecodeBase900ToBase10(numericCodewords, count);
            if (decoded is null) return -1;
            sb.Append(decoded);
        }

        return index;
    }

    private static int DecodeTextCompaction(int[] codewords, int index, DecodedText result, ref Mode textMode) {
        var textData = new List<int>((codewords.Length - index) * 2);
        var byteData = new List<int>((codewords.Length - index) * 2);

        while (index < codewords.Length) {
            var code = codewords[index++];
            if (code < 0) return -1;
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
                if (index >= codewords.Length || codewords[index] < 0 || codewords[index] > 255) return -1;
                textData.Add(ModeShiftToByte);
                byteData.Add(codewords[index++]);
                continue;
            }

            // mode switch
            index--;
            break;
        }

        textMode = DecodeTextCompactionData(textData, byteData, result, textMode);
        return index;
    }

    private static Mode DecodeTextCompactionData(List<int> textData, List<int> byteData, DecodedText result, Mode initialMode = Mode.Alpha) {
        var subMode = initialMode;
        var priorToShiftMode = initialMode;
        var latchedMode = initialMode;
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
                                if (byteIndex < byteData.Count) result.AppendByte((byte)byteData[byteIndex++]);
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
                                if (byteIndex < byteData.Count) result.AppendByte((byte)byteData[byteIndex++]);
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
                                if (byteIndex < byteData.Count) result.AppendByte((byte)byteData[byteIndex++]);
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
                                if (byteIndex < byteData.Count) result.AppendByte((byte)byteData[byteIndex++]);
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
                                if (byteIndex < byteData.Count) result.AppendByte((byte)byteData[byteIndex++]);
                                break;
                        }
                    }
                    break;
            }

            if (ch != null) {
                result.Append(ch.Value);
            } else if (subMode == latchedMode) {
                // no-op
            }
        }
        return latchedMode;
    }

    private static int DecodeByteCompaction(int mode, int[] codewords, int index, DecodedText result) {
        var end = false;
        var bytes = new List<byte>();

        while (index < codewords.Length && !end) {
            if (codewords[index] == CharsetEci) {
                if (bytes.Count > 0) result.AppendBytes(bytes);
                bytes.Clear();
                index++;
                if (!DecodeCharsetEci(codewords, ref index, result)) return -1;
                continue;
            }
            if (index >= codewords.Length || codewords[index] >= TextCompactionLatch) {
                end = true;
            } else {
                var value = 0L;
                var count = 0;
                do {
                    if (codewords[index] < 0) return -1;
                    value = 900 * value + codewords[index++];
                    count++;
                } while (count < 5 && index < codewords.Length && codewords[index] < TextCompactionLatch);

                if (count == 5 && (mode == ByteCompactionLatch6 || (index < codewords.Length && codewords[index] < TextCompactionLatch))) {
                    if (value >= (1L << 48)) return -1;
                    for (var i = 0; i < 6; i++) {
                        bytes.Add((byte)(value >> (8 * (5 - i))));
                    }
                } else {
                    if (mode == ByteCompactionLatch6) return -1;
                    index -= count;
                    while (index < codewords.Length && !end) {
                        var code = codewords[index++];
                        if (code < TextCompactionLatch) {
                            if (code < 0 || code > 255) return -1;
                            bytes.Add((byte)code);
                        } else if (code == CharsetEci) {
                            // Once 901 enters its residual bytes, ECI changes only their charset, not framing.
                            if (bytes.Count > 0) result.AppendBytes(bytes);
                            bytes.Clear();
                            if (!DecodeCharsetEci(codewords, ref index, result)) return -1;
                        } else {
                            index--;
                            end = true;
                        }
                    }
                }
            }
        }

        // Framing controls may split a character across groups, latches, or single-byte shifts.
        if (bytes.Count > 0) result.AppendBytes(bytes);
        return index;
    }

    private static int DecodeNumericCompaction(int[] codewords, int index, DecodedText result) {
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
                    case CharsetEci:
                    case GeneralPurposeEci:
                    case UserDefinedEci:
                    case MacroPdf417ControlBlock:
                        index--;
                        end = true;
                        break;
                    default:
                        return -1;
                }
            }

            if ((count % 15 == 0 || end) && count > 0) {
                var decoded = DecodeBase900ToBase10(numericCodewords, count);
                if (decoded is null) return -1;
                result.Append(decoded);
                count = 0;
            }
        }

        return index;
    }

    private static string? DecodeBase900ToBase10(int[] codewords, int count) {
        var result = BigInteger.Zero;
        for (var i = 0; i < count; i++) {
            if (codewords[i] < 0 || codewords[i] >= TextCompactionLatch) return null;
            result += Exp900[count - i - 1] * new BigInteger(codewords[i]);
        }
        var resultString = result.ToString();
        if (resultString.Length == 0 || resultString[0] != '1') return null;
        return resultString.Substring(1);
    }

    /// <summary>
    /// Keeps declared-charset bytes together across compaction controls, flushing before actual characters,
    /// charset changes, Macro metadata, and the end of the payload. Undeclared bytes retain legacy detection.
    /// </summary>
    private sealed class DecodedText {
        private readonly StringBuilder _text;
        private readonly List<byte> _pendingBytes = new();
        private Encoding? _encoding;

        public DecodedText(int capacity) {
            _text = new StringBuilder(capacity);
        }

        public void SetEncoding(Encoding encoding) {
            FlushBytes();
            _encoding = (Encoding)encoding.Clone();
            _encoding.DecoderFallback = DecoderFallback.ExceptionFallback;
        }

        public void AppendByte(byte value) {
            if (_encoding is null) {
                _text.Append(DecodeBytes(new[] { value }));
            } else {
                _pendingBytes.Add(value);
            }
        }

        public void AppendBytes(List<byte> bytes) {
            if (_encoding is null) {
                _text.Append(DecodeBytes(bytes.ToArray()));
            } else {
                _pendingBytes.AddRange(bytes);
            }
        }

        public void Append(char value) {
            FlushBytes();
            _text.Append(value);
        }

        public void Append(string value) {
            if (value.Length == 0) return;
            FlushBytes();
            _text.Append(value);
        }

        public void FlushBytes() {
            if (_pendingBytes.Count == 0) return;
            _text.Append(_encoding!.GetString(_pendingBytes.ToArray()));
            _pendingBytes.Clear();
        }

        public override string ToString() {
            FlushBytes();
            return _text.ToString();
        }
    }

    private static string DecodeBytes(byte[] bytes) {
        try {
            return EncodingUtils.Utf8Strict.GetString(bytes);
        } catch (DecoderFallbackException) {
            return EncodingUtils.Latin1.GetString(bytes);
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
