#if NET8_0_OR_GREATER
using PixelSpan = System.ReadOnlySpan<byte>;
#else
using PixelSpan = byte[];
#endif

using System;
using System.Collections.Generic;

namespace CodeGlyphX.Aztec;

internal static class AztecPixelDecoder {
    private readonly struct Candidate {
        public readonly int Size;
        public readonly bool Compact;
        public readonly int Layers;

        public Candidate(int size, bool compact, int layers) {
            Size = size;
            Compact = compact;
            Layers = layers;
        }
    }

    private static readonly Candidate[] Candidates = BuildCandidates();

    public static bool TryDecode(PixelSpan pixels, int width, int height, int stride, PixelFormat format, out string value) {
        value = string.Empty;
        if (width <= 0 || height <= 0 || stride < width * 4) return false;
        if (!AztecGrayImage.TryCreate(pixels, width, height, stride, format, out var image)) return false;

        if (TryDecodeAllOrientations(image, out value)) return true;
        return false;
    }

    private static bool TryDecodeAllOrientations(AztecGrayImage image, out string value) {
        if (TryDecodeWithThresholds(image, invert: false, out value)) return true;
        if (TryDecodeWithThresholds(image, invert: true, out value)) return true;

        var rot90 = image.Rotate90();
        if (TryDecodeWithThresholds(rot90, invert: false, out value)) return true;
        if (TryDecodeWithThresholds(rot90, invert: true, out value)) return true;

        var rot180 = image.Rotate180();
        if (TryDecodeWithThresholds(rot180, invert: false, out value)) return true;
        if (TryDecodeWithThresholds(rot180, invert: true, out value)) return true;

        var rot270 = image.Rotate270();
        if (TryDecodeWithThresholds(rot270, invert: false, out value)) return true;
        if (TryDecodeWithThresholds(rot270, invert: true, out value)) return true;

        var mirror = image.MirrorX();
        if (TryDecodeWithThresholds(mirror, invert: false, out value)) return true;
        if (TryDecodeWithThresholds(mirror, invert: true, out value)) return true;

        value = string.Empty;
        return false;
    }
    private static bool TryDecodeWithThresholds(AztecGrayImage image, bool invert, out string value) {
        value = string.Empty;
        var thresholds = BuildThresholds(image);

        for (var i = 0; i < thresholds.Length; i++) {
            var tImage = image.WithThreshold(thresholds[i]);
            if (TryFindBoundingBox(tImage, invert, out var minX, out var minY, out var maxX, out var maxY)) {
                var boxW = maxX - minX + 1;
                var boxH = maxY - minY + 1;
                var boxSize = Math.Min(boxW, boxH);
                if (boxSize >= 11 && TryDecodeFromBox(tImage, invert, minX, minY, boxW, boxH, out value)) {
                    return true;
                }
            }

            if (TryDecodeFromBullseye(tImage, invert, out value)) return true;
        }

        var fullFrameTries = Math.Min(2, thresholds.Length);
        for (var i = 0; i < fullFrameTries; i++) {
            var tImage = image.WithThreshold(thresholds[i]);
            if (TryDecodeFromFullFrame(tImage, invert, out value)) return true;
        }

        value = string.Empty;
        return false;
    }

    private static bool TryDecodeFromBox(AztecGrayImage image, bool invert, int minX, int minY, int boxW, int boxH, out string value) {
        var boxSize = Math.Min(boxW, boxH);
        for (var i = 0; i < Candidates.Length; i++) {
            var candidate = Candidates[i];
            if (candidate.Size > boxSize) continue;

            var moduleSize = boxSize / candidate.Size;
            if (moduleSize <= 0) continue;

            var gridPx = candidate.Size * moduleSize;
            var offsetX = minX + (boxW - gridPx) / 2;
            var offsetY = minY + (boxH - gridPx) / 2;

            if (TryDecodeWithOffsets(image, invert, candidate.Size, moduleSize, offsetX, offsetY, out value)) {
                return true;
            }
        }

        value = string.Empty;
        return false;
    }

    private static bool TryDecodeFromFullFrame(AztecGrayImage image, bool invert, out string value) {
        var sizePx = Math.Min(image.Width, image.Height);
        if (sizePx < 11) {
            value = string.Empty;
            return false;
        }

        for (var i = 0; i < Candidates.Length; i++) {
            var candidate = Candidates[i];
            for (var quietZone = 0; quietZone <= 8; quietZone++) {
                var totalModules = candidate.Size + quietZone * 2;
                if (totalModules <= 0) continue;

                var moduleSize = sizePx / totalModules;
                if (moduleSize <= 0) continue;

                var gridPx = totalModules * moduleSize;
                var frameOffsetX = (image.Width - gridPx) / 2;
                var frameOffsetY = (image.Height - gridPx) / 2;
                var offsetX = frameOffsetX + quietZone * moduleSize;
                var offsetY = frameOffsetY + quietZone * moduleSize;

                if (TryDecodeWithOffsets(image, invert, candidate.Size, moduleSize, offsetX, offsetY, out value)) {
                    return true;
                }
            }
        }

        value = string.Empty;
        return false;
    }

    private static bool TryDecodeFromBullseye(AztecGrayImage image, bool invert, out string value) {
        value = string.Empty;
        if (!TryEstimateCenter(image, invert, out var cx, out var cy)) return false;
        if (!TryEstimateModuleSize(image, invert, cx, cy, out var moduleSize)) return false;

        for (var delta = -1; delta <= 1; delta++) {
            var size = moduleSize + delta;
            if (size <= 0) continue;
            if (TryDecodeFromCenter(image, invert, cx, cy, size, out value)) return true;
        }

        return false;
    }

    private static bool TryDecodeFromCenter(AztecGrayImage image, bool invert, int centerX, int centerY, int moduleSize, out string value) {
        for (var i = 0; i < Candidates.Length; i++) {
            var candidate = Candidates[i];
            var totalPx = candidate.Size * moduleSize;
            if (totalPx <= 0) continue;

            var offsetX = centerX - totalPx / 2;
            var offsetY = centerY - totalPx / 2;

            if (TryDecodeWithOffsets(image, invert, candidate.Size, moduleSize, offsetX, offsetY, out value)) {
                return true;
            }
        }

        value = string.Empty;
        return false;
    }

    private static bool TryEstimateCenter(AztecGrayImage image, bool invert, out int centerX, out int centerY) {
        centerX = image.Width / 2;
        centerY = image.Height / 2;

        if (image.IsBlack(centerX, centerY, invert)) return true;

        for (var r = 1; r <= 4; r++) {
            for (var dy = -r; dy <= r; dy++) {
                var y = centerY + dy;
                if (y < 0 || y >= image.Height) continue;
                for (var dx = -r; dx <= r; dx++) {
                    var x = centerX + dx;
                    if (x < 0 || x >= image.Width) continue;
                    if (image.IsBlack(x, y, invert)) {
                        centerX = x;
                        centerY = y;
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private static bool TryEstimateModuleSize(AztecGrayImage image, bool invert, int centerX, int centerY, out int moduleSize) {
        moduleSize = 0;

        var left = MeasureRun(image, invert, centerX, centerY, -1, 0);
        var right = MeasureRun(image, invert, centerX, centerY, 1, 0);
        var up = MeasureRun(image, invert, centerX, centerY, 0, -1);
        var down = MeasureRun(image, invert, centerX, centerY, 0, 1);

        if (left == 0 || right == 0 || up == 0 || down == 0) return false;

        var avg = (left + right + up + down) / 4.0;
        if (avg < 1) return false;

        if (Math.Abs(left - avg) > avg * 0.75) return false;
        if (Math.Abs(right - avg) > avg * 0.75) return false;
        if (Math.Abs(up - avg) > avg * 0.75) return false;
        if (Math.Abs(down - avg) > avg * 0.75) return false;

        moduleSize = Math.Max(1, (int)Math.Round(avg));
        return true;
    }

    private static int MeasureRun(AztecGrayImage image, bool invert, int startX, int startY, int dx, int dy) {
        var color = image.IsBlack(startX, startY, invert);
        var x = startX;
        var y = startY;
        var len = 0;

        while (x >= 0 && x < image.Width && y >= 0 && y < image.Height) {
            if (image.IsBlack(x, y, invert) != color) break;
            len++;
            x += dx;
            y += dy;
        }

        return len;
    }

    private static bool TrySampleModules(AztecGrayImage image, bool invert, int size, int moduleSize, int offsetX, int offsetY, out BitMatrix modules) {
        modules = new BitMatrix(size, size);

        var centerOffset = moduleSize / 2;
        for (var y = 0; y < size; y++) {
            var sy = offsetY + y * moduleSize + centerOffset;
            for (var x = 0; x < size; x++) {
                var sx = offsetX + x * moduleSize + centerOffset;
                var black = SampleMajority3x3(image, sx, sy, invert);
                modules.Set(x, y, black);
            }
        }

        return true;
    }

    private static bool TryDecodeWithOffsets(AztecGrayImage image, bool invert, int size, int moduleSize, int offsetX, int offsetY, out string value) {
        var maxShift = Math.Max(1, moduleSize / 2);
        for (var dy = -maxShift; dy <= maxShift; dy++) {
            for (var dx = -maxShift; dx <= maxShift; dx++) {
                if (!TrySampleModules(image, invert, size, moduleSize, offsetX + dx, offsetY + dy, out var modules)) {
                    continue;
                }
                if (AztecDecoder.TryDecode(modules, out value)) return true;
            }
        }

        value = string.Empty;
        return false;
    }

    private static bool SampleMajority3x3(AztecGrayImage image, int px, int py, bool invert) {
        var black = 0;

        for (var dy = -1; dy <= 1; dy++) {
            var y = py + dy;
            if (y < 0) y = 0;
            else if (y >= image.Height) y = image.Height - 1;

            for (var dx = -1; dx <= 1; dx++) {
                var x = px + dx;
                if (x < 0) x = 0;
                else if (x >= image.Width) x = image.Width - 1;

                if (image.IsBlack(x, y, invert)) black++;
            }
        }

        return black >= 5;
    }

    private static bool TryFindBoundingBox(AztecGrayImage image, bool invert, out int minX, out int minY, out int maxX, out int maxY) {
        minX = image.Width;
        minY = image.Height;
        maxX = -1;
        maxY = -1;

        for (var y = 0; y < image.Height; y++) {
            for (var x = 0; x < image.Width; x++) {
                if (!image.IsBlack(x, y, invert)) continue;
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }
        }

        if (maxX < minX || maxY < minY) return false;
        return true;
    }

    private static Candidate[] BuildCandidates() {
        var list = new Candidate[36];
        var count = 0;
        for (var layers = 1; layers <= 4; layers++) {
            var size = 11 + layers * 4;
            list[count++] = new Candidate(size, compact: true, layers);
        }

        for (var layers = 1; layers <= 32; layers++) {
            var baseSize = 14 + layers * 4;
            var size = baseSize + 1 + 2 * ((baseSize / 2 - 1) / 15);
            list[count++] = new Candidate(size, compact: false, layers);
        }

        Array.Sort(list, 0, count, CandidateSizeComparer.Instance);
        if (count == list.Length) return list;

        var trimmed = new Candidate[count];
        Array.Copy(list, trimmed, count);
        return trimmed;
    }

    private sealed class CandidateSizeComparer : IComparer<Candidate> {
        public static readonly CandidateSizeComparer Instance = new();
        public int Compare(Candidate x, Candidate y) => x.Size.CompareTo(y.Size);
    }

    private static byte[] BuildThresholds(AztecGrayImage image) {
        var min = image.Min;
        var max = image.Max;
        var range = max - min;
        var mid = (byte)(min + range / 2);

        Span<byte> list = stackalloc byte[8];
        var count = 0;
        AddThreshold(ref list, ref count, image.Threshold);
        AddThreshold(ref list, ref count, mid);
        if (range > 0) {
            AddThreshold(ref list, ref count, (byte)(min + range / 3));
            AddThreshold(ref list, ref count, (byte)(min + (range * 2) / 3));
        }
        AddThreshold(ref list, ref count, (byte)Math.Min(255, min + 12));
        AddThreshold(ref list, ref count, (byte)Math.Max(0, max - 12));

        var result = new byte[count];
        for (var i = 0; i < count; i++) result[i] = list[i];
        return result;
    }

    private static void AddThreshold(ref Span<byte> list, ref int count, byte value) {
        for (var i = 0; i < count; i++) {
            if (list[i] == value) return;
        }
        list[count++] = value;
    }

    private readonly struct AztecGrayImage {
        public readonly int Width;
        public readonly int Height;
        private readonly byte[] _gray;
        private readonly byte _threshold;
        public readonly byte Min;
        public readonly byte Max;

        private AztecGrayImage(int width, int height, byte[] gray, byte threshold, byte min, byte max) {
            Width = width;
            Height = height;
            _gray = gray;
            _threshold = threshold;
            Min = min;
            Max = max;
        }

        public bool IsBlack(int x, int y, bool invert) {
            var lum = _gray[y * Width + x];
            var black = lum <= _threshold;
            return invert ? !black : black;
        }

        public byte Threshold => _threshold;

        public AztecGrayImage WithThreshold(byte threshold) {
            return new AztecGrayImage(Width, Height, _gray, threshold, Min, Max);
        }

        public AztecGrayImage Rotate90() {
            var w = Width;
            var h = Height;
            var rotated = new byte[w * h];
            for (var y = 0; y < h; y++) {
                var row = y * w;
                for (var x = 0; x < w; x++) {
                    var nx = h - 1 - y;
                    var ny = x;
                    rotated[ny * h + nx] = _gray[row + x];
                }
            }
            return new AztecGrayImage(h, w, rotated, _threshold, Min, Max);
        }

        public AztecGrayImage Rotate180() {
            var w = Width;
            var h = Height;
            var rotated = new byte[w * h];
            for (var y = 0; y < h; y++) {
                var row = y * w;
                var ny = h - 1 - y;
                var nrow = ny * w;
                for (var x = 0; x < w; x++) {
                    rotated[nrow + (w - 1 - x)] = _gray[row + x];
                }
            }
            return new AztecGrayImage(w, h, rotated, _threshold, Min, Max);
        }

        public AztecGrayImage Rotate270() {
            var w = Width;
            var h = Height;
            var rotated = new byte[w * h];
            for (var y = 0; y < h; y++) {
                var row = y * w;
                for (var x = 0; x < w; x++) {
                    var nx = y;
                    var ny = w - 1 - x;
                    rotated[ny * h + nx] = _gray[row + x];
                }
            }
            return new AztecGrayImage(h, w, rotated, _threshold, Min, Max);
        }

        public AztecGrayImage MirrorX() {
            var w = Width;
            var h = Height;
            var mirrored = new byte[w * h];
            for (var y = 0; y < h; y++) {
                var row = y * w;
                for (var x = 0; x < w; x++) {
                    mirrored[row + (w - 1 - x)] = _gray[row + x];
                }
            }
            return new AztecGrayImage(w, h, mirrored, _threshold, Min, Max);
        }

        public static bool TryCreate(PixelSpan pixels, int width, int height, int stride, PixelFormat format, out AztecGrayImage image) {
            image = default;
            if (width <= 0 || height <= 0 || stride < width * 4) return false;
#if NET8_0_OR_GREATER
            if (pixels.Length < (height - 1) * stride + width * 4) return false;
#else
            if (pixels.Length < (height - 1) * stride + width * 4) return false;
#endif

            var gray = new byte[width * height];
#if NET8_0_OR_GREATER
            Span<int> histogram = stackalloc int[256];
            for (var i = 0; i < histogram.Length; i++) histogram[i] = 0;
#else
            var histogram = new int[256];
#endif
            byte min = 255;
            byte max = 0;

            for (var y = 0; y < height; y++) {
                var row = y * stride;
                var dst = y * width;
                for (var x = 0; x < width; x++) {
                    var idx = row + x * 4;
                    byte r;
                    byte g;
                    byte b;
                    if (format == PixelFormat.Bgra32) {
                        b = pixels[idx + 0];
                        g = pixels[idx + 1];
                        r = pixels[idx + 2];
                    } else {
                        r = pixels[idx + 0];
                        g = pixels[idx + 1];
                        b = pixels[idx + 2];
                    }

                    var lum = (byte)((r * 299 + g * 587 + b * 114 + 500) / 1000);
                    gray[dst + x] = lum;
                    histogram[lum]++;
                    if (lum < min) min = lum;
                    if (lum > max) max = lum;
                }
            }

            var threshold = ComputeOtsuThreshold(histogram, gray.Length);
            image = new AztecGrayImage(width, height, gray, (byte)threshold, min, max);
            return true;
        }
    }

#if NET8_0_OR_GREATER
    private static int ComputeOtsuThreshold(ReadOnlySpan<int> hist, int total) {
#else
    private static int ComputeOtsuThreshold(int[] hist, int total) {
#endif
        long sum = 0;
        for (var i = 0; i < hist.Length; i++) sum += (long)i * hist[i];

        long sumB = 0;
        var wB = 0;
        var wF = 0;
        var maxVar = -1.0;
        var threshold = 0;

        for (var t = 0; t < hist.Length; t++) {
            wB += hist[t];
            if (wB == 0) continue;
            wF = total - wB;
            if (wF == 0) break;

            sumB += (long)t * hist[t];
            var mB = (double)sumB / wB;
            var mF = (double)(sum - sumB) / wF;
            var between = (double)wB * wF * (mB - mF) * (mB - mF);
            if (between > maxVar) {
                maxVar = between;
                threshold = t;
            }
        }

        return threshold;
    }
}
