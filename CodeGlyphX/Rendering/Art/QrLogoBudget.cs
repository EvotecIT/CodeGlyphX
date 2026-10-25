using System;
using System.Collections.Generic;

namespace CodeGlyphX.Rendering.Art;

/// <summary>A square logo placement measured in QR modules.
/// The knockout includes padding and counts every touched codeword as an unknown error,
/// including parity codewords. This is a coding budget, not camera or print certification.</summary>
public sealed class QrLogoBudget {
    /// <summary>Left coordinate of the knockout, excluding the quiet zone.</summary>
    public int XModules { get; }
    /// <summary>Top coordinate of the knockout, excluding the quiet zone.</summary>
    public int YModules { get; }
    /// <summary>Requested placement policy.</summary>
    public LogoPlacement Placement { get; }
    /// <summary>Whether this placement covers the central alignment pattern.</summary>
    public bool CoversAlignment { get; }
    /// <summary>Side of the full knockout, including the padding. Zero means no fit.</summary>
    public int KnockoutSideModules { get; }
    /// <summary>Maximum square logo side, excluding padding. Zero means no fit.</summary>
    public int LogoSideModules => KnockoutSideModules - PaddingModules * 2;
    /// <summary>Padding on each side of the logo, in modules.</summary>
    public int PaddingModules { get; }
    /// <summary>Unknown codeword errors correctable in each block, before the reserve.</summary>
    public int CorrectableErrorsPerBlock { get; }
    /// <summary>Codeword errors reserved in each block for damage outside the logo.</summary>
    public int ReservedErrorsPerBlock { get; }
    /// <summary>Worst-case distinct codewords touched in each block, including parity words.
    /// Counts are zero when no placement fits.</summary>
    public IReadOnlyList<int> CoveredCodewordsPerBlock { get; }
    /// <summary>Number of correction blocks affected by the knockout.</summary>
    public int BlocksTouched { get; }
    /// <summary>Unused correctable errors in each block after the knockout, before the reserve.</summary>
    public IReadOnlyList<int> MarginPerBlock { get; }

    internal QrLogoBudget(int x, int y, int side, int padding, int correctable, int reserve,
        int[] covered, LogoPlacement placement, bool coversAlignment) {
        XModules = x;
        YModules = y;
        Placement = placement;
        CoversAlignment = coversAlignment;
        KnockoutSideModules = side;
        PaddingModules = padding;
        CorrectableErrorsPerBlock = correctable;
        ReservedErrorsPerBlock = reserve;
        CoveredCodewordsPerBlock = Array.AsReadOnly((int[])covered.Clone());
        var margins = new int[covered.Length];
        for (var i = 0; i < margins.Length; i++) {
            margins[i] = correctable - covered[i];
            if (covered[i] > 0) BlocksTouched++;
        }
        MarginPerBlock = Array.AsReadOnly(margins);
    }
}
