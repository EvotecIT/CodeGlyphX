namespace CodeGlyphX;

/// <summary>Image-processing profile for direct-part-marked symbols.</summary>
public enum DirectPartMarkProfile {
    /// <summary>Tries low-contrast, locally illuminated, and dot-peen variants.</summary>
    Auto,
    /// <summary>Prioritizes locally adaptive contrast for laser or chemical etching.</summary>
    LaserEtch,
    /// <summary>Prioritizes closing and enlarging disconnected dot-peen modules.</summary>
    DotPeen
}
