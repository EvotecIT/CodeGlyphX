using System;

namespace CodeGlyphX.Rendering.Art;

/// <summary>
/// Two-colour ink and paper for <see cref="QrImageCompositionStyle.Halftone3x3"/> and
/// <see cref="QrImageCompositionStyle.PhotoHalftone"/>. Dark sub-cells, dark functional modules and data centres use the
/// ink; everything light, including the quiet zone, uses the paper.
/// The defaults are a navy ink on warm cream paper, in the spirit of a risograph print.
/// </summary>
public sealed class QrImageDuotoneOptions {
    /// <summary>Opaque dark ink; luminance must not exceed 64/255. Defaults to navy #1E3780.</summary>
    public Rgba32 Ink { get; set; } = new Rgba32(0x1E, 0x37, 0x80);

    /// <summary>Opaque light paper; luminance must be at least 200/255. Defaults to warm cream #F6EEDD.</summary>
    public Rgba32 Paper { get; set; } = new Rgba32(0xF6, 0xEE, 0xDD);

    internal void Validate() {
        if (Ink.A != 255 || Luminance(Ink) > 64)
            throw new ArgumentException("Duotone ink must be opaque and dark (luminance at most 64).", nameof(Ink));
        if (Paper.A != 255 || Luminance(Paper) < 200)
            throw new ArgumentException("Duotone paper must be opaque and light (luminance at least 200).", nameof(Paper));
    }

    private static double Luminance(Rgba32 color) => 0.299 * color.R + 0.587 * color.G + 0.114 * color.B;
}
