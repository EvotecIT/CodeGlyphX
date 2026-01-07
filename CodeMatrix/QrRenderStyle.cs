namespace CodeMatrix;

/// <summary>
/// Style presets for easy QR rendering.
/// </summary>
public enum QrRenderStyle {
    /// <summary>
    /// Plain, high-contrast QR (recommended).
    /// </summary>
    Default,
    /// <summary>
    /// Rounded modules for a softer look.
    /// </summary>
    Rounded,
    /// <summary>
    /// Decorative style with gradients and eye frames.
    /// </summary>
    Fancy,
}
