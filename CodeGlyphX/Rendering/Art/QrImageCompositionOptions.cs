using System;

namespace CodeGlyphX.Rendering.Art;

/// <summary>How an image contributes color to QR data modules.</summary>
public enum QrImageCompositionStyle {
    /// <summary>Adjust every image pixel to its module polarity, with a small high-contrast center.</summary>
    ColorModules,
    /// <summary>Reveal more of the image around contrasting square module centers.</summary>
    ImageOverlay,
    /// <summary>Dither the supplied image into 3 by 3 sub-cells per module, forcing the centre sub-cell to the encoded bit.</summary>
    Halftone3x3,
    /// <summary>Keep the source image behind opaque, contrasting data-module centres.</summary>
    ImageBackground
}

/// <summary>How a source image fits its square composition area.</summary>
public enum QrImageFit {
    /// <summary>Fill the area, cropping according to image alignment.</summary>
    Cover,
    /// <summary>Fit the whole image on white before optional zoom and alignment.</summary>
    Contain
}

/// <summary>
/// Deterministic image composition settings. Functional modules retain solid, high-contrast geometry,
/// and the quiet zone remains uniformly light (white by default). These constraints do not guarantee camera readability.
/// </summary>
public sealed class QrImageCompositionOptions {
    /// <summary>Pixels per module (6..64); defaults to 12. Halftone3x3 requires a multiple of three.</summary>
    public int ModuleSize { get; set; } = 12;

    /// <summary>Uniform light border in modules (4..16), white by default.</summary>
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
    /// Used by overlay and image-background modes when Art is null; rounded up to a centered whole-pixel footprint.
    /// </summary>
    public double CenterSize { get; set; } = 0.6;

    /// <summary>Horizontal image alignment in its crop/fit area (0 = left, 1 = right).</summary>
    public double ImagePositionX { get; set; } = 0.5;

    /// <summary>Vertical image alignment in its crop/fit area (0 = top, 1 = bottom).</summary>
    public double ImagePositionY { get; set; } = 0.5;

    /// <summary>Magnification after aspect-preserving fit (1..4). Position controls the resulting crop.</summary>
    public double ImageZoom { get; set; } = 1;

    /// <summary>Optional image-aware silhouettes, replacing the square treatment selected by Style.</summary>
    public QrImageArtOptions? Art { get; set; }

    /// <summary>Optional canvas. The image is fitted across the entire canvas for continuity around the QR.</summary>
    public QrImageCanvasOptions? Canvas { get; set; }

    internal QrImageCompositionOptions WithModuleSize(int moduleSize) => new QrImageCompositionOptions {
        ModuleSize = moduleSize, QuietZone = QuietZone, Style = Style, Fit = Fit, Strength = Strength,
        CenterSize = CenterSize, ImagePositionX = ImagePositionX, ImagePositionY = ImagePositionY,
        ImageZoom = ImageZoom, Art = Art, Canvas = Canvas
    };

    internal void Validate() {
        Art?.Validate();
        Canvas?.Validate();
        QrImageCanvasOptions.ValidatePosition(ImagePositionX, nameof(ImagePositionX));
        QrImageCanvasOptions.ValidatePosition(ImagePositionY, nameof(ImagePositionY));
        if (double.IsNaN(ImageZoom) || ImageZoom < 1 || ImageZoom > 4) throw new ArgumentOutOfRangeException(nameof(ImageZoom));
        if (ModuleSize is < 6 or > 64) throw new ArgumentOutOfRangeException(nameof(ModuleSize));
        if (Style == QrImageCompositionStyle.Halftone3x3 && ModuleSize % 3 != 0)
            throw new ArgumentException("Halftone3x3 needs a module size divisible by three for equal sub-cells.", nameof(ModuleSize));
        if (QuietZone is < 4 or > 16) throw new ArgumentOutOfRangeException(nameof(QuietZone));
        if (Style is < QrImageCompositionStyle.ColorModules or > QrImageCompositionStyle.ImageBackground)
            throw new ArgumentOutOfRangeException(nameof(Style));
        if ((Style == QrImageCompositionStyle.Halftone3x3 || Style == QrImageCompositionStyle.ImageBackground) && Art is not null)
            throw new ArgumentException("Image sub-cell and background treatments cannot be combined with module silhouettes.", nameof(Art));
        if (Fit != QrImageFit.Cover && Fit != QrImageFit.Contain) throw new ArgumentOutOfRangeException(nameof(Fit));
        if (double.IsNaN(Strength) || Strength < 0 || Strength > 1) throw new ArgumentOutOfRangeException(nameof(Strength));
        if (double.IsNaN(CenterSize) || CenterSize < 0.5 || CenterSize > 1) throw new ArgumentOutOfRangeException(nameof(CenterSize));
    }
}
