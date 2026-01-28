using System;

namespace CodeGlyphX.Rendering.Webp;

/// <summary>
/// Reads VP8L prefix codes from the bitstream.
/// </summary>
internal static class WebpPrefixCodeReader {
    private const int MaxPrefixBits = 15;
    private const int CodeLengthAlphabetSize = 19;

    // Order used to read code-length code lengths.
    private static readonly int[] CodeLengthCodeOrder = {
        17, 18, 0, 1, 2, 3, 4, 5, 16, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15
    };

    public static bool TryReadPrefixCode(ref WebpBitReader reader, int alphabetSize, out WebpPrefixCode code) {
        code = null!;
        if (alphabetSize <= 0) return false;

        var simpleCode = reader.ReadBits(1);
        if (simpleCode < 0) return false;
        if (simpleCode != 0) return TryReadSimplePrefixCode(ref reader, alphabetSize, out code);
        return TryReadNormalPrefixCode(ref reader, alphabetSize, out code);
    }

    public static bool TryReadPrefixCodeWithReason(
        ref WebpBitReader reader,
        int alphabetSize,
        out WebpPrefixCode code,
        out string reason) {
        code = null!;
        reason = string.Empty;
        if (alphabetSize <= 0) {
            reason = "Alphabet size must be positive.";
            return false;
        }

        var simpleCode = reader.ReadBits(1);
        if (simpleCode < 0) {
            reason = "Failed to read prefix code type.";
            return false;
        }
        if (simpleCode != 0) return TryReadSimplePrefixCodeWithReason(ref reader, alphabetSize, out code, out reason);
        return TryReadNormalPrefixCodeWithReason(ref reader, alphabetSize, out code, out reason);
    }

    private static bool TryReadSimplePrefixCode(ref WebpBitReader reader, int alphabetSize, out WebpPrefixCode code) {
        code = null!;

        var numSymbolsBit = reader.ReadBits(1);
        var firstIs8Bits = reader.ReadBits(1);
        if (numSymbolsBit < 0 || firstIs8Bits < 0) return false;

        var symbol0Bits = firstIs8Bits != 0 ? 8 : 1;
        var symbol0 = reader.ReadBits(symbol0Bits);
        if (symbol0 < 0 || symbol0 >= alphabetSize) return false;

        var lengths = new byte[alphabetSize];
        lengths[symbol0] = 1;

        if (numSymbolsBit != 0) {
            var symbol1 = reader.ReadBits(8);
            if (symbol1 < 0 || symbol1 >= alphabetSize) return false;
            lengths[symbol1] = 1;
        }

        return WebpPrefixCode.TryBuild(lengths, MaxPrefixBits, out code);
    }

    private static bool TryReadSimplePrefixCodeWithReason(
        ref WebpBitReader reader,
        int alphabetSize,
        out WebpPrefixCode code,
        out string reason) {
        code = null!;
        reason = string.Empty;

        var numSymbolsBit = reader.ReadBits(1);
        var firstIs8Bits = reader.ReadBits(1);
        if (numSymbolsBit < 0 || firstIs8Bits < 0) {
            reason = "Failed to read simple prefix code header.";
            return false;
        }

        var symbol0Bits = firstIs8Bits != 0 ? 8 : 1;
        var symbol0 = reader.ReadBits(symbol0Bits);
        if (symbol0 < 0 || symbol0 >= alphabetSize) {
            reason = "Simple prefix code symbol0 is out of range.";
            return false;
        }

        var lengths = new byte[alphabetSize];
        lengths[symbol0] = 1;

        if (numSymbolsBit != 0) {
            var symbol1 = reader.ReadBits(8);
            if (symbol1 < 0 || symbol1 >= alphabetSize) {
                reason = "Simple prefix code symbol1 is out of range.";
                return false;
            }
            lengths[symbol1] = 1;
        }

        if (!WebpPrefixCode.TryBuild(lengths, MaxPrefixBits, out code)) {
            reason = "Failed to build simple prefix code.";
            return false;
        }

        return true;
    }

    private static bool TryReadNormalPrefixCode(ref WebpBitReader reader, int alphabetSize, out WebpPrefixCode code) {
        code = null!;

        var numCodeLengthCodesBits = reader.ReadBits(4);
        if (numCodeLengthCodesBits < 0) return false;
        var numCodeLengthCodes = 4 + numCodeLengthCodesBits;
        if (numCodeLengthCodes > CodeLengthAlphabetSize) return false;

        var codeLengthCodeLengths = new byte[CodeLengthAlphabetSize];
        for (var i = 0; i < numCodeLengthCodes; i++) {
            var len = reader.ReadBits(3);
            if (len < 0) return false;
            codeLengthCodeLengths[CodeLengthCodeOrder[i]] = (byte)len;
        }

        if (!WebpPrefixCode.TryBuild(codeLengthCodeLengths, maxBits: 7, out var codeLengthCode)) return false;

        var useMaxSymbol = reader.ReadBits(1);
        if (useMaxSymbol < 0) return false;

        var maxSymbol = alphabetSize;
        if (useMaxSymbol != 0) {
            var lengthNbitsCode = reader.ReadBits(3);
            if (lengthNbitsCode < 0) return false;
            var lengthNbits = 2 + (2 * lengthNbitsCode);
            var maxSymbolMinus2 = reader.ReadBits(lengthNbits);
            if (maxSymbolMinus2 < 0) return false;
            maxSymbol = 2 + maxSymbolMinus2;
            if (maxSymbol > alphabetSize) return false;
        }

        var lengths = new byte[alphabetSize];
        var symbol = 0;
        var prevNonZero = 8;
        while (symbol < maxSymbol) {
            var codeLenSymbol = codeLengthCode.DecodeSymbol(ref reader);
            if (codeLenSymbol < 0) return false;

            if (codeLenSymbol <= 15) {
                lengths[symbol++] = (byte)codeLenSymbol;
                if (codeLenSymbol != 0) prevNonZero = codeLenSymbol;
                continue;
            }

            if (codeLenSymbol == 16) {
                var extra = reader.ReadBits(2);
                if (extra < 0) return false;
                var repeat = 3 + extra;
                if (!Repeat(lengths, ref symbol, maxSymbol, prevNonZero, repeat)) return false;
                continue;
            }

            if (codeLenSymbol == 17) {
                var extra = reader.ReadBits(3);
                if (extra < 0) return false;
                var repeat = 3 + extra;
                if (!Repeat(lengths, ref symbol, maxSymbol, 0, repeat)) return false;
                continue;
            }

            if (codeLenSymbol == 18) {
                var extra = reader.ReadBits(7);
                if (extra < 0) return false;
                var repeat = 11 + extra;
                if (!Repeat(lengths, ref symbol, maxSymbol, 0, repeat)) return false;
                continue;
            }

            return false;
        }

        return WebpPrefixCode.TryBuild(lengths, MaxPrefixBits, out code);
    }

    private static bool TryReadNormalPrefixCodeWithReason(
        ref WebpBitReader reader,
        int alphabetSize,
        out WebpPrefixCode code,
        out string reason) {
        code = null!;
        reason = string.Empty;

        var numCodeLengthCodesBits = reader.ReadBits(4);
        if (numCodeLengthCodesBits < 0) {
            reason = "Failed to read code-length code count.";
            return false;
        }
        var numCodeLengthCodes = 4 + numCodeLengthCodesBits;
        if (numCodeLengthCodes > CodeLengthAlphabetSize) {
            reason = "Code-length code count exceeds alphabet size.";
            return false;
        }

        var codeLengthCodeLengths = new byte[CodeLengthAlphabetSize];
        for (var i = 0; i < numCodeLengthCodes; i++) {
            var len = reader.ReadBits(3);
            if (len < 0) {
                reason = "Failed to read code-length code length.";
                return false;
            }
            codeLengthCodeLengths[CodeLengthCodeOrder[i]] = (byte)len;
        }

        if (!WebpPrefixCode.TryBuild(codeLengthCodeLengths, maxBits: 7, out var codeLengthCode)) {
            reason = "Failed to build code-length prefix code.";
            return false;
        }

        var useMaxSymbol = reader.ReadBits(1);
        if (useMaxSymbol < 0) {
            reason = "Failed to read use_max_symbol flag.";
            return false;
        }

        var maxSymbol = alphabetSize;
        if (useMaxSymbol != 0) {
            var lengthNbitsCode = reader.ReadBits(3);
            if (lengthNbitsCode < 0) {
                reason = "Failed to read max symbol bit length.";
                return false;
            }
            var lengthNbits = 2 + (2 * lengthNbitsCode);
            var maxSymbolMinus2 = reader.ReadBits(lengthNbits);
            if (maxSymbolMinus2 < 0) {
                reason = "Failed to read max symbol value.";
                return false;
            }
            maxSymbol = 2 + maxSymbolMinus2;
            if (maxSymbol > alphabetSize) {
                reason = "Max symbol exceeds alphabet size.";
                return false;
            }
        }

        var lengths = new byte[alphabetSize];
        var symbol = 0;
        var prevNonZero = 8;
        while (symbol < maxSymbol) {
            var codeLenSymbol = codeLengthCode.DecodeSymbol(ref reader);
            if (codeLenSymbol < 0) {
                reason = "Failed to decode code-length symbol.";
                return false;
            }

            if (codeLenSymbol <= 15) {
                lengths[symbol++] = (byte)codeLenSymbol;
                if (codeLenSymbol != 0) prevNonZero = codeLenSymbol;
                continue;
            }

            if (codeLenSymbol == 16) {
                var extra = reader.ReadBits(2);
                if (extra < 0) {
                    reason = "Failed to read code-length repeat bits (16).";
                    return false;
                }
                var repeat = 3 + extra;
                if (!Repeat(lengths, ref symbol, maxSymbol, prevNonZero, repeat)) {
                    reason = "Invalid code-length repeat (16).";
                    return false;
                }
                continue;
            }

            if (codeLenSymbol == 17) {
                var extra = reader.ReadBits(3);
                if (extra < 0) {
                    reason = "Failed to read code-length repeat bits (17).";
                    return false;
                }
                var repeat = 3 + extra;
                if (!Repeat(lengths, ref symbol, maxSymbol, 0, repeat)) {
                    reason = "Invalid code-length repeat (17).";
                    return false;
                }
                continue;
            }

            if (codeLenSymbol == 18) {
                var extra = reader.ReadBits(7);
                if (extra < 0) {
                    reason = "Failed to read code-length repeat bits (18).";
                    return false;
                }
                var repeat = 11 + extra;
                if (!Repeat(lengths, ref symbol, maxSymbol, 0, repeat)) {
                    reason = "Invalid code-length repeat (18).";
                    return false;
                }
                continue;
            }

            reason = "Unknown code-length symbol.";
            return false;
        }

        if (!WebpPrefixCode.TryBuild(lengths, MaxPrefixBits, out code)) {
            reason = "Failed to build normal prefix code.";
            return false;
        }

        return true;
    }

    private static bool Repeat(byte[] lengths, ref int symbol, int maxSymbol, int value, int repeat) {
        if (repeat <= 0) return false;
        var end = symbol + repeat;
        if (end > maxSymbol) return false;
        while (symbol < end) {
            lengths[symbol++] = (byte)value;
        }
        return true;
    }
}
