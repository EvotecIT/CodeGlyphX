using System;

namespace CodeGlyphX.Printing;

internal readonly struct PrintTransform {
    internal readonly double A, B, C, D, X, Y;
    internal PrintTransform(BarcodePrintPlacement? placement) {
        var p = placement ?? new BarcodePrintPlacement();
        if (!BarcodeBounds.Finite(p.X) || !BarcodeBounds.Finite(p.Y) || Math.Abs(p.X) > 10000 || Math.Abs(p.Y) > 10000)
            throw new ArgumentOutOfRangeException(nameof(placement), "Print origins must be finite and within 10 metres.");
        if (!BarcodeBounds.Finite(p.Scale) || p.Scale <= 0 || p.Scale > 1000) throw new ArgumentOutOfRangeException(nameof(placement), "Print scale must be positive and at most 1000.");
        if (!BarcodeBounds.Finite(p.RotationDegrees)) throw new ArgumentOutOfRangeException(nameof(placement));
        var radians = p.RotationDegrees % 360 * Math.PI / 180;
        var cos = Math.Cos(radians); var sin = Math.Sin(radians);
        if (Math.Abs(cos) < 1e-12) cos = 0;
        if (Math.Abs(sin) < 1e-12) sin = 0;
        A = D = cos * p.Scale; B = sin * p.Scale; C = -B; X = p.X; Y = p.Y;
    }
    private PrintTransform(PrintTransform source, double x, double y) {
        A = source.A; B = source.B; C = source.C; D = source.D; X = source.X + x; Y = source.Y + y;
    }
    internal BarcodePoint Map(BarcodePoint p) => new(A * p.X + C * p.Y + X, B * p.X + D * p.Y + Y);
    internal BarcodePoint Inverse(double x, double y) {
        var determinant = A * D - B * C;
        return new BarcodePoint((D * (x - X) - C * (y - Y)) / determinant, (-B * (x - X) + A * (y - Y)) / determinant);
    }
    internal BarcodeBounds Map(BarcodeBounds bounds) {
        var a = Map(new BarcodePoint(bounds.X, bounds.Y));
        var b = Map(new BarcodePoint(bounds.Right, bounds.Y));
        var c = Map(new BarcodePoint(bounds.Right, bounds.Bottom));
        var d = Map(new BarcodePoint(bounds.X, bounds.Bottom));
        var left = Math.Min(Math.Min(a.X, b.X), Math.Min(c.X, d.X));
        var top = Math.Min(Math.Min(a.Y, b.Y), Math.Min(c.Y, d.Y));
        return new BarcodeBounds(left, top, Math.Max(Math.Max(a.X, b.X), Math.Max(c.X, d.X)) - left,
            Math.Max(Math.Max(a.Y, b.Y), Math.Max(c.Y, d.Y)) - top);
    }
    internal static PrintTransform Resolve(BarcodePrintLayout layout, BarcodePrintPlacement? placement,
        out double width, out double height, out bool clipped, bool rejectClipping = true) {
        var transform = new PrintTransform(placement);
        var bounds = transform.Map(layout.Bounds);
        clipped = false;
        if (placement?.CanvasWidthMm.HasValue == true || placement?.CanvasHeightMm.HasValue == true) {
            if (!placement.CanvasWidthMm.HasValue || !placement.CanvasHeightMm.HasValue)
                throw new ArgumentException("Both physical canvas dimensions are required.", nameof(placement));
            width = placement.CanvasWidthMm.Value; height = placement.CanvasHeightMm.Value;
            if (!BarcodeBounds.Finite(width) || width <= 0 || width > 10000 || !BarcodeBounds.Finite(height) || height <= 0 || height > 10000)
                throw new ArgumentOutOfRangeException(nameof(placement), "The print canvas must have positive dimensions of at most 10 metres.");
            clipped = bounds.X < -1e-7 || bounds.Y < -1e-7 || bounds.Right > width + 1e-7 || bounds.Bottom > height + 1e-7;
            if (clipped && !placement.AllowClipping && rejectClipping) throw new ArgumentException("The canvas clips barcode ink or required space. Enlarge it or explicitly permit clipping.", nameof(placement));
        } else {
            var left = Math.Min(0, bounds.X); var top = Math.Min(0, bounds.Y);
            width = Math.Max(0, bounds.Right) - left; height = Math.Max(0, bounds.Bottom) - top;
            transform = new PrintTransform(transform, -left, -top);
            if (width > 10000 || height > 10000) throw new ArgumentException("The transformed print canvas exceeds 10 metres.", nameof(placement));
        }
        return transform;
    }
}
