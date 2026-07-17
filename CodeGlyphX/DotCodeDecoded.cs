using System;
using System.Collections.Generic;

namespace CodeGlyphX;

/// <summary>Detailed DotCode payload and control metadata.</summary>
public sealed class DotCodeDecoded {
    private readonly byte[] _bytes;

    /// <summary>Gets the decoded text.</summary>
    public string Text { get; }
    /// <summary>Gets a copy of the decoded payload bytes.</summary>
    public byte[] Bytes => (byte[])_bytes.Clone();
    /// <summary>Gets whether FNC1 separators occurred in the payload.</summary>
    public bool HasFnc1 { get; }
    /// <summary>Gets whether the symbol contains reader initialization.</summary>
    public bool ReaderInitialization { get; }
    /// <summary>Gets the selected data mask from 0 through 3.</summary>
    public int Mask { get; }
    /// <summary>Gets ECI assignments encountered in payload order.</summary>
    public IReadOnlyList<int> EciAssignments { get; }
    /// <summary>Gets the one-based structured-append index.</summary>
    public int? StructuredAppendIndex { get; }
    /// <summary>Gets the structured-append count.</summary>
    public int? StructuredAppendCount { get; }
    /// <summary>Gets the AIM symbology identifier for ordinary DotCode data.</summary>
    public string SymbologyIdentifier => HasFnc1 ? "]J1" : "]J0";

    internal DotCodeDecoded(string text, byte[] bytes, bool hasFnc1, bool readerInitialization, int mask,
        int[] eciAssignments, int? structuredAppendIndex, int? structuredAppendCount) {
        Text = text ?? throw new ArgumentNullException(nameof(text));
        _bytes = (byte[])(bytes ?? throw new ArgumentNullException(nameof(bytes))).Clone();
        HasFnc1 = hasFnc1;
        ReaderInitialization = readerInitialization;
        Mask = mask;
        EciAssignments = Array.AsReadOnly(eciAssignments ?? throw new ArgumentNullException(nameof(eciAssignments)));
        StructuredAppendIndex = structuredAppendIndex;
        StructuredAppendCount = structuredAppendCount;
    }
}
