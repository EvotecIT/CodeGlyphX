// Adapted from OfficeIMO.Core, Copyright (c) 2022 Evotec.
// Licensed under the MIT license; see Licenses/OfficeIMO-MIT.txt.

using System;

namespace CodeGlyphX.Internal.Fonts;

/// <summary>
/// Dependency-free two-dimensional point used by shared drawing descriptors.
/// Coordinates are expressed in the caller's layout unit.
/// </summary>
internal struct ManagedPoint : IEquatable<ManagedPoint> {
    /// <summary>Horizontal coordinate.</summary>
    public double X { get; }

    /// <summary>Vertical coordinate.</summary>
    public double Y { get; }

    /// <summary>Creates a point.</summary>
    public ManagedPoint(double x, double y) {
        X = x;
        Y = y;
    }

    /// <inheritdoc />
    public bool Equals(ManagedPoint other) => X.Equals(other.X) && Y.Equals(other.Y);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is ManagedPoint other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() {
        unchecked {
            return (X.GetHashCode() * 397) ^ Y.GetHashCode();
        }
    }

    /// <summary>Compares two points for equality.</summary>
    public static bool operator ==(ManagedPoint left, ManagedPoint right) => left.Equals(right);

    /// <summary>Compares two points for inequality.</summary>
    public static bool operator !=(ManagedPoint left, ManagedPoint right) => !left.Equals(right);
}
