using System;

namespace CodeGlyphX;

/// <summary>Encoded DotCode modules and selected layout metadata.</summary>
public sealed class DotCodeSymbol {
    /// <summary>Gets a clone of the encoded modules.</summary>
    public BitMatrix Modules { get; }
    /// <summary>Gets the selected mask from 0 through 7.</summary>
    public int Mask { get; }
    /// <summary>Gets whether the selected mask forced the orientation corners.</summary>
    public bool HasForcedCorners => Mask >= 4;
    /// <summary>Gets the number of data codewords, including pad codewords.</summary>
    public int DataCodewordCount { get; }
    /// <summary>Gets the number of error-correction codewords.</summary>
    public int ErrorCorrectionCodewordCount { get; }

    internal DotCodeSymbol(BitMatrix modules, int mask, int dataCodewordCount, int errorCorrectionCodewordCount) {
        Modules = modules ?? throw new ArgumentNullException(nameof(modules));
        Mask = mask;
        DataCodewordCount = dataCodewordCount;
        ErrorCorrectionCodewordCount = errorCorrectionCodewordCount;
    }
}
