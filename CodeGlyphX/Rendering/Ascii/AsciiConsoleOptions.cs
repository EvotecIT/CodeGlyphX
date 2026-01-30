using System.Text;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering.Ascii;

/// <summary>
/// Options for console-friendly ASCII rendering with auto-fit.
/// </summary>
public sealed class AsciiConsoleOptions {
    /// <summary>
    /// Optional console window width override (columns).
    /// </summary>
    public int? WindowWidth { get; set; }

    /// <summary>
    /// Optional console window height override (rows).
    /// </summary>
    public int? WindowHeight { get; set; }

    /// <summary>
    /// Optional maximum console width to use for auto-fit (columns).
    /// </summary>
    public int? MaxWindowWidth { get; set; }

    /// <summary>
    /// Optional maximum console height to use for auto-fit (rows).
    /// </summary>
    public int? MaxWindowHeight { get; set; }

    /// <summary>
    /// Optional target output width (columns, excluding padding).
    /// </summary>
    public int? TargetWidth { get; set; }

    /// <summary>
    /// Optional target output height (rows, excluding padding).
    /// </summary>
    public int? TargetHeight { get; set; }

    /// <summary>
    /// Horizontal padding to keep around the QR when auto-fitting.
    /// </summary>
    public int PaddingColumns { get; set; } = 4;

    /// <summary>
    /// Vertical padding to keep around the QR when auto-fitting.
    /// </summary>
    public int PaddingRows { get; set; } = 2;

    /// <summary>
    /// Minimum scale applied when auto-fitting.
    /// </summary>
    public int MinScale { get; set; } = 1;

    /// <summary>
    /// Maximum scale applied when auto-fitting.
    /// </summary>
    public int MaxScale { get; set; } = 4;

    /// <summary>
    /// Minimum quiet zone size when shrinking to fit.
    /// </summary>
    public int MinQuietZone { get; set; } = 2;

    /// <summary>
    /// Overrides quiet zone size (modules).
    /// </summary>
    public int? QuietZone { get; set; }

    /// <summary>
    /// When true, allows reducing quiet zone to fit within console size.
    /// </summary>
    public bool AllowQuietZoneShrink { get; set; } = true;

    /// <summary>
    /// When true, allows shrinking module width to fit within console size.
    /// </summary>
    public bool AllowModuleWidthShrink { get; set; } = true;

    /// <summary>
    /// Optional module width override.
    /// </summary>
    public int? ModuleWidth { get; set; }

    /// <summary>
    /// Optional module height override.
    /// </summary>
    public int? ModuleHeight { get; set; }

    /// <summary>
    /// Optional dark glyph override.
    /// </summary>
    public string? Dark { get; set; }

    /// <summary>
    /// Optional light glyph override.
    /// </summary>
    public string? Light { get; set; }

    /// <summary>
    /// Optional line separator override.
    /// </summary>
    public string? NewLine { get; set; }

    /// <summary>
    /// When true, uses Unicode half-blocks to compress height.
    /// </summary>
    public bool UseHalfBlocks { get; set; } = true;

    /// <summary>
    /// When true, uses ANSI background colors for half-block rendering.
    /// </summary>
    public bool HalfBlockUseBackground { get; set; } = true;

    /// <summary>
    /// When true, prefers Unicode block glyphs.
    /// </summary>
    public bool UseUnicodeBlocks { get; set; } = true;

    /// <summary>
    /// Enables or disables ANSI colors (null = auto-detect).
    /// </summary>
    public bool? UseAnsiColors { get; set; }

    /// <summary>
    /// Enables or disables ANSI truecolor (null = default true).
    /// </summary>
    public bool? UseTrueColor { get; set; }

    /// <summary>
    /// Enables or disables ANSI colorization of light modules (null = keep preset default).
    /// </summary>
    public bool? ColorizeLight { get; set; }

    /// <summary>
    /// Character cell aspect ratio (width / height). Used to auto-tune module width.
    /// </summary>
    public double? CellAspectRatio { get; set; }

    /// <summary>
    /// Optional ANSI gradient for dark modules.
    /// </summary>
    public AsciiGradientOptions? DarkGradient { get; set; }

    /// <summary>
    /// Optional ANSI palette for dark modules.
    /// </summary>
    public AsciiPaletteOptions? DarkPalette { get; set; }

    /// <summary>
    /// When true, enables scan-friendly defaults (quiet zone, background fill, contrast clamp).
    /// </summary>
    public bool PreferScanReliability { get; set; }

    /// <summary>
    /// When true, clamps dark colors to a maximum luminance.
    /// </summary>
    public bool EnsureDarkContrast { get; set; }

    /// <summary>
    /// Maximum luminance allowed for dark modules (0-1).
    /// </summary>
    public double MaxDarkLuminance { get; set; } = 0.45;

    /// <summary>
    /// Optional ANSI color for dark modules.
    /// </summary>
    public Rgba32? DarkColor { get; set; }

    /// <summary>
    /// Optional ANSI color for light modules.
    /// </summary>
    public Rgba32? LightColor { get; set; }

    /// <summary>
    /// Optional output encoding override used for Unicode capability checks.
    /// </summary>
    public Encoding? OutputEncoding { get; set; }

    /// <summary>
    /// When true, swaps dark and light output.
    /// </summary>
    public bool? Invert { get; set; }
}
