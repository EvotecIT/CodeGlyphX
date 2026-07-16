using System;

namespace CodeGlyphX;

/// <summary>
/// Describes an axis-aligned symbol bounding box in image pixel coordinates.
/// </summary>
public readonly struct SymbolBounds : IEquatable<SymbolBounds> {
    /// <summary>Gets the horizontal origin.</summary>
    public double X { get; }
    /// <summary>Gets the vertical origin.</summary>
    public double Y { get; }
    /// <summary>Gets the width.</summary>
    public double Width { get; }
    /// <summary>Gets the height.</summary>
    public double Height { get; }

    /// <summary>Creates a bounding box.</summary>
    public SymbolBounds(double x, double y, double width, double height) {
        if (double.IsNaN(x) || double.IsInfinity(x)) throw new ArgumentOutOfRangeException(nameof(x));
        if (double.IsNaN(y) || double.IsInfinity(y)) throw new ArgumentOutOfRangeException(nameof(y));
        if (double.IsNaN(width) || double.IsInfinity(width) || width < 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (double.IsNaN(height) || double.IsInfinity(height) || height < 0) throw new ArgumentOutOfRangeException(nameof(height));
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    /// <inheritdoc />
    public bool Equals(SymbolBounds other) => X.Equals(other.X) && Y.Equals(other.Y) && Width.Equals(other.Width) && Height.Equals(other.Height);
    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is SymbolBounds other && Equals(other);
    /// <inheritdoc />
    public override int GetHashCode() {
        unchecked {
            var hash = X.GetHashCode();
            hash = (hash * 397) ^ Y.GetHashCode();
            hash = (hash * 397) ^ Width.GetHashCode();
            return (hash * 397) ^ Height.GetHashCode();
        }
    }
    /// <inheritdoc />
    public override string ToString() => $"{X:0.###},{Y:0.###} {Width:0.###}x{Height:0.###}";
    /// <summary>Compares two bounds.</summary>
    public static bool operator ==(SymbolBounds left, SymbolBounds right) => left.Equals(right);
    /// <summary>Compares two bounds.</summary>
    public static bool operator !=(SymbolBounds left, SymbolBounds right) => !left.Equals(right);
}
