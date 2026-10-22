using System;
using System.Threading;
using CodeGlyphX.Internal;

namespace CodeGlyphX.Ultracode;

internal static partial class UltracodeImageDecoder {
    internal static bool Visit(byte[] rgba, int width, int height, ImageSamplingGrid? calibration,
        CancellationToken token, Func<UltracodeDecoded, SymbolGeometry, bool> visitor) {
        if (DecodeBudget.ShouldAbort(token)) return true;
        if ((long)width * height > 16_000_000) return false;
        if (calibration != null) {
            var quarter = calibration.RotationDegrees is 90 or 270;
            var columns = quarter ? calibration.Rows : calibration.Columns;
            var rows = quarter ? calibration.Columns : calibration.Rows;
            if (columns > 66 || rows > 66) return true;
            var corners = new[] { new SymbolPoint(0, 0), new SymbolPoint(width, 0), new SymbolPoint(width, height), new SymbolPoint(0, height) };
            var grid = Sample(rgba, width, height, corners, columns, rows, token);
            if (grid != null && UltracodeDecoder.TryDecode(grid, out var decoded, token)) visitor(decoded!, Geometry(corners, decoded!.RotationDegrees));
            return true;
        }
        var pixelCount = checked(width * height);
        var hasChroma = false;
        for (var at = 0; at < pixelCount; at++) {
            if ((at & 4095) == 0 && DecodeBudget.ShouldAbort(token)) return true;
            Channels(rgba, at * 4, out var r, out var g, out var b);
            var minimum = Math.Min(r, Math.Min(g, b)); var maximum = Math.Max(r, Math.Max(g, b));
            if (maximum - minimum > 80) { hasChroma = true; break; }
        }
        if (!hasChroma) return true;
        var mask = new byte[pixelCount];
        for (var at = 0; at < mask.Length; at++) {
            if ((at & 4095) == 0 && DecodeBudget.ShouldAbort(token)) return true;
            Channels(rgba, at * 4, out var r, out var g, out var b);
            if (Math.Max(r, Math.Max(g, b)) < 100) mask[at] = 1;
        }
        var candidates = 0;
        var limitReached = false;
        BinaryImageComponents.Visit(mask, width, height, invert: false, token, (points, count, touchesEdge) => {
            if (count < 80 || DecodeBudget.ShouldAbort(token)) return false;
            var corners = FindCorners(points, count, width, token);
            if (corners is null) return false;
            var horizontal = (Distance(corners[0], corners[1]) + Distance(corners[3], corners[2])) / 2;
            var vertical = (Distance(corners[0], corners[3]) + Distance(corners[1], corners[2])) / 2;
            if (horizontal < 22 || vertical < 22 || horizontal > vertical * 4 || vertical > horizontal * 4) return false;
            if (++candidates > 128) { limitReached = true; return true; }
            for (var rows = 11; rows <= 66; rows++) for (var columns = 11; columns <= 66; columns++) {
                if (DecodeBudget.ShouldAbort(token)) return true;
                if (UltracodeLayout.FromDimensions(columns, rows) is null && UltracodeLayout.FromDimensions(rows, columns) is null) continue;
                var pitchX = horizontal / columns; var pitchY = vertical / rows;
                if (Math.Min(pitchX, pitchY) < 2.5 || Math.Abs(pitchX - pitchY) > Math.Max(pitchX, pitchY) * .18) continue;
                var grid = Sample(rgba, width, height, corners, columns, rows, token);
                if (grid is null || !UltracodeDecoder.TryDecode(grid, out var decoded, token)) continue;
                return visitor(decoded!, Geometry(corners, decoded!.RotationDegrees));
            }
            return false;
        });
        return !limitReached;
    }

    private static SymbolPoint[]? FindCorners(int[] points, int count, int width, CancellationToken token) {
        var corners = new SymbolPoint[4];
        var best = new[] { double.MaxValue, double.MinValue, double.MinValue, double.MaxValue };
        for (var i = 0; i < count; i++) {
            if ((i & 4095) == 0 && DecodeBudget.ShouldAbort(token)) return null;
            var x = points[i] % width + .5; var y = points[i] / width + .5;
            if (x + y < best[0]) { best[0] = x + y; corners[0] = new SymbolPoint(x, y); }
            if (x - y > best[1]) { best[1] = x - y; corners[1] = new SymbolPoint(x, y); }
            if (x + y > best[2]) { best[2] = x + y; corners[2] = new SymbolPoint(x, y); }
            if (x - y < best[3]) { best[3] = x - y; corners[3] = new SymbolPoint(x, y); }
        }
        var horizontal = Distance(corners[0], corners[1]); var vertical = Distance(corners[0], corners[3]);
        if (horizontal < 2 || vertical < 2) return null;
        var ux = (corners[1].X - corners[0].X) / horizontal; var uy = (corners[1].Y - corners[0].Y) / horizontal;
        var vx = (corners[3].X - corners[0].X) / vertical; var vy = (corners[3].Y - corners[0].Y) / vertical;
        for (var corner = 0; corner < 4; corner++) {
            var sx = corner is 1 or 2 ? .5 : -.5; var sy = corner >= 2 ? .5 : -.5;
            corners[corner] = new SymbolPoint(corners[corner].X + sx * ux + sy * vx, corners[corner].Y + sx * uy + sy * vy);
        }
        return corners;
    }

    private static double Distance(SymbolPoint first, SymbolPoint second) =>
        Math.Sqrt((first.X - second.X) * (first.X - second.X) + (first.Y - second.Y) * (first.Y - second.Y));

    private static SymbolGeometry Geometry(SymbolPoint[] corners, int rotation) {
        var first = rotation / 90;
        return new SymbolGeometry(corners[first], corners[(first + 1) % 4], corners[(first + 2) % 4], corners[(first + 3) % 4]);
    }
}
