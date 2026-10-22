using System;
using System.Collections.Generic;

namespace CodeGlyphX;

/// <summary>Recovered Code One data and its size and interpretation controls.</summary>
public sealed class CodeOneDecoded {
    private readonly byte[] _bytes;
    /// <summary>Gets decoded text, or canonical decimal digits for a version S integer.</summary>
    public string Text { get; }
    /// <summary>Gets a copy of the payload bytes. For version S these are canonical decimal digits.</summary>
    public byte[] Bytes => (byte[])_bytes.Clone();
    /// <summary>Gets the resolved version.</summary>
    public CodeOneVersion Version { get; }
    /// <summary>Gets whether the symbol carries a GS1 FNC1 header.</summary>
    public bool IsGs1 { get; }
    /// <summary>Gets the version S integer, whose encoded representation does not preserve leading zeros.</summary>
    public ulong? NumericValue { get; }
    /// <summary>Gets ECI assignments encountered in payload order.</summary>
    public IReadOnlyList<int> EciAssignments { get; }
    internal CodeOneDecoded(string text, byte[] bytes, CodeOneVersion version, bool isGs1, int[] assignments, ulong? number = null) {
        Text = text; _bytes = (byte[])bytes.Clone(); Version = version; IsGs1 = isGs1;
        EciAssignments = Array.AsReadOnly(assignments); NumericValue = number;
    }
}
