using System;
using CodeGlyphX.Aztec.Internal;
using CodeGlyphX.Internal;

namespace CodeGlyphX.Aztec;

internal static class AztecEncoder {
    private const int DefaultEcPercent = 33;
    private const int MaxLayers = 32;
    private const int MaxCompactLayers = 4;

    public static BitMatrix Encode(string text, AztecEncodeOptions? options = null) {
        if (text is null) throw new ArgumentNullException(nameof(text));

        var bytes = EncodingUtils.Utf8Strict.GetBytes(text);
        var eccPercent = options?.ErrorCorrectionPercent ?? DefaultEcPercent;
        var userSpecifiedLayers = 0;
        if (options?.Layers is int layers && layers > 0) {
            var compact = options.Compact ?? layers <= MaxCompactLayers;
            userSpecifiedLayers = compact ? -layers : layers;
        }

        var symbol = Encode(bytes, eccPercent, userSpecifiedLayers);
        return symbol.Matrix;
    }

    internal static AztecSymbol Encode(byte[] data, int eccPercent, int userSpecifiedLayers) {
        var bits = new AztecHighLevelEncoder(data).Encode();
        var eccBits = bits.Size * eccPercent / 100 + 11;
        var totalSizeBits = bits.Size + eccBits;

        var compact = false;
        var layers = 0;
        var totalBits = 0;
        var wordSize = 0;
        AztecBitBuffer? stuffedBits = null;

        if (userSpecifiedLayers != 0) {
            compact = userSpecifiedLayers < 0;
            layers = Math.Abs(userSpecifiedLayers);

            if (compact && layers > MaxCompactLayers) {
                throw new ArgumentException($"Invalid compact layer count: {layers}. Maximum allowed is {MaxCompactLayers}.");
            }
            if (!compact && layers > MaxLayers) {
                throw new ArgumentException($"Invalid layer count: {layers}. Maximum allowed is {MaxLayers}.");
            }

            totalBits = TotalBitsInLayer(layers, compact);
            wordSize = AztecCommon.WordSize[layers];
            var usableBits = totalBits - (totalBits % wordSize);
            stuffedBits = StuffBits(bits, wordSize);

            if (stuffedBits.Size + eccBits > usableBits) throw new ArgumentException("Data too large for requested layers.");
            if (compact && stuffedBits.Size > wordSize * 64) throw new ArgumentException("Data too large for compact encoding.");
        } else {
            var found = false;
            for (var i = 0; i <= MaxLayers; i++) {
                compact = i <= 3;
                layers = compact ? i + 1 : i;
                if (layers < 1) continue;

                totalBits = TotalBitsInLayer(layers, compact);
                if (totalSizeBits > totalBits) continue;

                if (stuffedBits is null || wordSize != AztecCommon.WordSize[layers]) {
                    wordSize = AztecCommon.WordSize[layers];
                    stuffedBits = StuffBits(bits, wordSize);
                }

                var usableBits = totalBits - (totalBits % wordSize);
                if (stuffedBits.Size + eccBits > usableBits) continue;
                if (compact && stuffedBits.Size > wordSize * 64) continue;
                found = true;
                break;
            }
            if (!found) throw new ArgumentException("Data too large for Aztec symbol.");
        }

        if (stuffedBits is null || layers <= 0) throw new ArgumentException("Data too large for Aztec symbol.");

        var messageSizeInWords = stuffedBits.Size / wordSize;
        var messageBits = GenerateCheckWords(stuffedBits, totalBits, wordSize);
        var modeMessage = GenerateModeMessage(compact, layers, messageSizeInWords);

        var baseMatrixSize = compact ? (11 + layers * 4) : (14 + layers * 4);
        var matrixSize = baseMatrixSize;
        var alignmentMap = new int[baseMatrixSize];

        if (compact) {
            for (var i = 0; i < baseMatrixSize; i++) alignmentMap[i] = i;
        } else {
            var origCenter = baseMatrixSize / 2;
            matrixSize = baseMatrixSize + 1 + 2 * ((baseMatrixSize / 2 - 1) / 15);
            var center = matrixSize / 2;

            for (var i = 0; i < origCenter; i++) {
                var newOffset = i + i / 15;
                alignmentMap[origCenter - i - 1] = center - newOffset - 1;
                alignmentMap[origCenter + i] = center + newOffset + 1;
            }
        }

        var matrix = new BitMatrix(matrixSize, matrixSize);
        var rowOffset = 0;
        for (var layer = 0; layer < layers; layer++) {
            var rowSize = (layers - layer) * 4 + (compact ? 9 : 12);
            for (var i = 0; i < rowSize; i++) {
                var columnOffset = i * 2;
                for (var j = 0; j < 2; j++) {
                    if (messageBits.Get(rowOffset + columnOffset + j)) {
                        matrix.Set(alignmentMap[layer * 2 + j], alignmentMap[layer * 2 + i], true);
                    }
                    if (messageBits.Get(rowOffset + 2 * rowSize + columnOffset + j)) {
                        matrix.Set(alignmentMap[layer * 2 + i], alignmentMap[baseMatrixSize - 1 - layer * 2 - j], true);
                    }
                    if (messageBits.Get(rowOffset + 4 * rowSize + columnOffset + j)) {
                        matrix.Set(alignmentMap[baseMatrixSize - 1 - layer * 2 - j], alignmentMap[baseMatrixSize - 1 - layer * 2 - i], true);
                    }
                    if (messageBits.Get(rowOffset + 6 * rowSize + columnOffset + j)) {
                        matrix.Set(alignmentMap[baseMatrixSize - 1 - layer * 2 - i], alignmentMap[layer * 2 + j], true);
                    }
                }
            }
            rowOffset += rowSize * 8;
        }

        DrawModeMessage(matrix, compact, matrixSize, modeMessage);
        DrawBullsEye(matrix, matrixSize / 2, compact ? 5 : 7);

        return new AztecSymbol(compact, matrixSize, layers, messageSizeInWords, matrix);
    }

    private static void DrawBullsEye(BitMatrix matrix, int center, int size) {
        for (var i = 0; i < size; i += 2) {
            for (var j = center - i; j <= center + i; j++) {
                matrix.Set(j, center - i, true);
                matrix.Set(j, center + i, true);
                matrix.Set(center - i, j, true);
                matrix.Set(center + i, j, true);
            }
        }
        matrix.Set(center - size, center - size, true);
        matrix.Set(center - size + 1, center - size, true);
        matrix.Set(center - size, center - size + 1, true);
        matrix.Set(center + size, center - size, true);
        matrix.Set(center + size, center - size + 1, true);
        matrix.Set(center + size, center + size - 1, true);
    }

    private static void DrawModeMessage(BitMatrix matrix, bool compact, int matrixSize, AztecBitBuffer modeMessage) {
        var center = matrixSize / 2;
        if (compact) {
            for (var i = 0; i < 7; i++) {
                var offset = center - 3 + i;
                if (modeMessage.Get(i)) matrix.Set(offset, center - 5, true);
                if (modeMessage.Get(i + 7)) matrix.Set(center + 5, offset, true);
                if (modeMessage.Get(20 - i)) matrix.Set(offset, center + 5, true);
                if (modeMessage.Get(27 - i)) matrix.Set(center - 5, offset, true);
            }
        } else {
            for (var i = 0; i < 10; i++) {
                var offset = center - 5 + i + i / 5;
                if (modeMessage.Get(i)) matrix.Set(offset, center - 7, true);
                if (modeMessage.Get(i + 10)) matrix.Set(center + 7, offset, true);
                if (modeMessage.Get(29 - i)) matrix.Set(offset, center + 7, true);
                if (modeMessage.Get(39 - i)) matrix.Set(center - 7, offset, true);
            }
        }
    }

    internal static AztecBitBuffer BuildModeMessage(bool compact, int layers, int messageSizeInWords) {
        return GenerateModeMessage(compact, layers, messageSizeInWords);
    }

    private static AztecBitBuffer GenerateModeMessage(bool compact, int layers, int messageSizeInWords) {
        var modeMessage = new AztecBitBuffer();
        if (compact) {
            modeMessage.AppendBits(layers - 1, 2);
            modeMessage.AppendBits(messageSizeInWords - 1, 6);
            return GenerateCheckWords(modeMessage, 28, 4);
        }

        modeMessage.AppendBits(layers - 1, 5);
        modeMessage.AppendBits(messageSizeInWords - 1, 11);
        return GenerateCheckWords(modeMessage, 40, 4);
    }

    private static AztecBitBuffer GenerateCheckWords(AztecBitBuffer bits, int totalBits, int wordSize) {
        var messageSizeInWords = bits.Size / wordSize;
        var rsEncoder = new ReedSolomonEncoder(GetGf(wordSize));
        var totalWords = totalBits / wordSize;
        var messageWords = BitsToWords(bits, wordSize, totalWords);
        rsEncoder.Encode(messageWords, totalWords - messageSizeInWords);

        var messageBits = new AztecBitBuffer();
        var padding = totalBits % wordSize;
        if (padding > 0) messageBits.AppendBits(0, padding);
        for (var i = 0; i < totalWords; i++) {
            messageBits.AppendBits(messageWords[i], wordSize);
        }
        return messageBits;
    }

    private static int[] BitsToWords(AztecBitBuffer bits, int wordSize, int totalWords) {
        var message = new int[totalWords];
        for (var i = 0; i < totalWords; i++) {
            var value = 0;
            for (var j = 0; j < wordSize; j++) {
                var index = i * wordSize + j;
                if (index < bits.Size && bits.Get(index)) {
                    value |= 1 << (wordSize - j - 1);
                }
            }
            message[i] = value;
        }
        return message;
    }

    private static AztecBitBuffer StuffBits(AztecBitBuffer bits, int wordSize) {
        var outBits = new AztecBitBuffer();
        var n = bits.Size;
        var mask = (1 << wordSize) - 2;
        for (var i = 0; i < n; i += wordSize) {
            var word = 0;
            for (var j = 0; j < wordSize; j++) {
                var index = i + j;
                if (index >= n || bits.Get(index)) {
                    word |= 1 << (wordSize - 1 - j);
                }
            }
            if ((word & mask) == 0) {
                outBits.AppendBits(word | 1, wordSize);
                i--;
            } else if ((word & mask) == mask) {
                outBits.AppendBits(word & mask, wordSize);
                i--;
            } else {
                outBits.AppendBits(word, wordSize);
            }
        }
        return outBits;
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
