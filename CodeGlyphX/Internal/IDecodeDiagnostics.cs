namespace CodeGlyphX;

internal interface IDecodeDiagnostics {
    string? Failure { get; }
    void SetFailure(string? value);
}
