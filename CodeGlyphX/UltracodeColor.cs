namespace CodeGlyphX;

/// <summary>Logical Ultracode cell colours, independent of a renderer's selected RGB paints.</summary>
public enum UltracodeColor : byte {
    /// <summary>A cell that could not be classified when sampling an image.</summary>
    Unknown = 0,
    /// <summary>White background.</summary>
    White = 1,
    /// <summary>Black structural ink.</summary>
    Black = 2,
    /// <summary>Cyan data ink.</summary>
    Cyan = 3,
    /// <summary>Magenta data ink.</summary>
    Magenta = 4,
    /// <summary>Yellow data ink.</summary>
    Yellow = 5,
    /// <summary>Green data ink.</summary>
    Green = 6
}
