namespace CodeGlyphX.Rendering.Png;

/// <summary>
/// Placement modes for canvas splash blobs.
/// </summary>
public enum QrPngCanvasSplashPlacement {
    /// <summary>
    /// Default behavior: splashes are placed around the QR bounds.
    /// </summary>
    AroundQr,
    /// <summary>
    /// Splashes are placed near the outer canvas edges.
    /// </summary>
    CanvasEdges,
}

