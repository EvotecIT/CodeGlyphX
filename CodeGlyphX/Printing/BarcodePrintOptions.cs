using CodeGlyphX.Rendering;

namespace CodeGlyphX.Printing;

/// <summary>Opt-in physical layout settings. Distances are millimetres; text sizes are points.</summary>
/// <remarks>Existing pixel render options are independent of these settings. Layout snapshots all mutable inputs.</remarks>
public sealed class BarcodePrintOptions {
    /// <summary>Gets or sets the target printer resolution, from 24 to 2400 dots per inch.</summary>
    public int Dpi { get; set; } = 300;
    /// <summary>Gets or sets the requested narrow module width, or null for the family's nominal size.</summary>
    public double? XDimensionMm { get; set; }
    /// <summary>Gets or sets the linear or stacked bar height, excluding guards and text. Stacked separators retain their thickness.</summary>
    /// <remarks>Null selects the family default. Two-dimensional module grids and fixed-size formats retain their geometry.</remarks>
    public double? BarHeightMm { get; set; }
    /// <summary>Gets or sets whether orthogonal module pitch is rounded to whole printer pixels.</summary>
    /// <remarks>Swiss QR and the selected MaxiCode lattice retain their physical geometry. Preflight reports fractional pitch.</remarks>
    public bool SnapToPrinterPixels { get; set; } = true;
    /// <summary>Gets or sets whether to reserve the family's quiet zones. Omitting a required quiet zone creates a preflight error.</summary>
    public bool IncludeQuietZones { get; set; } = true;
    /// <summary>Gets or sets an explicit physical format for caller-created linear runs. It must agree with stored format metadata.</summary>
    public SymbolFormat? Format { get; set; }
    /// <summary>Gets or sets the exact readable text, including line breaks. Null derives retail digits where possible.</summary>
    /// <remarks>Use the owning GS1/HIBC/profile helper to prepare application text. Printed text is never input to a decoder.</remarks>
    public string? HumanReadableText { get; set; }
    /// <summary>Gets or sets whether to include readable text when available.</summary>
    public bool IncludeHumanReadable { get; set; } = true;
    /// <summary>Gets or sets a scoped supplied font. Required when printable text is requested.</summary>
    public IBarcodeFont? Font { get; set; }
    /// <summary>Gets or sets the readable text's em size in points.</summary>
    public double FontSizePoints { get; set; } = 10;
    /// <summary>Gets or sets a separate caption, positioned above the symbol. Line breaks are supported.</summary>
    public string? Caption { get; set; }
    /// <summary>Gets or sets the caption font, or null to use <see cref="Font"/>.</summary>
    public IBarcodeFont? CaptionFont { get; set; }
    /// <summary>Gets or sets the caption size in points.</summary>
    public double CaptionSizePoints { get; set; } = 10;
    /// <summary>Gets or sets whether captions may shrink to fit the symbol, subject to their minimum size.</summary>
    public bool FitCaptionToSymbolWidth { get; set; } = true;
    /// <summary>Gets or sets the minimum caption size in points.</summary>
    public double MinimumCaptionSizePoints { get; set; } = 9;
    /// <summary>Gets or sets whether missing glyphs or unsupported shaping must throw.</summary>
    public bool StrictText { get; set; } = true;
    /// <summary>Gets or sets whether the supplied readable font is a bold face, for application preflight.</summary>
    /// <remarks>This is an explicit caller declaration; font names are not treated as proof of weight.</remarks>
    public bool FontIsBold { get; set; }
    /// <summary>Gets or sets whether a separately supplied caption font is a bold face, for application preflight.</summary>
    public bool CaptionFontIsBold { get; set; }
    /// <summary>Gets or sets declared synthetic weight: unions the text outline with small radial offsets.</summary>
    /// <remarks>Prefer an actual bold face. Synthetic expansion is two percent of em and appears in the measured ink bounds.</remarks>
    public bool SyntheticBold { get; set; }
    /// <summary>Gets or sets the optional application-specific text policy.</summary>
    public BarcodePrintTextStyle TextStyle { get; set; }
    /// <summary>Gets or sets the foreground paint. Defaults to opaque black.</summary>
    public BarcodePaint Foreground { get; set; } = BarcodePaint.Solid(Rgba32.Black);
    /// <summary>Gets or sets the background paint. Defaults to opaque white.</summary>
    public BarcodePaint Background { get; set; } = BarcodePaint.Solid(Rgba32.White);
    /// <summary>Gets or sets the text paint, or null to use the foreground.</summary>
    public BarcodePaint? TextPaint { get; set; }
    /// <summary>Gets or sets whether retail quiet-zone chevrons are printed when the chosen font contains them.</summary>
    public bool QuietZoneIndicators { get; set; }
    /// <summary>Gets or sets whether an ITF-14 bearer surrounds all four sides instead of the two horizontal sides.</summary>
    public bool Itf14BearerFrame { get; set; }
    /// <summary>Gets or sets the ITF-14 bearer thickness in millimetres. Null selects 4.83 mm for a frame or five narrow modules for horizontal bearers.</summary>
    /// <remarks>Preflight requires at least twice the narrow bar width. Plate printing uses a 4.83 mm frame.</remarks>
    public double? Itf14BearerThicknessMm { get; set; }

    internal BarcodePrintOptions Snapshot() => (BarcodePrintOptions)MemberwiseClone();
}

/// <summary>Application text constraints selected explicitly when the physical carrier is ambiguous.</summary>
public enum BarcodePrintTextStyle {
    /// <summary>Ordinary readable text, without an application-specific minimum.</summary>
    Plain,
    /// <summary>GS1 readable interpretation, with a minimum 2 mm em size.</summary>
    Gs1,
    /// <summary>HIBC readable interpretation supplied by the HIBC owner.</summary>
    Hibc,
    /// <summary>USPS package readable interpretation, with cap-height and weight preflight.</summary>
    UspsPackage
}
