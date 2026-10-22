using System;
using System.Collections.Generic;
using System.Threading;
using CodeGlyphX.Qr;

namespace CodeGlyphX.Internal;

internal static partial class SpecialistImageDecoder {
    // The long outer bars establish the vertical vanishing direction. The top and
    // bottom printed edges establish the horizontal one; their widths may differ.
    // This preserves the real alignment between a narrow component and its carrier.
    private static bool TryCompositePerspective(ImageContext source, CancellationToken token,
        Gs1CompositeCarrier? carrier, out DetectedSymbol symbol, out SymbolGeometry geometry,
        out bool inverted, out bool mirrored) {
        symbol = null!; geometry = null!; inverted = mirrored = false;
        if ((long)source.Width * source.Height > 16000000) { source.ResourceLimitReached = true; return false; }
        var border = source.Mask[0] + source.Mask[source.Width - 1]
            + source.Mask[(source.Height - 1) * source.Width] + source.Mask[source.Mask.Length - 1];
        for (var quarter = 0; quarter < 2; quarter++) {
            if (DecodeBudget.ShouldAbort(token)) return false;
            var view = new MaskView(source.Mask, source.Width, source.Height, quarter, border > 2);
            if (!CompositePerspectiveCorners(view, token, out var corners)) continue;
            var targetWidth = (Distance(corners[0], corners[1]) + Distance(corners[3], corners[2])) / 2;
            var targetHeight = (Distance(corners[0], corners[3]) + Distance(corners[1], corners[2])) / 2;
            if (targetWidth < 50 || targetHeight < 6) continue;
            var forward = QrPerspectiveTransform.QuadrilateralToQuadrilateral(
                corners[0].X, corners[0].Y, corners[1].X, corners[1].Y,
                corners[2].X, corners[2].Y, corners[3].X, corners[3].Y,
                0, 0, targetWidth, 0, targetWidth, targetHeight, 0, targetHeight);
            var left = double.MaxValue; var top = double.MaxValue;
            var right = double.MinValue; var bottom = double.MinValue;
            var valid = true;
            foreach (var point in new[] { new SymbolPoint(0, 0), new SymbolPoint(view.Width, 0),
                new SymbolPoint(view.Width, view.Height), new SymbolPoint(0, view.Height) }) {
                forward.Transform(point.X, point.Y, out var x, out var y);
                if (double.IsNaN(x) || double.IsInfinity(x) || double.IsNaN(y) || double.IsInfinity(y)
                    || Math.Abs(x) > view.Width * 4d || Math.Abs(y) > view.Height * 4d) { valid = false; break; }
                left = Math.Min(left, x); top = Math.Min(top, y);
                right = Math.Max(right, x); bottom = Math.Max(bottom, y);
            }
            if (!valid) continue;
            left = Math.Floor(left) - 2; top = Math.Floor(top) - 2;
            var width = (int)Math.Ceiling(right - left + 2);
            var height = (int)Math.Ceiling(bottom - top + 2);
            if (width < 1 || height < 1) continue;
            if ((long)width * height > 16000000) { source.ResourceLimitReached = true; continue; }
            var inverse = QrPerspectiveTransform.QuadrilateralToQuadrilateral(
                0, 0, targetWidth, 0, targetWidth, targetHeight, 0, targetHeight,
                corners[0].X, corners[0].Y, corners[1].X, corners[1].Y,
                corners[2].X, corners[2].Y, corners[3].X, corners[3].Y);
            var mask = new byte[width * height];
            for (var y = 0; y < height; y++) {
                if (DecodeBudget.ShouldAbort(token)) return false;
                for (var x = 0; x < width; x++) {
                    if ((x & 4095) == 0 && DecodeBudget.ShouldAbort(token)) return false;
                    inverse.Transform(x + left + .5, y + top + .5, out var sx, out var sy);
                    if (sx >= 0 && sy >= 0 && sx < view.Width && sy < view.Height && view.Get((int)sx, (int)sy))
                        mask[y * width + x] = 1;
                }
            }
            var rectified = new ImageContext(mask, width, height);
            SymbolPoint Map(SymbolPoint point) {
                inverse.Transform(point.X + left, point.Y + top, out var x, out var y);
                return MapRectified(source, view.Map(x, y));
            }
            if (!TryDecodeAligned(rectified, SymbolFormat.Gs1Composite, null, token,
                out symbol, out geometry, out inverted, out mirrored, carrier)) {
                if (source.CompositeFallback is null && rectified.CompositeFallback is { } partial)
                    source.CompositeFallback = MapCompositeCandidate(partial, Map, invert: border > 2);
                continue;
            }
            geometry = new SymbolGeometry(Map(geometry.TopLeft), Map(geometry.TopRight), Map(geometry.BottomRight), Map(geometry.BottomLeft));
            if (symbol.Metadata is Gs1CompositeSymbolMetadata metadata)
                symbol = symbol.WithMetadata(metadata.MapImageGeometry(Map));
            inverted ^= border > 2;
            return true;
        }
        return false;
    }

    private static double Distance(SymbolPoint first, SymbolPoint second) {
        var x = first.X - second.X; var y = first.Y - second.Y;
        return Math.Sqrt(x * x + y * y);
    }

    private static bool CompositePerspectiveCorners(MaskView view, CancellationToken token, out SymbolPoint[] corners) {
        corners = Array.Empty<SymbolPoint>();
        var leftPoints = new List<SymbolPoint>();
        var rightPoints = new List<SymbolPoint>();
        var hullPoints = new List<SymbolPoint>();
        var step = Math.Max(1, view.Height / 1024);
        for (var y = 0; y < view.Height; y += step) {
            if (DecodeBudget.ShouldAbort(token)) return false;
            var left = 0; var right = view.Width - 1;
            while (left <= right && !view.Get(left, y)) {
                if ((left & 4095) == 0 && DecodeBudget.ShouldAbort(token)) return false;
                left++;
            }
            if (left > right) continue;
            while (!view.Get(right, y)) {
                if ((right & 4095) == 0 && DecodeBudget.ShouldAbort(token)) return false;
                right--;
            }
            leftPoints.Add(new SymbolPoint(left, y + .5));
            rightPoints.Add(new SymbolPoint(right + 1, y + .5));
            hullPoints.Add(new SymbolPoint(left, y + .5));
            hullPoints.Add(new SymbolPoint(right + 1, y + .5));
        }
        if (leftPoints.Count < 12) return false;
        var firstY = leftPoints[0].Y; var lastY = leftPoints[leftPoints.Count - 1].Y;
        // Stay clear of the sloped top and bottom edges of a rotated bar rectangle.
        var low = firstY + (lastY - firstY) * .45; var high = firstY + (lastY - firstY) * .70;
        if (!FitCompositeSide(leftPoints, low, high, out var leftSlope, out var leftOffset)
            || !FitCompositeSide(rightPoints, low, high, out var rightSlope, out var rightOffset)) return false;
        var hull = ConvexHull(hullPoints);
        var topSpan = 0d; var bottomSpan = 0d;
        var topSlope = 0d; var topOffset = 0d; var bottomSlope = 0d; var bottomOffset = 0d;
        for (var i = 0; i < hull.Count; i++) {
            var a = hull[i]; var b = hull[(i + 1) % hull.Count];
            var dx = b.X - a.X; var dy = b.Y - a.Y;
            if (Math.Abs(dx) < 20 || Math.Abs(dy / dx) > .35) continue;
            var slope = dy / dx; var offset = a.Y - slope * a.X;
            if ((a.Y + b.Y) / 2 < (firstY + lastY) / 2 && Math.Abs(dx) > topSpan) {
                topSpan = Math.Abs(dx); topSlope = slope; topOffset = offset - .5;
            } else if ((a.Y + b.Y) / 2 >= (firstY + lastY) / 2 && Math.Abs(dx) > bottomSpan) {
                bottomSpan = Math.Abs(dx); bottomSlope = slope; bottomOffset = offset + .5;
            }
        }
        if (topSpan == 0 || bottomSpan == 0) return false;
        SymbolPoint Intersect(double sideSlope, double sideOffset, double slope, double offset) {
            var x = (sideSlope * offset + sideOffset) / (1 - sideSlope * slope);
            return new SymbolPoint(x, slope * x + offset);
        }
        corners = new[] { Intersect(leftSlope, leftOffset, topSlope, topOffset),
            Intersect(rightSlope, rightOffset, topSlope, topOffset),
            Intersect(rightSlope, rightOffset, bottomSlope, bottomOffset),
            Intersect(leftSlope, leftOffset, bottomSlope, bottomOffset) };
        return corners[1].X > corners[0].X + 30 && corners[2].X > corners[3].X + 30
            && corners[3].Y > corners[0].Y + 4 && corners[2].Y > corners[1].Y + 4;
    }

    private static bool FitCompositeSide(List<SymbolPoint> points, double low, double high,
        out double slope, out double offset) {
        var count = 0; var sumX = 0d; var sumY = 0d; var sumYY = 0d; var sumXY = 0d;
        foreach (var point in points) {
            if (point.Y < low || point.Y > high) continue;
            count++; sumX += point.X; sumY += point.Y; sumYY += point.Y * point.Y; sumXY += point.X * point.Y;
        }
        slope = offset = 0;
        var denominator = count * sumYY - sumY * sumY;
        if (count < 8 || denominator <= 0) return false;
        slope = (count * sumXY - sumX * sumY) / denominator;
        offset = (sumX - slope * sumY) / count;
        if (Math.Abs(slope) > .35) return false;
        var errors = 0d;
        foreach (var point in points) {
            if (point.Y < low || point.Y > high) continue;
            var error = point.X - slope * point.Y - offset;
            errors += error * error;
        }
        return errors / count < 1;
    }
}
