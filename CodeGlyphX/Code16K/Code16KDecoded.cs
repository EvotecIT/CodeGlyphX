namespace CodeGlyphX.Code16K;

/// <summary>A validated Code 16K payload and its structural information.</summary>
public sealed class Code16KDecoded {
    /// <summary>Gets the decoded Latin-1 text.</summary>
    public string Text { get; }
    /// <summary>Gets whether the start mode declares GS1/FNC1 semantics.</summary>
    public bool IsGs1 { get; }
    /// <summary>Gets the number of data rows.</summary>
    public int Rows { get; }
    /// <summary>Gets whether FNC3 declares reader initialization.</summary>
    public bool ReaderInitialization { get; }
    /// <summary>Gets whether leading FNC2 requests appending this message to the next scanned message.</summary>
    public bool MessageAppend { get; }
    /// <summary>Gets the ordered-message position and total when present.</summary>
    public Code16KStructuredAppend? StructuredAppend { get; }

    internal Code16KDecoded(string text, bool isGs1, int rows, bool readerInitialization,
        bool messageAppend, Code16KStructuredAppend? structuredAppend) {
        Text = text;
        IsGs1 = isGs1;
        Rows = rows;
        ReaderInitialization = readerInitialization;
        MessageAppend = messageAppend;
        StructuredAppend = structuredAppend;
    }
}
