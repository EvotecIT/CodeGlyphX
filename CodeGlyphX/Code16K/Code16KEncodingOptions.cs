namespace CodeGlyphX.Code16K;

/// <summary>Encoding controls for a Code 16K symbol.</summary>
public sealed class Code16KEncodingOptions {
    /// <summary>Gets or sets whether the payload uses GS1 element-string semantics.</summary>
    public bool IsGs1 { get; set; }
    /// <summary>Gets or sets the data-row count, from two through sixteen; null selects the smallest symbol.</summary>
    public int? Rows { get; set; }
    /// <summary>Gets or sets whether FNC3 marks the content as reader configuration.</summary>
    /// <remarks>The library generates the marker and does not apply reader settings.</remarks>
    public bool ReaderInitialization { get; set; }
    /// <summary>Gets or sets whether FNC2 requests appending this message to the next scanned message.</summary>
    public bool MessageAppend { get; set; }
    /// <summary>Gets or sets this symbol's position in an ordered message.</summary>
    public Code16KStructuredAppend? StructuredAppend { get; set; }
}

/// <summary>Position and total for Code 16K extended data length or FNC2 concatenation.</summary>
public readonly struct Code16KStructuredAppend {
    /// <summary>Gets the one-based symbol index.</summary>
    public int Index { get; }
    /// <summary>Gets the number of symbols in the message.</summary>
    public int Total { get; }
    /// <summary>Gets whether this sequence uses sixteen-row extended-data blocks.</summary>
    /// <remarks>Extended data supports up to 107 blocks. FNC2 concatenation supports up to nine ordinary symbols.</remarks>
    public bool IsExtendedDataLength { get; }
    /// <summary>Gets whether the position and total are valid for the selected sequence type.</summary>
    public bool IsValid => Index >= 1 && Total >= 2 && Total <= (IsExtendedDataLength ? 107 : 9) && Index <= Total;
    /// <summary>Creates a sequence position; indices are one-based.</summary>
    public Code16KStructuredAppend(int index, int total, bool isExtendedDataLength = true) {
        Index = index;
        Total = total;
        IsExtendedDataLength = isExtendedDataLength;
    }
}
