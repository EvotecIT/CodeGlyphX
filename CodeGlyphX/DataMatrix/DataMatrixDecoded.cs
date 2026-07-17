using System;
using System.Collections.Generic;

namespace CodeGlyphX.DataMatrix;

/// <summary>
/// Detailed result of decoding a Data Matrix symbol.
/// </summary>
public sealed class DataMatrixDecoded {
    /// <summary>Gets the decoded text.</summary>
    public string Text { get; }

    /// <summary>Gets whether FNC1 in first position identified a GS1 payload.</summary>
    public bool IsGs1 { get; }

    /// <summary>Gets structured-append metadata, when present.</summary>
    public DataMatrixStructuredAppend? StructuredAppend { get; }

    /// <summary>Gets whether Reader Programming was declared.</summary>
    public bool ReaderProgramming { get; }

    /// <summary>Gets the Macro 05/06 control mode.</summary>
    public DataMatrixMacro Macro { get; }

    /// <summary>Gets ECI assignments encountered in payload order.</summary>
    public IReadOnlyList<int> EciAssignments { get; }

    /// <summary>Gets the symbol row count.</summary>
    public int Rows { get; }

    /// <summary>Gets the symbol column count.</summary>
    public int Columns { get; }

    /// <summary>Gets whether the symbol belongs to the ISO/IEC 21471 DMRE family.</summary>
    public bool IsDmre { get; }

    internal DataMatrixDecoded(
        string text,
        bool isGs1,
        DataMatrixStructuredAppend? structuredAppend,
        bool readerProgramming,
        DataMatrixMacro macro,
        int[] eciAssignments,
        int rows,
        int columns,
        bool isDmre) {
        Text = text ?? throw new ArgumentNullException(nameof(text));
        IsGs1 = isGs1;
        StructuredAppend = structuredAppend;
        ReaderProgramming = readerProgramming;
        Macro = macro;
        EciAssignments = Array.AsReadOnly(eciAssignments ?? throw new ArgumentNullException(nameof(eciAssignments)));
        Rows = rows;
        Columns = columns;
        IsDmre = isDmre;
    }
}
