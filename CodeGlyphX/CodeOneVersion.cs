namespace CodeGlyphX;

/// <summary>The fixed Code One symbol sizes.</summary>
public enum CodeOneVersion {
    /// <summary>Selects the smallest version A through H that holds the data.</summary>
    Auto = 0,
    /// <summary>16 rows by 18 columns.</summary>
    A = 1,
    /// <summary>22 rows by 22 columns.</summary>
    B = 2,
    /// <summary>28 rows by 32 columns.</summary>
    C = 3,
    /// <summary>40 rows by 42 columns.</summary>
    D = 4,
    /// <summary>52 rows by 54 columns.</summary>
    E = 5,
    /// <summary>70 rows by 76 columns.</summary>
    F = 6,
    /// <summary>104 rows by 98 columns.</summary>
    G = 7,
    /// <summary>148 rows by 134 columns.</summary>
    H = 8,
    /// <summary>8 rows by 11 columns; an integer of up to six digits.</summary>
    S10 = 9,
    /// <summary>8 rows by 21 columns; an integer of up to twelve digits.</summary>
    S20 = 10,
    /// <summary>8 rows by 31 columns; an integer of up to eighteen digits.</summary>
    S30 = 11,
    /// <summary>16 rows by 17 columns.</summary>
    T16 = 12,
    /// <summary>16 rows by 33 columns.</summary>
    T32 = 13,
    /// <summary>16 rows by 49 columns.</summary>
    T48 = 14
}
