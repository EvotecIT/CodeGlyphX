using System;
using System.Collections.Generic;

namespace CodeGlyphX.Rendering.Art;

/// <summary>A rendered, decode-validated logo composition and its actual placement report.</summary>
public sealed class QrLogoComposition {
    private readonly byte[] _png;
    private readonly QrRenderOptions _appearance;
    /// <summary>The actual encoded symbol, including any explicitly permitted version or correction upgrade.</summary>
    public QrCode Symbol { get; }
    /// <summary>Chosen placement, logo size, blocks touched, and per-block correction margins.</summary>
    public QrLogoBudget Budget { get; }
    /// <summary>Warnings, including explicit coverage of a central alignment pattern.</summary>
    public IReadOnlyList<string> Warnings { get; }
    /// <summary>Implementations that verified the final PNG. These are not physical phone-scan results.</summary>
    public IReadOnlyList<string> VerifiedDecoders { get; }
    /// <summary>Returns a caller-owned copy of the verified PNG.</summary>
    public byte[] GetPng() => (byte[])_png.Clone();

    /// <summary>Returns an independent copy of the resolved appearance, including actual module sizing,
    /// protected quiet zone and logo offsets. Use it with the unchanged Symbol to export another format.
    /// Only the original PNG is certified by VerifiedDecoders; validate final delivery assets separately.</summary>
    public QrRenderOptions GetRenderOptions() => QrRenderer.CloneOptions(_appearance);

    internal QrLogoComposition(QrCode symbol, QrLogoBudget budget, byte[] png, string[] warnings, string[] decoders, QrRenderOptions appearance) {
        Symbol = symbol;
        Budget = budget;
        _png = png;
        _appearance = QrRenderer.CloneOptions(appearance);
        Warnings = Array.AsReadOnly(warnings);
        VerifiedDecoders = Array.AsReadOnly(decoders);
    }
}
