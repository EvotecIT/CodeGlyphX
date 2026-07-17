using System;

namespace CodeGlyphX;

/// <summary>
/// Defines an integer region within an image.
/// </summary>
public readonly struct ImageRegion : IEquatable<ImageRegion> {
    /// <summary>Gets the horizontal origin.</summary>
    public int X { get; }
    /// <summary>Gets the vertical origin.</summary>
    public int Y { get; }
    /// <summary>Gets the region width.</summary>
    public int Width { get; }
    /// <summary>Gets the region height.</summary>
    public int Height { get; }
    /// <summary>Gets the exclusive right edge.</summary>
    public int Right => checked(X + Width);
    /// <summary>Gets the exclusive bottom edge.</summary>
    public int Bottom => checked(Y + Height);

    /// <summary>
    /// Creates an image region.
    /// </summary>
    public ImageRegion(int x, int y, int width, int height) {
        if (x < 0) throw new ArgumentOutOfRangeException(nameof(x));
        if (y < 0) throw new ArgumentOutOfRangeException(nameof(y));
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        _ = checked(x + width);
        _ = checked(y + height);
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    /// <summary>
    /// Returns the intersection with the supplied image dimensions, or <see langword="null"/> when there is no overlap.
    /// </summary>
    public ImageRegion? ClipTo(int imageWidth, int imageHeight) {
        if (imageWidth <= 0) throw new ArgumentOutOfRangeException(nameof(imageWidth));
        if (imageHeight <= 0) throw new ArgumentOutOfRangeException(nameof(imageHeight));
        var right = Math.Min(Right, imageWidth);
        var bottom = Math.Min(Bottom, imageHeight);
        if (X >= right || Y >= bottom) return null;
        return new ImageRegion(X, Y, right - X, bottom - Y);
    }

    /// <inheritdoc />
    public bool Equals(ImageRegion other) => X == other.X && Y == other.Y && Width == other.Width && Height == other.Height;
    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is ImageRegion other && Equals(other);
    /// <inheritdoc />
    public override int GetHashCode() {
        unchecked {
            var hash = X;
            hash = (hash * 397) ^ Y;
            hash = (hash * 397) ^ Width;
            return (hash * 397) ^ Height;
        }
    }
    /// <inheritdoc />
    public override string ToString() => $"{X},{Y} {Width}x{Height}";
    /// <summary>Compares two regions.</summary>
    public static bool operator ==(ImageRegion left, ImageRegion right) => left.Equals(right);
    /// <summary>Compares two regions.</summary>
    public static bool operator !=(ImageRegion left, ImageRegion right) => !left.Equals(right);
}
