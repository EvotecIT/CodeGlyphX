using System;

namespace CodeGlyphX.Rendering.Art;

/// <summary>
/// A logo centred on a plate inside an image composition. The plate is placed with the same correction budget as
/// <see cref="QrArt.FitLogo"/>: it never covers a function pattern, and every codeword it touches is counted against each
/// block's correction capacity. When no placement fits, composition throws instead of omitting the logo.
/// </summary>
public sealed class QrImageLogoOptions {
    /// <summary>Logo image in any format <see cref="ImageReader"/> reads. Transparency is composed over the plate.</summary>
    public byte[]? Image { get; set; }

    /// <summary>Largest plate side in modules, including padding; zero uses the QR size. Defaults to 0.</summary>
    public int MaximumSideModules { get; set; }

    /// <summary>Plate padding on each side of the logo, in modules (0..4); defaults to 1.</summary>
    public int PaddingModules { get; set; } = 1;

    /// <summary>Unknown codeword errors reserved in each block for other damage (0..30); defaults to 3.</summary>
    public int ReservedErrorsPerBlock { get; set; } = 3;

    /// <summary>Smallest acceptable logo side, excluding padding (1..60); defaults to 3.</summary>
    public int MinimumLogoSideModules { get; set; } = 3;

    /// <summary>Opaque plate colour; null uses the quiet-zone colour (white, or the duotone paper).</summary>
    public Rgba32? Plate { get; set; }

    internal void Validate() {
        if (Image is null || Image.Length == 0) throw new ArgumentException("A logo image is required.", nameof(Image));
        if (MaximumSideModules < 0) throw new ArgumentOutOfRangeException(nameof(MaximumSideModules));
        if (PaddingModules is < 0 or > 4) throw new ArgumentOutOfRangeException(nameof(PaddingModules));
        if (ReservedErrorsPerBlock is < 0 or > 30) throw new ArgumentOutOfRangeException(nameof(ReservedErrorsPerBlock));
        if (MinimumLogoSideModules is < 1 or > 60) throw new ArgumentOutOfRangeException(nameof(MinimumLogoSideModules));
        if (Plate is { A: not 255 }) throw new ArgumentException("The logo plate must be opaque.", nameof(Plate));
    }
}
