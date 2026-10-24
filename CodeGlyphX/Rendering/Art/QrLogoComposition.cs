using System;
using System.Collections.Generic;

namespace CodeGlyphX.Rendering.Art;

/// <summary>A rendered, decode-validated logo composition and its actual placement report.</summary>
public sealed class QrLogoComposition {
    private readonly byte[] _png;
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

    internal QrLogoComposition(QrCode symbol, QrLogoBudget budget, byte[] png, string[] warnings, string[] decoders) {
        Symbol = symbol;
        Budget = budget;
        _png = png;
        Warnings = Array.AsReadOnly(warnings);
        VerifiedDecoders = Array.AsReadOnly(decoders);
    }
}
