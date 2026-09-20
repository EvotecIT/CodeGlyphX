using System;

namespace CodeGlyphX.Rendering.Art;

/// <summary>Protects a manually selected image region without recognizing or generating subjects.</summary>
public sealed class QrImageSubjectOptions {
    /// <summary>Focal point in normalized source-image coordinates.</summary>
    public double X { get; set; } = 0.5;
    /// <summary>Focal point in normalized source-image coordinates.</summary>
    public double Y { get; set; } = 0.5;
    /// <summary>Radius relative to the shorter source-image side (0.01..1).</summary>
    public double Radius { get; set; } = 0.25;
    /// <summary>Protection strength (0..1). Scan anchors and functional patterns remain protected.</summary>
    public double Strength { get; set; } = 1;
    /// <summary>Optional source-aligned grayscale mask. Replaces the circular focal region when supplied.</summary>
    public QrImageProtectionMask? Mask { get; set; }

    internal void Validate() {
        QrImageCanvasOptions.ValidatePosition(X, nameof(X));
        QrImageCanvasOptions.ValidatePosition(Y, nameof(Y));
        QrImageCanvasOptions.ValidatePosition(Strength, nameof(Strength));
        if (double.IsNaN(Radius) || Radius < 0.01 || Radius > 1) throw new ArgumentOutOfRangeException(nameof(Radius));
    }

    internal double Sample(double x, double y, int width, int height) {
        if (x < 0 || y < 0 || x > 1 || y > 1) return 0;
        if (Mask is not null) return Strength * Mask.Sample(x, y);
        var radius = Radius * Math.Min(width, height);
        var dx = (x - X) * width / radius;
        var dy = (y - Y) * height / radius;
        var distance = Math.Sqrt(dx * dx + dy * dy);
        var t = Math.Max(0, Math.Min(1, (1 - distance) / 0.3));
        return Strength * t * t * (3 - 2 * t);
    }
}

/// <summary>Immutable grayscale protection map, stretched to the source image: white protects, black leaves decoration unchanged.</summary>
public sealed class QrImageProtectionMask {
    private readonly byte[] _values;
    /// <summary>Mask width in pixels.</summary>
    public int Width { get; }
    /// <summary>Mask height in pixels.</summary>
    public int Height { get; }
    /// <summary>Copies a tightly packed one-byte-per-pixel grayscale mask.</summary>
    public QrImageProtectionMask(byte[] values, int width, int height) {
        if (values is null) throw new ArgumentNullException(nameof(values));
        if (width <= 0 || height <= 0 || (long)width * height != values.Length) throw new ArgumentException("Expected a tightly packed grayscale mask.", nameof(values));
        Width = width;
        Height = height;
        _values = (byte[])values.Clone();
    }
    internal double Sample(double x, double y) {
        x = Math.Max(0, Math.Min(Width - 1, x * Width - 0.5));
        y = Math.Max(0, Math.Min(Height - 1, y * Height - 0.5));
        var ix = (int)x;
        var iy = (int)y;
        var nx = Math.Min(Width - 1, ix + 1);
        var ny = Math.Min(Height - 1, iy + 1);
        var fx = x - ix;
        var fy = y - iy;
        return ((_values[iy * Width + ix] * (1 - fx) + _values[iy * Width + nx] * fx) * (1 - fy)
            + (_values[ny * Width + ix] * (1 - fx) + _values[ny * Width + nx] * fx) * fy) / 255;
    }
}
