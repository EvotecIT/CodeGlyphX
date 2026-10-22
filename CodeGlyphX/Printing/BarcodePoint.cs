using System;

namespace CodeGlyphX.Printing;

/// <summary>A point in the coordinate units of a barcode layout or text shape.</summary>
public readonly struct BarcodePoint {
    /// <summary>Gets the horizontal coordinate.</summary>
    public double X { get; }
    /// <summary>Gets the vertical coordinate.</summary>
    public double Y { get; }
    /// <summary>Creates a finite point.</summary>
    public BarcodePoint(double x, double y) {
        if (double.IsNaN(x) || double.IsInfinity(x)) throw new ArgumentOutOfRangeException(nameof(x));
        if (double.IsNaN(y) || double.IsInfinity(y)) throw new ArgumentOutOfRangeException(nameof(y));
        X = x; Y = y;
    }
}
