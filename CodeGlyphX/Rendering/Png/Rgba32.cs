namespace CodeGlyphX.Rendering.Png;

/// <summary>
/// A simple RGBA color value.
/// </summary>
public readonly struct Rgba32 {
    /// <summary>
    /// Gets the red channel.
    /// </summary>
    public byte R { get; }
    /// <summary>
    /// Gets the green channel.
    /// </summary>
    public byte G { get; }
    /// <summary>
    /// Gets the blue channel.
    /// </summary>
    public byte B { get; }
    /// <summary>
    /// Gets the alpha channel.
    /// </summary>
    public byte A { get; }

    /// <summary>
    /// Creates a new <see cref="Rgba32"/>.
    /// </summary>
    public Rgba32(byte r, byte g, byte b, byte a = 255) {
        R = r;
        G = g;
        B = b;
        A = a;
    }

    /// <summary>
    /// Opaque black.
    /// </summary>
    public static Rgba32 Black => new(0, 0, 0, 255);

    /// <summary>
    /// Opaque white.
    /// </summary>
    public static Rgba32 White => new(255, 255, 255, 255);

    /// <summary>
    /// Transparent (alpha = 0).
    /// </summary>
    public static Rgba32 Transparent => new(0, 0, 0, 0);
}
