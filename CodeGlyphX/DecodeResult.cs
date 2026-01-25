using System;
using CodeGlyphX.Rendering;

namespace CodeGlyphX;

/// <summary>
/// Represents the outcome of a decode attempt.
/// </summary>
public readonly struct DecodeResult<T> {
    /// <summary>
    /// Gets the decoded value when successful.
    /// </summary>
    public T? Value { get; }

    /// <summary>
    /// Gets the failure reason.
    /// </summary>
    public DecodeFailureReason Failure { get; }

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
    public string Message { get; }

    /// <summary>
    /// Gets a value indicating whether decoding succeeded.
    /// </summary>
    public bool IsSuccess => Failure == DecodeFailureReason.None;

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
    public DecodeResult(T value, ImageInfo image, TimeSpan elapsed) {
        Value = value;
        Failure = DecodeFailureReason.None;
        Image = image;
        Elapsed = elapsed;
        Message = string.Empty;
    }

    /// <summary>
    /// Creates a failed decode result.
    /// </summary>
    public DecodeResult(DecodeFailureReason failure, ImageInfo image, TimeSpan elapsed, string? message = null) {
        Value = default;
        Failure = failure;
        Image = image;
        Elapsed = elapsed;
        Message = message ?? DefaultMessage(failure);
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
            _ => "unknown"
        };
    }
}
