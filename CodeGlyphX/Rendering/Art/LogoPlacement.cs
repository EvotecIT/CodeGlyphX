namespace CodeGlyphX.Rendering.Art;

/// <summary>How a requested logo is positioned within a QR symbol.</summary>
public enum LogoPlacement {
    /// <summary>Largest pattern-free placement nearest the center, retaining every functional pattern.</summary>
    Auto,
    /// <summary>Centered brand-first placement which may cover the central alignment pattern.
    /// Available through RenderLogo only after native and independent decode verification.</summary>
    CenterOverAlignment
}
