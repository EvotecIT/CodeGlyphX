using System;
using System.IO;
using CodeMatrix.Rendering.Png;

namespace CodeMatrix.Rendering;

/// <summary>
/// Options for embedding a PNG logo inside a QR render (SVG/HTML).
/// </summary>
public sealed class QrLogoOptions {
    /// <summary>
    /// PNG bytes for the logo image.
    /// </summary>
    public byte[] Png { get; }

    /// <summary>
    /// Maximum logo size relative to the QR area (excluding quiet zone).
    /// </summary>
    public double Scale { get; set; } = 0.20;

    /// <summary>
    /// Padding in pixels around the logo.
    /// </summary>
    public int PaddingPx { get; set; } = 4;

    /// <summary>
    /// Whether to draw a background plate behind the logo.
    /// </summary>
    public bool DrawBackground { get; set; } = true;

    /// <summary>
    /// Background color for the logo plate.
    /// </summary>
    public Rgba32 Background { get; set; } = Rgba32.White;

    /// <summary>
    /// Corner radius for the background plate in pixels.
    /// </summary>
    public int CornerRadiusPx { get; set; }

    /// <summary>
    /// Creates a logo option with PNG bytes.
    /// </summary>
    public QrLogoOptions(byte[] png) {
        if (png is null) throw new ArgumentNullException(nameof(png));
        if (png.Length == 0) throw new ArgumentException("PNG data is empty.", nameof(png));
        Png = png;
    }

    /// <summary>
    /// Creates a logo option from a PNG file.
    /// </summary>
    /// <param name="path">PNG file path.</param>
    /// <returns>Logo options.</returns>
    public static QrLogoOptions FromPngFile(string path) {
        if (path is null) throw new ArgumentNullException(nameof(path));
        var png = File.ReadAllBytes(path);
        return new QrLogoOptions(png);
    }

    internal void Validate() {
        if (Scale is <= 0 or > 1.0) throw new ArgumentOutOfRangeException(nameof(Scale));
        if (PaddingPx < 0) throw new ArgumentOutOfRangeException(nameof(PaddingPx));
        if (Png.Length == 0) throw new ArgumentException("PNG data is empty.", nameof(Png));
    }

    internal static bool TryReadPngSize(byte[] png, out int width, out int height) {
        width = 0;
        height = 0;
        if (png is null || png.Length < 24) return false;

        // PNG signature.
        if (png[0] != 137 || png[1] != 80 || png[2] != 78 || png[3] != 71 ||
            png[4] != 13 || png[5] != 10 || png[6] != 26 || png[7] != 10) return false;

        // First chunk must be IHDR.
        if (png[12] != (byte)'I' || png[13] != (byte)'H' || png[14] != (byte)'D' || png[15] != (byte)'R') {
            return false;
        }

        width = (png[16] << 24) | (png[17] << 16) | (png[18] << 8) | png[19];
        height = (png[20] << 24) | (png[21] << 16) | (png[22] << 8) | png[23];

        return width > 0 && height > 0;
    }
}
