namespace CodeGlyphX.Rendering;

/// <summary>Silhouettes for the background and optional ring behind a logo.</summary>
public enum QrLogoPlateShape {
    /// <summary>Follow the finder shape, using a rounded square for other silhouettes.</summary>
    MatchEyes,
    /// <summary>Square plate.</summary>
    Square,
    /// <summary>Rounded square with an explicit or automatic radius.</summary>
    RoundedSquare,
    /// <summary>Circular plate.</summary>
    Circle,
    /// <summary>Fourth-order superellipse.</summary>
    Squircle
}
