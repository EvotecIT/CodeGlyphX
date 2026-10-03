namespace CodeGlyphX.Rendering.Art;

/// <summary>Coordinated finder frames, retaining the seven/five/three module ring proportions.</summary>
public enum QrImageFinderStyle {
    /// <summary>Standard square finder patterns.</summary>
    Square,
    /// <summary>Rounded frames and dots.</summary>
    Rounded,
    /// <summary>Soft square frames and dots.</summary>
    Squircle,
    /// <summary>Circular finder rings and dots, retaining the seven/five/three module diameters.</summary>
    Circular,
    /// <summary>Square finder rings with clipped corners.</summary>
    Chamfered
}
