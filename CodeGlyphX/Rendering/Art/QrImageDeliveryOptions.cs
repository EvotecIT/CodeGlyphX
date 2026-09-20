using System;

namespace CodeGlyphX.Rendering.Art;

/// <summary>Reproducible delivery simulations. They do not certify a physical camera or printer.</summary>
public sealed class QrImageDeliveryOptions {
    /// <summary>Long side of the screen-sized export (32..2048 pixels).</summary>
    public int ScreenSize { get; set; } = 320;
    /// <summary>JPEG quality for the compression probe (1..100).</summary>
    public int JpegQuality { get; set; } = 70;
    /// <summary>Horizontal inset of each top corner as a fraction of image width (0..0.2).</summary>
    public double PerspectiveInset { get; set; } = 0.1;
    /// <summary>Long side of the printed artwork in millimeters (5..200).</summary>
    public double PrintMillimeters { get; set; } = 40;
    /// <summary>Raster resolution for the print simulation (72..600 DPI).</summary>
    public int PrintDpi { get; set; } = 150;
    /// <summary>Cooperative recognition budget per check, in milliseconds (1..10000).</summary>
    public int DecodeBudgetMilliseconds { get; set; } = 1000;
    internal void Validate() {
        if (ScreenSize < 32 || ScreenSize > 2048) throw new ArgumentOutOfRangeException(nameof(ScreenSize));
        if (JpegQuality < 1 || JpegQuality > 100) throw new ArgumentOutOfRangeException(nameof(JpegQuality));
        if (double.IsNaN(PerspectiveInset) || PerspectiveInset < 0 || PerspectiveInset > 0.2) throw new ArgumentOutOfRangeException(nameof(PerspectiveInset));
        if (double.IsNaN(PrintMillimeters) || PrintMillimeters < 5 || PrintMillimeters > 200) throw new ArgumentOutOfRangeException(nameof(PrintMillimeters));
        if (PrintDpi < 72 || PrintDpi > 600 || PrintMillimeters / 25.4 * PrintDpi > 4096) throw new ArgumentOutOfRangeException(nameof(PrintDpi));
        if (DecodeBudgetMilliseconds < 1 || DecodeBudgetMilliseconds > 10000) throw new ArgumentOutOfRangeException(nameof(DecodeBudgetMilliseconds));
    }
}
