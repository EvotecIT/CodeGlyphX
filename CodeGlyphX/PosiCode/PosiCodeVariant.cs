namespace CodeGlyphX.PosiCode;

/// <summary>The two PosiCode gap sizes and their restricted character variants.</summary>
public enum PosiCodeVariant {
    /// <summary>PosiCode A, with one-module minimum gaps.</summary>
    A,
    /// <summary>PosiCode B, with two-module minimum gaps.</summary>
    B,
    /// <summary>Limited PosiCode A: digits, uppercase letters, hyphen and period.</summary>
    LimitedA,
    /// <summary>Limited PosiCode B: digits, uppercase letters, hyphen and period.</summary>
    LimitedB
}
