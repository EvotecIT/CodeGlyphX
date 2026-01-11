using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering;

/// <summary>
/// Shared rendering defaults across formats.
/// </summary>
public static class RenderDefaults {
    /// <summary>
    /// Default module size for QR and 2D matrices.
    /// </summary>
    public const int QrModuleSize = 6;

    /// <summary>
    /// Default quiet zone size for QR and 2D matrices.
    /// </summary>
    public const int QrQuietZone = 4;

    /// <summary>
    /// Default foreground color for QR and 2D matrices.
    /// </summary>
    public static readonly Rgba32 QrForeground = Rgba32.Black;

    /// <summary>
    /// Default background color for QR and 2D matrices.
    /// </summary>
    public static readonly Rgba32 QrBackground = Rgba32.White;

    /// <summary>
    /// Default foreground CSS color for QR and 2D matrices.
    /// </summary>
    public const string QrForegroundCss = "#000";

    /// <summary>
    /// Default background CSS color for QR and 2D matrices.
    /// </summary>
    public const string QrBackgroundCss = "#fff";

    /// <summary>
    /// Default module size for 1D barcodes.
    /// </summary>
    public const int BarcodeModuleSize = 2;

    /// <summary>
    /// Default quiet zone size for 1D barcodes.
    /// </summary>
    public const int BarcodeQuietZone = 10;

    /// <summary>
    /// Default height in modules for 1D barcodes.
    /// </summary>
    public const int BarcodeHeightModules = 40;

    /// <summary>
    /// Default foreground color for 1D barcodes.
    /// </summary>
    public static readonly Rgba32 BarcodeForeground = Rgba32.Black;

    /// <summary>
    /// Default background color for 1D barcodes.
    /// </summary>
    public static readonly Rgba32 BarcodeBackground = Rgba32.White;

    /// <summary>
    /// Default foreground CSS color for 1D barcodes.
    /// </summary>
    public const string BarcodeForegroundCss = "#000";

    /// <summary>
    /// Default background CSS color for 1D barcodes.
    /// </summary>
    public const string BarcodeBackgroundCss = "#fff";

    /// <summary>
    /// Default label font size in pixels for 1D barcodes.
    /// </summary>
    public const int BarcodeLabelFontSize = 12;

    /// <summary>
    /// Default label margin in pixels for 1D barcodes.
    /// </summary>
    public const int BarcodeLabelMargin = 4;

    /// <summary>
    /// Default label font family for 1D barcodes.
    /// </summary>
    public const string BarcodeLabelFontFamily = "Segoe UI, Arial, sans-serif";
}
