namespace CodeGlyphX.DataMatrix;

/// <summary>
/// Structured-append metadata for a sequence of Data Matrix symbols.
/// </summary>
public readonly struct DataMatrixStructuredAppend {
    /// <summary>Gets the one-based symbol index (1..16).</summary>
    public int Index { get; }

    /// <summary>Gets the total number of symbols (2..16).</summary>
    public int Total { get; }

    /// <summary>Gets the first file identifier codeword (1..254).</summary>
    public int FileId1 { get; }

    /// <summary>Gets the second file identifier codeword (1..254).</summary>
    public int FileId2 { get; }

    /// <summary>Gets whether all values are valid for Data Matrix structured append.</summary>
    public bool IsValid => Index >= 1 && Total >= 2 && Total <= 16 && Index <= Total
        && FileId1 >= 1 && FileId1 <= 254 && FileId2 >= 1 && FileId2 <= 254;

    /// <summary>
    /// Creates structured-append metadata.
    /// </summary>
    /// <param name="index">One-based symbol index (1..16).</param>
    /// <param name="total">Total symbols in the sequence (2..16).</param>
    /// <param name="fileId1">First file identifier codeword (1..254).</param>
    /// <param name="fileId2">Second file identifier codeword (1..254).</param>
    public DataMatrixStructuredAppend(int index, int total, int fileId1 = 1, int fileId2 = 1) {
        Index = index;
        Total = total;
        FileId1 = fileId1;
        FileId2 = fileId2;
    }

    /// <inheritdoc />
    public override string ToString() {
        return IsValid ? $"{Index}/{Total} id {FileId1:D3}{FileId2:D3}" : $"invalid ({Index}/{Total})";
    }
}
