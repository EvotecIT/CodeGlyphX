using System;

namespace CodeGlyphX.Rendering.Art;

/// <summary>Extends the same artwork around the QR while retaining its uninterrupted light quiet zone.</summary>
public sealed class QrImageCanvasOptions {
    /// <summary>Half the extra canvas width/height, in modules (0..64). Position distributes this space around the QR.</summary>
    public int PaddingModules { get; set; } = 8;

    /// <summary>Horizontal QR placement in the available canvas space (0 = left, 1 = right).</summary>
    public double PositionX { get; set; } = 0.5;

    /// <summary>Vertical QR placement in the available canvas space (0 = top, 1 = bottom).</summary>
    public double PositionY { get; set; } = 0.5;

    /// <summary>
    /// Artwork strength outside the quiet zone (0..1). Lower values wash the surrounding art toward the functional
    /// background (white without art options), giving the code a calm field. One keeps the artwork unchanged.
    /// </summary>
    public double ArtOpacity { get; set; } = 1;

    /// <summary>
    /// Outer corner radius of the quiet zone, in modules (0..3). Rounded corners let the surrounding art meet the light
    /// field softly; at most three modules keeps a clear quiet zone along every side of the code.
    /// </summary>
    public double CornerRadiusModules { get; set; }

    internal void Validate() {
        if (double.IsNaN(CornerRadiusModules) || CornerRadiusModules < 0 || CornerRadiusModules > 3) throw new ArgumentOutOfRangeException(nameof(CornerRadiusModules));
        if (PaddingModules is < 0 or > 64) throw new ArgumentOutOfRangeException(nameof(PaddingModules));
        if (double.IsNaN(ArtOpacity) || ArtOpacity < 0 || ArtOpacity > 1) throw new ArgumentOutOfRangeException(nameof(ArtOpacity));
        ValidatePosition(PositionX, nameof(PositionX));
        ValidatePosition(PositionY, nameof(PositionY));
    }

    internal static void ValidatePosition(double value, string name) {
        if (double.IsNaN(value) || value < 0 || value > 1) throw new ArgumentOutOfRangeException(name);
    }
}
