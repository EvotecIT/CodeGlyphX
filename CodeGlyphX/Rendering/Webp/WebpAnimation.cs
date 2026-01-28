using System;

namespace CodeGlyphX.Rendering.Webp;

/// <summary>
/// Describes a single RGBA32 frame for animated WebP encoding.
/// </summary>
public readonly struct WebpAnimationFrame {
    /// <summary>
    /// Creates a WebP animation frame backed by an RGBA32 buffer.
    /// </summary>
    public WebpAnimationFrame(
        byte[] rgba,
        int width,
        int height,
        int stride,
        int durationMs,
        int x = 0,
        int y = 0,
        bool blend = true,
        bool disposeToBackground = false) {
        Rgba = rgba ?? throw new ArgumentNullException(nameof(rgba));
        Width = width;
        Height = height;
        Stride = stride;
        DurationMs = durationMs;
        X = x;
        Y = y;
        Blend = blend;
        DisposeToBackground = disposeToBackground;
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

    /// <summary>
    /// Whether to alpha-blend the frame over the canvas.
    /// </summary>
    public bool Blend { get; }

    /// <summary>
    /// Whether to dispose to background after the frame.
    /// </summary>
    public bool DisposeToBackground { get; }
}

/// <summary>
/// Options for animated WebP encoding.
/// </summary>
public readonly struct WebpAnimationOptions {
    /// <summary>
    /// Creates animation options.
    /// </summary>
    public WebpAnimationOptions(int loopCount = 0, uint backgroundBgra = 0) {
        LoopCount = loopCount;
        BackgroundBgra = backgroundBgra;
    }

    /// <summary>
    /// Loop count (0 = infinite).
    /// </summary>
    public int LoopCount { get; }

    /// <summary>
    /// Background color in BGRA byte order.
    /// </summary>
    public uint BackgroundBgra { get; }
}
