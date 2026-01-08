using System;
using System.Collections.Generic;
using System.Text;

namespace CodeMatrix.DataMatrix;

/// <summary>
/// Decodes Data Matrix (ECC200) symbols.
/// </summary>
public static class DataMatrixDecoder {
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

    /// <summary>
    /// Attempts to decode a Data Matrix symbol from pixels.
    /// </summary>
    public static bool TryDecode(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat format, out string value) {
        if (TryExtractModules(pixels, width, height, stride, format, out var modules)) {
            if (TryDecode(modules, out value)) return true;
            if (TryDecode(Rotate90(modules), out value)) return true;
            if (TryDecode(Rotate180(modules), out value)) return true;
            if (TryDecode(Rotate270(modules), out value)) return true;
        }
        value = string.Empty;
        return false;
    }

    /// <summary>
    /// Attempts to decode a Data Matrix symbol from pixels.
    /// </summary>
    public static bool TryDecode(byte[] pixels, int width, int height, int stride, PixelFormat format, out string value) {
        if (pixels is null) throw new ArgumentNullException(nameof(pixels));
        return TryDecode((ReadOnlySpan<byte>)pixels, width, height, stride, format, out value);
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

        value = DecodeAscii(data);
        return true;
    }

    private static bool Fail(out string value) {
        value = string.Empty;
        return false;
    }

    private static string DecodeAscii(ReadOnlySpan<byte> data) {
        var sb = new StringBuilder(data.Length);
        var base256Bytes = new List<byte>();

        for (var i = 0; i < data.Length; i++) {
            var cw = data[i];
            var pos = i + 1; // 1-based position

            if (cw == 129) break; // Padding

            if (cw <= 128) {
                sb.Append((char)(cw - 1));
                continue;
            }

            if (cw <= 229) {
                var val = cw - 130;
                sb.Append((char)('0' + (val / 10)));
                sb.Append((char)('0' + (val % 10)));
                continue;
            }

            if (cw == 231) {
                base256Bytes.Clear();
                if (i + 1 >= data.Length) break;

                var lenCodeword = Unrandomize255(data[++i], pos + 1);
                var length = lenCodeword;
                if (lenCodeword >= 250) {
                    if (i + 1 >= data.Length) break;
                    var len2 = Unrandomize255(data[++i], pos + 2);
                    length = (lenCodeword - 249) * 250 + len2;
                }

                for (var j = 0; j < length && i + 1 < data.Length; j++) {
                    var idx = ++i;
                    var b = Unrandomize255(data[idx], idx + 1);
                    base256Bytes.Add((byte)b);
                }

                if (base256Bytes.Count > 0) {
                    sb.Append(DecodeBase256Bytes(base256Bytes));
                }
                continue;
            }

            // Unsupported encodation modes (C40/Text/X12/EDIFACT).
            break;
        }

        return sb.ToString();
    }

    private static string DecodeBase256Bytes(List<byte> bytes) {
        if (bytes.Count == 0) return string.Empty;
        try {
            var utf8 = new UTF8Encoding(false, true);
            return utf8.GetString(bytes.ToArray());
        } catch (DecoderFallbackException) {
            return Encoding.Latin1.GetString(bytes.ToArray());
        }
    }

    private static int Unrandomize255(byte value, int position) {
        var pseudo = ((149 * position) % 255) + 1;
        var temp = value - pseudo;
        if (temp < 0) temp += 256;
        return temp;
    }

    private static bool TryExtractModules(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat format, out BitMatrix modules) {
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
            sy = Math.Clamp(sy, 0, height - 1);
            for (var x = 0; x < cols; x++) {
                var sx = (int)Math.Round(box.Left + (x * moduleSize) + half);
                sx = Math.Clamp(sx, 0, width - 1);
                var dark = IsDark(pixels, width, height, stride, format, sx, sy);
                modules[x, y] = invert ? !dark : dark;
            }
        }

        return true;
    }

    private static bool TryEstimateModuleSize(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat format, BoundingBox box, bool invert, out int moduleSize) {
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

    private static bool TryFindMinRun(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat format, int start, int end, int fixedPos, bool horizontal, bool invert, out int minRun) {
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

    private static bool TryFindBoundingBox(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat format, bool invert, out BoundingBox box) {
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

    private static bool IsDark(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat format, int x, int y) {
        if ((uint)x >= (uint)width || (uint)y >= (uint)height) return false;
        var row = y * stride;
        return IsDarkAt(pixels, row, x, format);
    }

    private static bool IsDarkAt(ReadOnlySpan<byte> pixels, int row, int x, PixelFormat format) {
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
