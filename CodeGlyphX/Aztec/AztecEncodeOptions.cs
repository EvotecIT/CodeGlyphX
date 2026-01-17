namespace CodeGlyphX.Aztec;

/// <summary>
/// Options for future Aztec encoding support.
/// </summary>
public sealed class AztecEncodeOptions {
    /// <summary>
    /// Target number of layers. When null, the encoder will choose automatically.
    /// </summary>
    public int? Layers { get; set; }

    /// <summary>
    /// Target error correction percentage. When null, the encoder will choose automatically.
    /// </summary>
    public int? ErrorCorrectionPercent { get; set; }

    /// <summary>
    /// When true, encode as compact Aztec when possible.
    /// </summary>
    public bool? Compact { get; set; }
}
