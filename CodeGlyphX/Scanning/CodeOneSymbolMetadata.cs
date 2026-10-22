using System.Collections.Generic;

namespace CodeGlyphX;

/// <summary>Code One size, application controls and numeric interpretation.</summary>
public sealed class CodeOneSymbolMetadata : SymbolMetadata {
    /// <summary>Gets the resolved version.</summary>
    public CodeOneVersion Version { get; }
    /// <summary>Gets whether the symbol carries a GS1 header.</summary>
    public bool IsGs1 { get; }
    /// <summary>Gets the version S integer, whose leading zeros are not recoverable.</summary>
    public ulong? NumericValue { get; }
    /// <summary>Gets ECI assignments encountered in payload order.</summary>
    public IReadOnlyList<int> EciAssignments { get; }
    internal CodeOneSymbolMetadata(CodeOneDecoded value) {
        Version = value.Version; IsGs1 = value.IsGs1; NumericValue = value.NumericValue;
        var assignments = new int[value.EciAssignments.Count];
        for (var i = 0; i < assignments.Length; i++) assignments[i] = value.EciAssignments[i];
        EciAssignments = System.Array.AsReadOnly(assignments);
    }
}
