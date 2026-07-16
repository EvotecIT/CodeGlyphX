using System;

namespace CodeGlyphX;

/// <summary>
/// Represents a point in image pixel coordinates.
/// </summary>
public readonly struct SymbolPoint : IEquatable<SymbolPoint> {
    /// <summary>Gets the horizontal coordinate.</summary>
    public double X { get; }
    /// <summary>Gets the vertical coordinate.</summary>
    public double Y { get; }

    /// <summary>Creates a point in image pixel coordinates.</summary>
    public SymbolPoint(double x, double y) {
        if (double.IsNaN(x) || double.IsInfinity(x)) throw new ArgumentOutOfRangeException(nameof(x));
        if (double.IsNaN(y) || double.IsInfinity(y)) throw new ArgumentOutOfRangeException(nameof(y));
        X = x;
        Y = y;
    }

    /// <inheritdoc />
    public bool Equals(SymbolPoint other) => X.Equals(other.X) && Y.Equals(other.Y);
    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is SymbolPoint other && Equals(other);
    /// <inheritdoc />
    public override int GetHashCode() {
        unchecked {
            return (X.GetHashCode() * 397) ^ Y.GetHashCode();
        }
    }
    /// <inheritdoc />
    public override string ToString() => $"{X:0.###},{Y:0.###}";
    /// <summary>Compares two points.</summary>
    public static bool operator ==(SymbolPoint left, SymbolPoint right) => left.Equals(right);
    /// <summary>Compares two points.</summary>
    public static bool operator !=(SymbolPoint left, SymbolPoint right) => !left.Equals(right);
}
