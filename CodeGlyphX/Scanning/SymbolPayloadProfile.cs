namespace CodeGlyphX;

/// <summary>
/// Identifies a recognized payload profile layered on a physical symbol format.
/// </summary>
public enum SymbolPayloadProfile {
    /// <summary>No specialized payload profile was identified.</summary>
    None,
    /// <summary>GS1/FNC1 payload semantics were identified.</summary>
    Gs1
}
