namespace CodeGlyphX.Printing;

/// <summary>A scoped font supplying matching measurement and filled contours for barcode text.</summary>
/// <remarks>Implementations must preserve case and must not depend on an installed font with the same name.</remarks>
public interface IBarcodeFont {
    /// <summary>Gets the selected face's display name.</summary>
    string Name { get; }
    /// <summary>Shapes one line at the requested em size, returning independent owned contours and metrics.</summary>
    /// <param name="text">The exact text, without line breaks.</param>
    /// <param name="emSize">The size in the desired output units. Print layout passes millimetres.</param>
    /// <param name="strict">Whether unsupported text must fail instead of using a replacement glyph.</param>
    BarcodeTextShape Shape(string text, double emSize, bool strict = true);
}
