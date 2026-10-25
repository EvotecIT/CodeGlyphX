using System;

namespace CodeGlyphX.Rendering.Art;

/// <summary>Placement and validation settings for a requested QR logo.</summary>
public sealed class QrLogoRenderOptions {
    /// <summary>Pattern-preserving Auto is the default. Covering alignment requires explicit opt-in.</summary>
    public LogoPlacement Placement { get; set; } = LogoPlacement.Auto;
    /// <summary>Appearance of the QR and optional surrounding canvas. Integer module sizing is required.</summary>
    public QrRenderOptions? Appearance { get; set; }
    /// <summary>Maximum knockout side including padding; zero uses the QR size.</summary>
    public int MaximumSideModules { get; set; }
    /// <summary>Padding on each side of the logo, counted in the block budget.</summary>
    public int PaddingModules { get; set; } = 1;
    /// <summary>Unknown codeword errors reserved in each block for other damage.</summary>
    public int ReservedErrorsPerBlock { get; set; } = 2;
    /// <summary>Minimum readable logo side, excluding padding.</summary>
    public int MinimumLogoSideModules { get; set; } = 3;
    /// <summary>Permit the text overload to try higher correction levels within the encoding version limits.</summary>
    public bool AllowErrorCorrectionUpgrade { get; set; }
    /// <summary>Permit the text overload to try larger versions, up to the encoding options' MaxVersion.</summary>
    public bool AllowVersionUpgrade { get; set; }
    /// <summary>Name of an independent decoder implementation. Required for CenterOverAlignment.</summary>
    public string? IndependentDecoderName { get; set; }
    /// <summary>Optional caller-supplied independent decoder, returning exact decoded text or null.
    /// Required for CenterOverAlignment. It must use a different implementation from CodeGlyphX.
    /// Receives a private copy of the final PNG. No external decoder is shipped by this library.</summary>
    public Func<byte[], string?>? IndependentDecoder { get; set; }
}
