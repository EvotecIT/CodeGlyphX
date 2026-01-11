using System;
using System.Collections.Generic;
using System.Text;
using CodeGlyphX.Internal;
using CodeGlyphX;

#if NET8_0_OR_GREATER
using PixelSpan = System.ReadOnlySpan<byte>;
#else
using PixelSpan = byte[];
#endif

namespace CodeGlyphX.DataMatrix;

/// <summary>
/// Decodes Data Matrix (ECC200) symbols.
/// </summary>
public static class DataMatrixDecoder {
    private enum DataMatrixEncodation {
        Ascii,
        C40,
        Text,
        X12,
        Edifact,
        Base256
    }

    private static readonly char[] C40_SHIFT2_SET_CHARS = {
        '!', '"', '#', '$', '%', '&', '\'', '(', ')', '*', '+', ',', '-', '.', '/', ':',
        ';', '<', '=', '>', '?', '@', '[', '\\', ']', '^', '_'
    };

    private static readonly char[] TEXT_SHIFT2_SET_CHARS = C40_SHIFT2_SET_CHARS;

    private static readonly char[] C40_SHIFT3_SET_CHARS = "`abcdefghijklmnopqrstuvwxyz{|}~\u007f".ToCharArray();

    private static readonly char[] TEXT_SHIFT3_SET_CHARS = "`ABCDEFGHIJKLMNOPQRSTUVWXYZ{|}~\u007f".ToCharArray();
    /// <summary>
    /// Attempts to decode a Data Matrix symbol from a module matrix.
    /// </summary>
    public static bool TryDecode(BitMatrix modules, out string value) {
        if (modules is null) throw new ArgumentNullException(nameof(modules));

        if (!DataMatrixSymbolInfo.TryGetForSize(modules.Height, modules.Width, out var symbol)) {
            value = string.Empty;
            return false;
        }

        var dataRegion = ExtractDataRegion(modules, symbol);
        var codewords = DataMatrixPlacement.ReadCodewords(dataRegion, symbol.CodewordCount);
        if (!TryDecodeCodewords(codewords, symbol, out value)) {
            value = string.Empty;
            return false;
        }

        return true;
    }

#if NET8_0_OR_GREATER
    /// <summary>
    /// Attempts to decode a Data Matrix symbol from pixels.
    /// </summary>
    public static bool TryDecode(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat format, out string value) {
        return TryDecodePixels(pixels, width, height, stride, format, out value);
    }
#endif

    /// <summary>
    /// Attempts to decode a Data Matrix symbol from pixels.
    /// </summary>
    public static bool TryDecode(byte[] pixels, int width, int height, int stride, PixelFormat format, out string value) {
        if (pixels is null) throw new ArgumentNullException(nameof(pixels));
        return TryDecodePixels(pixels, width, height, stride, format, out value);
    }

    private static bool TryDecodePixels(PixelSpan pixels, int width, int height, int stride, PixelFormat format, out string value) {
        if (TryExtractModules(pixels, width, height, stride, format, out var modules)) {
            if (TryDecode(modules, out value)) return true;
            if (TryDecode(Rotate90(modules), out value)) return true;
            if (TryDecode(Rotate180(modules), out value)) return true;
            if (TryDecode(Rotate270(modules), out value)) return true;
        }
        value = string.Empty;
        return false;
    }

    private static BitMatrix ExtractDataRegion(BitMatrix modules, DataMatrixSymbolInfo symbol) {
        var dataRegion = new BitMatrix(symbol.DataRegionCols, symbol.DataRegionRows);
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
                for (var y = 0; y < regionDataRows; y++) {
                    for (var x = 0; x < regionDataCols; x++) {
                        var dataRow = regionRow * regionDataRows + y;
                        var dataCol = regionCol * regionDataCols + x;
                        dataRegion[dataCol, dataRow] = modules[startCol + 1 + x, startRow + 1 + y];
                    }
                }
            }
        }

        return dataRegion;
    }

    private static bool TryDecodeCodewords(byte[] codewords, DataMatrixSymbolInfo symbol, out string value) {
        var blocks = symbol.BlockCount;
        var maxDataBlock = 0;
        for (var i = 0; i < blocks; i++) {
            if (symbol.DataBlockSizes[i] > maxDataBlock) maxDataBlock = symbol.DataBlockSizes[i];
        }

        var dataBlocks = new byte[blocks][];
        var eccBlocks = new byte[blocks][];
        for (var i = 0; i < blocks; i++) {
            dataBlocks[i] = new byte[symbol.DataBlockSizes[i]];
            eccBlocks[i] = new byte[symbol.EccBlockSize];
        }

        var offset = 0;
        for (var i = 0; i < maxDataBlock; i++) {
            for (var b = 0; b < blocks; b++) {
                if (i >= dataBlocks[b].Length) continue;
                if (offset >= codewords.Length) return Fail(out value);
                dataBlocks[b][i] = codewords[offset++];
            }
        }

        for (var i = 0; i < symbol.EccBlockSize; i++) {
            for (var b = 0; b < blocks; b++) {
                if (offset >= codewords.Length) return Fail(out value);
                eccBlocks[b][i] = codewords[offset++];
            }
        }

        var data = new byte[symbol.DataCodewords];
        var dataOffset = 0;
        for (var b = 0; b < blocks; b++) {
            var block = new byte[dataBlocks[b].Length + eccBlocks[b].Length];
            Buffer.BlockCopy(dataBlocks[b], 0, block, 0, dataBlocks[b].Length);
            Buffer.BlockCopy(eccBlocks[b], 0, block, dataBlocks[b].Length, eccBlocks[b].Length);
            if (!DataMatrixReedSolomonDecoder.TryCorrectInPlace(block, symbol.EccBlockSize)) return Fail(out value);
            Buffer.BlockCopy(block, 0, data, dataOffset, dataBlocks[b].Length);
            dataOffset += dataBlocks[b].Length;
        }

        value = DecodeData(data);
        return true;
    }

    private static bool Fail(out string value) {
        value = string.Empty;
        return false;
    }

    internal static string DecodeDataCodewords(byte[] dataCodewords) {
        if (dataCodewords is null) throw new ArgumentNullException(nameof(dataCodewords));
        return DecodeData(dataCodewords);
    }

    private static string DecodeData(PixelSpan data) {
        var sb = new StringBuilder(data.Length);
        var mode = DataMatrixEncodation.Ascii;
        var index = 0;
        var upperShift = false;
        string? macroTrailer = null;
        Encoding? base256Encoding = null;

        while (index < data.Length) {
            switch (mode) {
                case DataMatrixEncodation.Ascii:
                    mode = DecodeAsciiSegment(data, ref index, sb, ref upperShift, ref macroTrailer, ref base256Encoding);
                    break;
                case DataMatrixEncodation.C40:
                    mode = DecodeC40TextSegment(data, ref index, sb, isText: false, ref upperShift);
                    break;
                case DataMatrixEncodation.Text:
                    mode = DecodeC40TextSegment(data, ref index, sb, isText: true, ref upperShift);
                    break;
                case DataMatrixEncodation.X12:
                    mode = DecodeX12Segment(data, ref index, sb, ref upperShift);
                    break;
                case DataMatrixEncodation.Edifact:
                    mode = DecodeEdifactSegment(data, ref index, sb, ref upperShift);
                    break;
                case DataMatrixEncodation.Base256:
                    DecodeBase256Segment(data, ref index, sb, base256Encoding);
                    mode = DataMatrixEncodation.Ascii;
                    break;
                default:
                    index = data.Length;
                    break;
            }
        }

        if (!string.IsNullOrEmpty(macroTrailer)) sb.Append(macroTrailer);
        return sb.ToString();
    }

    private static DataMatrixEncodation DecodeAsciiSegment(
        PixelSpan data,
        ref int index,
        StringBuilder sb,
        ref bool upperShift,
        ref string? macroTrailer,
        ref Encoding? base256Encoding) {
        if (index >= data.Length) return DataMatrixEncodation.Ascii;

        var cw = data[index++];

        if (cw == 129) {
            index = data.Length;
            return DataMatrixEncodation.Ascii;
        }

        if (cw <= 128) {
            AppendChar(sb, (char)(cw - 1), ref upperShift);
            return DataMatrixEncodation.Ascii;
        }

        if (cw <= 229) {
            var val = cw - 130;
            AppendChar(sb, (char)('0' + (val / 10)), ref upperShift);
            AppendChar(sb, (char)('0' + (val % 10)), ref upperShift);
            return DataMatrixEncodation.Ascii;
        }

        switch (cw) {
            case 230:
                return DataMatrixEncodation.C40;
            case 231:
                return DataMatrixEncodation.Base256;
            case 232:
                sb.Append(Gs1.GroupSeparator);
                return DataMatrixEncodation.Ascii;
            case 233:
                if (index + 1 < data.Length) index += 2;
                return DataMatrixEncodation.Ascii;
            case 234:
                return DataMatrixEncodation.Ascii;
            case 235:
                upperShift = true;
                return DataMatrixEncodation.Ascii;
            case 236:
                sb.Append("[)>\u001E05\u001D");
                macroTrailer ??= "\u001E\u0004";
                return DataMatrixEncodation.Ascii;
            case 237:
                sb.Append("[)>\u001E06\u001D");
                macroTrailer ??= "\u001E\u0004";
                return DataMatrixEncodation.Ascii;
            case 238:
                return DataMatrixEncodation.X12;
            case 239:
                return DataMatrixEncodation.Text;
            case 240:
                return DataMatrixEncodation.Edifact;
            case 241:
                if (index < data.Length) {
                    // Best-effort ECI: skip one codeword for now.
                    index++;
                }
                return DataMatrixEncodation.Ascii;
            default:
                return DataMatrixEncodation.Ascii;
        }
    }

    private static DataMatrixEncodation DecodeC40TextSegment(
        PixelSpan data,
        ref int index,
        StringBuilder sb,
        bool isText,
        ref bool upperShift) {
        var shift = 0;

        while (index < data.Length) {
            var cw1 = data[index];
            if (cw1 == 254) {
                index++;
                return DataMatrixEncodation.Ascii;
            }

            if (index + 1 >= data.Length) {
                index = data.Length;
                return DataMatrixEncodation.Ascii;
            }

            var cw2 = data[index + 1];
            index += 2;

            ParseTwoBytes(cw1, cw2, out var c1, out var c2, out var c3);
            DecodeC40TextValue(c1, sb, isText, ref shift, ref upperShift);
            DecodeC40TextValue(c2, sb, isText, ref shift, ref upperShift);
            DecodeC40TextValue(c3, sb, isText, ref shift, ref upperShift);
        }

        return DataMatrixEncodation.Ascii;
    }

    private static void DecodeC40TextValue(int value, StringBuilder sb, bool isText, ref int shift, ref bool upperShift) {
        if (shift == 0) {
            if (value <= 2) {
                shift = value + 1;
                return;
            }
            if (value == 3) {
                AppendChar(sb, ' ', ref upperShift);
                return;
            }
            if (value <= 13) {
                AppendChar(sb, (char)('0' + (value - 4)), ref upperShift);
                return;
            }
            if (value <= 39) {
                var baseChar = isText ? 'a' : 'A';
                AppendChar(sb, (char)(baseChar + (value - 14)), ref upperShift);
            }
            return;
        }

        if (shift == 1) {
            AppendChar(sb, (char)value, ref upperShift);
            shift = 0;
            return;
        }

        if (shift == 2) {
            if (value < C40_SHIFT2_SET_CHARS.Length) {
                AppendChar(sb, C40_SHIFT2_SET_CHARS[value], ref upperShift);
            } else if (value == 27) {
                sb.Append(Gs1.GroupSeparator);
            } else if (value == 30) {
                upperShift = true;
            }
            shift = 0;
            return;
        }

        if (shift == 3) {
            var set = isText ? TEXT_SHIFT3_SET_CHARS : C40_SHIFT3_SET_CHARS;
            if (value < set.Length) {
                AppendChar(sb, set[value], ref upperShift);
            }
            shift = 0;
        }
    }

    private static DataMatrixEncodation DecodeX12Segment(
        PixelSpan data,
        ref int index,
        StringBuilder sb,
        ref bool upperShift) {
        while (index < data.Length) {
            var cw1 = data[index];
            if (cw1 == 254) {
                index++;
                return DataMatrixEncodation.Ascii;
            }

            if (index + 1 >= data.Length) {
                index = data.Length;
                return DataMatrixEncodation.Ascii;
            }

            var cw2 = data[index + 1];
            index += 2;

            ParseTwoBytes(cw1, cw2, out var c1, out var c2, out var c3);
            DecodeX12Value(c1, sb, ref upperShift);
            DecodeX12Value(c2, sb, ref upperShift);
            DecodeX12Value(c3, sb, ref upperShift);
        }

        return DataMatrixEncodation.Ascii;
    }

    private static void DecodeX12Value(int value, StringBuilder sb, ref bool upperShift) {
        if (value == 0) {
            AppendChar(sb, '\r', ref upperShift);
            return;
        }
        if (value == 1) {
            AppendChar(sb, '*', ref upperShift);
            return;
        }
        if (value == 2) {
            AppendChar(sb, '>', ref upperShift);
            return;
        }
        if (value == 3) {
            AppendChar(sb, ' ', ref upperShift);
            return;
        }
        if (value <= 13) {
            AppendChar(sb, (char)('0' + (value - 4)), ref upperShift);
            return;
        }
        if (value <= 39) {
            AppendChar(sb, (char)('A' + (value - 14)), ref upperShift);
        }
    }

    private static DataMatrixEncodation DecodeEdifactSegment(
        PixelSpan data,
        ref int index,
        StringBuilder sb,
        ref bool upperShift) {
        while (index + 2 < data.Length) {
            var cw1 = data[index++];
            var cw2 = data[index++];
            var cw3 = data[index++];

            var bits = (cw1 << 16) | (cw2 << 8) | cw3;
            for (var i = 0; i < 4; i++) {
                var value = (bits >> (18 - (6 * i))) & 0x3F;
                if (value == 0x1F) {
                    return DataMatrixEncodation.Ascii;
                }
                AppendChar(sb, (char)(value + 32), ref upperShift);
            }
        }

        index = data.Length;
        return DataMatrixEncodation.Ascii;
    }

    private static void DecodeBase256Segment(
        PixelSpan data,
        ref int index,
        StringBuilder sb,
        Encoding? encoding) {
        if (index >= data.Length) return;

        var base256Bytes = new List<byte>();
        var lenCodeword = Unrandomize255(data[index], index + 1);
        index++;
        var length = lenCodeword;
        if (lenCodeword >= 250) {
            if (index >= data.Length) return;
            var len2 = Unrandomize255(data[index], index + 1);
            index++;
            length = (lenCodeword - 249) * 250 + len2;
        }

        for (var j = 0; j < length && index < data.Length; j++) {
            var b = Unrandomize255(data[index], index + 1);
            base256Bytes.Add((byte)b);
            index++;
        }

        if (base256Bytes.Count == 0) return;

        sb.Append(DecodeBase256Bytes(base256Bytes, encoding));
    }

    private static string DecodeBase256Bytes(List<byte> bytes, Encoding? encoding) {
        if (bytes.Count == 0) return string.Empty;
        try {
            if (encoding is not null) return encoding.GetString(bytes.ToArray());
            var utf8 = new UTF8Encoding(false, true);
            return utf8.GetString(bytes.ToArray());
        } catch (DecoderFallbackException) {
            return EncodingUtils.Latin1.GetString(bytes.ToArray());
        }
    }

    private static void AppendChar(StringBuilder sb, char value, ref bool upperShift) {
        if (upperShift) {
            value = (char)(value + 128);
            upperShift = false;
        }
        sb.Append(value);
    }

    private static void ParseTwoBytes(byte cw1, byte cw2, out int c1, out int c2, out int c3) {
        var full = (cw1 << 8) + cw2 - 1;
        c1 = full / 1600;
        var rem = full % 1600;
        c2 = rem / 40;
        c3 = rem % 40;
    }

    private static int Unrandomize255(byte value, int position) {
        var pseudo = ((149 * position) % 255) + 1;
        var temp = value - pseudo;
        if (temp < 0) temp += 256;
        return temp;
    }

    private static int Clamp(int value, int min, int max) {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }

    private static bool TryExtractModules(PixelSpan pixels, int width, int height, int stride, PixelFormat format, out BitMatrix modules) {
        modules = null!;
        if (width <= 0 || height <= 0 || stride <= 0) return false;

        var invert = false;
        if (!TryFindBoundingBox(pixels, width, height, stride, format, invert: false, out var box)) {
            if (!TryFindBoundingBox(pixels, width, height, stride, format, invert: true, out box)) return false;
            invert = true;
        }

        if (box.Width <= 1 || box.Height <= 1) return false;

        if (!TryEstimateModuleSize(pixels, width, height, stride, format, box, invert, out var moduleSize)) {
            return false;
        }

        var cols = (int)Math.Round((double)box.Width / moduleSize);
        var rows = (int)Math.Round((double)box.Height / moduleSize);
        if (cols <= 0 || rows <= 0) return false;

        modules = new BitMatrix(cols, rows);
        var half = moduleSize / 2.0;
        for (var y = 0; y < rows; y++) {
            var sy = (int)Math.Round(box.Top + (y * moduleSize) + half);
            sy = Clamp(sy, 0, height - 1);
            for (var x = 0; x < cols; x++) {
                var sx = (int)Math.Round(box.Left + (x * moduleSize) + half);
                sx = Clamp(sx, 0, width - 1);
                var dark = IsDark(pixels, width, height, stride, format, sx, sy);
                modules[x, y] = invert ? !dark : dark;
            }
        }

        return true;
    }

    private static bool TryEstimateModuleSize(PixelSpan pixels, int width, int height, int stride, PixelFormat format, BoundingBox box, bool invert, out int moduleSize) {
        moduleSize = 0;

        var midY = box.Top + box.Height / 2;
        var midX = box.Left + box.Width / 2;

        if (!TryFindMinRun(pixels, width, height, stride, format, box.Left, box.Right, midY, horizontal: true, invert, out var hMin)) {
            return false;
        }
        if (!TryFindMinRun(pixels, width, height, stride, format, box.Top, box.Bottom, midX, horizontal: false, invert, out var vMin)) {
            return false;
        }

        moduleSize = Math.Min(hMin, vMin);
        return moduleSize > 0;
    }

    private static bool TryFindMinRun(PixelSpan pixels, int width, int height, int stride, PixelFormat format, int start, int end, int fixedPos, bool horizontal, bool invert, out int minRun) {
        minRun = int.MaxValue;

        var prev = false;
        var run = 0;
        var sawAny = false;

        for (var i = start; i <= end; i++) {
            var x = horizontal ? i : fixedPos;
            var y = horizontal ? fixedPos : i;
            var dark = IsDark(pixels, width, height, stride, format, x, y);
            var bit = invert ? !dark : dark;

            if (!sawAny) {
                prev = bit;
                run = 1;
                sawAny = true;
                continue;
            }

            if (bit == prev) {
                run++;
            } else {
                if (run > 0 && run < minRun) minRun = run;
                prev = bit;
                run = 1;
            }
        }

        if (run > 0 && run < minRun) minRun = run;
        if (minRun == int.MaxValue) return false;

        return true;
    }

    private static bool TryFindBoundingBox(PixelSpan pixels, int width, int height, int stride, PixelFormat format, bool invert, out BoundingBox box) {
        var left = width;
        var right = -1;
        var top = height;
        var bottom = -1;

        for (var y = 0; y < height; y++) {
            var row = y * stride;
            for (var x = 0; x < width; x++) {
                var dark = IsDarkAt(pixels, row, x, format);
                if (invert) dark = !dark;
                if (!dark) continue;

                if (x < left) left = x;
                if (x > right) right = x;
                if (y < top) top = y;
                if (y > bottom) bottom = y;
            }
        }

        if (right < left || bottom < top) {
            box = default;
            return false;
        }

        box = new BoundingBox(left, top, right, bottom);
        return true;
    }

    private static bool IsDark(PixelSpan pixels, int width, int height, int stride, PixelFormat format, int x, int y) {
        if ((uint)x >= (uint)width || (uint)y >= (uint)height) return false;
        var row = y * stride;
        return IsDarkAt(pixels, row, x, format);
    }

    private static bool IsDarkAt(PixelSpan pixels, int row, int x, PixelFormat format) {
        var p = row + x * 4;
        byte r;
        byte g;
        byte b;
        if (format == PixelFormat.Bgra32) {
            b = pixels[p + 0];
            g = pixels[p + 1];
            r = pixels[p + 2];
        } else {
            r = pixels[p + 0];
            g = pixels[p + 1];
            b = pixels[p + 2];
        }

        var lum = (r * 77 + g * 150 + b * 29) >> 8;
        return lum < 128;
    }

    private static BitMatrix Rotate90(BitMatrix matrix) {
        var result = new BitMatrix(matrix.Height, matrix.Width);
        for (var y = 0; y < matrix.Height; y++) {
            for (var x = 0; x < matrix.Width; x++) {
                result[matrix.Height - 1 - y, x] = matrix[x, y];
            }
        }
        return result;
    }

    private static BitMatrix Rotate180(BitMatrix matrix) {
        var result = new BitMatrix(matrix.Width, matrix.Height);
        for (var y = 0; y < matrix.Height; y++) {
            for (var x = 0; x < matrix.Width; x++) {
                result[matrix.Width - 1 - x, matrix.Height - 1 - y] = matrix[x, y];
            }
        }
        return result;
    }

    private static BitMatrix Rotate270(BitMatrix matrix) {
        var result = new BitMatrix(matrix.Height, matrix.Width);
        for (var y = 0; y < matrix.Height; y++) {
            for (var x = 0; x < matrix.Width; x++) {
                result[y, matrix.Width - 1 - x] = matrix[x, y];
            }
        }
        return result;
    }

    private readonly struct BoundingBox {
        public int Left { get; }
        public int Top { get; }
        public int Right { get; }
        public int Bottom { get; }
        public int Width => Right - Left + 1;
        public int Height => Bottom - Top + 1;

        public BoundingBox(int left, int top, int right, int bottom) {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }
    }
}
