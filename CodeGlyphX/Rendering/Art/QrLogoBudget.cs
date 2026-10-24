using System;
using System.Collections.Generic;

namespace CodeGlyphX.Rendering.Art;

/// <summary>A conservative centered square logo placement measured in QR modules.
/// The knockout includes padding and counts every touched codeword as an unknown error,
/// including parity codewords. This is a coding budget, not camera or print certification.</summary>
public sealed class QrLogoBudget {
    /// <summary>Whether a logo and its padding fit without covering functional modules.</summary>
    public bool CanPlace => KnockoutSideModules > 0;
    /// <summary>Left and top coordinate of the square knockout, excluding the quiet zone.</summary>
    public int OffsetModules { get; }
    /// <summary>Side of the full knockout, including the padding. Zero means no fit.</summary>
    public int KnockoutSideModules { get; }
    /// <summary>Maximum square logo side, excluding padding. Zero means no fit.</summary>
    public int LogoSideModules => CanPlace ? KnockoutSideModules - PaddingModules * 2 : 0;
    /// <summary>Padding on each side of the logo, in modules.</summary>
    public int PaddingModules { get; }
    /// <summary>Unknown codeword errors correctable in each block, before the reserve.</summary>
    public int CorrectableErrorsPerBlock { get; }
    /// <summary>Codeword errors reserved in each block for damage outside the logo.</summary>
    public int ReservedErrorsPerBlock { get; }
    /// <summary>Worst-case distinct codewords touched in each block, including parity words.
    /// Counts are zero when no placement fits.</summary>
    public IReadOnlyList<int> CoveredCodewordsPerBlock { get; }

    internal QrLogoBudget(int offset, int side, int padding, int correctable, int reserve, int[] covered) {
        OffsetModules = offset;
        KnockoutSideModules = side;
        PaddingModules = padding;
        CorrectableErrorsPerBlock = correctable;
        ReservedErrorsPerBlock = reserve;
        CoveredCodewordsPerBlock = Array.AsReadOnly((int[])covered.Clone());
    }
}
