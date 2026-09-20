using System;

namespace CodeGlyphX.Rendering.Art;

/// <summary>How an image contributes color to QR data modules.</summary>
public enum QrImageCompositionStyle {
    /// <summary>Adjust every image pixel to its module polarity, with a small high-contrast center.</summary>
    ColorModules,
    /// <summary>Reveal more of the image around contrasting square module centers.</summary>
    ImageOverlay
}

/// <summary>How a source image fits the square QR area, excluding the quiet zone.</summary>
public enum QrImageFit {
    /// <summary>Fill the QR area, cropping equally on opposite sides.</summary>
    Cover,
    /// <summary>Show the whole image, centered on white.</summary>
    Contain
}

/// <summary>
/// Deterministic image composition settings. Functional modules remain solid black/white,
/// and the quiet zone remains white. These constraints do not guarantee camera readability.
/// </summary>
public sealed class QrImageCompositionOptions {
    /// <summary>Pixels per module (6..64); defaults to 12.</summary>
    public int ModuleSize { get; set; } = 12;

    /// <summary>White border in modules (4..16).</summary>
    public int QuietZone { get; set; } = 4;

    /// <summary>Image treatment; defaults to full-module color adaptation.</summary>
    public QrImageCompositionStyle Style { get; set; }

    /// <summary>Aspect-preserving fit; defaults to a centered crop.</summary>
    public QrImageFit Fit { get; set; }

    /// <summary>
    /// Image strength (0..1). Zero produces black/white modules; higher values retain more image color.
    /// In overlay mode this also relaxes contrast outside protected module centers.
    /// </summary>
    public double Strength { get; set; } = 0.75;

    /// <summary>
    /// Width of the protected square center relative to a data module (0.5..1).
    /// Used only by overlay mode; rounded up to a centered whole-pixel footprint.
    /// </summary>
    public double CenterSize { get; set; } = 0.6;

    internal void Validate() {
        if (ModuleSize is < 6 or > 64) throw new ArgumentOutOfRangeException(nameof(ModuleSize));
        if (QuietZone is < 4 or > 16) throw new ArgumentOutOfRangeException(nameof(QuietZone));
        if (Style != QrImageCompositionStyle.ColorModules && Style != QrImageCompositionStyle.ImageOverlay)
            throw new ArgumentOutOfRangeException(nameof(Style));
        if (Fit != QrImageFit.Cover && Fit != QrImageFit.Contain) throw new ArgumentOutOfRangeException(nameof(Fit));
        if (double.IsNaN(Strength) || Strength < 0 || Strength > 1) throw new ArgumentOutOfRangeException(nameof(Strength));
        if (double.IsNaN(CenterSize) || CenterSize < 0.5 || CenterSize > 1) throw new ArgumentOutOfRangeException(nameof(CenterSize));
    }
}
