namespace CodeGlyphX.Bc412;

/// <summary>The BC412 checksum and identifier conventions.</summary>
public enum Bc412Variant {
    /// <summary>SEMI T1 identifiers: 7..18 data characters, a second-position weighted check, and start/stop guards.</summary>
    Semi,
    /// <summary>General IBM BC412 data, with an optional trailing modulo-35 check and optional start/stop guards.</summary>
    Ibm
}
