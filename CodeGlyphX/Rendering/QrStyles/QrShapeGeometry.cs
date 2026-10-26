using System;

namespace CodeGlyphX.Rendering;

/// <summary>Continuous unit-cell geometry shared by styled raster and vector outputs.</summary>
internal static class QrShapeGeometry {
    internal static bool Connected(QrModuleShape shape) => shape is QrModuleShape.Liquid
        or QrModuleShape.ConnectedRounded or QrModuleShape.ConnectedSquircle
        or QrModuleShape.BarsVertical or QrModuleShape.BarsHorizontal or QrModuleShape.Classy;

    // Neighbour bits are N, E, S, W. Coordinates are relative to the cell, not pixel centres.
    internal static bool Contains(QrModuleShape shape, double x, double y, double scale = 1,
        double radius = 0, int neighbours = 0) {
        if (shape == QrModuleShape.Dot) scale *= 0.8;
        if (shape == QrModuleShape.DotGrid) scale *= 0.9;
        if (shape == QrModuleShape.BarsVertical) neighbours &= 5;
        if (shape == QrModuleShape.BarsHorizontal) neighbours &= 10;
        var inset = (1 - scale) / 2;
        var connected = Connected(shape);
        var left = connected && (neighbours & 8) != 0 ? 0 : inset;
        var right = connected && (neighbours & 2) != 0 ? 1 : 1 - inset;
        var top = connected && (neighbours & 1) != 0 ? 0 : inset;
        var bottom = connected && (neighbours & 4) != 0 ? 1 : 1 - inset;
        if (x < left || x > right || y < top || y > bottom) return false;
        var u = (x - left) / (right - left);
        var v = (y - top) / (bottom - top);
        var dx = Math.Abs(u - 0.5) * 2;
        var dy = Math.Abs(v - 0.5) * 2;
        switch (shape) {
            case QrModuleShape.Square: return true;
            case QrModuleShape.Circle:
            case QrModuleShape.Dot: return dx * dx + dy * dy <= 1;
            case QrModuleShape.Diamond: return dx + dy <= 1;
            case QrModuleShape.SoftDiamond: return Math.Pow(dx, 1.5) + Math.Pow(dy, 1.5) <= 1;
            case QrModuleShape.Squircle: return dx * dx * dx * dx + dy * dy * dy * dy <= 1;
            case QrModuleShape.Leaf:
                return Rounded(u, v, 0.5, true, false, true, false);
            case QrModuleShape.CutCorner: return dx + dy <= 1.65;
            case QrModuleShape.Classy: return Rounded(u, v, 0.5, (neighbours & 9) == 0,
                (neighbours & 3) == 0, (neighbours & 6) == 0, (neighbours & 12) == 0);
            case QrModuleShape.ExtraRounded: return Rounded(u, v, 0.5, true, true, true, true);
            case QrModuleShape.BarsVertical:
                return Rounded(u, v, 0.5, (neighbours & 1) == 0, (neighbours & 1) == 0,
                    (neighbours & 4) == 0, (neighbours & 4) == 0);
            case QrModuleShape.BarsHorizontal:
                return Rounded(u, v, 0.5, (neighbours & 8) == 0, (neighbours & 2) == 0,
                    (neighbours & 2) == 0, (neighbours & 8) == 0);
            case QrModuleShape.Plus: return dx <= 0.35 || dy <= 0.35;
            case QrModuleShape.Heart:
                var hx = (u - 0.5) * 2.3;
                var hy = (0.55 - v) * 2.3;
                var h = hx * hx + hy * hy - 0.65;
                return h * h * h - hx * hx * hy * hy * hy <= 0;
            case QrModuleShape.Star:
                var angle = Math.Atan2(v - 0.5, u - 0.5) + Math.PI / 2;
                return Math.Sqrt(dx * dx + dy * dy) <= 0.72 + 0.28 * Math.Cos(5 * angle);
            case QrModuleShape.DotGrid:
                var gx = Math.Min(Math.Abs(u - 0.27), Math.Abs(u - 0.73));
                var gy = Math.Min(Math.Abs(v - 0.27), Math.Abs(v - 0.73));
                return gx * gx + gy * gy <= 0.045;
            case QrModuleShape.Wave:
            case QrModuleShape.Blob:
                var a = Math.Atan2(v - 0.5, u - 0.5);
                var r = 0.9 + 0.1 * Math.Sin((shape == QrModuleShape.Wave ? 4 : 5) * a);
                return dx * dx + dy * dy <= r * r;
            default:
                var corner = radius > 0 ? Math.Min(0.5, radius / scale) : shape == QrModuleShape.Liquid ? 0.5 : 0.25;
                if (shape == QrModuleShape.ConnectedSquircle) corner = 0.35;
                return Rounded(u, v, corner, !connected || (neighbours & 9) == 0,
                    !connected || (neighbours & 3) == 0, !connected || (neighbours & 6) == 0,
                    !connected || (neighbours & 12) == 0);
        }
    }

    internal static bool ContainsEye(QrModuleShape shape, double x, double y, double radius = 0, int outwardCorner = -1) {
        // Mirrored leaf eyes have one outward tip. Preserve the legacy lens for callers
        // that did not request the outward-oriented eye treatment.
        if (shape == QrModuleShape.Leaf && outwardCorner < 0) return Contains(shape, x, y, radius: radius);
        if (outwardCorner == 1) x = 1 - x;
        if (outwardCorner == 2) y = 1 - y;
        if (shape == QrModuleShape.Leaf)
            return x >= 0 && x <= 1 && y >= 0 && y <= 1 && Rounded(x, y, .5, false, true, false, true)
                && Rounded(x, y, .4, false, false, true, false);
        if (shape == QrModuleShape.Classy)
            return x >= 0 && x <= 1 && y >= 0 && y <= 1 && Rounded(x, y, .25, false, true, true, true);
        return Contains(shape, x, y, radius: radius);
    }

    private static bool Rounded(double x, double y, double r, bool tl, bool tr, bool br, bool bl) {
        if (tl && x < r && y < r) return Distance(x - r, y - r) <= r * r;
        if (tr && x > 1 - r && y < r) return Distance(x - 1 + r, y - r) <= r * r;
        if (br && x > 1 - r && y > 1 - r) return Distance(x - 1 + r, y - 1 + r) <= r * r;
        if (bl && x < r && y > 1 - r) return Distance(x - r, y - 1 + r) <= r * r;
        return true;
    }

    private static double Distance(double x, double y) => x * x + y * y;
}
