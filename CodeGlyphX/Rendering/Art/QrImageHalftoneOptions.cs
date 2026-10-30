using System;

namespace CodeGlyphX.Rendering.Art;

/// <summary>How <see cref="QrImageCompositionStyle.PhotoHalftone"/> reproduces the picture around each module centre.</summary>
public enum QrHalftoneScreen {
    /// <summary>
    /// One dot per sub-cell, sized by the local tone like a printed halftone. Light tones are small ink dots on paper and
    /// dark tones small paper holes in ink, so the whole tonal range stays smooth.
    /// </summary>
    Dots,
    /// <summary>
    /// Serpentine Floyd–Steinberg error diffusion across the sub-cell grid. Every sub-cell is either ink or paper, and the
    /// fixed module centres take part in the diffusion, so their tone is accounted for in the surrounding picture.
    /// </summary>
    ErrorDiffusion,
    /// <summary>An 8 by 8 Bayer threshold matrix: a regular, print-like texture without the grain of error diffusion.</summary>
    Ordered
}

/// <summary>Shape of the halftone dots and of the module centres in <see cref="QrImageCompositionStyle.PhotoHalftone"/>.</summary>
public enum QrHalftoneDotShape {
    /// <summary>Round dots and round module centres.</summary>
    Round,
    /// <summary>Square dots and square module centres.</summary>
    Square
}

/// <summary>
/// Settings for <see cref="QrImageCompositionStyle.PhotoHalftone"/>, where the picture forms the code: each module is split
/// into sub-cells, its centre carries the module colour at decode-safe contrast, and the surrounding sub-cells reproduce the
/// photo. <see cref="QrImageCompositionOptions.Strength"/> sets how far those sub-cells may follow the photo instead of the
/// module colour, and <see cref="QrImageCompositionOptions.Duotone"/> switches from photo colour to a two-colour print.
/// The defaults are conservative. Validate the exported image at its delivery size.
/// </summary>
public sealed class QrImageHalftoneOptions {
    /// <summary>
    /// Sub-cells per module side (3..7); defaults to 5. Odd counts keep a sub-cell on the module centre. Every sub-cell under
    /// the centre is held at the module colour, so with 5 the solid centre is a small cross that reaches 0.3 module along
    /// each axis. Coarser screens (3 or 4) hold less of the centre: raise <see cref="CenterSize"/> with them. Give each
    /// sub-cell at least 4 pixels (ModuleSize of 20 or more for 5) for visible dots.
    /// </summary>
    public int SubCells { get; set; } = 5;

    /// <summary>How the surrounding sub-cells reproduce the photo; defaults to <see cref="QrHalftoneScreen.Dots"/>.</summary>
    public QrHalftoneScreen Screen { get; set; }

    /// <summary>Dot and module-centre shape; defaults to <see cref="QrHalftoneDotShape.Round"/>.</summary>
    public QrHalftoneDotShape DotShape { get; set; }

    /// <summary>
    /// Width of the solid module centre relative to a module (0.3..0.6); defaults to 0.42. A larger centre leaves more scan
    /// margin and shows less of the picture. Timing, format and version modules always use at least 0.6.
    /// </summary>
    public double CenterSize { get; set; } = 0.42;

    /// <summary>Tone contrast around the middle grey of the stretched photo (0.5..2); defaults to 1.15.</summary>
    public double Contrast { get; set; } = 1.15;

    /// <summary>Tone shift, as a fraction of the full range (-0.4..0.4); positive values lighten. Defaults to 0.</summary>
    public double Brightness { get; set; }

    /// <summary>Finder and alignment geometry; defaults to rounded rings. Their proportions and polarity never change.</summary>
    public QrImageFinderStyle Finders { get; set; } = QrImageFinderStyle.Rounded;

    internal QrImageHalftoneOptions Clone() => new QrImageHalftoneOptions {
        SubCells = SubCells, Screen = Screen, DotShape = DotShape, CenterSize = CenterSize,
        Contrast = Contrast, Brightness = Brightness, Finders = Finders
    };

    internal void Validate() {
        if (SubCells is < 3 or > 7) throw new ArgumentOutOfRangeException(nameof(SubCells));
        if (Screen is < QrHalftoneScreen.Dots or > QrHalftoneScreen.Ordered) throw new ArgumentOutOfRangeException(nameof(Screen));
        if (DotShape is < QrHalftoneDotShape.Round or > QrHalftoneDotShape.Square) throw new ArgumentOutOfRangeException(nameof(DotShape));
        if (double.IsNaN(CenterSize) || CenterSize < 0.3 || CenterSize > 0.6) throw new ArgumentOutOfRangeException(nameof(CenterSize));
        if (double.IsNaN(Contrast) || Contrast < 0.5 || Contrast > 2) throw new ArgumentOutOfRangeException(nameof(Contrast));
        if (double.IsNaN(Brightness) || Brightness < -0.4 || Brightness > 0.4) throw new ArgumentOutOfRangeException(nameof(Brightness));
        if (Finders is < QrImageFinderStyle.Square or > QrImageFinderStyle.Chamfered) throw new ArgumentOutOfRangeException(nameof(Finders));
    }
}
