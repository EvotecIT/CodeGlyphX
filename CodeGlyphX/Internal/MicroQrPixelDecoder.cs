#if NET8_0_OR_GREATER
using PixelSpan = System.ReadOnlySpan<byte>;
#else
using PixelSpan = System.Byte[];
#endif

using System;
using System.Collections.Generic;
using System.Threading;

namespace CodeGlyphX.Internal;

internal static partial class MicroQrPixelDecoder {
    private static readonly int[] Dimensions = { 11, 13, 15, 17 };
    private static readonly double[] CandidateScaleFactors = { 1.0, 0.94, 1.06 };

    internal static bool TryDecode(
        PixelSpan pixels,
        int width,
        int height,
        int stride,
        PixelFormat format,
        CancellationToken cancellationToken,
        out MicroQrDecoded decoded,
        out MicroQrPixelDecodeInfo info) {
        decoded = null!;
        info = null!;
        if (!GrayImage.TryCreate(pixels, width, height, stride, format, cancellationToken, out var image)) return false;

        var thresholds = BuildThresholds(image);
        for (var i = 0; i < thresholds.Length; i++) {
            if (cancellationToken.IsCancellationRequested) return false;
            var likelyInverted = IsBorderForeground(image, thresholds[i], inverted: false);
            for (var polarity = 0; polarity < 2; polarity++) {
                var inverted = polarity == 0 ? likelyInverted : !likelyInverted;
                if (TryDecodeFromFinderPatterns(image, thresholds[i], inverted, cancellationToken, out var model2Only, out decoded, out info)) return true;
                if (model2Only) break;
                if (TryDecodeFromForegroundBounds(image, thresholds[i], inverted, cancellationToken, out decoded, out info)) return true;
            }
        }

        decoded = null!;
        info = null!;
        return false;
    }

    private static bool TryDecodeFromForegroundBounds(
        GrayImage image,
        byte threshold,
        bool inverted,
        CancellationToken cancellationToken,
        out MicroQrDecoded decoded,
        out MicroQrPixelDecodeInfo info) {
        decoded = null!;
        info = null!;
        if (!TryFindForegroundBounds(image, threshold, inverted, cancellationToken, out var bounds)) return false;
        if (bounds.Width < 11 || bounds.Height < 11) return false;

        var ratio = bounds.Width / (double)bounds.Height;
        if (ratio < 0.72 || ratio > 1.38) return false;

        // Fast paths cover the overwhelmingly common upright and quarter-turned images.
        for (var d = 0; d < Dimensions.Length; d++) {
            var dimension = Dimensions[d];
            for (var angle = 0; angle < 360; angle += 90) {
                if (cancellationToken.IsCancellationRequested) return false;
                if (TryDecodeBoundsHypothesis(image, threshold, inverted, bounds, dimension, angle, mirrored: false, out decoded, out info)) return true;
                if (TryDecodeBoundsHypothesis(image, threshold, inverted, bounds, dimension, angle, mirrored: true, out decoded, out info)) return true;
            }
        }

        // An isolated rotated symbol still has a square foreground envelope. Solving the
        // envelope for each angle avoids a separate deskew bitmap and preserves geometry.
        for (var angle = 1; angle < 360; angle++) {
            if ((angle % 90) == 0) continue;
            if ((angle & 7) == 0 && cancellationToken.IsCancellationRequested) return false;
            for (var d = 0; d < Dimensions.Length; d++) {
                if (TryDecodeBoundsHypothesis(image, threshold, inverted, bounds, Dimensions[d], angle, mirrored: false, out decoded, out info)) return true;
                if (TryDecodeBoundsHypothesis(image, threshold, inverted, bounds, Dimensions[d], angle, mirrored: true, out decoded, out info)) return true;
            }
        }

        return false;
    }

    private static bool TryDecodeBoundsHypothesis(
        GrayImage image,
        byte threshold,
        bool inverted,
        PixelBounds bounds,
        int dimension,
        int angleDegrees,
        bool mirrored,
        out MicroQrDecoded decoded,
        out MicroQrPixelDecodeInfo info) {
        var radians = angleDegrees * Math.PI / 180.0;
        var ux = Math.Cos(radians);
        var uy = Math.Sin(radians);
        var vx = mirrored ? Math.Sin(radians) : -Math.Sin(radians);
        var vy = mirrored ? -Math.Cos(radians) : Math.Cos(radians);
        var extentX = Math.Abs(ux) + Math.Abs(vx);
        var extentY = Math.Abs(uy) + Math.Abs(vy);
        if (extentX <= 0 || extentY <= 0) return Fail(out decoded, out info);

        var moduleX = bounds.Width / (dimension * extentX);
        var moduleY = bounds.Height / (dimension * extentY);
        var moduleSize = (moduleX + moduleY) * 0.5;
        if (moduleSize < 0.75) return Fail(out decoded, out info);

        var spanX = dimension * moduleSize;
        var spanY = dimension * moduleSize;
        var minRelativeX = Math.Min(0, Math.Min(spanX * ux, Math.Min(spanY * vx, spanX * ux + spanY * vx)));
        var minRelativeY = Math.Min(0, Math.Min(spanX * uy, Math.Min(spanY * vy, spanX * uy + spanY * vy)));
        var topLeftX = bounds.Left - minRelativeX;
        var topLeftY = bounds.Top - minRelativeY;
        var centerX = topLeftX + 3.5 * moduleSize * (ux + vx);
        var centerY = topLeftY + 3.5 * moduleSize * (uy + vy);

        return TrySampleAndDecode(
            image, threshold, inverted, centerX, centerY, moduleSize, ux, uy, vx, vy,
            dimension, mirrored, out decoded, out info);
    }

    private static bool TryDecodeFromFinderPatterns(
        GrayImage image,
        byte threshold,
        bool inverted,
        CancellationToken cancellationToken,
        out bool model2Only,
        out MicroQrDecoded decoded,
        out MicroQrPixelDecodeInfo info) {
        var candidates = FindFinderCandidates(image, threshold, inverted, cancellationToken);
        var model2Candidates = FindModel2QrCandidates(candidates);
        var usableCandidates = 0;
        for (var c = 0; c < candidates.Count; c++) {
            if (model2Candidates[c]) continue;
            usableCandidates++;
            var candidate = candidates[c];
            for (var angle = 0; angle < 360; angle += 5) {
                if ((angle % 20) == 0 && cancellationToken.IsCancellationRequested) {
                    model2Only = false;
                    return Fail(out decoded, out info);
                }
                var radians = angle * Math.PI / 180.0;
                var ux = Math.Cos(radians);
                var uy = Math.Sin(radians);
                var lineCorrection = Math.Max(Math.Abs(ux), Math.Abs(uy));
                var baseModuleSize = candidate.ModuleSize * lineCorrection;

                for (var mirror = 0; mirror < 2; mirror++) {
                    var mirrored = mirror != 0;
                    var vx = mirrored ? Math.Sin(radians) : -Math.Sin(radians);
                    var vy = mirrored ? -Math.Cos(radians) : Math.Cos(radians);
                    for (var scaleIndex = 0; scaleIndex < CandidateScaleFactors.Length; scaleIndex++) {
                        var moduleSize = baseModuleSize * CandidateScaleFactors[scaleIndex];
                        for (var d = 0; d < Dimensions.Length; d++) {
                            if (TrySampleAndDecode(
                                image, threshold, inverted, candidate.X, candidate.Y, moduleSize,
                                ux, uy, vx, vy, Dimensions[d], mirrored, out decoded, out info)) {
                                model2Only = false;
                                return true;
                            }
                        }
                    }
                }
            }
        }

        model2Only = candidates.Count >= 3 && usableCandidates == 0;
        return Fail(out decoded, out info);
    }

    private static bool[] FindModel2QrCandidates(List<FinderCandidate> candidates) {
        var excluded = new bool[candidates.Count];
        for (var i = 0; i < candidates.Count - 2; i++) {
            for (var j = i + 1; j < candidates.Count - 1; j++) {
                if (!SimilarModuleSize(candidates[i], candidates[j])) continue;
                for (var k = j + 1; k < candidates.Count; k++) {
                    if (!SimilarModuleSize(candidates[i], candidates[k]) || !SimilarModuleSize(candidates[j], candidates[k])) continue;
                    var d0 = DistanceSquared(candidates[i], candidates[j]);
                    var d1 = DistanceSquared(candidates[i], candidates[k]);
                    var d2 = DistanceSquared(candidates[j], candidates[k]);
                    SortThree(ref d0, ref d1, ref d2);
                    var module = (candidates[i].ModuleSize + candidates[j].ModuleSize + candidates[k].ModuleSize) / 3.0;
                    var minimumLeg = module * 11.0;
                    if (d0 < minimumLeg * minimumLeg) continue;
                    if (d1 > d0 * 2.25) continue;
                    if (Math.Abs(d2 - d0 - d1) > d2 * 0.22) continue;
                    excluded[i] = true;
                    excluded[j] = true;
                    excluded[k] = true;
                    ExcludeCandidatesInsideModel2Symbol(candidates, excluded, i, j, k, module);
                }
            }
        }
        return excluded;
    }

    private static void ExcludeCandidatesInsideModel2Symbol(
        List<FinderCandidate> candidates,
        bool[] excluded,
        int first,
        int second,
        int third,
        double moduleSize) {
        var minX = Math.Min(candidates[first].X, Math.Min(candidates[second].X, candidates[third].X)) - moduleSize * 4.5;
        var minY = Math.Min(candidates[first].Y, Math.Min(candidates[second].Y, candidates[third].Y)) - moduleSize * 4.5;
        var maxX = Math.Max(candidates[first].X, Math.Max(candidates[second].X, candidates[third].X)) + moduleSize * 4.5;
        var maxY = Math.Max(candidates[first].Y, Math.Max(candidates[second].Y, candidates[third].Y)) + moduleSize * 4.5;
        for (var i = 0; i < candidates.Count; i++) {
            var candidate = candidates[i];
            if (candidate.X >= minX && candidate.X <= maxX && candidate.Y >= minY && candidate.Y <= maxY) excluded[i] = true;
        }
    }

    private static bool SimilarModuleSize(FinderCandidate left, FinderCandidate right) {
        var larger = Math.Max(left.ModuleSize, right.ModuleSize);
        return larger > 0 && Math.Abs(left.ModuleSize - right.ModuleSize) <= larger * 0.35;
    }

    private static double DistanceSquared(FinderCandidate left, FinderCandidate right) {
        var dx = left.X - right.X;
        var dy = left.Y - right.Y;
        return dx * dx + dy * dy;
    }

    private static void SortThree(ref double first, ref double second, ref double third) {
        if (first > second) Swap(ref first, ref second);
        if (second > third) Swap(ref second, ref third);
        if (first > second) Swap(ref first, ref second);
    }

    private static void Swap(ref double left, ref double right) {
        var value = left;
        left = right;
        right = value;
    }

    private static bool TrySampleAndDecode(
        GrayImage image,
        byte threshold,
        bool inverted,
        double finderCenterX,
        double finderCenterY,
        double moduleSize,
        double ux,
        double uy,
        double vx,
        double vy,
        int dimension,
        bool mirrored,
        out MicroQrDecoded decoded,
        out MicroQrPixelDecodeInfo info) {
        if (moduleSize < 0.75) return Fail(out decoded, out info);
        if (!MatchesFunctionPatterns(image, threshold, inverted, finderCenterX, finderCenterY, moduleSize, ux, uy, vx, vy, dimension)) {
            return Fail(out decoded, out info);
        }

        var modules = new BitMatrix(dimension, dimension);
        for (var y = 0; y < dimension; y++) {
            for (var x = 0; x < dimension; x++) {
                if (!TrySampleModule(image, threshold, inverted, finderCenterX, finderCenterY, moduleSize, ux, uy, vx, vy, x, y, out var dark)) {
                    return Fail(out decoded, out info);
                }
                modules[x, y] = dark;
            }
        }

        if (!MicroQrDecoder.TryDecode(modules, out decoded)) return Fail(out decoded, out info);

        var topLeftX = finderCenterX - 3.5 * moduleSize * (ux + vx);
        var topLeftY = finderCenterY - 3.5 * moduleSize * (uy + vy);
        var topRightX = topLeftX + dimension * moduleSize * ux;
        var topRightY = topLeftY + dimension * moduleSize * uy;
        var bottomLeftX = topLeftX + dimension * moduleSize * vx;
        var bottomLeftY = topLeftY + dimension * moduleSize * vy;
        var geometry = new SymbolGeometry(
            new SymbolPoint(topLeftX, topLeftY),
            new SymbolPoint(topRightX, topRightY),
            new SymbolPoint(topRightX + dimension * moduleSize * vx, topRightY + dimension * moduleSize * vy),
            new SymbolPoint(bottomLeftX, bottomLeftY));
        info = new MicroQrPixelDecodeInfo(geometry, inverted, mirrored, threshold);
        return true;
    }

    private static bool MatchesFunctionPatterns(
        GrayImage image,
        byte threshold,
        bool inverted,
        double centerX,
        double centerY,
        double moduleSize,
        double ux,
        double uy,
        double vx,
        double vy,
        int dimension) {
        var mismatches = 0;
        var samples = 0;
        for (var y = 0; y < 7; y++) {
            for (var x = 0; x < 7; x++) {
                if (!TrySampleModuleFast(image, threshold, inverted, centerX, centerY, moduleSize, ux, uy, vx, vy, x, y, out var dark)) return false;
                var expected = x == 0 || x == 6 || y == 0 || y == 6 || (x >= 2 && x <= 4 && y >= 2 && y <= 4);
                if (dark != expected) mismatches++;
                samples++;
            }
        }
        if (mismatches > 6) return false;

        for (var i = 0; i < 8; i++) {
            if (!TrySampleModuleFast(image, threshold, inverted, centerX, centerY, moduleSize, ux, uy, vx, vy, 7, i, out var right) || right) mismatches++;
            if (!TrySampleModuleFast(image, threshold, inverted, centerX, centerY, moduleSize, ux, uy, vx, vy, i, 7, out var bottom) || bottom) mismatches++;
            samples += 2;
        }
        if (mismatches > 9) return false;

        for (var i = 1; i < dimension - 7; i++) {
            var coordinate = 7 + i;
            var expected = (i & 1) == 1;
            if (!TrySampleModuleFast(image, threshold, inverted, centerX, centerY, moduleSize, ux, uy, vx, vy, coordinate, 0, out var horizontal) || horizontal != expected) mismatches++;
            if (!TrySampleModuleFast(image, threshold, inverted, centerX, centerY, moduleSize, ux, uy, vx, vy, 0, coordinate, out var vertical) || vertical != expected) mismatches++;
            samples += 2;
        }

        return mismatches <= Math.Max(10, samples / 7);
    }

    private static bool TrySampleModule(
        GrayImage image,
        byte threshold,
        bool inverted,
        double centerX,
        double centerY,
        double moduleSize,
        double ux,
        double uy,
        double vx,
        double vy,
        int moduleX,
        int moduleY,
        out bool dark) {
        var dx = (moduleX - 3) * moduleSize;
        var dy = (moduleY - 3) * moduleSize;
        var x = centerX + dx * ux + dy * vx;
        var y = centerY + dx * uy + dy * vy;
        if (x < -0.5 || y < -0.5 || x > image.Width - 0.5 || y > image.Height - 0.5) {
            dark = false;
            return false;
        }

        if (moduleSize < 2.0) {
            dark = image.IsForeground((int)Math.Round(x), (int)Math.Round(y), threshold, inverted);
            return true;
        }

        var offset = moduleSize * 0.18;
        var count = 0;
        for (var oy = -1; oy <= 1; oy++) {
            for (var ox = -1; ox <= 1; ox++) {
                var sampleX = x + ox * offset * ux + oy * offset * vx;
                var sampleY = y + ox * offset * uy + oy * offset * vy;
                var px = Clamp((int)Math.Round(sampleX), 0, image.Width - 1);
                var py = Clamp((int)Math.Round(sampleY), 0, image.Height - 1);
                if (image.IsForeground(px, py, threshold, inverted)) count++;
            }
        }
        dark = count >= 5;
        return true;
    }

    private static bool TrySampleModuleFast(
        GrayImage image,
        byte threshold,
        bool inverted,
        double centerX,
        double centerY,
        double moduleSize,
        double ux,
        double uy,
        double vx,
        double vy,
        int moduleX,
        int moduleY,
        out bool dark) {
        var dx = (moduleX - 3) * moduleSize;
        var dy = (moduleY - 3) * moduleSize;
        var x = centerX + dx * ux + dy * vx;
        var y = centerY + dx * uy + dy * vy;
        if (x < -0.5 || y < -0.5 || x > image.Width - 0.5 || y > image.Height - 0.5) {
            dark = false;
            return false;
        }
        dark = image.IsForeground(
            Clamp((int)Math.Round(x), 0, image.Width - 1),
            Clamp((int)Math.Round(y), 0, image.Height - 1),
            threshold,
            inverted);
        return true;
    }

    private static List<FinderCandidate> FindFinderCandidates(GrayImage image, byte threshold, bool inverted, CancellationToken cancellationToken) {
        var candidates = new List<FinderCandidate>(8);
        var rowStep = Math.Max(1, image.Height / 512);
        for (var y = 0; y < image.Height; y += rowStep) {
            if (cancellationToken.IsCancellationRequested) break;
            var counts = new int[5];
            var state = 0;
            for (var x = 0; x < image.Width; x++) {
                var foreground = image.IsForeground(x, y, threshold, inverted);
                if (foreground) {
                    if ((state & 1) != 0) state++;
                    counts[state]++;
                } else {
                    if ((state & 1) == 0) {
                        if (state == 4) {
                            if (IsFinderRatio(counts)) TryAddFinderCandidate(image, threshold, inverted, counts, x, y, candidates);
                            counts[0] = counts[2];
                            counts[1] = counts[3];
                            counts[2] = counts[4];
                            counts[3] = 1;
                            counts[4] = 0;
                            state = 3;
                            continue;
                        }
                        state++;
                    }
                    counts[state]++;
                }
            }
            if (state == 4 && IsFinderRatio(counts)) TryAddFinderCandidate(image, threshold, inverted, counts, image.Width, y, candidates);
            if (candidates.Count >= 16) break;
        }
        candidates.Sort((a, b) => b.Count.CompareTo(a.Count));
        if (candidates.Count > 12) candidates.RemoveRange(12, candidates.Count - 12);
        return candidates;
    }

    private static void TryAddFinderCandidate(
        GrayImage image,
        byte threshold,
        bool inverted,
        int[] rowCounts,
        int endX,
        int rowY,
        List<FinderCandidate> candidates) {
        var centerX = CenterFromEnd(rowCounts, endX);
        var centerXi = (int)Math.Round(centerX);
        if ((uint)centerXi >= (uint)image.Width) return;
        if (!TryCrossCheckVertical(image, threshold, inverted, centerXi, rowY, Sum(rowCounts), out var centerY, out var verticalTotal)) return;
        var moduleSize = (Sum(rowCounts) + verticalTotal) / 14.0;
        if (moduleSize < 0.75) return;

        for (var i = 0; i < candidates.Count; i++) {
            var existing = candidates[i];
            var dx = existing.X - centerX;
            var dy = existing.Y - centerY;
            var distance = Math.Sqrt(dx * dx + dy * dy);
            if (distance > Math.Max(3.0, moduleSize * 2.0) || Math.Abs(existing.ModuleSize - moduleSize) > moduleSize * 0.55) continue;
            var count = existing.Count + 1;
            candidates[i] = new FinderCandidate(
                (existing.X * existing.Count + centerX) / count,
                (existing.Y * existing.Count + centerY) / count,
                (existing.ModuleSize * existing.Count + moduleSize) / count,
                count);
            return;
        }

        candidates.Add(new FinderCandidate(centerX, centerY, moduleSize, 1));
    }

    private static bool TryCrossCheckVertical(
        GrayImage image,
        byte threshold,
        bool inverted,
        int centerX,
        int centerY,
        int originalTotal,
        out double checkedCenterY,
        out int total) {
        var counts = new int[5];
        var y = centerY;
        while (y >= 0 && image.IsForeground(centerX, y, threshold, inverted)) { counts[2]++; y--; }
        if (counts[2] == 0) return FailCenter(out checkedCenterY, out total);
        while (y >= 0 && !image.IsForeground(centerX, y, threshold, inverted) && counts[1] <= originalTotal) { counts[1]++; y--; }
        if (counts[1] == 0 || y < 0) return FailCenter(out checkedCenterY, out total);
        while (y >= 0 && image.IsForeground(centerX, y, threshold, inverted) && counts[0] <= originalTotal) { counts[0]++; y--; }
        if (counts[0] == 0) return FailCenter(out checkedCenterY, out total);

        y = centerY + 1;
        while (y < image.Height && image.IsForeground(centerX, y, threshold, inverted)) { counts[2]++; y++; }
        while (y < image.Height && !image.IsForeground(centerX, y, threshold, inverted) && counts[3] <= originalTotal) { counts[3]++; y++; }
        if (counts[3] == 0 || y == image.Height) return FailCenter(out checkedCenterY, out total);
        while (y < image.Height && image.IsForeground(centerX, y, threshold, inverted) && counts[4] <= originalTotal) { counts[4]++; y++; }
        if (counts[4] == 0) return FailCenter(out checkedCenterY, out total);

        total = Sum(counts);
        if (Math.Abs(total - originalTotal) > originalTotal) return FailCenter(out checkedCenterY, out total);
        if (!IsFinderRatio(counts)) return FailCenter(out checkedCenterY, out total);
        checkedCenterY = CenterFromEnd(counts, y);
        return true;
    }

    private static bool IsFinderRatio(int[] counts) {
        var total = Sum(counts);
        if (total < 7) return false;
        var module = total / 7.0;
        var tolerance = module * 0.8;
        return Math.Abs(counts[0] - module) <= tolerance &&
               Math.Abs(counts[1] - module) <= tolerance &&
               Math.Abs(counts[2] - 3.0 * module) <= 3.0 * tolerance &&
               Math.Abs(counts[3] - module) <= tolerance &&
               Math.Abs(counts[4] - module) <= tolerance;
    }

    private static double CenterFromEnd(int[] counts, int end) => end - counts[4] - counts[3] - counts[2] / 2.0;
    private static int Sum(int[] counts) => counts[0] + counts[1] + counts[2] + counts[3] + counts[4];

    private static bool TryFindForegroundBounds(
        GrayImage image,
        byte threshold,
        bool inverted,
        CancellationToken cancellationToken,
        out PixelBounds bounds) {
        var left = image.Width;
        var top = image.Height;
        var right = -1;
        var bottom = -1;
        for (var y = 0; y < image.Height; y++) {
            if ((y & 31) == 0 && cancellationToken.IsCancellationRequested) { bounds = default; return false; }
            for (var x = 0; x < image.Width; x++) {
                if (!image.IsForeground(x, y, threshold, inverted)) continue;
                if (x < left) left = x;
                if (x > right) right = x;
                if (y < top) top = y;
                if (y > bottom) bottom = y;
            }
        }
        if (right < left || bottom < top) { bounds = default; return false; }
        bounds = new PixelBounds(left, top, right, bottom);
        return true;
    }

    private static bool IsBorderForeground(GrayImage image, byte threshold, bool inverted) {
        var foreground = 0;
        var total = 0;
        var stepX = Math.Max(1, image.Width / 64);
        var stepY = Math.Max(1, image.Height / 64);
        for (var x = 0; x < image.Width; x += stepX) {
            if (image.IsForeground(x, 0, threshold, inverted)) foreground++;
            if (image.Height > 1 && image.IsForeground(x, image.Height - 1, threshold, inverted)) foreground++;
            total += image.Height > 1 ? 2 : 1;
        }
        for (var y = stepY; y < image.Height - 1; y += stepY) {
            if (image.IsForeground(0, y, threshold, inverted)) foreground++;
            if (image.Width > 1 && image.IsForeground(image.Width - 1, y, threshold, inverted)) foreground++;
            total += image.Width > 1 ? 2 : 1;
        }
        return foreground * 2 > total;
    }

    private static byte[] BuildThresholds(GrayImage image) {
        var values = new List<byte>(5);
        AddThreshold(values, image.OtsuThreshold);
        if (image.Max - image.Min >= 160) return values.ToArray();
        AddThreshold(values, (byte)(image.Min + (image.Max - image.Min) / 2));
        AddThreshold(values, (byte)(image.Min + (image.Max - image.Min) / 3));
        AddThreshold(values, (byte)(image.Min + ((image.Max - image.Min) * 2) / 3));
        return values.ToArray();
    }

    private static void AddThreshold(List<byte> values, byte value) {
        if (!values.Contains(value)) values.Add(value);
    }

    private static bool Fail(out MicroQrDecoded decoded, out MicroQrPixelDecodeInfo info) {
        decoded = null!;
        info = null!;
        return false;
    }

    private static bool FailCenter(out double center, out int total) {
        center = 0;
        total = 0;
        return false;
    }

    private static int Clamp(int value, int min, int max) {
        if (value < min) return min;
        return value > max ? max : value;
    }

    private readonly struct FinderCandidate {
        internal FinderCandidate(double x, double y, double moduleSize, int count) {
            X = x;
            Y = y;
            ModuleSize = moduleSize;
            Count = count;
        }
        internal double X { get; }
        internal double Y { get; }
        internal double ModuleSize { get; }
        internal int Count { get; }
    }

    private readonly struct PixelBounds {
        internal PixelBounds(int left, int top, int right, int bottom) {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }
        internal int Left { get; }
        internal int Top { get; }
        internal int Right { get; }
        internal int Bottom { get; }
        internal int Width => Right - Left + 1;
        internal int Height => Bottom - Top + 1;
    }

}
