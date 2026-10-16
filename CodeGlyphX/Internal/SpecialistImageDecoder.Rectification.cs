using System;
using System.Collections.Generic;
using System.Threading;

namespace CodeGlyphX.Internal;

internal static partial class SpecialistImageDecoder {
    private static ImageContext? Rectify(ImageContext source, CancellationToken token) {
        var mask = source.Mask; var width = source.Width; var height = source.Height;
        var border = 0;
        for (var x = 0; x < width; x++) border += mask[x] + mask[(height - 1) * width + x];
        for (var y = 0; y < height; y++) border += mask[y * width] + mask[y * width + width - 1];
        var background = border > width + height ? (byte)1 : (byte)0;
        var points = new List<SymbolPoint>();
        var increment = Math.Max(1, height / 1024);
        for (var y = 0; y < height; y += increment) {
            if (DecodeBudget.ShouldAbort(token)) return null;
            var left = 0; while (left < width && mask[y * width + left] == background) left++;
            if (left == width) continue;
            var right = width - 1; while (right > left && mask[y * width + right] == background) right--;
            points.Add(new SymbolPoint(left, y)); points.Add(new SymbolPoint(right, y));
        }
        var hull = ConvexHull(points);
        if (hull.Count < 4) return null;
        var bestArea = double.MaxValue; var angle = 0d;
        for (var edge = 0; edge < hull.Count; edge++) {
            if (DecodeBudget.ShouldAbort(token)) return null;
            var first = hull[edge]; var second = hull[(edge + 1) % hull.Count];
            var candidate = Math.Atan2(second.Y - first.Y, second.X - first.X);
            while (candidate > Math.PI / 4) candidate -= Math.PI / 2;
            while (candidate < -Math.PI / 4) candidate += Math.PI / 2;
            var cos = Math.Cos(candidate); var sin = Math.Sin(candidate);
            var left = double.MaxValue; var right = double.MinValue; var top = double.MaxValue; var bottom = double.MinValue;
            foreach (var point in hull) {
                var x = point.X * cos + point.Y * sin; var y = -point.X * sin + point.Y * cos;
                left = Math.Min(left, x); right = Math.Max(right, x); top = Math.Min(top, y); bottom = Math.Max(bottom, y);
            }
            var area = (right - left + 1) * (bottom - top + 1);
            if (area < bestArea * .999 || area <= bestArea * 1.001 && Math.Abs(candidate) < Math.Abs(angle)) {
                bestArea = area; angle = candidate;
            }
        }
        if (Math.Abs(angle) < .008 || Math.Abs(angle) > Math.PI / 6) return null;
        var cosine = Math.Cos(angle); var sine = Math.Sin(angle);
        var targetWidth = (int)Math.Ceiling(width * Math.Abs(cosine) + height * Math.Abs(sine));
        var targetHeight = (int)Math.Ceiling(height * Math.Abs(cosine) + width * Math.Abs(sine));
        // Bound the optional fallback workspace independently of the source image's codec allowance.
        if ((long)targetWidth * targetHeight > 16000000) return null;
        var target = new byte[checked(targetWidth * targetHeight)];
        var result = new ImageContext(target, targetWidth, targetHeight) {
            Cos = cosine, Sin = sine, SourceCenterX = width / 2d, SourceCenterY = height / 2d,
            TargetCenterX = targetWidth / 2d, TargetCenterY = targetHeight / 2d
        };
        for (var y = 0; y < targetHeight; y++) {
            if (DecodeBudget.ShouldAbort(token)) return null;
            for (var x = 0; x < targetWidth; x++) {
                var point = MapRectified(result, new SymbolPoint(x + .5, y + .5));
                var sx = (int)Math.Floor(point.X); var sy = (int)Math.Floor(point.Y);
                target[y * targetWidth + x] = (uint)sx < (uint)width && (uint)sy < (uint)height
                    ? mask[sy * width + sx] : background;
            }
        }
        return result;
    }

    private static SymbolPoint MapRectified(ImageContext context, SymbolPoint point) {
        var x = point.X - context.TargetCenterX; var y = point.Y - context.TargetCenterY;
        return new SymbolPoint(context.SourceCenterX + context.Cos * x - context.Sin * y,
            context.SourceCenterY + context.Sin * x + context.Cos * y);
    }

    private static List<SymbolPoint> ConvexHull(List<SymbolPoint> points) {
        points.Sort((first, second) => {
            var x = first.X.CompareTo(second.X);
            return x == 0 ? first.Y.CompareTo(second.Y) : x;
        });
        var hull = new List<SymbolPoint>();
        foreach (var point in points) {
            while (hull.Count >= 2 && Cross(hull[hull.Count - 2], hull[hull.Count - 1], point) <= 0) hull.RemoveAt(hull.Count - 1);
            hull.Add(point);
        }
        var lowerCount = hull.Count;
        for (var i = points.Count - 2; i >= 0; i--) {
            var point = points[i];
            while (hull.Count > lowerCount && Cross(hull[hull.Count - 2], hull[hull.Count - 1], point) <= 0) hull.RemoveAt(hull.Count - 1);
            hull.Add(point);
        }
        if (hull.Count > 0) hull.RemoveAt(hull.Count - 1);
        return hull;
    }

    private static double Cross(SymbolPoint first, SymbolPoint second, SymbolPoint third) =>
        (second.X - first.X) * (third.Y - first.Y) - (second.Y - first.Y) * (third.X - first.X);
}
