using System;

namespace CodeGlyphX.Rendering.Art;

/// <summary>How an image contributes color to QR data modules.</summary>
public enum QrImageCompositionStyle {
    /// <summary>
    /// Tint every module from the image: dark modules become a darkened image colour and light modules a lightened one,
    /// keeping the image's own hue and tonal variation. Finders and alignment patterns are rounded, image-tinted rings.
    /// </summary>
    ColorModules,
    /// <summary>
    /// Photo first: the image stays full-bleed and unaltered across each module, and only a small square centre mark
    /// (sized by CenterSize; 0.45 module by default) carries the dark or light value. Where the image opposes a mark, it is
    /// nudged slightly towards it around the mark. Dark photos leave the least scan margin of all photo treatments;
    /// QrArt.Compose refuses output that does not decode. Finders and alignment patterns are rounded, image-tinted rings; <see cref="QrImageCompositionOptions.SolidFunctionPatterns"/> restores solid black and white function patterns.
    /// </summary>
    ImageOverlay,
    /// <summary>
    /// Dither the supplied image into 3 by 3 sub-cells per module, forcing the centre sub-cell to the encoded bit.
    /// Black and white by default; <see cref="QrImageCompositionOptions.Duotone"/> selects a two-colour ink and paper.
    /// Works best with high-contrast subjects such as portraits, silhouettes, logos and bold shapes. Low-contrast or
    /// flat-colour scenes, such as soft landscapes, may not stay recognisable within the scan limits; use ColorModules
    /// or ImageBackground for those.
    /// </summary>
    Halftone3x3,
    /// <summary>
    /// Keep the image across the whole symbol, including the finder, timing and alignment zones, with crisp antialiased
    /// module dots tinted from the image's luminance and no halo around them. Finder and alignment patterns are drawn as rounded, image-tinted rings on top.
    /// </summary>
    ImageBackground,
    /// <summary>
    /// The picture forms the code. Each module is split into sub-cells (<see cref="QrImageHalftoneOptions.SubCells"/>); a
    /// solid centre carries the module colour at decode-safe contrast, and the surrounding sub-cells reproduce the photo as
    /// halftone dots, error diffusion or an ordered screen, in the photo's own colours or in two colours when
    /// <see cref="QrImageCompositionOptions.Duotone"/> is set. The photo's tones are stretched and kept inside a band that
    /// leaves every centre clear contrast, and <see cref="QrImageCompositionOptions.Strength"/> sets how far the surrounding
    /// sub-cells follow the photo rather than the module colour. Finders and alignment patterns are solid rings in the
    /// picture's own deep and light tones. Tune it with <see cref="QrImageCompositionOptions.Halftone"/>. Deliver at about
    /// 4.5 px per module or more; below that, blurred captures may not read.
    /// </summary>
    PhotoHalftone
}

/// <summary>How a source image fits its square composition area.</summary>
public enum QrImageFit {
    /// <summary>Fill the area, cropping according to image alignment.</summary>
    Cover,
    /// <summary>Fit the whole image on white before optional zoom and alignment.</summary>
    Contain
}

/// <summary>
/// Deterministic image composition settings. Function patterns keep their geometry and polarity,
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
    /// ImageOverlay keeps the image unaltered outside its centre marks at every strength.
    /// Halftone3x3 caps the image contribution at 0.7 so each module's average tone keeps following its bit.
    /// PhotoHalftone moves each sub-cell around a module centre towards the module colour by half of (1 - Strength), so 1
    /// shows the photo unchanged around the solid centres and lower values trade picture for scan margin.
    /// </summary>
    public double Strength { get; set; } = 0.75;

    /// <summary>
    /// Size of the contrasting module mark relative to a module (0.5..1), used when Art is null. ImageBackground draws a
    /// round dot covering about the same area as a square of this width; ImageOverlay draws a square centre mark of
    /// 0.75 times this width, between 0.3 and 0.45 module. Smaller marks show more image but leave less scan margin.
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

    /// <summary>
    /// Smallest QR version that <see cref="QrArt.Compose(string, byte[], QrImageCompositionOptions?, ImageDecodeOptions?)"/>
    /// encodes (1..40); defaults to 6. Short payloads otherwise produce so few modules that every photo treatment reads as
    /// coarse blocks. Longer payloads still use the larger version they need. <see cref="QrImageComposer"/> and
    /// <see cref="QrArt.SearchImage"/> keep the versions they are given or explore.
    /// </summary>
    public int MinimumVersion { get; set; } = 6;

    /// <summary>
    /// When true (default), <see cref="QrArt.Compose(string, byte[], QrImageCompositionOptions?, ImageDecodeOptions?)"/>
    /// decodes the composed pixels with the native decoder and throws <see cref="InvalidOperationException"/> when the
    /// payload is not recovered. This does not certify resized, compressed or printed output.
    /// </summary>
    public bool VerifyDecode { get; set; } = true;

    /// <summary>
    /// When true, ColorModules, ImageOverlay and ImageBackground draw every function pattern (finders, separators, timing,
    /// alignment, format and version modules) as solid black and white squares, as earlier releases did. This is the
    /// highest-contrast option but cuts the image out around the finders. Defaults to false: finders and alignment
    /// patterns are rounded, image-tinted rings, the other function modules share the data treatment, and the image
    /// continues underneath. Ignored for Halftone3x3 and when Art is set.
    /// </summary>
    public bool SolidFunctionPatterns { get; set; }

    /// <summary>
    /// Optional two-colour ink and paper for <see cref="QrImageCompositionStyle.Halftone3x3"/> and
    /// <see cref="QrImageCompositionStyle.PhotoHalftone"/>. Halftone3x3 is black and white without it; PhotoHalftone uses
    /// the photo's own colours without it. For a black and white PhotoHalftone, set black ink on white paper.
    /// </summary>
    public QrImageDuotoneOptions? Duotone { get; set; }

    /// <summary>Screen, sub-cell, centre, tone and finder settings for <see cref="QrImageCompositionStyle.PhotoHalftone"/>; null uses the defaults.</summary>
    public QrImageHalftoneOptions? Halftone { get; set; }

    /// <summary>Optional logo on a plate at the centre of the symbol, placed within the error-correction budget.</summary>
    public QrImageLogoOptions? Logo { get; set; }

    /// <summary>
    /// When positive (16..8192), the finished composition is area-resampled so the QR, including its quiet zone, is this
    /// many pixels wide; a canvas scales with it. The image is first composed at <see cref="ModuleSize"/>, so choose a module
    /// size that gives fine screens enough pixels. Defaults to 0, which keeps the composed size.
    /// </summary>
    public int TargetSizePx { get; set; }

    internal QrImageCompositionOptions WithModuleSize(int moduleSize) => new QrImageCompositionOptions {
        ModuleSize = moduleSize, QuietZone = QuietZone, Style = Style, Fit = Fit, Strength = Strength,
        CenterSize = CenterSize, ImagePositionX = ImagePositionX, ImagePositionY = ImagePositionY,
        ImageZoom = ImageZoom, Art = Art, Canvas = Canvas, MinimumVersion = MinimumVersion,
        VerifyDecode = VerifyDecode, Duotone = Duotone, SolidFunctionPatterns = SolidFunctionPatterns,
        Halftone = Halftone, Logo = Logo, TargetSizePx = TargetSizePx
    };

    internal void Validate() {
        Art?.Validate();
        Canvas?.Validate();
        Duotone?.Validate();
        Halftone?.Validate();
        Logo?.Validate();
        if (TargetSizePx != 0 && (TargetSizePx < 16 || TargetSizePx > 8192)) throw new ArgumentOutOfRangeException(nameof(TargetSizePx));
        if (MinimumVersion is < 1 or > 40) throw new ArgumentOutOfRangeException(nameof(MinimumVersion));
        QrImageCanvasOptions.ValidatePosition(ImagePositionX, nameof(ImagePositionX));
        QrImageCanvasOptions.ValidatePosition(ImagePositionY, nameof(ImagePositionY));
        if (double.IsNaN(ImageZoom) || ImageZoom < 1 || ImageZoom > 4) throw new ArgumentOutOfRangeException(nameof(ImageZoom));
        if (ModuleSize is < 6 or > 64) throw new ArgumentOutOfRangeException(nameof(ModuleSize));
        if (Style == QrImageCompositionStyle.Halftone3x3 && ModuleSize % 3 != 0)
            throw new ArgumentException("Halftone3x3 needs a module size divisible by three for equal sub-cells.", nameof(ModuleSize));
        if (QuietZone is < 4 or > 16) throw new ArgumentOutOfRangeException(nameof(QuietZone));
        if (Style is < QrImageCompositionStyle.ColorModules or > QrImageCompositionStyle.PhotoHalftone)
            throw new ArgumentOutOfRangeException(nameof(Style));
        if ((Style == QrImageCompositionStyle.Halftone3x3 || Style == QrImageCompositionStyle.ImageBackground
            || Style == QrImageCompositionStyle.PhotoHalftone) && Art is not null)
            throw new ArgumentException("Image sub-cell and background treatments cannot be combined with module silhouettes.", nameof(Art));
        if (Duotone is not null && Style != QrImageCompositionStyle.Halftone3x3 && Style != QrImageCompositionStyle.PhotoHalftone)
            throw new ArgumentException("Duotone ink and paper apply to the Halftone3x3 and PhotoHalftone styles only.", nameof(Duotone));
        if (Halftone is not null && Style != QrImageCompositionStyle.PhotoHalftone)
            throw new ArgumentException("Halftone settings apply to the PhotoHalftone style only.", nameof(Halftone));
        if (Fit != QrImageFit.Cover && Fit != QrImageFit.Contain) throw new ArgumentOutOfRangeException(nameof(Fit));
        if (double.IsNaN(Strength) || Strength < 0 || Strength > 1) throw new ArgumentOutOfRangeException(nameof(Strength));
        if (double.IsNaN(CenterSize) || CenterSize < 0.5 || CenterSize > 1) throw new ArgumentOutOfRangeException(nameof(CenterSize));
    }
}
