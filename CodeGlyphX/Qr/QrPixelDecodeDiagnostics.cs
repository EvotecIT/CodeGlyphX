#if NET8_0_OR_GREATER
namespace CodeGlyphX.Qr;

internal readonly struct QrPixelDecodeDiagnostics {
    public int Scale { get; }
    public byte Threshold { get; }
    public bool Invert { get; }
    public int CandidateCount { get; }
    public int CandidateTriplesTried { get; }
    public int Dimension { get; }
    public QrDecodeDiagnostics ModuleDiagnostics { get; }

    public QrPixelDecodeDiagnostics(
        int scale,
        byte threshold,
        bool invert,
        int candidateCount,
        int candidateTriplesTried,
        int dimension,
        QrDecodeDiagnostics moduleDiagnostics) {
        Scale = scale;
        Threshold = threshold;
        Invert = invert;
        CandidateCount = candidateCount;
        CandidateTriplesTried = candidateTriplesTried;
        Dimension = dimension;
        ModuleDiagnostics = moduleDiagnostics;
    }

    public override string ToString() {
        var inv = Invert ? "inv" : "norm";
        var dim = Dimension > 0 ? $" dim{Dimension}" : string.Empty;
        return $"s{Scale} t{Threshold} {inv} cand{CandidateCount} tri{CandidateTriplesTried}{dim} {ModuleDiagnostics}";
    }
}
#endif

