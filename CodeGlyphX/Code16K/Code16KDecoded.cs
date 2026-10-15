namespace CodeGlyphX.Code16K;

/// <summary>A validated Code 16K payload and its structural information.</summary>
public sealed class Code16KDecoded {
    /// <summary>Gets the decoded Latin-1 text.</summary>
    public string Text { get; }
    /// <summary>Gets whether the start mode declares GS1/FNC1 semantics.</summary>
    public bool IsGs1 { get; }
    /// <summary>Gets the number of data rows.</summary>
    public int Rows { get; }
    /// <summary>Gets whether the start mode declares reader initialization.</summary>
    public bool ReaderInitialization { get; }

    internal Code16KDecoded(string text, bool isGs1, int rows, bool readerInitialization) {
        Text = text;
        IsGs1 = isGs1;
        Rows = rows;
        ReaderInitialization = readerInitialization;
    }
}
