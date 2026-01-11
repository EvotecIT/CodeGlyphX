namespace CodeGlyphX;

/// <summary>
/// Diagnostics returned by pixel-based QR decoding attempts.
/// </summary>
public readonly struct QrPixelDecodeInfo {
    /// <summary>
    /// Gets the scale factor applied before decoding.
    /// </summary>
    public int Scale { get; }

    /// <summary>
    /// Gets the binarization threshold (0..255).
    /// </summary>
    public byte Threshold { get; }

    /// <summary>
    /// Gets a value indicating whether the image was inverted.
    /// </summary>
    public bool Invert { get; }

    /// <summary>
    /// Gets the number of candidate finder patterns detected.
    /// </summary>
    public int CandidateCount { get; }

    /// <summary>
    /// Gets the number of finder triples evaluated.
    /// </summary>
    public int CandidateTriplesTried { get; }

    /// <summary>
    /// Gets the decoded module dimension, when available.
    /// </summary>
    public int Dimension { get; }

    /// <summary>
    /// Gets the underlying module-level diagnostics.
    /// </summary>
    public QrDecodeInfo Module { get; }

    /// <summary>
    /// Gets a value indicating whether decoding succeeded.
    /// </summary>
    public bool IsSuccess => Module.IsSuccess;

    /// <summary>
    /// Gets a confidence score (0..1) for the decode.
    /// </summary>
    public double Confidence => QrDecodeConfidence.Estimate(this);

    internal QrPixelDecodeInfo(
        int scale,
        byte threshold,
        bool invert,
        int candidateCount,
        int candidateTriplesTried,
        int dimension,
        QrDecodeInfo module) {
        Scale = scale;
        Threshold = threshold;
        Invert = invert;
        CandidateCount = candidateCount;
        CandidateTriplesTried = candidateTriplesTried;
        Dimension = dimension;
        Module = module;
    }

#if NET8_0_OR_GREATER
    internal static QrPixelDecodeInfo FromInternal(CodeGlyphX.Qr.QrPixelDecodeDiagnostics diagnostics) {
        return new QrPixelDecodeInfo(
            diagnostics.Scale,
            diagnostics.Threshold,
            diagnostics.Invert,
            diagnostics.CandidateCount,
            diagnostics.CandidateTriplesTried,
            diagnostics.Dimension,
            QrDecodeInfo.FromInternal(diagnostics.ModuleDiagnostics));
    }
#endif

    /// <inheritdoc />
    public override string ToString() {
        var inv = Invert ? "inv" : "norm";
        var dim = Dimension > 0 ? $" dim{Dimension}" : string.Empty;
        return $"s{Scale} t{Threshold} {inv} cand{CandidateCount} tri{CandidateTriplesTried}{dim} {Module}";
    }
}
