namespace CodeGlyphX.Rendering.Art;

/// <summary>How a requested logo is positioned within a QR symbol.</summary>
public enum LogoPlacement {
    /// <summary>Largest pattern-free placement with its center in the central third of the QR core,
    /// nearest the exact center among equally sized candidates, retaining every functional pattern.</summary>
    Auto,
    /// <summary>Centered brand-first placement which may cover the central alignment pattern.
    /// Available through RenderLogo only after native and independent decode verification.</summary>
    CenterOverAlignment
}
