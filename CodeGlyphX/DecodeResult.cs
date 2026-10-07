using System;
using CodeGlyphX.Rendering;

namespace CodeGlyphX;

/// <summary>
/// Represents the outcome of a decode attempt.
/// </summary>
public readonly struct DecodeResult<T> {
    private readonly bool _initialized;
    private readonly DecodeFailureReason _failure;
    private readonly string? _message;

    /// <summary>
    /// Gets the decoded value when successful.
    /// </summary>
    public T? Value { get; }

    /// <summary>
    /// Gets the failure reason, or <see cref="DecodeFailureReason.Uninitialized"/> for a default result.
    /// </summary>
    public DecodeFailureReason Failure => _initialized ? _failure : DecodeFailureReason.Uninitialized;

    /// <summary>
    /// Gets image metadata when available.
    /// </summary>
    public ImageInfo Image { get; }

    /// <summary>
    /// Gets the elapsed decoding time.
    /// </summary>
    public TimeSpan Elapsed { get; }

    /// <summary>
    /// Gets a human-friendly message describing the outcome.
    /// </summary>
    public string Message => _message ?? DefaultMessage(Failure);

    /// <summary>
    /// Gets a value indicating whether decoding succeeded.
    /// </summary>
    public bool IsSuccess => _initialized && _failure == DecodeFailureReason.None;

    /// <summary>
    /// Gets the image format when available.
    /// </summary>
    public ImageFormat Format => Image.Format;

    /// <summary>
    /// Gets the image width when available.
    /// </summary>
    public int Width => Image.Width;

    /// <summary>
    /// Gets the image height when available.
    /// </summary>
    public int Height => Image.Height;

    /// <summary>
    /// Creates a successful decode result.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
    public DecodeResult(T value, ImageInfo image, TimeSpan elapsed) {
        if (value is null) throw new ArgumentNullException(nameof(value));
        Value = value;
        _failure = DecodeFailureReason.None;
        Image = image;
        Elapsed = elapsed;
        _message = string.Empty;
        _initialized = true;
    }

    /// <summary>
    /// Creates a failed decode result.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="failure"/> is None or is not a defined failure reason.</exception>
    public DecodeResult(DecodeFailureReason failure, ImageInfo image, TimeSpan elapsed, string? message = null) {
        if (failure is < DecodeFailureReason.InvalidInput or > DecodeFailureReason.Uninitialized) {
            throw new ArgumentOutOfRangeException(nameof(failure), "A defined failure reason other than None is required.");
        }
        Value = default;
        _failure = failure;
        Image = image;
        Elapsed = elapsed;
        _message = message ?? DefaultMessage(failure);
        _initialized = true;
    }

    private static string DefaultMessage(DecodeFailureReason failure) {
        return failure switch {
            DecodeFailureReason.None => string.Empty,
            DecodeFailureReason.InvalidInput => "invalid input",
            DecodeFailureReason.UnsupportedFormat => "unsupported format",
            DecodeFailureReason.PlatformNotSupported => "platform not supported",
            DecodeFailureReason.Cancelled => "cancelled",
            DecodeFailureReason.NoResult => "no result",
            DecodeFailureReason.Error => "error",
            DecodeFailureReason.Uninitialized => "decode result is uninitialized",
            _ => "unknown"
        };
    }
}
