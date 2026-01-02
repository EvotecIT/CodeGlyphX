namespace CodeMatrix;

/// <summary>
/// Supported raw pixel formats for QR decoding.
/// </summary>
public enum PixelFormat {
    /// <summary>
    /// 4 bytes per pixel: B, G, R, A.
    /// </summary>
    Bgra32,
    /// <summary>
    /// 4 bytes per pixel: R, G, B, A.
    /// </summary>
    Rgba32,
}
