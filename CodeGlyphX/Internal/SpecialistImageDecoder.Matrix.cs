using System;
using System.Threading;
using CodeGlyphX.RmQr;
using CodeGlyphX.D3Aqr;

namespace CodeGlyphX.Internal;

internal static partial class SpecialistImageDecoder {
    private static bool TryMatrix(MaskView view, ImageRegion region, SymbolFormat format, CancellationToken token,
        out ImageRegion sampledRegion, out DetectedSymbol symbol, out int rotation, out bool mirrored) {
        sampledRegion = region; symbol = null!; rotation = 0; mirrored = false;
        if (format == SymbolFormat.RmQrCode) {
            for (var version = 1; version <= 32; version++) {
                var width = RmQrTables.GetWidth(version); var height = RmQrTables.GetHeight(version);
                var ratio = region.Width / (double)width / (region.Height / (double)height);
                if (ratio < .65 || ratio > 1.55) continue;
                if (TrySample(view, region, width, height, token, out var grid)
                    && TryOrientations(grid, format, token, out symbol, out rotation, out mirrored)) return true;
            }
            return false;
        }
        if (format == SymbolFormat.D3Aqr) return TryD3(view, region, token, out sampledRegion, out symbol, out rotation, out mirrored);
        if (format == SymbolFormat.DotCode) return TryDots(view, region, token, out symbol, out rotation, out mirrored);
        if (region.Width / (double)region.Height < .65 || region.Width / (double)region.Height > 1.55) return false;
        var pitch = EstimatePitch(view, region);
        for (var size = 23; size <= 189; size += 2) {
            if (DecodeBudget.ShouldAbort(token)) return false;
            if (pitch > 0 && Math.Abs(region.Width / (double)size / pitch - 1) > .22) continue;
            if (TrySample(view, region, size, size, token, out var grid)
                && TryOrientations(grid, format, token, out symbol, out rotation, out mirrored)) return true;
        }
        return false;
    }

    private static bool TryD3(MaskView view, ImageRegion region, CancellationToken token,
        out ImageRegion sampledRegion, out DetectedSymbol symbol, out int rotation, out bool mirrored) {
        sampledRegion = region; symbol = null!; rotation = 0; mirrored = false;
        if (region.Width / (double)region.Height < .8 || region.Width / (double)region.Height > 1.25) return false;
        var measured = EstimatePitch(view, region);
        for (var choice = 0; choice < 3; choice++) {
            D3AqrLayout.GetPitch(choice == 0 ? 2 : choice == 1 ? 2.5 : 3, out var half, out var host);
            for (var version = 1; version <= 40; version++) {
                if (DecodeBudget.ShouldAbort(token)) return false;
                var size = D3AqrLayout.GetSize(version * 4 + 17, half, host, out _);
                foreach (var divisor in new[] { host, half, half * 2 }) {
                    var pitch = measured / divisor;
                    if (pitch < 1 || region.Width / pitch > size + 1 || region.Width / pitch < size - 8 * half - 1) continue;
                    var pixels = (int)Math.Round(size * pitch);
                    // The fixed timing cells reach the right and bottom grid edges. Preserve the
                    // category-dependent white cells before the host instead of trimming them away.
                    var left = region.X + region.Width - pixels; var top = region.Y + region.Height - pixels;
                    if (left < 0 || top < 0 || left + pixels > view.Width || top + pixels > view.Height) continue;
                    var area = new ImageRegion(left, top, pixels, pixels);
                    if (TrySample(view, area, size, size, token, out var grid)
                        && TryOrientations(grid, SymbolFormat.D3Aqr, token, out symbol, out rotation, out mirrored)) {
                        sampledRegion = area; return true;
                    }
                }
            }
        }
        return false;
    }

    private static bool TryDots(MaskView view, ImageRegion region, CancellationToken token,
        out DetectedSymbol symbol, out int rotation, out bool mirrored) {
        symbol = null!; rotation = 0; mirrored = false;
        // Circular dots can overlap in projection while remaining separate components. Use their
        // centers, not occupied projection bands or the renderer-dependent dot diameter.
        var local = new byte[checked(region.Width * region.Height)];
        for (var y = 0; y < region.Height; y++) {
            if (DecodeBudget.ShouldAbort(token)) return false;
            for (var x = 0; x < region.Width; x++) local[y * region.Width + x] = (byte)(view.Get(region.X + x, region.Y + y) ? 1 : 0);
        }
        var points = CollectDotComponents(local, region, token, out var diameters);
        if (points.Count < 4 || points.Count > 20000) return false;
        diameters.Sort();
        var tolerance = Math.Max(.5, diameters[diameters.Count / 2] * .45);
        var xs = DotCenters(points, horizontal: true, tolerance);
        var ys = DotCenters(points, horizontal: false, tolerance);
        if (xs.Count < 3 || ys.Count < 3) return false;
        var stepX = MinimumCenterStep(xs); var stepY = MinimumCenterStep(ys);
        if (stepX < 1 || stepY < 1) return false;
        var width = (int)Math.Round((xs[xs.Count - 1] - xs[0]) / stepX) + 1;
        var height = (int)Math.Round((ys[ys.Count - 1] - ys[0]) / stepY) + 1;
        if (width < 5 || width > 200 || height < 5 || height > 200) return false;
        stepX = (xs[xs.Count - 1] - xs[0]) / (width - 1);
        stepY = (ys[ys.Count - 1] - ys[0]) / (height - 1);
        var grid = new BitMatrix(width, height);
        for (var y = 0; y < height; y++) {
            if (DecodeBudget.ShouldAbort(token)) return false;
            for (var x = 0; x < width; x++) {
                var px = Math.Min(region.X + region.Width - 1, (int)Math.Round(xs[0] + stepX * x));
                var py = Math.Min(region.Y + region.Height - 1, (int)Math.Round(ys[0] + stepY * y));
                grid[x, y] = view.Get(px, py);
            }
        }
        return TryOrientations(grid, SymbolFormat.DotCode, token, out symbol, out rotation, out mirrored);
    }

    private static System.Collections.Generic.List<SymbolPoint> CollectDotComponents(byte[] mask, ImageRegion region,
        CancellationToken token, out System.Collections.Generic.List<int> diameters) {
        var points = new System.Collections.Generic.List<SymbolPoint>();
        var sizes = new System.Collections.Generic.List<int>();
        BinaryImageComponents.Visit(mask, region.Width, region.Height, false, token, (queue, length, edge) => {
            if (length < 2) return false;
            var left = region.Width; var right = 0; var top = region.Height; var bottom = 0;
            long sumX = 0; long sumY = 0;
            for (var i = 0; i < length; i++) {
                var x = queue[i] % region.Width; var y = queue[i] / region.Width;
                left = Math.Min(left, x); right = Math.Max(right, x); top = Math.Min(top, y); bottom = Math.Max(bottom, y);
                sumX += x; sumY += y;
            }
            if (right - left > (bottom - top + 1) * 2 || bottom - top > (right - left + 1) * 2) return false;
            points.Add(new SymbolPoint(region.X + sumX / (double)length, region.Y + sumY / (double)length));
            sizes.Add(Math.Min(right - left + 1, bottom - top + 1));
            return points.Count > 20000;
        });
        diameters = sizes;
        return points;
    }

    private static System.Collections.Generic.List<double> DotCenters(System.Collections.Generic.List<SymbolPoint> points,
        bool horizontal, double tolerance) {
        var centers = new System.Collections.Generic.List<double>();
        foreach (var point in points) centers.Add(horizontal ? point.X : point.Y);
        centers.Sort();
        var grouped = new System.Collections.Generic.List<double>();
        var sum = centers[0]; var count = 1; var start = centers[0];
        for (var i = 1; i < centers.Count; i++) {
            if (centers[i] - start < tolerance) { sum += centers[i]; count++; continue; }
            grouped.Add(sum / count); sum = start = centers[i]; count = 1;
        }
        grouped.Add(sum / count);
        return grouped;
    }

    private static double MinimumCenterStep(System.Collections.Generic.List<double> centers) {
        var steps = new System.Collections.Generic.List<double>();
        for (var i = 1; i < centers.Count; i++) steps.Add(centers[i] - centers[i - 1]);
        steps.Sort();
        var sum = 0d; var count = 0;
        foreach (var step in steps) {
            if (step > steps[0] * 1.5) break;
            sum += step; count++;
        }
        return sum / count;
    }
}
