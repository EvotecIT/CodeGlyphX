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

    internal void Validate() {
        if (PaddingModules is < 0 or > 64) throw new ArgumentOutOfRangeException(nameof(PaddingModules));
        ValidatePosition(PositionX, nameof(PositionX));
        ValidatePosition(PositionY, nameof(PositionY));
    }

    internal static void ValidatePosition(double value, string name) {
        if (double.IsNaN(value) || value < 0 || value > 1) throw new ArgumentOutOfRangeException(name);
    }
}
