using System;

namespace CodeGlyphX.Printing;

/// <summary>An axis-aligned extent in barcode layout or text-shape units.</summary>
public readonly struct BarcodeBounds {
    /// <summary>Gets the left edge.</summary>
    public double X { get; }
    /// <summary>Gets the top edge.</summary>
    public double Y { get; }
    /// <summary>Gets the width.</summary>
    public double Width { get; }
    /// <summary>Gets the height.</summary>
    public double Height { get; }
    /// <summary>Gets the right edge.</summary>
    public double Right => X + Width;
    /// <summary>Gets the bottom edge.</summary>
    public double Bottom => Y + Height;
    /// <summary>Gets whether the extent has no area.</summary>
    public bool IsEmpty => Width == 0 || Height == 0;
    /// <summary>Creates a finite extent with nonnegative dimensions.</summary>
    public BarcodeBounds(double x, double y, double width, double height) {
        if (!Finite(x) || !Finite(y) || !Finite(width) || !Finite(height) || !Finite(x + width) || !Finite(y + height))
            throw new ArgumentOutOfRangeException(nameof(width), "Barcode bounds must be finite.");
        if (width < 0 || height < 0) throw new ArgumentOutOfRangeException(nameof(width));
        X = x; Y = y; Width = width; Height = height;
    }
    /// <summary>Returns the smallest rectangle enclosing both nonempty extents.</summary>
    public BarcodeBounds Union(BarcodeBounds other) {
        if (IsEmpty) return other;
        if (other.IsEmpty) return this;
        var left = Math.Min(X, other.X); var top = Math.Min(Y, other.Y);
        return new BarcodeBounds(left, top, Math.Max(Right, other.Right) - left, Math.Max(Bottom, other.Bottom) - top);
    }
    /// <summary>Moves the extent without changing its size.</summary>
    public BarcodeBounds Translate(double x, double y) => new(X + x, Y + y, Width, Height);
    internal static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
}
