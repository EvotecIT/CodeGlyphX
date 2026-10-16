namespace CodeGlyphX;

/// <summary>Code 16K row count and message controls.</summary>
public sealed class Code16KSymbolMetadata : SymbolMetadata {
    /// <summary>Gets the number of data rows.</summary>
    public int Rows { get; }
    /// <summary>Gets whether FNC3 marks reader configuration.</summary>
    public bool ReaderInitialization { get; }
    /// <summary>Gets whether FNC2 requests message append.</summary>
    public bool MessageAppend { get; }
    /// <summary>Gets the ordered-message position and total when present.</summary>
    public Code16K.Code16KStructuredAppend? StructuredAppend { get; }
    internal Code16KSymbolMetadata(Code16K.Code16KDecoded value) {
        Rows = value.Rows; ReaderInitialization = value.ReaderInitialization;
        MessageAppend = value.MessageAppend; StructuredAppend = value.StructuredAppend;
    }
}

/// <summary>Code 49 row count and sequence controls.</summary>
public sealed class Code49SymbolMetadata : SymbolMetadata {
    /// <summary>Gets the number of data rows.</summary>
    public int Rows { get; }
    /// <summary>Gets the start mode.</summary>
    public int Mode { get; }
    /// <summary>Gets whether mode 1 marks a continuation symbol.</summary>
    public bool IsContinuation => Mode == 1;
    /// <summary>Gets the ordered-group position and total when present.</summary>
    public Code49.Code49StructuredAppend? StructuredAppend { get; }
    internal Code49SymbolMetadata(Code49.Code49Decoded value) {
        Rows = value.Rows; Mode = value.Mode; StructuredAppend = value.StructuredAppend;
    }
}

/// <summary>Codablock F data-row and column counts.</summary>
public sealed class CodablockFSymbolMetadata : SymbolMetadata {
    /// <summary>Gets the number of data rows.</summary>
    public int Rows { get; }
    /// <summary>Gets the number of data-codeword columns per row.</summary>
    public int Columns { get; }
    internal CodablockFSymbolMetadata(CodablockF.CodablockFDecoded value) { Rows = value.Rows; Columns = value.Columns; }
}

/// <summary>PosiCode variant and payload function controls.</summary>
public sealed class PosiCodeSymbolMetadata : SymbolMetadata {
    /// <summary>Gets the normal or limited gap variant.</summary>
    public PosiCode.PosiCodeVariant Variant { get; }
    /// <summary>Gets whether leading FNC1 declares GS1 semantics.</summary>
    public bool IsGs1 { get; }
    /// <summary>Gets whether FNC3 marks reader configuration.</summary>
    public bool ReaderInitialization { get; }
    /// <summary>Gets whether FNC2 requests message append.</summary>
    public bool MessageAppend { get; }
    internal PosiCodeSymbolMetadata(PosiCode.PosiCodeDecoded value) {
        Variant = value.Variant; IsGs1 = value.IsGs1;
        ReaderInitialization = value.ReaderInitialization; MessageAppend = value.MessageAppend;
    }
}
