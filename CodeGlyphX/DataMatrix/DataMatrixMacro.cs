namespace CodeGlyphX.DataMatrix;

/// <summary>
/// Selects the ISO/IEC 16022 Macro control header emitted before a Data Matrix payload body.
/// </summary>
public enum DataMatrixMacro {
    /// <summary>No Macro control codeword.</summary>
    None = 0,
    /// <summary>Macro 05 (<c>[)&gt; RS 05 GS ... RS EOT</c>).</summary>
    Macro05 = 1,
    /// <summary>Macro 06 (<c>[)&gt; RS 06 GS ... RS EOT</c>).</summary>
    Macro06 = 2
}
