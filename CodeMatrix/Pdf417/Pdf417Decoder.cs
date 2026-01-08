using System;
using System.Collections.Generic;
using CodeMatrix.Pdf417.Ec;

namespace CodeMatrix.Pdf417;

/// <summary>
/// Decodes PDF417 barcodes.
/// </summary>
public static class Pdf417Decoder {
    private const int StartPatternWidth = 17;
    private const int StopPatternWidth = 18;

    private static readonly Dictionary<int, int>[] PatternToCodeword = BuildPatternMaps();

    /// <summary>
    /// Attempts to decode a PDF417 symbol from a module matrix.
    /// </summary>
    public static bool TryDecode(BitMatrix modules, out string value) {
        if (modules is null) throw new ArgumentNullException(nameof(modules));

        var width = modules.Width;
        var height = modules.Height;

        if (!TryGetDimensions(width, out var cols, out var compact)) {
            value = string.Empty;
            return false;
        }

        var codewords = new List<int>(cols * height);
        var rowWidth = width;

        for (var rowIndex = 0; rowIndex < height; rowIndex++) {
            var y = height - 1 - rowIndex;
            var cluster = rowIndex % 3;
            var offset = 0;
            offset += StartPatternWidth;

            if (!TryReadCodeword(modules, y, offset, 17, cluster, out _)) {
                value = string.Empty;
                return false;
            }
            offset += 17;

            for (var x = 0; x < cols; x++) {
                if (!TryReadCodeword(modules, y, offset, 17, cluster, out var cw)) {
                    value = string.Empty;
                    return false;
                }
                codewords.Add(cw);
                offset += 17;
            }

            if (!compact) {
                if (!TryReadCodeword(modules, y, offset, 17, cluster, out _)) {
                    value = string.Empty;
                    return false;
                }
                offset += 17;
            }

            offset += compact ? 1 : StopPatternWidth;
            if (offset != rowWidth) {
                value = string.Empty;
                return false;
            }
        }

        if (codewords.Count == 0) {
            value = string.Empty;
            return false;
        }

        var received = codewords.ToArray();
        var total = received.Length;
        var eccCount = 0;
        var corrected = false;

        var ec = new ErrorCorrection();
        for (var level = 0; level <= 8; level++) {
            var k = 1 << (level + 1);
            if (total <= k) continue;
            var expectedLength = total - k;
            var candidate = (int[])received.Clone();
            if (!ec.Decode(candidate, k)) continue;
            if (candidate[0] != expectedLength) continue;
            received = candidate;
            eccCount = k;
            corrected = true;
            break;
        }

        var lengthDescriptor = received[0];
        if (lengthDescriptor <= 0 || lengthDescriptor > total) {
            value = string.Empty;
            return false;
        }
        if (!corrected) {
            eccCount = total - lengthDescriptor;
            if (IsPowerOfTwo(eccCount) && eccCount is >= 2 and <= 512) {
                var candidate = (int[])received.Clone();
                if (ec.Decode(candidate, eccCount)) {
                    received = candidate;
                    lengthDescriptor = received[0];
                }
            }
        }

        if (eccCount > 0 && lengthDescriptor > total - eccCount) {
            value = string.Empty;
            return false;
        }

        var dataCodewords = new int[lengthDescriptor - 1];
        Array.Copy(received, 1, dataCodewords, 0, dataCodewords.Length);
        var decoded = Pdf417DecodedBitStreamParser.Decode(dataCodewords);
        if (decoded is null) {
            value = string.Empty;
            return false;
        }
        value = decoded;
        return true;
    }

    private static bool IsPowerOfTwo(int value) => value > 0 && (value & (value - 1)) == 0;

    /// <summary>
    /// Attempts to decode a PDF417 symbol from pixels.
    /// </summary>
    public static bool TryDecode(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat format, out string value) {
        if (TryDecodeFromPixels(pixels, width, height, stride, format, out value)) return true;
        value = string.Empty;
        return false;
    }

    /// <summary>
    /// Attempts to decode a PDF417 symbol from pixels.
    /// </summary>
    public static bool TryDecode(byte[] pixels, int width, int height, int stride, PixelFormat format, out string value) {
        if (pixels is null) throw new ArgumentNullException(nameof(pixels));
        return TryDecode((ReadOnlySpan<byte>)pixels, width, height, stride, format, out value);
    }

    private static bool TryDecodeFromPixels(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat format, out string value) {
        if (width <= 0 || height <= 0 || stride <= 0) {
            value = string.Empty;
            return false;
        }

        if (TryFindBoundingBox(pixels, width, height, stride, format, invert: false, out var box)) {
            if (TryDecodeFromBox(pixels, width, height, stride, format, box, invert: false, out value)) return true;
        }

        if (TryFindBoundingBox(pixels, width, height, stride, format, invert: true, out box)) {
            if (TryDecodeFromBox(pixels, width, height, stride, format, box, invert: true, out value)) return true;
        }

        // Fallback to legacy extractor.
        if (TryExtractModules(pixels, width, height, stride, format, out var modules)) {
            if (TryDecodeWithRotations(modules, out value)) return true;
            var trimmed = TrimModuleBorder(modules);
            if (!ReferenceEquals(trimmed, modules) && TryDecodeWithRotations(trimmed, out value)) return true;
        }

        value = string.Empty;
        return false;
    }

    private static bool TryDecodeFromBox(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat format, BoundingBox box, bool invert, out string value) {
        foreach (var candidate in BuildCandidates(pixels, width, height, stride, format, box, invert)) {
            var modules = SampleModules(pixels, width, height, stride, format, box, candidate.WidthModules, candidate.HeightModules, candidate.ModuleSize, invert);
            if (TryDecodeWithRotations(modules, out value)) return true;

            var trimmed = TrimModuleBorder(modules);
            if (!ReferenceEquals(trimmed, modules) && TryDecodeWithRotations(trimmed, out value)) return true;
        }

        value = string.Empty;
        return false;
    }

    private static List<Candidate> BuildCandidates(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat format, BoundingBox box, bool invert) {
        var seen = new HashSet<(int module, int width, int height)>();

        if (TryEstimateModuleSize(pixels, width, height, stride, format, box, invert, out var estimated)) {
            AddCandidateFromModuleSize(box, estimated, seen);
        }

        for (var compact = 0; compact <= 1; compact++) {
            var offset = compact == 1 ? 35 : 69;
            for (var cols = 1; cols <= 30; cols++) {
                var widthModules = cols * 17 + offset;
                var moduleSize = (int)Math.Round(box.Width / (double)widthModules);
                if (moduleSize <= 0) continue;
                AddCandidate(box, moduleSize, widthModules, seen);
            }
        }

        var candidates = new List<Candidate>(seen.Count);
        foreach (var entry in seen) {
            candidates.Add(new Candidate(entry.module, entry.width, entry.height));
        }

        return candidates;
    }

    private static void AddCandidateFromModuleSize(BoundingBox box, int moduleSize, HashSet<(int module, int width, int height)> seen) {
        var widthModules = (int)Math.Round(box.Width / (double)moduleSize);
        var heightModules = (int)Math.Round(box.Height / (double)moduleSize);
        if (widthModules <= 0 || heightModules <= 0) return;
        AddCandidate(box, moduleSize, widthModules, seen);
    }

    private static void AddCandidate(BoundingBox box, int moduleSize, int widthModules, HashSet<(int module, int width, int height)> seen) {
        var heightModules = (int)Math.Round(box.Height / (double)moduleSize);
        if (heightModules < 3 || heightModules > 90) return;

        var widthPx = widthModules * moduleSize;
        var heightPx = heightModules * moduleSize;
        if (Math.Abs(widthPx - box.Width) > moduleSize * 2) return;
        if (Math.Abs(heightPx - box.Height) > moduleSize * 2) return;

        if (!TryGetDimensions(widthModules, out var cols, out _)) return;
        if (cols < 1 || cols > 30) return;

        seen.Add((moduleSize, widthModules, heightModules));
    }

    private static BitMatrix SampleModules(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat format, BoundingBox box, int widthModules, int heightModules, int moduleSize, bool invert) {
        var modules = new BitMatrix(widthModules, heightModules);
        var totalWidth = widthModules * moduleSize;
        var totalHeight = heightModules * moduleSize;
        var offsetX = box.Left + (box.Width - totalWidth) / 2.0;
        var offsetY = box.Top + (box.Height - totalHeight) / 2.0;

        var half = moduleSize / 2.0;
        for (var y = 0; y < heightModules; y++) {
            var sy = (int)Math.Round(offsetY + (y * moduleSize) + half);
            sy = Math.Clamp(sy, 0, height - 1);
            for (var x = 0; x < widthModules; x++) {
                var sx = (int)Math.Round(offsetX + (x * moduleSize) + half);
                sx = Math.Clamp(sx, 0, width - 1);
                var dark = IsDark(pixels, width, height, stride, format, sx, sy);
                modules[x, y] = invert ? !dark : dark;
            }
        }

        return modules;
    }

    private static bool TryGetDimensions(int width, out int cols, out bool compact) {
        if (width >= 69 && (width - 69) % 17 == 0) {
            cols = (width - 69) / 17;
            compact = false;
            return cols > 0;
        }
        if (width >= 35 && (width - 35) % 17 == 0) {
            cols = (width - 35) / 17;
            compact = true;
            return cols > 0;
        }
        cols = 0;
        compact = false;
        return false;
    }

    private static bool TryDecodeWithRotations(BitMatrix modules, out string value) {
        if (TryDecode(modules, out value)) return true;
        if (TryDecode(Rotate90(modules), out value)) return true;
        if (TryDecode(Rotate180(modules), out value)) return true;
        if (TryDecode(Rotate270(modules), out value)) return true;
        value = string.Empty;
        return false;
    }

    private static BitMatrix TrimModuleBorder(BitMatrix modules) {
        var width = modules.Width;
        var height = modules.Height;
        if (width <= 2 || height <= 2) return modules;

        var left = 0;
        var right = width - 1;
        var top = 0;
        var bottom = height - 1;

        var rowThreshold = Math.Max(1, width / 40);
        var colThreshold = Math.Max(1, height / 40);

        while (top <= bottom && CountDarkRow(modules, top, left, right) <= rowThreshold) top++;
        while (top <= bottom && CountDarkRow(modules, bottom, left, right) <= rowThreshold) bottom--;
        while (left <= right && CountDarkCol(modules, left, top, bottom) <= colThreshold) left++;
        while (left <= right && CountDarkCol(modules, right, top, bottom) <= colThreshold) right--;

        if (left <= 0 && right >= width - 1 && top <= 0 && bottom >= height - 1) return modules;
        if (right < left || bottom < top) return modules;

        var trimmed = new BitMatrix(right - left + 1, bottom - top + 1);
        for (var y = top; y <= bottom; y++) {
            for (var x = left; x <= right; x++) {
                trimmed[x - left, y - top] = modules[x, y];
            }
        }

        return trimmed;
    }

    private static int CountDarkRow(BitMatrix modules, int y, int left, int right) {
        var count = 0;
        for (var x = left; x <= right; x++) {
            if (modules[x, y]) count++;
        }
        return count;
    }

    private static int CountDarkCol(BitMatrix modules, int x, int top, int bottom) {
        var count = 0;
        for (var y = top; y <= bottom; y++) {
            if (modules[x, y]) count++;
        }
        return count;
    }

    private static bool TryReadCodeword(BitMatrix modules, int row, int offset, int length, int cluster, out int codeword) {
        var pattern = 0;
        for (var i = 0; i < length; i++) {
            pattern <<= 1;
            if (modules[offset + i, row]) pattern |= 1;
        }
        if (PatternToCodeword[cluster].TryGetValue(pattern, out var cw)) {
            codeword = cw;
            return true;
        }
        codeword = 0;
        return false;
    }

    private static Dictionary<int, int>[] BuildPatternMaps() {
        var maps = new Dictionary<int, int>[3];
        for (var i = 0; i < 3; i++) {
            var map = new Dictionary<int, int>(Pdf417CodewordTable.Table[i].Length);
            for (var cw = 0; cw < Pdf417CodewordTable.Table[i].Length; cw++) {
                map[Pdf417CodewordTable.Table[i][cw]] = cw;
            }
            maps[i] = map;
        }
        return maps;
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
        if (!TryEstimateModuleSize(pixels, width, height, stride, format, box, invert, out var moduleSize)) return false;

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

        if (!TryFindMinRun(pixels, width, height, stride, format, box.Left, box.Right, midY, horizontal: true, invert, out var hMin)) return false;
        if (!TryFindMinRun(pixels, width, height, stride, format, box.Top, box.Bottom, midX, horizontal: false, invert, out var vMin)) return false;

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
        return minRun != int.MaxValue;
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

        var found = new BoundingBox(left, top, right, bottom);
        var trimmed = TrimBoundingBox(pixels, width, height, stride, format, found, invert);
        box = trimmed.Width >= 3 && trimmed.Height >= 3 ? trimmed : found;
        return true;
    }

    private static BoundingBox TrimBoundingBox(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat format, BoundingBox box, bool invert) {
        var left = box.Left;
        var right = box.Right;
        var top = box.Top;
        var bottom = box.Bottom;

        var rowThreshold = Math.Max(2, (right - left + 1) / 40);
        var colThreshold = Math.Max(2, (bottom - top + 1) / 40);

        while (top <= bottom && CountDarkRow(pixels, width, height, stride, format, left, right, top, invert) <= rowThreshold) top++;
        while (top <= bottom && CountDarkRow(pixels, width, height, stride, format, left, right, bottom, invert) <= rowThreshold) bottom--;
        while (left <= right && CountDarkCol(pixels, width, height, stride, format, left, top, bottom, invert) <= colThreshold) left++;
        while (left <= right && CountDarkCol(pixels, width, height, stride, format, right, top, bottom, invert) <= colThreshold) right--;

        if (right < left || bottom < top) return box;
        return new BoundingBox(left, top, right, bottom);
    }

    private static int CountDarkRow(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat format, int left, int right, int y, bool invert) {
        if ((uint)y >= (uint)height) return 0;
        var row = y * stride;
        var count = 0;
        for (var x = left; x <= right; x++) {
            if ((uint)x >= (uint)width) continue;
            var dark = IsDarkAt(pixels, row, x, format);
            if (invert) dark = !dark;
            if (dark) count++;
        }

        return count;
    }

    private static int CountDarkCol(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat format, int x, int top, int bottom, bool invert) {
        if ((uint)x >= (uint)width) return 0;
        var count = 0;
        for (var y = top; y <= bottom; y++) {
            if ((uint)y >= (uint)height) continue;
            var row = y * stride;
            var dark = IsDarkAt(pixels, row, x, format);
            if (invert) dark = !dark;
            if (dark) count++;
        }

        return count;
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
