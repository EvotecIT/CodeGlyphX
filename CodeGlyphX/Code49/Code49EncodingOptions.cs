namespace CodeGlyphX.Code49;

/// <summary>Encoding controls for a Code 49 symbol.</summary>
public sealed class Code49EncodingOptions {
    /// <summary>Gets or sets whether the payload uses GS1 element-string semantics.</summary>
    public bool IsGs1 { get; set; }
    /// <summary>Gets or sets the data-row count, from two through eight; null selects the smallest symbol.</summary>
    public int? Rows { get; set; }
    /// <summary>Gets or sets whether mode 1 marks this as a continuation of the previously scanned symbol.</summary>
    public bool IsContinuation { get; set; }
    /// <summary>Gets or sets the position and total for a mode 3 ordered group.</summary>
    public Code49StructuredAppend? StructuredAppend { get; set; }
}

/// <summary>The one-based position and total for a group of two through nine Code 49 symbols.</summary>
public readonly struct Code49StructuredAppend {
    /// <summary>Gets the one-based symbol index.</summary>
    public int Index { get; }
    /// <summary>Gets the total number of symbols.</summary>
    public int Total { get; }
    /// <summary>Gets whether the position and total are valid.</summary>
    public bool IsValid => Index >= 1 && Total >= 2 && Total <= 9 && Index <= Total;
    /// <summary>Creates a sequence position; indices are one-based.</summary>
    public Code49StructuredAppend(int index, int total) { Index = index; Total = total; }
    internal int Codeword => Total * (Total - 1) / 2 - 1 + Index;
    internal static bool TryRead(int codeword, out Code49StructuredAppend append) {
        for (var total = 2; total <= 9; total++) {
            var index = codeword - (total * (total - 1) / 2 - 1);
            if (index >= 1 && index <= total) { append = new Code49StructuredAppend(index, total); return true; }
        }
        append = default;
        return false;
    }
}
