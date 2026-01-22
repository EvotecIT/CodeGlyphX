using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading;
using CodeGlyphX;
using CodeGlyphX.Internal;
using CodeGlyphX.Pdf417.Ec;

#if NET8_0_OR_GREATER
using PixelSpan = System.ReadOnlySpan<byte>;
#else
using PixelSpan = byte[];
#endif

namespace CodeGlyphX.Pdf417;

public static partial class Pdf417Decoder {
#if NET8_0_OR_GREATER
    private static bool TryDecodeWithPerspective(PixelSpan pixels, int width, int height, int stride, PixelFormat format, BoundingBox box, Candidate candidate, int threshold, bool invert, CancellationToken cancellationToken, out Pdf417Decoded decoded) {
        decoded = null!;
        if (DecodeBudget.ShouldAbort(cancellationToken)) return false;

        if (!TryFindRowEdges(pixels, width, height, stride, format, threshold, invert, box, candidate, cancellationToken, out var topLeft, out var topRight, out var bottomRight, out var bottomLeft)) {
            return false;
        }

        var transform = Qr.QrPerspectiveTransform.QuadrilateralToQuadrilateral(
            0, 0,
            candidate.WidthModules - 1, 0,
            candidate.WidthModules - 1, candidate.HeightModules - 1,
            0, candidate.HeightModules - 1,
            topLeft.x, topLeft.y,
            topRight.x, topRight.y,
            bottomRight.x, bottomRight.y,
            bottomLeft.x, bottomLeft.y);

        var offsets = new (double x, double y)[] {
            (0, 0),
            (0.25, 0),
            (-0.25, 0),
            (0, 0.25),
            (0, -0.25),
            (0.25, 0.25),
            (-0.25, 0.25),
            (0.25, -0.25),
            (-0.25, -0.25),
        };

        for (var i = 0; i < offsets.Length; i++) {
            if (DecodeBudget.ShouldAbort(cancellationToken)) return false;
            var modules = SampleModulesPerspective(pixels, width, height, stride, format, candidate.WidthModules, candidate.HeightModules, threshold, invert, transform, offsets[i].x, offsets[i].y, cancellationToken);
            if (DecodeBudget.ShouldAbort(cancellationToken)) return false;
            if (TryDecodeWithRotations(modules, cancellationToken, out decoded)) return true;

            var trimmed = TrimModuleBorder(modules);
            if (!ReferenceEquals(trimmed, modules) && TryDecodeWithRotations(trimmed, cancellationToken, out decoded)) return true;
        }

        decoded = null!;
        return false;
    }

    private static bool TryDecodeWithPerspective(PixelSpan pixels, int width, int height, int stride, PixelFormat format, BoundingBox box, Candidate candidate, int threshold, bool invert, CancellationToken cancellationToken, Pdf417DecodeDiagnostics diagnostics, out string value) {
        value = string.Empty;
        if (DecodeBudget.ShouldAbort(cancellationToken)) { diagnostics.Failure = "Cancelled."; return false; }

        if (!TryFindRowEdges(pixels, width, height, stride, format, threshold, invert, box, candidate, cancellationToken, out var topLeft, out var topRight, out var bottomRight, out var bottomLeft)) {
            return false;
        }

        var transform = Qr.QrPerspectiveTransform.QuadrilateralToQuadrilateral(
            0, 0,
            candidate.WidthModules - 1, 0,
            candidate.WidthModules - 1, candidate.HeightModules - 1,
            0, candidate.HeightModules - 1,
            topLeft.x, topLeft.y,
            topRight.x, topRight.y,
            bottomRight.x, bottomRight.y,
            bottomLeft.x, bottomLeft.y);

        var offsets = new (double x, double y)[] {
            (0, 0),
            (0.25, 0),
            (-0.25, 0),
            (0, 0.25),
            (0, -0.25),
            (0.25, 0.25),
            (-0.25, 0.25),
            (0.25, -0.25),
            (-0.25, -0.25),
        };

        for (var i = 0; i < offsets.Length; i++) {
            if (DecodeBudget.ShouldAbort(cancellationToken)) { diagnostics.Failure = "Cancelled."; return false; }
            var modules = SampleModulesPerspective(pixels, width, height, stride, format, candidate.WidthModules, candidate.HeightModules, threshold, invert, transform, offsets[i].x, offsets[i].y, cancellationToken);
            if (DecodeBudget.ShouldAbort(cancellationToken)) { diagnostics.Failure = "Cancelled."; return false; }
            if (TryDecodeWithRotations(modules, cancellationToken, diagnostics, out value)) return true;

            var trimmed = TrimModuleBorder(modules);
            if (!ReferenceEquals(trimmed, modules) && TryDecodeWithRotations(trimmed, cancellationToken, diagnostics, out value)) return true;
        }

        value = string.Empty;
        return false;
    }

    private static BitMatrix SampleModulesPerspective(PixelSpan pixels, int width, int height, int stride, PixelFormat format, int widthModules, int heightModules, int threshold, bool invert, Qr.QrPerspectiveTransform transform, double offsetX, double offsetY, CancellationToken cancellationToken) {
        var modules = new BitMatrix(widthModules, heightModules);
        for (var y = 0; y < heightModules; y++) {
            if (DecodeBudget.ShouldAbort(cancellationToken)) return modules;
            for (var x = 0; x < widthModules; x++) {
                if (DecodeBudget.ShouldAbort(cancellationToken)) return modules;
                transform.Transform(x + 0.5 + offsetX, y + 0.5 + offsetY, out var sx, out var sy);
                if (double.IsNaN(sx) || double.IsNaN(sy)) continue;
                var dark = IsDarkAverage(pixels, width, height, stride, format, sx, sy, threshold);
                modules[x, y] = invert ? !dark : dark;
            }
        }
        return modules;
    }

    private static bool IsDarkAverage(PixelSpan pixels, int width, int height, int stride, PixelFormat format, double x, double y, int threshold) {
        var cx = (int)Math.Round(x);
        var cy = (int)Math.Round(y);
        var sum = 0;
        var count = 0;
        for (var dy = -1; dy <= 1; dy++) {
            var yy = cy + dy;
            if ((uint)yy >= (uint)height) continue;
            var row = yy * stride;
            for (var dx = -1; dx <= 1; dx++) {
                var xx = cx + dx;
                if ((uint)xx >= (uint)width) continue;
                var p = row + xx * 4;
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
                sum += lum;
                count++;
            }
        }

        if (count == 0) return false;
        var avg = sum / count;
        return avg < threshold;
    }
#endif

    private static bool TryFindRowEdges(
        PixelSpan pixels,
        int width,
        int height,
        int stride,
        PixelFormat format,
        int threshold,
        bool invert,
        BoundingBox box,
        Candidate candidate,
        CancellationToken cancellationToken,
        out (double x, double y) topLeft,
        out (double x, double y) topRight,
        out (double x, double y) bottomRight,
        out (double x, double y) bottomLeft) {
        topLeft = topRight = bottomRight = bottomLeft = default;
        if (DecodeBudget.ShouldAbort(cancellationToken)) return false;

        var minRun = Math.Max(1, candidate.ModuleSize / 3);
        var topY = box.Top + Math.Max(1, box.Height / 8);
        var bottomY = box.Bottom - Math.Max(1, box.Height / 8);
        var slack = Math.Max(candidate.ModuleSize * 2, box.Height / 6);

        if (!TryFindEdgeAtRowWithSlack(pixels, width, height, stride, format, threshold, invert, box.Left, box.Right, topY, minRun, slack, cancellationToken, out var leftTop, out var rightTop, out topY)) {
            if (!TryFindEdgeAtRowWithSlack(pixels, width, height, stride, format, threshold, invert, box.Left, box.Right, topY, minRun, box.Height / 3, cancellationToken, out leftTop, out rightTop, out topY)) return false;
        }
        if (!TryFindEdgeAtRowWithSlack(pixels, width, height, stride, format, threshold, invert, box.Left, box.Right, bottomY, minRun, slack, cancellationToken, out var leftBottom, out var rightBottom, out bottomY)) {
            if (!TryFindEdgeAtRowWithSlack(pixels, width, height, stride, format, threshold, invert, box.Left, box.Right, bottomY, minRun, box.Height / 3, cancellationToken, out leftBottom, out rightBottom, out bottomY)) return false;
        }

        // Fall back to nearby rows if edges are noisy.
        if (Math.Abs(leftTop - leftBottom) > candidate.ModuleSize * 8 || Math.Abs(rightTop - rightBottom) > candidate.ModuleSize * 8) {
            var midY = box.Top + box.Height / 2;
            if (!TryFindEdgeAtRow(pixels, width, height, stride, format, threshold, invert, box.Left, box.Right, midY, minRun, cancellationToken, out var leftMid, out var rightMid)) return false;
            leftTop = (leftTop + leftMid) / 2.0;
            rightTop = (rightTop + rightMid) / 2.0;
            leftBottom = (leftBottom + leftMid) / 2.0;
            rightBottom = (rightBottom + rightMid) / 2.0;
        }

        topLeft = (leftTop, topY);
        topRight = (rightTop, topY);
        bottomLeft = (leftBottom, bottomY);
        bottomRight = (rightBottom, bottomY);
        return true;
    }

    private static bool TryFindEdgeAtRowWithSlack(
        PixelSpan pixels,
        int width,
        int height,
        int stride,
        PixelFormat format,
        int threshold,
        bool invert,
        int left,
        int right,
        int y,
        int minRun,
        int slack,
        CancellationToken cancellationToken,
        out double leftEdge,
        out double rightEdge,
        out int yUsed) {
        leftEdge = 0;
        rightEdge = 0;
        yUsed = y;
        var start = Math.Max(0, y - slack);
        var end = Math.Min(height - 1, y + slack);
        for (var yy = start; yy <= end; yy++) {
            if (DecodeBudget.ShouldAbort(cancellationToken)) return false;
            if (TryFindEdgeAtRow(pixels, width, height, stride, format, threshold, invert, left, right, yy, minRun, cancellationToken, out leftEdge, out rightEdge)) {
                yUsed = yy;
                return true;
            }
        }
        return false;
    }

    private static bool TryFindEdgeAtRow(
        PixelSpan pixels,
        int width,
        int height,
        int stride,
        PixelFormat format,
        int threshold,
        bool invert,
        int left,
        int right,
        int y,
        int minRun,
        CancellationToken cancellationToken,
        out double leftEdge,
        out double rightEdge) {
        leftEdge = 0;
        rightEdge = 0;
        if ((uint)y >= (uint)height) return false;

        if (!TryFindLeftEdgeRun(pixels, width, height, stride, format, left, right, y, threshold, invert, minRun, cancellationToken, out var leftX) ||
            !TryFindRightEdgeRun(pixels, width, height, stride, format, left, right, y, threshold, invert, minRun, cancellationToken, out var rightX)) {
            return TryFindEdgeAtRowLoose(pixels, width, height, stride, format, threshold, invert, left, right, y, cancellationToken, out leftEdge, out rightEdge);
        }

        leftEdge = leftX;
        rightEdge = rightX;
        return rightEdge > leftEdge;
    }

    private static bool TryFindEdgeAtRowLoose(
        PixelSpan pixels,
        int width,
        int height,
        int stride,
        PixelFormat format,
        int threshold,
        bool invert,
        int left,
        int right,
        int y,
        CancellationToken cancellationToken,
        out double leftEdge,
        out double rightEdge) {
        leftEdge = 0;
        rightEdge = 0;
        if ((uint)y >= (uint)height) return false;
        var row = y * stride;
        var foundLeft = false;
        for (var x = left; x <= right; x++) {
            if (DecodeBudget.ShouldAbort(cancellationToken)) return false;
            if ((uint)x >= (uint)width) continue;
            var dark = IsDarkAt(pixels, row, x, format, threshold);
            if (invert) dark = !dark;
            if (dark) {
                leftEdge = x;
                foundLeft = true;
                break;
            }
        }
        if (!foundLeft) return false;
        for (var x = right; x >= left; x--) {
            if (DecodeBudget.ShouldAbort(cancellationToken)) return false;
            if ((uint)x >= (uint)width) continue;
            var dark = IsDarkAt(pixels, row, x, format, threshold);
            if (invert) dark = !dark;
            if (dark) {
                rightEdge = x;
                break;
            }
        }

        return rightEdge > leftEdge;
    }

    private static bool TryFindRightEdgeRun(PixelSpan pixels, int width, int height, int stride, PixelFormat format, int left, int right, int y, int threshold, bool invert, int minRun, CancellationToken cancellationToken, out int xFound) {
        xFound = 0;
        if ((uint)y >= (uint)height) return false;
        var row = y * stride;
        var run = 0;
        for (var x = right; x >= left; x--) {
            if (DecodeBudget.ShouldAbort(cancellationToken)) return false;
            if ((uint)x >= (uint)width) continue;
            var dark = IsDarkAt(pixels, row, x, format, threshold);
            if (invert) dark = !dark;
            if (dark) {
                run++;
                if (run >= minRun) {
                    xFound = x + run - 1;
                    return true;
                }
            } else {
                run = 0;
            }
        }
        return false;
    }

    private static BitMatrix SampleModulesSheared(PixelSpan pixels, int width, int height, int stride, PixelFormat format, BoundingBox box, int widthModules, int heightModules, int moduleSize, int threshold, bool invert, double shearModulesPerRow, int? leftEdgeMid, CancellationToken cancellationToken) {
        var modules = new BitMatrix(widthModules, heightModules);
        var totalWidth = widthModules * moduleSize;
        var totalHeight = heightModules * moduleSize;
        var half = moduleSize / 2.0;
        var offsetX = leftEdgeMid.HasValue
            ? leftEdgeMid.Value - half
            : box.Left + (box.Width - totalWidth) / 2.0;
        var offsetY = box.Top + (box.Height - totalHeight) / 2.0;

        var midRow = (heightModules - 1) / 2.0;

        for (var y = 0; y < heightModules; y++) {
            if (DecodeBudget.ShouldAbort(cancellationToken)) return modules;
            var rowShift = (y - midRow) * shearModulesPerRow * moduleSize;
            var sy = (int)Math.Round(offsetY + (y * moduleSize) + half);
            sy = Clamp(sy, 0, height - 1);
            for (var x = 0; x < widthModules; x++) {
                if (DecodeBudget.ShouldAbort(cancellationToken)) return modules;
                var sx = (int)Math.Round(offsetX + rowShift + (x * moduleSize) + half);
                sx = Clamp(sx, 0, width - 1);
                var dark = IsDark(pixels, width, height, stride, format, sx, sy, threshold);
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

    private static bool TryDecodeWithRotations(BitMatrix modules, CancellationToken cancellationToken, out string value) {
        if (DecodeBudget.ShouldAbort(cancellationToken)) { value = string.Empty; return false; }
        if (TryDecode(modules, cancellationToken, out value)) return true;
        if (DecodeBudget.ShouldAbort(cancellationToken)) { value = string.Empty; return false; }
        if (TryDecode(Rotate90(modules), cancellationToken, out value)) return true;
        if (DecodeBudget.ShouldAbort(cancellationToken)) { value = string.Empty; return false; }
        if (TryDecode(Rotate180(modules), cancellationToken, out value)) return true;
        if (DecodeBudget.ShouldAbort(cancellationToken)) { value = string.Empty; return false; }
        if (TryDecode(Rotate270(modules), cancellationToken, out value)) return true;
        value = string.Empty;
        return false;
    }

    private static bool TryDecodeWithRotations(BitMatrix modules, CancellationToken cancellationToken, out Pdf417Decoded decoded) {
        if (DecodeBudget.ShouldAbort(cancellationToken)) { decoded = null!; return false; }
        if (TryDecode(modules, cancellationToken, out Pdf417Decoded pdf417)) { decoded = pdf417; return true; }
        if (DecodeBudget.ShouldAbort(cancellationToken)) { decoded = null!; return false; }
        if (TryDecode(Rotate90(modules), cancellationToken, out pdf417)) { decoded = pdf417; return true; }
        if (DecodeBudget.ShouldAbort(cancellationToken)) { decoded = null!; return false; }
        if (TryDecode(Rotate180(modules), cancellationToken, out pdf417)) { decoded = pdf417; return true; }
        if (DecodeBudget.ShouldAbort(cancellationToken)) { decoded = null!; return false; }
        if (TryDecode(Rotate270(modules), cancellationToken, out pdf417)) { decoded = pdf417; return true; }
        decoded = null!;
        return false;
    }

    private static bool TryDecodeWithRotations(BitMatrix modules, CancellationToken cancellationToken, Pdf417DecodeDiagnostics diagnostics, out string value) {
        if (DecodeBudget.ShouldAbort(cancellationToken)) { value = string.Empty; diagnostics.Failure = "Cancelled."; return false; }
        if (TryDecodeInternal(modules, cancellationToken, diagnostics, out value)) return true;
        if (DecodeBudget.ShouldAbort(cancellationToken)) { value = string.Empty; diagnostics.Failure = "Cancelled."; return false; }
        if (TryDecodeInternal(Rotate90(modules), cancellationToken, diagnostics, out value)) return true;
        if (DecodeBudget.ShouldAbort(cancellationToken)) { value = string.Empty; diagnostics.Failure = "Cancelled."; return false; }
        if (TryDecodeInternal(Rotate180(modules), cancellationToken, diagnostics, out value)) return true;
        if (DecodeBudget.ShouldAbort(cancellationToken)) { value = string.Empty; diagnostics.Failure = "Cancelled."; return false; }
        if (TryDecodeInternal(Rotate270(modules), cancellationToken, diagnostics, out value)) return true;
        value = string.Empty;
        return false;
    }

    private static bool TryDecodeWithStartPattern(BitMatrix modules, CancellationToken cancellationToken, out string value) {
        var height = modules.Height;
        if (height == 0) {
            value = string.Empty;
            return false;
        }

        var offsets = new Dictionary<int, OffsetScore>();
        var rows = new[] { height / 2, height / 3, (height * 2) / 3 };
        for (var i = 0; i < rows.Length; i++) {
            if (DecodeBudget.ShouldAbort(cancellationToken)) { value = string.Empty; return false; }
            var row = rows[i];
            if ((uint)row >= (uint)height) continue;
            foreach (var hit in FindPatternOffsets(modules, row, StartPattern, StartPatternWidth, maxErrors: 2)) {
                if (!offsets.TryGetValue(hit.Offset, out var score)) {
                    score = new OffsetScore { Count = 0, MinErrors = hit.Errors };
                }
                score.Count++;
                if (hit.Errors < score.MinErrors) score.MinErrors = hit.Errors;
                offsets[hit.Offset] = score;
            }
        }

        if (offsets.Count == 0) {
            value = string.Empty;
            return false;
        }

        var ordered = new List<OffsetCandidate>(offsets.Count);
        foreach (var kvp in offsets) {
            ordered.Add(new OffsetCandidate(kvp.Key, kvp.Value.Count, kvp.Value.MinErrors));
        }
        ordered.Sort(OffsetCandidateComparer.Instance);

        for (var i = 0; i < ordered.Count; i++) {
            if (DecodeBudget.ShouldAbort(cancellationToken)) { value = string.Empty; return false; }
            if (TryDecodeWithOffset(modules, ordered[i].Offset, cancellationToken, out value)) return true;
        }

        value = string.Empty;
        return false;
    }

    private static bool TryDecodeWithStartPattern(BitMatrix modules, CancellationToken cancellationToken, out string value, out Pdf417MacroMetadata? macro) {
        var height = modules.Height;
        macro = null;
        if (height == 0) {
            value = string.Empty;
            return false;
        }

        var offsets = new Dictionary<int, OffsetScore>();
        var rows = new[] { height / 2, height / 3, (height * 2) / 3 };
        for (var i = 0; i < rows.Length; i++) {
            if (DecodeBudget.ShouldAbort(cancellationToken)) { value = string.Empty; return false; }
            var row = rows[i];
            if ((uint)row >= (uint)height) continue;
            foreach (var hit in FindPatternOffsets(modules, row, StartPattern, StartPatternWidth, maxErrors: 2)) {
                if (!offsets.TryGetValue(hit.Offset, out var score)) {
                    score = new OffsetScore { Count = 0, MinErrors = hit.Errors };
                }
                score.Count++;
                if (hit.Errors < score.MinErrors) score.MinErrors = hit.Errors;
                offsets[hit.Offset] = score;
            }
        }

        if (offsets.Count == 0) {
            value = string.Empty;
            return false;
        }

        var ordered = new List<OffsetCandidate>(offsets.Count);
        foreach (var kvp in offsets) {
            ordered.Add(new OffsetCandidate(kvp.Key, kvp.Value.Count, kvp.Value.MinErrors));
        }
        ordered.Sort(OffsetCandidateComparer.Instance);

        for (var i = 0; i < ordered.Count; i++) {
            if (DecodeBudget.ShouldAbort(cancellationToken)) { value = string.Empty; return false; }
            if (TryDecodeWithOffset(modules, ordered[i].Offset, cancellationToken, out value, out macro)) return true;
        }

        value = string.Empty;
        macro = null;
        return false;
    }

    private static bool TryDecodeWithStartPattern(BitMatrix modules, CancellationToken cancellationToken, Pdf417DecodeDiagnostics diagnostics, out string value) {
        var height = modules.Height;
        if (height == 0) {
            value = string.Empty;
            diagnostics.Failure ??= "Invalid module size.";
            return false;
        }

        var offsets = new Dictionary<int, OffsetScore>();
        var rows = new[] { height / 2, height / 3, (height * 2) / 3 };
        for (var i = 0; i < rows.Length; i++) {
            if (DecodeBudget.ShouldAbort(cancellationToken)) { value = string.Empty; diagnostics.Failure = "Cancelled."; return false; }
            var row = rows[i];
            if ((uint)row >= (uint)height) continue;
            foreach (var hit in FindPatternOffsets(modules, row, StartPattern, StartPatternWidth, maxErrors: 2)) {
                if (!offsets.TryGetValue(hit.Offset, out var score)) {
                    score = new OffsetScore { Count = 0, MinErrors = hit.Errors };
                }
                score.Count++;
                if (hit.Errors < score.MinErrors) score.MinErrors = hit.Errors;
                offsets[hit.Offset] = score;
            }
        }

        if (offsets.Count == 0) {
            value = string.Empty;
            diagnostics.Failure ??= "Start pattern not found.";
            return false;
        }

        var ordered = new List<OffsetCandidate>(offsets.Count);
        foreach (var kvp in offsets) {
            ordered.Add(new OffsetCandidate(kvp.Key, kvp.Value.Count, kvp.Value.MinErrors));
        }
        ordered.Sort(OffsetCandidateComparer.Instance);
        diagnostics.StartPatternCandidates += ordered.Count;

        for (var i = 0; i < ordered.Count; i++) {
            if (DecodeBudget.ShouldAbort(cancellationToken)) { value = string.Empty; diagnostics.Failure = "Cancelled."; return false; }
            diagnostics.StartPatternAttempts++;
            if (TryDecodeWithOffset(modules, ordered[i].Offset, cancellationToken, diagnostics, out value)) return true;
        }

        value = string.Empty;
        return false;
    }

    private static bool TryDecodeWithOffset(BitMatrix modules, int startOffset, CancellationToken cancellationToken, out string value) {
        var maxWidth = modules.Width - startOffset;
        if (maxWidth < StartPatternWidth + 17 + 17 + 1) {
            value = string.Empty;
            return false;
        }

        for (var compact = 0; compact <= 1; compact++) {
            if (DecodeBudget.ShouldAbort(cancellationToken)) { value = string.Empty; return false; }
            var baseOffset = compact == 1 ? 35 : 69;
            for (var cols = 1; cols <= 30; cols++) {
                if (DecodeBudget.ShouldAbort(cancellationToken)) { value = string.Empty; return false; }
                var widthModules = cols * 17 + baseOffset;
                if (widthModules > maxWidth) break;

                var cropped = CropColumns(modules, startOffset, widthModules);
                if (TryDecodeCore(cropped, cancellationToken, out value)) return true;
            }
        }

        value = string.Empty;
        return false;
    }

    private static bool TryDecodeWithOffset(BitMatrix modules, int startOffset, CancellationToken cancellationToken, out string value, out Pdf417MacroMetadata? macro) {
        var maxWidth = modules.Width - startOffset;
        macro = null;
        if (maxWidth < StartPatternWidth + 17 + 17 + 1) {
            value = string.Empty;
            return false;
        }

        for (var compact = 0; compact <= 1; compact++) {
            if (DecodeBudget.ShouldAbort(cancellationToken)) { value = string.Empty; return false; }
            var baseOffset = compact == 1 ? 35 : 69;
            for (var cols = 1; cols <= 30; cols++) {
                if (DecodeBudget.ShouldAbort(cancellationToken)) { value = string.Empty; return false; }
                var widthModules = cols * 17 + baseOffset;
                if (widthModules > maxWidth) break;

                var cropped = CropColumns(modules, startOffset, widthModules);
                if (TryDecodeCore(cropped, cancellationToken, out value, out macro)) return true;
            }
        }

        value = string.Empty;
        macro = null;
        return false;
    }

    private static bool TryDecodeWithOffset(BitMatrix modules, int startOffset, CancellationToken cancellationToken, Pdf417DecodeDiagnostics diagnostics, out string value) {
        var maxWidth = modules.Width - startOffset;
        if (maxWidth < StartPatternWidth + 17 + 17 + 1) {
            value = string.Empty;
            diagnostics.Failure ??= "Invalid start offset.";
            return false;
        }

        for (var compact = 0; compact <= 1; compact++) {
            if (DecodeBudget.ShouldAbort(cancellationToken)) { value = string.Empty; diagnostics.Failure = "Cancelled."; return false; }
            var baseOffset = compact == 1 ? 35 : 69;
            for (var cols = 1; cols <= 30; cols++) {
                if (DecodeBudget.ShouldAbort(cancellationToken)) { value = string.Empty; diagnostics.Failure = "Cancelled."; return false; }
                var widthModules = cols * 17 + baseOffset;
                if (widthModules > maxWidth) break;

                var cropped = CropColumns(modules, startOffset, widthModules);
                if (TryDecodeCore(cropped, cancellationToken, diagnostics, out value)) return true;
            }
        }

        value = string.Empty;
        return false;
    }

    private static IEnumerable<OffsetMatch> FindPatternOffsets(BitMatrix modules, int row, int pattern, int length, int maxErrors) {
        var width = modules.Width;
        if (length <= 0 || width < length) yield break;

        for (var x = 0; x <= width - length; x++) {
            var bits = 0;
            for (var i = 0; i < length; i++) {
                bits = (bits << 1) | (modules[x + i, row] ? 1 : 0);
            }
            var diff = bits ^ pattern;
            var errors = PopCount(diff);
            if (errors <= maxErrors) yield return new OffsetMatch(x, errors);
        }
    }

    private readonly struct OffsetMatch {
        public int Offset { get; }
        public int Errors { get; }

        public OffsetMatch(int offset, int errors) {
            Offset = offset;
            Errors = errors;
        }
    }

    private struct OffsetScore {
        public int Count;
        public int MinErrors;
    }

    private readonly struct OffsetCandidate {
        public int Offset { get; }
        public int Count { get; }
        public int Errors { get; }

        public OffsetCandidate(int offset, int count, int errors) {
            Offset = offset;
            Count = count;
            Errors = errors;
        }
    }

    private sealed class OffsetCandidateComparer : IComparer<OffsetCandidate> {
        public static readonly OffsetCandidateComparer Instance = new();
        public int Compare(OffsetCandidate x, OffsetCandidate y) {
            var cmp = x.Errors.CompareTo(y.Errors);
            if (cmp != 0) return cmp;
            cmp = y.Count.CompareTo(x.Count);
            if (cmp != 0) return cmp;
            return x.Offset.CompareTo(y.Offset);
        }
    }

    private static int PopCount(int value) {
        var count = 0;
        while (value != 0) {
            count += value & 1;
            value >>= 1;
        }
        return count;
    }

    private static BitMatrix CropColumns(BitMatrix modules, int left, int width) {
        var height = modules.Height;
        var cropped = new BitMatrix(width, height);
        for (var y = 0; y < height; y++) {
            for (var x = 0; x < width; x++) {
                cropped[x, y] = modules[left + x, y];
            }
        }
        return cropped;
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

}
