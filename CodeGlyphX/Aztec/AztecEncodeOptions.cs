namespace CodeGlyphX.Aztec;

/// <summary>
/// Options for Aztec encoding.
/// </summary>
public sealed class AztecEncodeOptions {
    /// <summary>Gets or sets the text encoding. By default Latin-1 is used when possible, otherwise UTF-8 with ECI 26.</summary>
    public System.Text.Encoding? TextEncoding { get; set; }

    /// <summary>Gets or sets an ECI assignment (0..999999). For text it must agree with TextEncoding; for binary input it describes the supplied bytes.</summary>
    public int? EciAssignmentNumber { get; set; }

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
