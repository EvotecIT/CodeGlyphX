using System;
using System.Collections.Generic;
using System.Threading;
using CodeGlyphX.Internal;

#if NET8_0_OR_GREATER
using PixelSpan = System.ReadOnlySpan<byte>;
#else
using PixelSpan = byte[];
#endif

namespace CodeGlyphX.Pdf417;

public static partial class Pdf417Decoder {
    private static List<Candidate> BuildCandidates(PixelSpan pixels, int width, int height, int stride, PixelFormat format, int threshold, BoundingBox box, bool invert) {
        var seen = new HashSet<(int module, int width, int height)>();

        if (TryEstimateModuleSize(pixels, width, height, stride, format, threshold, box, invert, out var estimated)) {
            for (var delta = -2; delta <= 2; delta++) {
                var candidate = estimated + delta;
                if (candidate <= 0) continue;
                AddCandidateFromModuleSize(box, candidate, seen);
            }
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
        if (Math.Abs(widthPx - box.Width) > moduleSize * 4) return;
        if (Math.Abs(heightPx - box.Height) > moduleSize * 4) return;

        if (!TryGetDimensions(widthModules, out var cols, out _)) return;
        if (cols < 1 || cols > 30) return;

        seen.Add((moduleSize, widthModules, heightModules));
    }

    private static BitMatrix SampleModules(PixelSpan pixels, int width, int height, int stride, PixelFormat format, BoundingBox box, int widthModules, int heightModules, int moduleSize, int threshold, bool invert, CancellationToken cancellationToken) {
        var totalWidth = widthModules * moduleSize;
        var totalHeight = heightModules * moduleSize;
        var offsetX = box.Left + (box.Width - totalWidth) / 2.0;
        var offsetY = box.Top + (box.Height - totalHeight) / 2.0;

        return SampleGrid(pixels, width, height, stride, format, offsetX, offsetY, widthModules, heightModules,
            moduleSize, moduleSize, roundCenters: true, threshold, invert, cancellationToken);
    }

    private static BitMatrix SampleGrid(PixelSpan pixels, int width, int height, int stride, PixelFormat format,
        double left, double top, int widthModules, int heightModules, double stepX, double stepY, bool roundCenters,
        int threshold, bool invert, CancellationToken cancellationToken) {
        var modules = new BitMatrix(widthModules, heightModules);
        for (var y = 0; y < heightModules; y++) {
            if (DecodeBudget.ShouldAbort(cancellationToken)) return modules;
            var centerY = top + (y + 0.5) * stepY;
            var sy = (int)(roundCenters ? Math.Round(centerY) : Math.Floor(centerY));
            sy = Clamp(sy, 0, height - 1);
            for (var x = 0; x < widthModules; x++) {
                if (DecodeBudget.ShouldAbort(cancellationToken)) return modules;
                var centerX = left + (x + 0.5) * stepX;
                var sx = (int)(roundCenters ? Math.Round(centerX) : Math.Floor(centerX));
                sx = Clamp(sx, 0, width - 1);
                var dark = IsDark(pixels, width, height, stride, format, sx, sy, threshold);
                modules[x, y] = invert ? !dark : dark;
            }
        }

        return modules;
    }

    // Physical layouts can rasterize each module to a fractional number of pixels. Fit both
    // axes to the detected symbol extent rather than accumulating a rounded pitch across a row.
    private static bool TryDecodeFittedGrid(PixelSpan pixels, int width, int height, int stride, PixelFormat format,
        BoundingBox box, int threshold, bool invert, CancellationToken cancellationToken,
        Pdf417DecodeDiagnostics? diagnostics, out Pdf417Decoded decoded) {
        decoded = null!;
        diagnostics ??= new Pdf417DecodeDiagnostics();
        var seenWidths = new HashSet<int>();
        for (var compact = 0; compact <= 1; compact++) {
            var offset = compact == 1 ? 35 : 69;
            for (var columns = 1; columns <= 30; columns++) {
                if (DecodeBudget.ShouldAbort(cancellationToken)) return false;
                var widthModules = columns * 17 + offset;
                if (!seenWidths.Add(widthModules)) continue;
                var stepX = box.Width / (double)widthModules;
                if (stepX < 1) continue;
                var estimatedRows = (int)Math.Round(box.Height / stepX);
                // Axis rounding may shift the inferred row count by one. Keep the attempt
                // space within the existing PDF417 row bounds and do not rescale the image.
                for (var delta = 0; delta < 3; delta++) {
                    if (DecodeBudget.ShouldAbort(cancellationToken)) return false;
                    var rows = estimatedRows + (delta == 0 ? 0 : delta == 1 ? -1 : 1);
                    if (rows < 3 || rows > 90) continue;
                    var stepY = box.Height / (double)rows;
                    if (stepY < 1) continue;
                    var modules = SampleGrid(pixels, width, height, stride, format, box.Left, box.Top,
                        widthModules, rows, stepX, stepY, roundCenters: false, threshold, invert, cancellationToken);
                    if (DecodeBudget.ShouldAbort(cancellationToken)) return false;
                    if (!TryDecodeWithRotations(modules, cancellationToken, diagnostics, out var value)) continue;
                    decoded = new Pdf417Decoded(value, diagnostics.Macro);
                    return true;
                }
            }
        }
        return false;
    }

}
