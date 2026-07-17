namespace CodeGlyphX;

/// <summary>
/// Describes how image rows are ordered in a raw pixel buffer.
/// </summary>
public enum ImageRowOrder {
    /// <summary>
    /// The first row in the buffer is the top row of the image.
    /// </summary>
    TopDown,
    /// <summary>
    /// The first row in the buffer is the bottom row of the image.
    /// </summary>
    BottomUp
}
