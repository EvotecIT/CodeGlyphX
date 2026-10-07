using System;

namespace CodeGlyphX;

/// <summary>Encoded Han Xin Code modules and selected symbol metadata.</summary>
public sealed class HanXinSymbol : MatrixSymbol {
    /// <summary>Gets the version from 1 through 84.</summary>
    public int Version { get; }
    /// <summary>Gets the error-correction level from 1 through 4.</summary>
    public int ErrorCorrectionLevel { get; }
    /// <summary>Gets the data mask from 0 through 3.</summary>
    public int Mask { get; }

    internal HanXinSymbol(BitMatrix modules, int version, int errorCorrectionLevel, int mask)
        : base(SymbolFormat.HanXin, modules) {
        Version = version;
        ErrorCorrectionLevel = errorCorrectionLevel;
        Mask = mask;
    }
}
