using System;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering.Art;

/// <summary>Physical scene export. Width and DPI determine a fresh 256..4096 pixel scene.</summary>
public sealed class QrSceneExportOptions {
    /// <summary>Artwork width and height in millimeters (10..200).</summary>
    public double WidthMillimeters { get; set; } = 100;
    /// <summary>PNG resolution metadata and raster resolution (72..600 dots per inch).</summary>
    public int Dpi { get; set; } = 300;
    /// <summary>Lossless PNG compression, 0..9. Zero stores uncompressed data.</summary>
    public int PngCompressionLevel { get; set; } = 6;
    /// <summary>Rounded pixel width implied by the physical size and DPI.</summary>
    public int PixelSize { get { Validate(); return (int)Math.Round(WidthMillimeters / 25.4 * Dpi); } }
    internal QrSceneExportOptions Copy() => new() { WidthMillimeters = WidthMillimeters, Dpi = Dpi, PngCompressionLevel = PngCompressionLevel };
    internal void Validate() {
        QrSceneComposition.ValidatePhysicalWidth(WidthMillimeters, allowZero: false);
        if (Dpi < 72 || Dpi > 600) throw new ArgumentOutOfRangeException(nameof(Dpi));
        var pixels = Math.Round(WidthMillimeters / 25.4 * Dpi);
        if (pixels < 256 || pixels > 4096) throw new ArgumentException("Choose width and DPI which produce 256..4096 pixels.");
        if (PngCompressionLevel < 0 || PngCompressionLevel > 9) throw new ArgumentOutOfRangeException(nameof(PngCompressionLevel));
    }
}

/// <summary>A retained physical export. Its scene and settings are independent of later control edits.</summary>
public sealed class QrSceneExport {
    private readonly QrSceneExportOptions _options;
    /// <summary>Artwork rendered directly at the requested raster dimensions.</summary>
    public QrSceneComposition Scene { get; }
    /// <summary>Physical artwork width and height in millimeters.</summary>
    public double WidthMillimeters => _options.WidthMillimeters;
    /// <summary>Requested resolution, recorded as PNG pixels per meter with nearest-integer rounding.</summary>
    public int Dpi => _options.Dpi;
    internal QrSceneExport(QrSceneComposition scene, QrSceneExportOptions options) { Scene = scene; _options = options; }
    /// <summary>Encodes the exact export grid with lossless compression and physical resolution metadata.</summary>
    public byte[] ToPng() => PngImageEncoder.EncodeRgba32(Scene.Image.PixelSpan, Scene.Image.Size, Scene.Image.Size, _options.PngCompressionLevel, _options.Dpi);
    /// <summary>Exports shared scene geometry and the exact QR contours at the requested physical size.</summary>
    public string ToSvg() => Scene.ToSvg(_options.WidthMillimeters);
    /// <summary>Embeds the finished raster artwork in a PDF page at the requested physical size.</summary>
    public byte[] ToPdf() => Scene.ToPdf(_options.WidthMillimeters);
}
