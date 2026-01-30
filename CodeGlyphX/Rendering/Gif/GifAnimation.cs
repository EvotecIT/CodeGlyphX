using System;

namespace CodeGlyphX.Rendering.Gif;

/// <summary>
/// Describes a single RGBA32 frame for animated GIF decoding.
/// </summary>
public readonly struct GifAnimationFrame {
    /// <summary>
    /// Creates a GIF animation frame backed by an RGBA32 buffer.
    /// </summary>
    public GifAnimationFrame(
        byte[] rgba,
        int width,
        int height,
        int stride,
        int durationMs,
        int x = 0,
        int y = 0,
        GifDisposalMethod disposalMethod = GifDisposalMethod.None) {
        Rgba = rgba ?? throw new ArgumentNullException(nameof(rgba));
        Width = width;
        Height = height;
        Stride = stride;
        DurationMs = durationMs;
        X = x;
        Y = y;
        DisposalMethod = disposalMethod;
    }

    /// <summary>
    /// Creates a GIF animation frame backed by an RGBA32 buffer with a convenience disposal flag.
    /// </summary>
    public GifAnimationFrame(
        byte[] rgba,
        int width,
        int height,
        int stride,
        int durationMs,
        bool disposeToBackground)
        : this(rgba, width, height, stride, durationMs, 0, 0, disposeToBackground ? GifDisposalMethod.RestoreBackground : GifDisposalMethod.None) {
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
    /// Disposal method for the frame.
    /// </summary>
    public GifDisposalMethod DisposalMethod { get; }
}

/// <summary>
/// GIF disposal methods.
/// </summary>
public enum GifDisposalMethod {
    /// <summary>
    /// No disposal specified.
    /// </summary>
    None = 0,
    /// <summary>
    /// Do not dispose (keep frame).
    /// </summary>
    DoNotDispose = 1,
    /// <summary>
    /// Restore to background.
    /// </summary>
    RestoreBackground = 2,
    /// <summary>
    /// Restore to previous.
    /// </summary>
    RestorePrevious = 3
}

/// <summary>
/// Options extracted from an animated GIF.
/// </summary>
public readonly struct GifAnimationOptions {
    /// <summary>
    /// Creates animation options.
    /// </summary>
    public GifAnimationOptions(int loopCount, uint backgroundRgba) {
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
