namespace CodeGlyphX.DataMatrix;

/// <summary>
/// Selects the Data Matrix symbol family used for encoding.
/// </summary>
public enum DataMatrixShape {
    /// <summary>Uses the standard square ECC 200 family. This preserves the historical default.</summary>
    Square = 0,
    /// <summary>Uses any rectangular symbol, including original rectangles and current DMRE models.</summary>
    Rectangular = 1,
    /// <summary>Uses only the six original ISO/IEC 16022 rectangular symbols.</summary>
    OriginalRectangular = 2,
    /// <summary>Uses only ISO/IEC 21471 Data Matrix Rectangular Extension models.</summary>
    Dmre = 3,
    /// <summary>Chooses the smallest-area fitting symbol across square and rectangular families.</summary>
    Any = 4
}
