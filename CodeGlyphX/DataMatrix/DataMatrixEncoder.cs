using System;
using System.Collections.Generic;
using System.Text;
using CodeGlyphX.Internal;

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
            mode = CanEncodeLatin1(text) ? DataMatrixEncodingMode.Ascii : DataMatrixEncodingMode.Base256;
        }

        if (mode == DataMatrixEncodingMode.Ascii) {
            if (!CanEncodeLatin1(text)) throw new ArgumentException("Text contains characters outside Latin-1.", nameof(text));
            var bytes = EncodingUtils.Latin1.GetBytes(text);
            return EncodeBytes(bytes, DataMatrixEncodingMode.Ascii);
        }

        var utf8 = Encoding.UTF8.GetBytes(text);
        return EncodeBytes(utf8, DataMatrixEncodingMode.Base256);
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
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unsupported Data Matrix encoding mode.")
        };

        if (!DataMatrixSymbolInfo.TryGetForData(codewords.Count, out var symbol)) {
            throw new ArgumentException("Data too long for supported Data Matrix symbols.", nameof(data));
        }

        PadCodewords(codewords, symbol.DataCodewords);
        var fullCodewords = AddErrorCorrection(codewords, symbol);

        var dataRegion = DataMatrixPlacement.PlaceCodewords(fullCodewords, symbol.DataRegionRows, symbol.DataRegionCols);
        return BuildSymbol(dataRegion, symbol);
    }
#else
    private static BitMatrix EncodeBytesCore(byte[] data, DataMatrixEncodingMode mode) {
        if (mode == DataMatrixEncodingMode.Auto) {
            mode = DataMatrixEncodingMode.Base256;
        }

        var codewords = mode switch {
            DataMatrixEncodingMode.Ascii => EncodeAscii(data),
            DataMatrixEncodingMode.Base256 => EncodeBase256(data),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unsupported Data Matrix encoding mode.")
        };

        if (!DataMatrixSymbolInfo.TryGetForData(codewords.Count, out var symbol)) {
            throw new ArgumentException("Data too long for supported Data Matrix symbols.", nameof(data));
        }

        PadCodewords(codewords, symbol.DataCodewords);
        var fullCodewords = AddErrorCorrection(codewords, symbol);

        var dataRegion = DataMatrixPlacement.PlaceCodewords(fullCodewords, symbol.DataRegionRows, symbol.DataRegionCols);
        return BuildSymbol(dataRegion, symbol);
    }
#endif

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
