using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading;
using CodeGlyphX;
using CodeGlyphX.Pdf417.Ec;

#if NET8_0_OR_GREATER
using PixelSpan = System.ReadOnlySpan<byte>;
#else
using PixelSpan = byte[];
#endif

namespace CodeGlyphX.Pdf417;

public static partial class Pdf417Decoder {
    private static bool TryExtractModules(PixelSpan pixels, int width, int height, int stride, PixelFormat format, int threshold, out BitMatrix modules) {
        modules = null!;
        if (width <= 0 || height <= 0 || stride <= 0) return false;

        var invert = false;
        if (!TryFindBoundingBox(pixels, width, height, stride, format, threshold, invert: false, out var box)) {
            if (!TryFindBoundingBox(pixels, width, height, stride, format, threshold, invert: true, out box)) return false;
            invert = true;
        }

        if (box.Width <= 1 || box.Height <= 1) return false;
        if (!TryEstimateModuleSize(pixels, width, height, stride, format, threshold, box, invert, out var moduleSize)) return false;

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
                var dark = IsDark(pixels, width, height, stride, format, sx, sy, threshold);
                modules[x, y] = invert ? !dark : dark;
            }
        }

        return true;
    }

    private static bool TryEstimateModuleSize(PixelSpan pixels, int width, int height, int stride, PixelFormat format, int threshold, BoundingBox box, bool invert, out int moduleSize) {
        moduleSize = 0;
        var midY = box.Top + box.Height / 2;
        var midX = box.Left + box.Width / 2;

        if (!TryFindMinRun(pixels, width, height, stride, format, threshold, box.Left, box.Right, midY, horizontal: true, invert, out var hMin)) return false;
        if (!TryFindMinRun(pixels, width, height, stride, format, threshold, box.Top, box.Bottom, midX, horizontal: false, invert, out var vMin)) return false;

        moduleSize = Math.Min(hMin, vMin);
        return moduleSize > 0;
    }

    private static bool TryFindMinRun(PixelSpan pixels, int width, int height, int stride, PixelFormat format, int threshold, int start, int end, int fixedPos, bool horizontal, bool invert, out int minRun) {
        minRun = int.MaxValue;
        var prev = false;
        var run = 0;
        var sawAny = false;

        for (var i = start; i <= end; i++) {
            var x = horizontal ? i : fixedPos;
            var y = horizontal ? fixedPos : i;
            var dark = IsDark(pixels, width, height, stride, format, x, y, threshold);
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
        return minRun != int.MaxValue;
    }

    private static bool TryFindBoundingBox(PixelSpan pixels, int width, int height, int stride, PixelFormat format, int threshold, bool invert, out BoundingBox box) {
        var left = width;
        var right = -1;
        var top = height;
        var bottom = -1;

        for (var y = 0; y < height; y++) {
            var row = y * stride;
            for (var x = 0; x < width; x++) {
                var dark = IsDarkAt(pixels, row, x, format, threshold);
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

        var found = new BoundingBox(left, top, right, bottom);
        var trimmed = TrimBoundingBox(pixels, width, height, stride, format, threshold, found, invert);
        box = trimmed.Width >= 3 && trimmed.Height >= 3 ? trimmed : found;
        return true;
    }

    private static BoundingBox TrimBoundingBox(PixelSpan pixels, int width, int height, int stride, PixelFormat format, int threshold, BoundingBox box, bool invert) {
        var left = box.Left;
        var right = box.Right;
        var top = box.Top;
        var bottom = box.Bottom;

        var rowThreshold = Math.Max(2, (right - left + 1) / 40);
        var colThreshold = Math.Max(2, (bottom - top + 1) / 40);

        while (top <= bottom && CountDarkRow(pixels, width, height, stride, format, threshold, left, right, top, invert) <= rowThreshold) top++;
        while (top <= bottom && CountDarkRow(pixels, width, height, stride, format, threshold, left, right, bottom, invert) <= rowThreshold) bottom--;
        while (left <= right && CountDarkCol(pixels, width, height, stride, format, threshold, left, top, bottom, invert) <= colThreshold) left++;
        while (left <= right && CountDarkCol(pixels, width, height, stride, format, threshold, right, top, bottom, invert) <= colThreshold) right--;

        if (right < left || bottom < top) return box;
        return new BoundingBox(left, top, right, bottom);
    }

    private static int CountDarkRow(PixelSpan pixels, int width, int height, int stride, PixelFormat format, int threshold, int left, int right, int y, bool invert) {
        if ((uint)y >= (uint)height) return 0;
        var row = y * stride;
        var count = 0;
        for (var x = left; x <= right; x++) {
            if ((uint)x >= (uint)width) continue;
            var dark = IsDarkAt(pixels, row, x, format, threshold);
            if (invert) dark = !dark;
            if (dark) count++;
        }

        return count;
    }

    private static int CountDarkCol(PixelSpan pixels, int width, int height, int stride, PixelFormat format, int threshold, int x, int top, int bottom, bool invert) {
        if ((uint)x >= (uint)width) return 0;
        var count = 0;
        for (var y = top; y <= bottom; y++) {
            if ((uint)y >= (uint)height) continue;
            var row = y * stride;
            var dark = IsDarkAt(pixels, row, x, format, threshold);
            if (invert) dark = !dark;
            if (dark) count++;
        }

        return count;
    }

    private static bool IsDark(PixelSpan pixels, int width, int height, int stride, PixelFormat format, int x, int y, int threshold) {
        if ((uint)x >= (uint)width || (uint)y >= (uint)height) return false;
        var row = y * stride;
        return IsDarkAt(pixels, row, x, format, threshold);
    }

    private static bool IsDarkAt(PixelSpan pixels, int row, int x, PixelFormat format, int threshold) {
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
        return lum < threshold;
    }

    private static IEnumerable<int> BuildThresholds(PixelSpan pixels, int width, int height, int stride, PixelFormat format) {
        var list = new List<int>(4) { DefaultThreshold };
        if (TryGetLuminanceRange(pixels, width, height, stride, format, out var min, out var max)) {
            var range = max - min;
            if (range > 8) {
                list.Add(min + range / 2);
                list.Add(min + range / 3);
                list.Add(min + (range * 2) / 3);
            }
        }

        for (var i = 0; i < list.Count; i++) {
            var t = list[i];
            if (t < 0) t = 0;
            if (t > 255) t = 255;
            list[i] = t;
        }

        list.Sort();
        for (var i = list.Count - 1; i > 0; i--) {
            if (list[i] == list[i - 1]) list.RemoveAt(i);
        }

        return list;
    }

    private static bool TryGetLuminanceRange(PixelSpan pixels, int width, int height, int stride, PixelFormat format, out int min, out int max) {
        min = 255;
        max = 0;

        if (width <= 0 || height <= 0 || stride <= 0) return false;
        var stepX = Math.Max(1, width / 160);
        var stepY = Math.Max(1, height / 160);

        for (var y = 0; y < height; y += stepY) {
            var row = y * stride;
            for (var x = 0; x < width; x += stepX) {
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
                if (lum < min) min = lum;
                if (lum > max) max = lum;
            }
        }

        return max >= min;
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

    private static int Clamp(int value, int min, int max) {
        if (value < min) return min;
        if (value > max) return max;
        return value;
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

    private readonly struct Candidate {
        public int ModuleSize { get; }
        public int WidthModules { get; }
        public int HeightModules { get; }

        public Candidate(int moduleSize, int widthModules, int heightModules) {
            ModuleSize = moduleSize;
            WidthModules = widthModules;
            HeightModules = heightModules;
        }
    }

}
