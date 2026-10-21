using System;

namespace CodeGlyphX;

/// <summary>An encoded Ultracode with an owned logical-colour grid.</summary>
public sealed class UltracodeSymbol {
    /// <summary>Gets the physical format.</summary>
    public SymbolFormat Format => SymbolFormat.Ultracode;
    /// <summary>Gets the selected error-correction level.</summary>
    public UltracodeErrorCorrectionLevel ErrorCorrectionLevel { get; }
    /// <summary>Gets the frozen cell grid without its one-cell quiet zone.</summary>
    public UltracodeGrid Cells { get; }
    /// <summary>Renders fixed data colours and the selected structural paints to a PNG.</summary>
    public byte[] RenderPng(UltracodeRenderOptions? options = null) => Ultracode.UltracodeRenderer.RenderPng(Cells, options);
    /// <summary>Renders colour-cell rectangles as portable SVG vectors.</summary>
    public string RenderSvg(UltracodeRenderOptions? options = null) => Ultracode.UltracodeRenderer.RenderSvg(Cells, options);
    internal UltracodeSymbol(UltracodeGrid cells, UltracodeErrorCorrectionLevel errorCorrectionLevel) {
        Cells = (cells ?? throw new ArgumentNullException(nameof(cells))).Clone().Freeze();
        ErrorCorrectionLevel = errorCorrectionLevel;
    }
}
