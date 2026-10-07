using System;
using CodeGlyphX.Rendering.Jpeg;

namespace CodeGlyphX;

/// <summary>
/// Options for decoding from image sources (non-QR).
/// Use <see cref="Guarded"/>/<see cref="Strict"/> or set explicit limits for untrusted inputs.
/// Limit values must be nonnegative; zero explicitly disables a limit and nullable limits inherit when null.
/// </summary>
public sealed partial class ImageDecodeOptions {
    private int _maxDimension;
    private long? _maxPixels;
    private int? _maxBytes;
    private long? _maxDecodedBytes;
    private int _recognitionBudgetMilliseconds;
    private int? _maxAnimationFrames;
    private int? _maxAnimationDurationMs;
    private long? _maxAnimationFramePixels;

    /// <summary>
    /// Maximum output dimension, in pixels, for single-image decoding and symbol recognition.
    /// Raster codecs validate the original image against <see cref="MaxPixels"/> first, then resize
    /// the decoded RGBA output. Symbol recognition uses only that resized output and does not retry
    /// at the original resolution. This setting does not reduce codec memory use. Set to 0 to keep
    /// the original dimensions.
    /// </summary>
    public int MaxDimension {
        get => _maxDimension;
        set {
            ValidateNonNegative(value, nameof(MaxDimension));
            _maxDimension = value;
        }
    }

    /// <summary>
    /// Maximum pixel count allowed for decoding (width * height).
    /// <see langword="null"/> uses <see cref="Rendering.ImageReader.MaxPixels"/>; 0 disables this limit.
    /// </summary>
    public long? MaxPixels {
        get => _maxPixels;
        set {
            ValidateNonNegative(value, nameof(MaxPixels));
            _maxPixels = value;
        }
    }

    /// <summary>
    /// Maximum input size in bytes for decoding.
    /// <see langword="null"/> uses <see cref="Rendering.ImageReader.MaxImageBytes"/>; 0 disables this limit.
    /// </summary>
    public int? MaxBytes {
        get => _maxBytes;
        set {
            ValidateNonNegative(value, nameof(MaxBytes));
            _maxBytes = value;
        }
    }

    /// <summary>
    /// Maximum bytes in an individual decoded pixel or raster working buffer, and in retained
    /// animation frame pixels. This is not a cap on total process memory or cumulative allocations.
    /// Encoded input copies and codec metadata are not included.
    /// Null inherits <see cref="Rendering.ImageReader.MaxDecodedBytes"/>; zero disables this limit.
    /// </summary>
    public long? MaxDecodedBytes {
        get => _maxDecodedBytes;
        set {
            ValidateNonNegative(value, nameof(MaxDecodedBytes));
            _maxDecodedBytes = value;
        }
    }

    /// <summary>
    /// Cooperative time budget, in milliseconds, for barcode and matrix recognition after raster
    /// decoding. Image codecs do not use this value. <see cref="SymbolScanner"/> additionally applies
    /// <see cref="ScanOptions.TimeoutMilliseconds"/> to the complete read, decode, and recognition operation.
    /// Set to 0 to disable the recognition budget.
    /// </summary>
    public int RecognitionBudgetMilliseconds {
        get => _recognitionBudgetMilliseconds;
        set {
            ValidateNonNegative(value, nameof(RecognitionBudgetMilliseconds));
            _recognitionBudgetMilliseconds = value;
        }
    }

    /// <summary>
    /// Maximum animation frame count allowed for decoding.
    /// <see langword="null"/> uses <see cref="Rendering.ImageReader.MaxAnimationFrames"/>; 0 disables this limit.
    /// </summary>
    public int? MaxAnimationFrames {
        get => _maxAnimationFrames;
        set {
            ValidateNonNegative(value, nameof(MaxAnimationFrames));
            _maxAnimationFrames = value;
        }
    }

    /// <summary>
    /// Maximum total animation duration, in milliseconds, allowed for decoding.
    /// <see langword="null"/> uses <see cref="Rendering.ImageReader.MaxAnimationDurationMs"/>; 0 disables this limit.
    /// </summary>
    public int? MaxAnimationDurationMs {
        get => _maxAnimationDurationMs;
        set {
            ValidateNonNegative(value, nameof(MaxAnimationDurationMs));
            _maxAnimationDurationMs = value;
        }
    }

    /// <summary>
    /// Maximum pixel count allowed per animation frame.
    /// <see langword="null"/> uses <see cref="Rendering.ImageReader.MaxAnimationFramePixels"/>; 0 disables this limit.
    /// </summary>
    public long? MaxAnimationFramePixels {
        get => _maxAnimationFramePixels;
        set {
            ValidateNonNegative(value, nameof(MaxAnimationFramePixels));
            _maxAnimationFramePixels = value;
        }
    }

    /// <summary>
    /// Optional JPEG decoding options (chroma upsampling, truncated handling).
    /// </summary>
    public JpegDecodeOptions? JpegOptions { get; set; }

    /// <summary>
    /// Screen preset (budgeted decode for UI capture scenarios).
    /// </summary>
    public static ImageDecodeOptions Screen(int recognitionBudgetMilliseconds = 300, int maxDimension = 1200) {
        ValidateNonNegative(recognitionBudgetMilliseconds, nameof(recognitionBudgetMilliseconds));
        ValidateNonNegative(maxDimension, nameof(maxDimension));
        return new ImageDecodeOptions {
            RecognitionBudgetMilliseconds = recognitionBudgetMilliseconds,
            MaxDimension = maxDimension
        };
    }

    /// <summary>
    /// Guarded preset for untrusted images (caps input bytes, decoded bytes, pixels, and animation limits).
    /// The decoded byte limit is 16 bytes per permitted pixel, capped at 256 MiB; when the pixel
    /// limit is disabled it is 256 MiB. Override <see cref="MaxDecodedBytes"/> for larger legal
    /// codec buffers, such as padded TIFF tiles.
    /// </summary>
    public static ImageDecodeOptions Guarded(
        int maxBytes = 64 * 1024 * 1024,
        long maxPixels = 20_000_000,
        int maxAnimationFrames = 120,
        int maxAnimationDurationMs = 60_000,
        long maxAnimationFramePixels = 20_000_000,
        int maxDimension = 0) {
        ValidateNonNegative(maxBytes, nameof(maxBytes));
        ValidateNonNegative(maxPixels, nameof(maxPixels));
        ValidateNonNegative(maxAnimationFrames, nameof(maxAnimationFrames));
        ValidateNonNegative(maxAnimationDurationMs, nameof(maxAnimationDurationMs));
        ValidateNonNegative(maxAnimationFramePixels, nameof(maxAnimationFramePixels));
        ValidateNonNegative(maxDimension, nameof(maxDimension));
        return new ImageDecodeOptions {
            MaxBytes = maxBytes,
            MaxPixels = maxPixels,
            MaxDecodedBytes = maxPixels > 0
                ? System.Math.Min(maxPixels, Rendering.ImageReader.DefaultMaxDecodedBytes / 16) * 16
                : Rendering.ImageReader.DefaultMaxDecodedBytes,
            MaxAnimationFrames = maxAnimationFrames,
            MaxAnimationDurationMs = maxAnimationDurationMs,
            MaxAnimationFramePixels = maxAnimationFramePixels,
            MaxDimension = maxDimension
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

    private static void ValidateNonNegative(long? value, string parameterName) {
        if (value < 0) throw new ArgumentOutOfRangeException(parameterName, value, "Limit values must be nonnegative; use zero to disable a limit.");
    }
}
