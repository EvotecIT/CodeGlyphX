using CodeGlyphX.Rendering.Jpeg;

namespace CodeGlyphX;

/// <summary>
/// Options for decoding from image sources (non-QR).
/// Use <see cref="Guarded"/>/<see cref="Strict"/> or set explicit limits for untrusted inputs.
/// </summary>
public sealed partial class ImageDecodeOptions {
    /// <summary>
    /// Maximum output dimension, in pixels, for single-image decoding and symbol recognition.
    /// Raster codecs validate the original image against <see cref="MaxPixels"/> first, then resize
    /// the decoded RGBA output. This setting does not reduce codec memory use. Set to 0 to keep the
    /// original dimensions.
    /// </summary>
    public int MaxDimension { get; set; } = 0;

    /// <summary>
    /// Maximum pixel count allowed for decoding (width * height).
    /// <see langword="null"/> uses <see cref="Rendering.ImageReader.MaxPixels"/>; 0 disables this limit.
    /// </summary>
    public long? MaxPixels { get; set; }

    /// <summary>
    /// Maximum input size in bytes for decoding.
    /// <see langword="null"/> uses <see cref="Rendering.ImageReader.MaxImageBytes"/>; 0 disables this limit.
    /// </summary>
    public int? MaxBytes { get; set; }

    /// <summary>
    /// Cooperative time budget, in milliseconds, for barcode and matrix recognition after raster
    /// decoding. Image codecs do not use this value. Set to 0 to disable the recognition budget.
    /// </summary>
    public int RecognitionBudgetMilliseconds { get; set; }

    /// <summary>
    /// Maximum animation frame count allowed for decoding.
    /// <see langword="null"/> uses <see cref="Rendering.ImageReader.MaxAnimationFrames"/>; 0 disables this limit.
    /// </summary>
    public int? MaxAnimationFrames { get; set; }

    /// <summary>
    /// Maximum total animation duration, in milliseconds, allowed for decoding.
    /// <see langword="null"/> uses <see cref="Rendering.ImageReader.MaxAnimationDurationMs"/>; 0 disables this limit.
    /// </summary>
    public int? MaxAnimationDurationMs { get; set; }

    /// <summary>
    /// Maximum pixel count allowed per animation frame.
    /// <see langword="null"/> uses <see cref="Rendering.ImageReader.MaxAnimationFramePixels"/>; 0 disables this limit.
    /// </summary>
    public long? MaxAnimationFramePixels { get; set; }

    /// <summary>
    /// Optional JPEG decoding options (chroma upsampling, truncated handling).
    /// </summary>
    public JpegDecodeOptions? JpegOptions { get; set; }

    /// <summary>
    /// Screen preset (budgeted decode for UI capture scenarios).
    /// </summary>
    public static ImageDecodeOptions Screen(int recognitionBudgetMilliseconds = 300, int maxDimension = 1200) {
        return new ImageDecodeOptions {
            RecognitionBudgetMilliseconds = recognitionBudgetMilliseconds < 0 ? 0 : recognitionBudgetMilliseconds,
            MaxDimension = maxDimension < 0 ? 0 : maxDimension
        };
    }

    /// <summary>
    /// Guarded preset for untrusted images (caps bytes, pixels, and animation limits).
    /// </summary>
    public static ImageDecodeOptions Guarded(
        int maxBytes = 64 * 1024 * 1024,
        long maxPixels = 20_000_000,
        int maxAnimationFrames = 120,
        int maxAnimationDurationMs = 60_000,
        long maxAnimationFramePixels = 20_000_000,
        int maxDimension = 0) {
        var resolvedMaxPixels = maxPixels < 0 ? 0 : maxPixels;
        var resolvedMaxAnimationFramePixels = maxAnimationFramePixels < 0 ? 0 : maxAnimationFramePixels;
        return new ImageDecodeOptions {
            MaxBytes = maxBytes < 0 ? 0 : maxBytes,
            MaxPixels = resolvedMaxPixels,
            MaxAnimationFrames = maxAnimationFrames < 0 ? 0 : maxAnimationFrames,
            MaxAnimationDurationMs = maxAnimationDurationMs < 0 ? 0 : maxAnimationDurationMs,
            MaxAnimationFramePixels = resolvedMaxAnimationFramePixels,
            MaxDimension = maxDimension < 0 ? 0 : maxDimension
        };
    }

    /// <summary>
    /// Strict preset for untrusted images (tighter caps for hostile inputs).
    /// </summary>
    public static ImageDecodeOptions Strict(
        int maxBytes = 8 * 1024 * 1024,
        long maxPixels = 8_000_000,
        int maxAnimationFrames = 60,
        int maxAnimationDurationMs = 15_000,
        long maxAnimationFramePixels = 8_000_000,
        int maxDimension = 0) {
        return Guarded(maxBytes, maxPixels, maxAnimationFrames, maxAnimationDurationMs, maxAnimationFramePixels, maxDimension);
    }
}
