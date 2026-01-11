using System;
using System.Collections.Generic;
using System.Text;
using CodeGlyphX.Internal;
using CodeGlyphX;

namespace CodeGlyphX.DataMatrix;

/// <summary>
/// Encodes Data Matrix (ECC200) symbols.
/// </summary>
public static class DataMatrixEncoder {
    /// <summary>
    /// Encodes a string into a Data Matrix symbol.
    /// </summary>
    public static BitMatrix Encode(string text, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto) {
        if (text is null) throw new ArgumentNullException(nameof(text));

        if (mode == DataMatrixEncodingMode.Auto) {
            if (!CanEncodeLatin1(text)) {
                mode = DataMatrixEncodingMode.Base256;
            } else {
                var bestCodewords = EncodeAscii(EncodingUtils.Latin1.GetBytes(text));

                TryUpdateBest(text, DataMatrixEncodingMode.C40, ref bestCodewords);
                TryUpdateBest(text, DataMatrixEncodingMode.Text, ref bestCodewords);
                TryUpdateBest(text, DataMatrixEncodingMode.X12, ref bestCodewords);
                TryUpdateBest(text, DataMatrixEncodingMode.Edifact, ref bestCodewords);

                return EncodeCodewords(bestCodewords);
            }
        }

        if (mode == DataMatrixEncodingMode.Ascii) {
            if (!CanEncodeLatin1(text)) throw new ArgumentException("Text contains characters outside Latin-1.", nameof(text));
            var bytes = EncodingUtils.Latin1.GetBytes(text);
            return EncodeCodewords(EncodeAscii(bytes));
        }

        if (mode == DataMatrixEncodingMode.Base256) {
            var utf8 = Encoding.UTF8.GetBytes(text);
            return EncodeCodewords(EncodeBase256(utf8));
        }

        var codewords = mode switch {
            DataMatrixEncodingMode.C40 => EncodeC40Text(text, isText: false),
            DataMatrixEncodingMode.Text => EncodeC40Text(text, isText: true),
            DataMatrixEncodingMode.X12 => EncodeX12(text),
            DataMatrixEncodingMode.Edifact => EncodeEdifact(text),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unsupported Data Matrix encoding mode.")
        };

        return EncodeCodewords(codewords);
    }

    /// <summary>
    /// Encodes raw bytes into a Data Matrix symbol.
    /// </summary>
    public static BitMatrix EncodeBytes(byte[] data, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto) {
        if (data is null) throw new ArgumentNullException(nameof(data));
        return EncodeBytesCore(data, mode);
    }

#if NET8_0_OR_GREATER
    /// <summary>
    /// Encodes raw bytes into a Data Matrix symbol.
    /// </summary>
    public static BitMatrix EncodeBytes(ReadOnlySpan<byte> data, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto) {
        return EncodeBytesCore(data, mode);
    }
#endif

#if NET8_0_OR_GREATER
    private static BitMatrix EncodeBytesCore(ReadOnlySpan<byte> data, DataMatrixEncodingMode mode) {
        if (mode == DataMatrixEncodingMode.Auto) {
            mode = DataMatrixEncodingMode.Base256;
        }

        var codewords = mode switch {
            DataMatrixEncodingMode.Ascii => EncodeAscii(data),
            DataMatrixEncodingMode.Base256 => EncodeBase256(data),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unsupported Data Matrix encoding mode for bytes.")
        };

        return EncodeCodewords(codewords);
    }
#else
    private static BitMatrix EncodeBytesCore(byte[] data, DataMatrixEncodingMode mode) {
        if (mode == DataMatrixEncodingMode.Auto) {
            mode = DataMatrixEncodingMode.Base256;
        }

        var codewords = mode switch {
            DataMatrixEncodingMode.Ascii => EncodeAscii(data),
            DataMatrixEncodingMode.Base256 => EncodeBase256(data),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unsupported Data Matrix encoding mode for bytes.")
        };

        return EncodeCodewords(codewords);
    }
#endif

    private static void TryUpdateBest(string text, DataMatrixEncodingMode mode, ref List<byte> bestCodewords) {
        if (!TryEncodeTextMode(text, mode, out var codewords)) return;
        if (codewords.Count < bestCodewords.Count) {
            bestCodewords = codewords;
        }
    }

    private static bool TryEncodeTextMode(string text, DataMatrixEncodingMode mode, out List<byte> codewords) {
        try {
            codewords = mode switch {
                DataMatrixEncodingMode.C40 => EncodeC40Text(text, isText: false),
                DataMatrixEncodingMode.Text => EncodeC40Text(text, isText: true),
                DataMatrixEncodingMode.X12 => EncodeX12(text),
                DataMatrixEncodingMode.Edifact => EncodeEdifact(text),
                _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unsupported Data Matrix encoding mode.")
            };
            return true;
        } catch (ArgumentException) {
            codewords = null!;
            return false;
        }
    }

    private static BitMatrix EncodeCodewords(List<byte> codewords) {
        if (!DataMatrixSymbolInfo.TryGetForData(codewords.Count, out var symbol)) {
            throw new ArgumentException("Data too long for supported Data Matrix symbols.", nameof(codewords));
        }

        PadCodewords(codewords, symbol.DataCodewords);
        var fullCodewords = AddErrorCorrection(codewords, symbol);

        var dataRegion = DataMatrixPlacement.PlaceCodewords(fullCodewords, symbol.DataRegionRows, symbol.DataRegionCols);
        return BuildSymbol(dataRegion, symbol);
    }

#if NET8_0_OR_GREATER
    private static List<byte> EncodeAscii(ReadOnlySpan<byte> data) {
        var codewords = new List<byte>(data.Length + 8);
        for (var i = 0; i < data.Length; i++) {
            var b = data[i];
            if (i + 1 < data.Length && IsDigit(b) && IsDigit(data[i + 1])) {
                var val = (b - (byte)'0') * 10 + (data[i + 1] - (byte)'0');
                codewords.Add((byte)(130 + val));
                i++;
            } else if (b <= 127) {
                codewords.Add((byte)(b + 1));
            } else {
                codewords.Add(235);
                codewords.Add((byte)(b - 127));
            }
        }
        return codewords;
    }

    private static List<byte> EncodeBase256(ReadOnlySpan<byte> data) {
        var codewords = new List<byte>(data.Length + 4) { 231 };
        if (data.Length <= 249) {
            codewords.Add((byte)data.Length);
        } else {
            var full = data.Length;
            var high = (full / 250) + 249;
            var low = full % 250;
            codewords.Add((byte)high);
            codewords.Add((byte)low);
        }

        for (var i = 0; i < data.Length; i++) {
            codewords.Add(data[i]);
        }

        // Randomize length + data codewords (skip 231 switch).
        for (var i = 1; i < codewords.Count; i++) {
            var position = i + 1; // 1-based position in codeword stream
            codewords[i] = Randomize255(codewords[i], position);
        }

        return codewords;
    }
#else
    private static List<byte> EncodeAscii(byte[] data) {
        var codewords = new List<byte>(data.Length + 8);
        for (var i = 0; i < data.Length; i++) {
            var b = data[i];
            if (i + 1 < data.Length && IsDigit(b) && IsDigit(data[i + 1])) {
                var val = (b - (byte)'0') * 10 + (data[i + 1] - (byte)'0');
                codewords.Add((byte)(130 + val));
                i++;
            } else if (b <= 127) {
                codewords.Add((byte)(b + 1));
            } else {
                codewords.Add(235);
                codewords.Add((byte)(b - 127));
            }
        }
        return codewords;
    }

    private static List<byte> EncodeBase256(byte[] data) {
        var codewords = new List<byte>(data.Length + 4) { 231 };
        if (data.Length <= 249) {
            codewords.Add((byte)data.Length);
        } else {
            var full = data.Length;
            var high = (full / 250) + 249;
            var low = full % 250;
            codewords.Add((byte)high);
            codewords.Add((byte)low);
        }

        for (var i = 0; i < data.Length; i++) {
            codewords.Add(data[i]);
        }

        // Randomize length + data codewords (skip 231 switch).
        for (var i = 1; i < codewords.Count; i++) {
            var position = i + 1; // 1-based position in codeword stream
            codewords[i] = Randomize255(codewords[i], position);
        }

        return codewords;
    }
#endif

    private static void PadCodewords(List<byte> codewords, int capacity) {
        if (codewords.Count >= capacity) return;
        codewords.Add(129);
        while (codewords.Count < capacity) {
            var position = codewords.Count + 1; // 1-based
            codewords.Add(Randomize253(129, position));
        }
    }

    private static byte[] AddErrorCorrection(List<byte> dataCodewords, DataMatrixSymbolInfo symbol) {
        var total = symbol.DataCodewords + symbol.EccCodewords;
        var full = new byte[total];

        var blocks = symbol.BlockCount;
        var maxDataBlock = 0;
        for (var i = 0; i < blocks; i++) {
            if (symbol.DataBlockSizes[i] > maxDataBlock) maxDataBlock = symbol.DataBlockSizes[i];
        }

        var dataBlocks = new byte[blocks][];
        var eccBlocks = new byte[blocks][];
        var dataOffset = 0;
        var divisor = DataMatrixReedSolomon.ComputeDivisor(symbol.EccBlockSize);

        for (var i = 0; i < blocks; i++) {
            var blockSize = symbol.DataBlockSizes[i];
            var block = new byte[blockSize];
            dataCodewords.CopyTo(dataOffset, block, 0, blockSize);
            dataOffset += blockSize;
            dataBlocks[i] = block;
            eccBlocks[i] = DataMatrixReedSolomon.ComputeRemainder(block, divisor);
        }

        var offset = 0;
        for (var i = 0; i < maxDataBlock; i++) {
            for (var b = 0; b < blocks; b++) {
                var block = dataBlocks[b];
                if (i >= block.Length) continue;
                full[offset++] = block[i];
            }
        }

        for (var i = 0; i < symbol.EccBlockSize; i++) {
            for (var b = 0; b < blocks; b++) {
                full[offset++] = eccBlocks[b][i];
            }
        }

        return full;
    }

    private static BitMatrix BuildSymbol(BitMatrix dataRegion, DataMatrixSymbolInfo symbol) {
        var matrix = new BitMatrix(symbol.SymbolCols, symbol.SymbolRows);
        var regionRows = symbol.RegionRows;
        var regionCols = symbol.RegionCols;
        var regionTotalRows = symbol.RegionTotalRows;
        var regionTotalCols = symbol.RegionTotalCols;
        var regionDataRows = symbol.RegionDataRows;
        var regionDataCols = symbol.RegionDataCols;

        for (var regionRow = 0; regionRow < regionRows; regionRow++) {
            for (var regionCol = 0; regionCol < regionCols; regionCol++) {
                var startRow = regionRow * regionTotalRows;
                var startCol = regionCol * regionTotalCols;

                // Top border (alternating)
                for (var x = 0; x < regionTotalCols; x++) {
                    matrix[startCol + x, startRow] = (x & 1) == 0;
                }
                // Bottom border (solid)
                for (var x = 0; x < regionTotalCols; x++) {
                    matrix[startCol + x, startRow + regionTotalRows - 1] = true;
                }
                // Left border (solid) + Right border (alternating)
                for (var y = 1; y < regionTotalRows - 1; y++) {
                    matrix[startCol, startRow + y] = true;
                    matrix[startCol + regionTotalCols - 1, startRow + y] = (y & 1) == 0;
                }

                for (var y = 0; y < regionDataRows; y++) {
                    for (var x = 0; x < regionDataCols; x++) {
                        var dataRow = regionRow * regionDataRows + y;
                        var dataCol = regionCol * regionDataCols + x;
                        var value = dataRegion[dataCol, dataRow];
                        matrix[startCol + 1 + x, startRow + 1 + y] = value;
                    }
                }
            }
        }

        return matrix;
    }

    private static bool IsDigit(byte b) => b is >= (byte)'0' and <= (byte)'9';

    private static bool CanEncodeLatin1(string text) {
        for (var i = 0; i < text.Length; i++) {
            if (text[i] > 255) return false;
        }
        return true;
    }

    private static List<byte> EncodeC40Text(string text, bool isText) {
        if (text.Length == 0) return new List<byte> { 254 };
        var sequences = new List<(char Ch, int[] Values)>(text.Length);
        var totalValues = 0;

        for (var i = 0; i < text.Length; i++) {
            var c = text[i];
            var values = new List<int>(3);
            if (!TryEncodeC40Char(c, isText, values)) {
                throw new ArgumentException("Text contains characters not supported by C40/Text encoding.", nameof(text));
            }
            sequences.Add((c, values.ToArray()));
            totalValues += values.Count;
        }

        var tailChars = new List<char>();
        while (totalValues % 3 != 0 && sequences.Count > 0) {
            var last = sequences[sequences.Count - 1];
            sequences.RemoveAt(sequences.Count - 1);
            totalValues -= last.Values.Length;
            tailChars.Insert(0, last.Ch);
        }

        if (sequences.Count == 0) {
            return EncodeAscii(EncodingUtils.Latin1.GetBytes(text));
        }

        var output = new List<byte>(2 + (totalValues / 3) * 2 + 4) {
            (byte)(isText ? 239 : 230)
        };

        var buffer = new List<int>(totalValues);
        for (var i = 0; i < sequences.Count; i++) {
            buffer.AddRange(sequences[i].Values);
        }

        for (var i = 0; i < buffer.Count; i += 3) {
            var full = 1600 * buffer[i] + 40 * buffer[i + 1] + buffer[i + 2] + 1;
            output.Add((byte)(full / 256));
            output.Add((byte)(full % 256));
        }

        output.Add(254); // unlatch to ASCII for padding / tail data

        if (tailChars.Count > 0) {
            var tailText = new string(tailChars.ToArray());
            output.AddRange(EncodeAscii(EncodingUtils.Latin1.GetBytes(tailText)));
        }

        return output;
    }

    private static List<byte> EncodeX12(string text) {
        if (text.Length == 0) return new List<byte> { 254 };
        var values = new List<int>(text.Length);
        for (var i = 0; i < text.Length; i++) {
            if (!TryEncodeX12Char(text[i], out var value)) {
                throw new ArgumentException("Text contains characters not supported by X12 encoding.", nameof(text));
            }
            values.Add(value);
        }

        var tailChars = new List<char>();
        while (values.Count % 3 != 0 && values.Count > 0) {
            tailChars.Insert(0, text[values.Count - 1]);
            values.RemoveAt(values.Count - 1);
        }

        if (values.Count == 0) {
            return EncodeAscii(EncodingUtils.Latin1.GetBytes(text));
        }

        var output = new List<byte>(2 + (values.Count / 3) * 2 + 4) { 238 };
        for (var i = 0; i < values.Count; i += 3) {
            var full = 1600 * values[i] + 40 * values[i + 1] + values[i + 2] + 1;
            output.Add((byte)(full / 256));
            output.Add((byte)(full % 256));
        }

        output.Add(254); // unlatch to ASCII

        if (tailChars.Count > 0) {
            var tailText = new string(tailChars.ToArray());
            output.AddRange(EncodeAscii(EncodingUtils.Latin1.GetBytes(tailText)));
        }

        return output;
    }

    private static List<byte> EncodeEdifact(string text) {
        var values = new List<int>(text.Length + 4);
        for (var i = 0; i < text.Length; i++) {
            var c = text[i];
            if (c is < ' ' or > '_') {
                throw new ArgumentException("Text contains characters not supported by EDIFACT encoding.", nameof(text));
            }
            values.Add(c - 32);
        }

        values.Add(31); // unlatch
        while (values.Count % 4 != 0) {
            values.Add(0);
        }

        var output = new List<byte>(1 + (values.Count / 4) * 3) { 240 };
        for (var i = 0; i < values.Count; i += 4) {
            var bits = (values[i] << 18) | (values[i + 1] << 12) | (values[i + 2] << 6) | values[i + 3];
            output.Add((byte)((bits >> 16) & 0xFF));
            output.Add((byte)((bits >> 8) & 0xFF));
            output.Add((byte)(bits & 0xFF));
        }

        return output;
    }

    private static bool TryEncodeX12Char(char c, out int value) {
        switch (c) {
            case '\r':
                value = 0;
                return true;
            case '*':
                value = 1;
                return true;
            case '>':
                value = 2;
                return true;
            case ' ':
                value = 3;
                return true;
        }

        if (c is >= '0' and <= '9') {
            value = c - '0' + 4;
            return true;
        }

        if (c is >= 'A' and <= 'Z') {
            value = c - 'A' + 14;
            return true;
        }

        value = 0;
        return false;
    }

    private static bool TryEncodeC40Char(char c, bool isText, List<int> values) {
        if (c <= 0x1F) {
            values.Add(0);
            values.Add(c);
            return true;
        }

        if (c == ' ') {
            values.Add(3);
            return true;
        }

        if (c is >= '0' and <= '9') {
            values.Add(c - '0' + 4);
            return true;
        }

        if (!isText && c is >= 'A' and <= 'Z') {
            values.Add(c - 'A' + 14);
            return true;
        }

        if (isText && c is >= 'a' and <= 'z') {
            values.Add(c - 'a' + 14);
            return true;
        }

        var shift2Index = Array.IndexOf(C40_SHIFT2_SET_CHARS, c);
        if (shift2Index >= 0) {
            values.Add(1);
            values.Add(shift2Index);
            return true;
        }

        if (c == Gs1.GroupSeparator) {
            values.Add(1);
            values.Add(27);
            return true;
        }

        if (c is >= (char)128 and <= (char)255) {
            values.Add(1);
            values.Add(30); // upper shift
            return TryEncodeC40Char((char)(c - 128), isText, values);
        }

        var shift3Set = isText ? TEXT_SHIFT3_SET_CHARS : C40_SHIFT3_SET_CHARS;
        var shift3Index = Array.IndexOf(shift3Set, c);
        if (shift3Index >= 0) {
            values.Add(2);
            values.Add(shift3Index);
            return true;
        }

        return false;
    }

    private static readonly char[] C40_SHIFT2_SET_CHARS = {
        '!', '"', '#', '$', '%', '&', '\'', '(', ')', '*', '+', ',', '-', '.', '/', ':',
        ';', '<', '=', '>', '?', '@', '[', '\\', ']', '^', '_'
    };

    private static readonly char[] C40_SHIFT3_SET_CHARS = "`abcdefghijklmnopqrstuvwxyz{|}~\u007f".ToCharArray();

    private static readonly char[] TEXT_SHIFT3_SET_CHARS = "`ABCDEFGHIJKLMNOPQRSTUVWXYZ{|}~\u007f".ToCharArray();

    private static byte Randomize253(byte value, int position) {
        var pseudo = ((149 * position) % 253) + 1;
        var temp = value + pseudo;
        if (temp > 254) temp -= 254;
        return (byte)temp;
    }

    private static byte Randomize255(byte value, int position) {
        var pseudo = ((149 * position) % 255) + 1;
        var temp = value + pseudo;
        if (temp > 255) temp -= 256;
        return (byte)temp;
    }
}
