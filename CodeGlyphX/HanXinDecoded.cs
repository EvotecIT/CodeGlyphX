using System;
using System.Collections.Generic;

namespace CodeGlyphX;

/// <summary>Detailed decoded Han Xin Code payload and structural metadata.</summary>
public sealed class HanXinDecoded {
    private readonly byte[] _bytes;
    /// <summary>Gets decoded text.</summary>
    public string Text { get; }
    /// <summary>Gets a copy of decoded bytes.</summary>
    public byte[] Bytes => (byte[])_bytes.Clone();
    /// <summary>Gets the decoded version.</summary>
    public int Version { get; }
    /// <summary>Gets the decoded error-correction level.</summary>
    public int ErrorCorrectionLevel { get; }
    /// <summary>Gets the decoded mask.</summary>
    public int Mask { get; }
    /// <summary>Gets ECI assignments encountered in payload order.</summary>
    public IReadOnlyList<int> EciAssignments { get; }
    /// <summary>Gets the AIM symbology identifier.</summary>
    public string SymbologyIdentifier => "]h0";

    internal HanXinDecoded(string text, byte[] bytes, int version, int errorCorrectionLevel, int mask, int[] eciAssignments) {
        Text = text ?? throw new ArgumentNullException(nameof(text));
        _bytes = (byte[])(bytes ?? throw new ArgumentNullException(nameof(bytes))).Clone();
        Version = version; ErrorCorrectionLevel = errorCorrectionLevel; Mask = mask;
        EciAssignments = Array.AsReadOnly(eciAssignments ?? throw new ArgumentNullException(nameof(eciAssignments)));
    }
}
