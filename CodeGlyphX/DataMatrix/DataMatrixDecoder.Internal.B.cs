using System;
using System.Buffers;
using System.Text;
using System.Threading;
using CodeGlyphX.Internal;
using CodeGlyphX;

#if NET8_0_OR_GREATER
using PixelSpan = System.ReadOnlySpan<byte>;
#else
using PixelSpan = byte[];
#endif

namespace CodeGlyphX.DataMatrix;

public static partial class DataMatrixDecoder {
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

        var lenCodeword = Unrandomize255(data[index], index + 1);
        index++;
        var length = lenCodeword;
        if (lenCodeword >= 250) {
            if (index >= data.Length) return;
            var len2 = Unrandomize255(data[index], index + 1);
            index++;
            length = (lenCodeword - 249) * 250 + len2;
        }

        if (length <= 0) return;

        var rented = ArrayPool<byte>.Shared.Rent(length);
        var count = 0;
        try {
            for (var j = 0; j < length && index < data.Length; j++) {
                var b = Unrandomize255(data[index], index + 1);
                rented[count++] = (byte)b;
                index++;
            }

            if (count == 0) return;

            sb.Append(DecodeBase256Bytes(rented, count, encoding));
        } finally {
            ArrayPool<byte>.Shared.Return(rented);
        }
    }

    private static string DecodeBase256Bytes(byte[] bytes, int count, Encoding? encoding) {
        if (count == 0) return string.Empty;
        try {
            if (encoding is not null) return encoding.GetString(bytes, 0, count);
            return EncodingUtils.Utf8Strict.GetString(bytes, 0, count);
        } catch (DecoderFallbackException) {
            return EncodingUtils.Latin1.GetString(bytes, 0, count);
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

    private static BitMatrix MirrorX(BitMatrix matrix) {
        var result = new BitMatrix(matrix.Width, matrix.Height);
        for (var y = 0; y < matrix.Height; y++) {
            for (var x = 0; x < matrix.Width; x++) {
                result[matrix.Width - 1 - x, y] = matrix[x, y];
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
