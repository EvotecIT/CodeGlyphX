using System;

namespace CodeGlyphX.Rendering;

/// <summary>
/// Describes a single RGBA32 animation frame.
/// </summary>
public readonly struct ImageAnimationFrame {
    /// <summary>
    /// Creates an animation frame backed by an RGBA32 buffer.
    /// </summary>
    public ImageAnimationFrame(
        byte[] rgba,
        int width,
        int height,
        int stride,
        int durationMs,
        int x = 0,
        int y = 0) {
        Rgba = rgba ?? throw new ArgumentNullException(nameof(rgba));
        Width = width;
        Height = height;
        Stride = stride;
        DurationMs = durationMs;
        X = x;
        Y = y;
    }

    /// <summary>
    /// RGBA32 pixel buffer.
    /// </summary>
    public byte[] Rgba { get; }

    /// <summary>
    /// Frame width in pixels.
    /// </summary>
    public int Width { get; }

    /// <summary>
    /// Frame height in pixels.
    /// </summary>
    public int Height { get; }

    /// <summary>
    /// Frame stride in bytes.
    /// </summary>
    public int Stride { get; }

    /// <summary>
    /// Frame duration in milliseconds.
    /// </summary>
    public int DurationMs { get; }

    /// <summary>
    /// Frame X offset on the canvas.
    /// </summary>
    public int X { get; }

    /// <summary>
    /// Frame Y offset on the canvas.
    /// </summary>
    public int Y { get; }
}

/// <summary>
/// Options for decoded animations.
/// </summary>
public readonly struct ImageAnimationOptions {
    /// <summary>
    /// Creates animation options.
    /// </summary>
    public ImageAnimationOptions(int loopCount = 0, uint backgroundRgba = 0) {
        LoopCount = loopCount;
        BackgroundRgba = backgroundRgba;
    }

    /// <summary>
    /// Loop count (0 = infinite).
    /// </summary>
    public int LoopCount { get; }

    /// <summary>
    /// Background color in RGBA byte order.
    /// </summary>
    public uint BackgroundRgba { get; }
}
