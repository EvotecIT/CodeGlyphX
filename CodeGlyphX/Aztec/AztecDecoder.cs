using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using CodeGlyphX;
using CodeGlyphX.Aztec.Internal;
using CodeGlyphX.Internal;

namespace CodeGlyphX.Aztec;

internal static class AztecDecoder {
    private enum Table {
        Upper,
        Lower,
        Mixed,
        Digit,
        Punct,
        Binary
    }

    private static readonly string[] UpperTable = {
        "CTRL_PS", " ", "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P",
        "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z", "CTRL_LL", "CTRL_ML", "CTRL_DL", "CTRL_BS"
    };

    private static readonly string[] LowerTable = {
        "CTRL_PS", " ", "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o", "p",
        "q", "r", "s", "t", "u", "v", "w", "x", "y", "z", "CTRL_US", "CTRL_ML", "CTRL_DL", "CTRL_BS"
    };

    private static readonly string[] MixedTable = {
        "CTRL_PS", " ", "\u0001", "\u0002", "\u0003", "\u0004", "\u0005", "\u0006", "\u0007", "\b", "\t", "\n",
        "\u000B", "\f", "\r", "\u001B", "\u001C", "\u001D", "\u001E", "\u001F", "@", "\\", "^", "_",
        "`", "|", "~", "\u007F", "CTRL_LL", "CTRL_UL", "CTRL_PL", "CTRL_BS"
    };

    private static readonly string[] PunctTable = {
        "FLG(n)", "\r", "\r\n", ". ", ", ", ": ", "!", "\"", "#", "$", "%", "&", "'", "(", ")",
        "*", "+", ",", "-", ".", "/", ":", ";", "<", "=", ">", "?", "[", "]", "{", "}", "CTRL_UL"
    };

    private static readonly string[] DigitTable = {
        "CTRL_PS", " ", "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", ",", ".", "CTRL_UL", "CTRL_US"
    };

    public static bool TryDecode(BitMatrix modules, out string value) {
        return TryDecode(modules, CancellationToken.None, out value);
    }

    public static bool TryDecode(BitMatrix modules, CancellationToken cancellationToken, out string value) {
        value = string.Empty;
        if (modules is null) return false;
        if (cancellationToken.IsCancellationRequested) return false;
        if (!AztecDetector.TryDetect(modules, out var detectorResult)) return false;
        return TryDecode(detectorResult, out value);
    }

#if NET8_0_OR_GREATER
    public static bool TryDecode(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat format, out string value) {
        return AztecPixelDecoder.TryDecode(pixels, width, height, stride, format, CancellationToken.None, out value);
    }

    public static bool TryDecode(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat format, CancellationToken cancellationToken, out string value) {
        return AztecPixelDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out value);
    }
#endif

    public static bool TryDecode(byte[] pixels, int width, int height, int stride, PixelFormat format, out string value) {
        return TryDecode(pixels, width, height, stride, format, CancellationToken.None, out value);
    }

    public static bool TryDecode(byte[] pixels, int width, int height, int stride, PixelFormat format, CancellationToken cancellationToken, out string value) {
        if (pixels is null) throw new ArgumentNullException(nameof(pixels));
        return AztecPixelDecoder.TryDecode(pixels, width, height, stride, format, cancellationToken, out value);
    }

    internal static bool TryDecode(AztecDetectorResult detectorResult, out string value) {
        value = string.Empty;
        try {
            value = Decode(detectorResult);
            return true;
        } catch {
            value = string.Empty;
            return false;
        }
    }

    internal static string Decode(AztecDetectorResult detectorResult) {
        var rawBits = ExtractBits(detectorResult.Bits, detectorResult.Compact, detectorResult.NbLayers);
        var correctedBits = CorrectBits(rawBits, detectorResult.NbLayers, detectorResult.NbDataBlocks);
        return GetEncodedData(correctedBits);
    }

    private static bool[] CorrectBits(bool[] rawBits, int layers, int dataBlocks) {
        var codewordSize = AztecCommon.WordSize[layers];
        var numCodewords = rawBits.Length / codewordSize;
        var numDataCodewords = dataBlocks;
        if (numCodewords < numDataCodewords) throw new InvalidOperationException("Invalid data blocks.");
        var numEcCodewords = numCodewords - numDataCodewords;

        var dataWords = new int[numCodewords];
        var offset = rawBits.Length % codewordSize;
        for (var i = 0; i < numCodewords; i++, offset += codewordSize) {
            dataWords[i] = ReadCode(rawBits, offset, codewordSize);
        }

        var rsDecoder = new ReedSolomonDecoder(GetGf(codewordSize));
        rsDecoder.Decode(dataWords, numEcCodewords);

        var mask = (1 << codewordSize) - 1;
        var correctedBits = new bool[numDataCodewords * codewordSize];
        var correctedBitsOffset = 0;

        for (var i = 0; i < numDataCodewords; i++) {
            var dataWord = dataWords[i];
            if (dataWord == 0 || dataWord == mask) throw new InvalidOperationException("Invalid data word.");

            if (dataWord == 1 || dataWord == mask - 1) {
                var fill = dataWord > 1;
                for (var j = 0; j < codewordSize - 1; j++) {
                    correctedBits[correctedBitsOffset + j] = fill;
                }
                correctedBitsOffset += codewordSize - 1;
            } else {
                for (var j = codewordSize - 1; j >= 0; j--) {
                    correctedBits[correctedBitsOffset + j] = (dataWord & 1) == 1;
                    dataWord >>= 1;
                }
                correctedBitsOffset += codewordSize;
            }
        }

        return correctedBits;
    }

    private static bool[] ExtractBits(BitMatrix matrix, bool compact, int layers) {
        var baseMatrixSize = (compact ? 11 : 14) + layers * 4;
        var alignmentMap = new int[baseMatrixSize];
        var rawBits = new bool[TotalBitsInLayer(layers, compact)];

        if (compact) {
            for (var i = 0; i < alignmentMap.Length; i++) alignmentMap[i] = i;
        } else {
            var matrixSize = baseMatrixSize + 1 + 2 * ((baseMatrixSize / 2 - 1) / 15);
            var origCenter = baseMatrixSize / 2;
            var center = matrixSize / 2;
            for (var i = 0; i < origCenter; i++) {
                var newOffset = i + i / 15;
                alignmentMap[origCenter - i - 1] = center - newOffset - 1;
                alignmentMap[origCenter + i] = center + newOffset + 1;
            }
        }

        for (int layer = 0, rawBitsOffset = 0; layer < layers; layer++) {
            var rowSize = (layers - layer) * 4 + (compact ? 9 : 12);
            var low = layer * 2;
            var high = baseMatrixSize - 1 - low;

            for (var i = 0; i < rowSize; i++) {
                var columnOffset = i * 2;
                for (var j = 0; j < 2; j++) {
                    rawBits[rawBitsOffset + columnOffset + j] = matrix.Get(alignmentMap[low + j], alignmentMap[low + i]);
                    rawBits[rawBitsOffset + 2 * rowSize + columnOffset + j] = matrix.Get(alignmentMap[low + i], alignmentMap[high - j]);
                    rawBits[rawBitsOffset + 4 * rowSize + columnOffset + j] = matrix.Get(alignmentMap[high - j], alignmentMap[high - i]);
                    rawBits[rawBitsOffset + 6 * rowSize + columnOffset + j] = matrix.Get(alignmentMap[high - i], alignmentMap[low + j]);
                }
            }

            rawBitsOffset += rowSize * 8;
        }

        return rawBits;
    }

    private static string GetEncodedData(bool[] correctedBits) {
        var endIndex = correctedBits.Length;
        var latchTable = Table.Upper;
        var shiftTable = Table.Upper;
        var result = new StringBuilder((correctedBits.Length - 5) / 4);
        var binaryBuffer = new List<byte>();
        var encoding = EncodingUtils.Latin1;

        var index = 0;
        while (index < endIndex) {
            if (shiftTable == Table.Binary) {
                if (endIndex - index < 5) break;

                var length = ReadCode(correctedBits, index, 5);
                index += 5;
                if (length == 0) {
                    if (endIndex - index < 11) break;
                    length = ReadCode(correctedBits, index, 11) + 31;
                    index += 11;
                }

                for (var i = 0; i < length; i++) {
                    if (endIndex - index < 8) {
                        index = endIndex;
                        break;
                    }
                    var code = ReadCode(correctedBits, index, 8);
                    binaryBuffer.Add((byte)code);
                    index += 8;
                }
                shiftTable = latchTable;
            } else {
                var size = shiftTable == Table.Digit ? 4 : 5;
                if (endIndex - index < size) break;

                var code = ReadCode(correctedBits, index, size);
                index += size;
                var str = GetCharacter(shiftTable, code);
                if (str == "FLG(n)") {
                    if (endIndex - index < 3) break;
                    var n = ReadCode(correctedBits, index, 3);
                    index += 3;

                    if (binaryBuffer.Count > 0) {
                        result.Append(encoding.GetString(binaryBuffer.ToArray()));
                        binaryBuffer.Clear();
                    }

                    switch (n) {
                        case 0:
                            result.Append((char)29);
                            break;
                        case 7:
                            throw new InvalidOperationException("Reserved FLG(7) encountered.");
                        default:
                            if (endIndex - index < 4 * n) break;
                            var eci = 0;
                            while (n-- > 0) {
                                var nextDigit = ReadCode(correctedBits, index, 4);
                                index += 4;
                                if (nextDigit < 2 || nextDigit > 11) {
                                    throw new InvalidOperationException("Invalid ECI digit.");
                                }
                                eci = eci * 10 + (nextDigit - 2);
                            }
                            _ = eci;
                            // Keep Latin1 for now; ECI mapping can be added later.
                            encoding = EncodingUtils.Latin1;
                            break;
                    }

                    shiftTable = latchTable;
                } else if (str.StartsWith("CTRL_", StringComparison.Ordinal)) {
                    latchTable = shiftTable;
                    shiftTable = GetTable(str[5]);
                    if (str[6] == 'L') {
                        latchTable = shiftTable;
                    }
                } else {
                    if (binaryBuffer.Count > 0) {
                        result.Append(encoding.GetString(binaryBuffer.ToArray()));
                        binaryBuffer.Clear();
                    }
                    result.Append(str);
                    if (shiftTable != latchTable) {
                        shiftTable = latchTable;
                    }
                }
            }
        }

        if (binaryBuffer.Count > 0) {
            result.Append(encoding.GetString(binaryBuffer.ToArray()));
        }

        return result.ToString();
    }

    private static string GetCharacter(Table table, int code) {
        return table switch {
            Table.Upper => UpperTable[code],
            Table.Lower => LowerTable[code],
            Table.Mixed => MixedTable[code],
            Table.Digit => DigitTable[code],
            Table.Punct => PunctTable[code],
            _ => string.Empty
        };
    }

    private static Table GetTable(char code) {
        return code switch {
            'L' => Table.Lower,
            'P' => Table.Punct,
            'M' => Table.Mixed,
            'D' => Table.Digit,
            'B' => Table.Binary,
            _ => Table.Upper
        };
    }

    private static int ReadCode(bool[] bits, int startIndex, int length) {
        var result = 0;
        for (var i = startIndex; i < startIndex + length; i++) {
            result <<= 1;
            if (bits[i]) result |= 1;
        }
        return result;
    }

    private static GenericGf GetGf(int wordSize) {
        return wordSize switch {
            4 => GenericGf.AztecParam,
            6 => GenericGf.AztecData6,
            8 => GenericGf.AztecData8,
            10 => GenericGf.AztecData10,
            12 => GenericGf.AztecData12,
            _ => throw new ArgumentOutOfRangeException(nameof(wordSize), "Unsupported word size.")
        };
    }

    private static int TotalBitsInLayer(int layers, bool compact) {
        return ((compact ? 88 : 112) + layers * 16) * layers;
    }
}
