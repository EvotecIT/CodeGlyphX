using System;

namespace CodeGlyphX;

/// <summary>
/// Describes a detected symbol quadrilateral and its orientation.
/// </summary>
public sealed class SymbolGeometry {
    /// <summary>Gets the top-left corner.</summary>
    public SymbolPoint TopLeft { get; }
    /// <summary>Gets the top-right corner.</summary>
    public SymbolPoint TopRight { get; }
    /// <summary>Gets the bottom-right corner.</summary>
    public SymbolPoint BottomRight { get; }
    /// <summary>Gets the bottom-left corner.</summary>
    public SymbolPoint BottomLeft { get; }
    /// <summary>Gets the axis-aligned bounds enclosing all four corners.</summary>
    public SymbolBounds Bounds { get; }
    /// <summary>Gets the clockwise rotation in degrees, normalized to the range [0, 360).</summary>
    public double RotationDegrees { get; }

    /// <summary>
    /// Creates symbol geometry from four corners in reading order.
    /// </summary>
    public SymbolGeometry(SymbolPoint topLeft, SymbolPoint topRight, SymbolPoint bottomRight, SymbolPoint bottomLeft) {
        TopLeft = topLeft;
        TopRight = topRight;
        BottomRight = bottomRight;
        BottomLeft = bottomLeft;

        var minX = Math.Min(Math.Min(topLeft.X, topRight.X), Math.Min(bottomRight.X, bottomLeft.X));
        var minY = Math.Min(Math.Min(topLeft.Y, topRight.Y), Math.Min(bottomRight.Y, bottomLeft.Y));
        var maxX = Math.Max(Math.Max(topLeft.X, topRight.X), Math.Max(bottomRight.X, bottomLeft.X));
        var maxY = Math.Max(Math.Max(topLeft.Y, topRight.Y), Math.Max(bottomRight.Y, bottomLeft.Y));
        Bounds = new SymbolBounds(minX, minY, maxX - minX, maxY - minY);

        var angle = Math.Atan2(topRight.Y - topLeft.Y, topRight.X - topLeft.X) * 180d / Math.PI;
        RotationDegrees = angle < 0 ? angle + 360d : angle;
    }

    /// <summary>
    /// Creates axis-aligned symbol geometry from a bounding box.
    /// </summary>
    public static SymbolGeometry FromBounds(SymbolBounds bounds) {
        return new SymbolGeometry(
            new SymbolPoint(bounds.X, bounds.Y),
            new SymbolPoint(bounds.X + bounds.Width, bounds.Y),
            new SymbolPoint(bounds.X + bounds.Width, bounds.Y + bounds.Height),
            new SymbolPoint(bounds.X, bounds.Y + bounds.Height));
    }
}
