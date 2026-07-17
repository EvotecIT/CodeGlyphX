namespace CodeGlyphX.DataMatrix;

/// <summary>
/// Controls Data Matrix high-level encoding and symbol selection.
/// </summary>
public sealed class DataMatrixEncodingOptions {
    /// <summary>Gets or sets the high-level data encoding mode.</summary>
    public DataMatrixEncodingMode Mode { get; set; } = DataMatrixEncodingMode.Auto;

    /// <summary>Gets or sets the allowed symbol family.</summary>
    public DataMatrixShape Shape { get; set; } = DataMatrixShape.Square;

    /// <summary>Gets or sets an exact symbol row count. Rows and columns must be specified together.</summary>
    public int? Rows { get; set; }

    /// <summary>Gets or sets an exact symbol column count. Rows and columns must be specified together.</summary>
    public int? Columns { get; set; }

    /// <summary>Gets or sets whether FNC1 in first position identifies a GS1 Data Matrix payload.</summary>
    public bool IsGs1 { get; set; }

    /// <summary>
    /// Gets or sets an optional ECI assignment number (0..999999). String payloads support the library's
    /// known ECI character sets with Auto or Base256 encodation; use EncodeBytes for custom assignments.
    /// </summary>
    public int? EciAssignmentNumber { get; set; }

    /// <summary>Gets or sets whether the Reader Programming codeword is emitted.</summary>
    public bool ReaderProgramming { get; set; }

    /// <summary>Gets or sets Macro 05 or Macro 06 compression for a payload body.</summary>
    public DataMatrixMacro Macro { get; set; }

    /// <summary>Gets or sets structured-append metadata for this symbol.</summary>
    public DataMatrixStructuredAppend? StructuredAppend { get; set; }

    internal DataMatrixEncodingOptions Clone() {
        return new DataMatrixEncodingOptions {
            Mode = Mode,
            Shape = Shape,
            Rows = Rows,
            Columns = Columns,
            IsGs1 = IsGs1,
            EciAssignmentNumber = EciAssignmentNumber,
            ReaderProgramming = ReaderProgramming,
            Macro = Macro,
            StructuredAppend = StructuredAppend
        };
    }
}
