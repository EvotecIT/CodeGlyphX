namespace CodeGlyphX;

/// <summary>Identifies the two-dimensional component used by a GS1 Composite symbol.</summary>
public enum Gs1CompositeComponent {
    /// <summary>Select the smallest component that can hold the data.</summary>
    Auto = 0,
    /// <summary>Composite Component A, optimized for compact messages.</summary>
    CcA = 1,
    /// <summary>Composite Component B, based on MicroPDF417.</summary>
    CcB = 2,
    /// <summary>Composite Component C, based on PDF417 and available with a GS1-128 carrier.</summary>
    CcC = 3
}
