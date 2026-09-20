namespace CodeGlyphX.Rendering.Art;

/// <summary>Deterministic decorative treatments, always retaining fixed scan centers.</summary>
public enum QrImageArtStyle {
    /// <summary>Use the configured PNG module silhouette.</summary>
    ModuleShape,
    /// <summary>Fine diagonal hatching, following local image direction.</summary>
    Engraving,
    /// <summary>Round stipples whose diameter responds to image luminance.</summary>
    Halftone,
    /// <summary>Curved strokes following local image gradients.</summary>
    Contours,
    /// <summary>Faceted tiles with varied orientation and beveled edges.</summary>
    Mosaic,
    /// <summary>Leaf silhouettes and fine stems, oriented by image detail.</summary>
    Botanical,
    /// <summary>Continuous curved ribbons joining adjacent modules of equal polarity.</summary>
    Ribbons,
    /// <summary>Interlaced narrow strands joining adjacent modules of equal polarity.</summary>
    Weave
}
