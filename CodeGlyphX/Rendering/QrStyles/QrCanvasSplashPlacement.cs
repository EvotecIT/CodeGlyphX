namespace CodeGlyphX.Rendering;

/// <summary>
/// Placement modes for canvas splash blobs.
/// </summary>
public enum QrCanvasSplashPlacement {
    /// <summary>
    /// Default behavior: splashes are placed around the QR bounds.
    /// </summary>
    AroundQr,
    /// <summary>
    /// Splashes are placed near the outer canvas edges.
    /// </summary>
    CanvasEdges,
}

