using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using CodeGlyphX.Rendering;

namespace CodeGlyphX.Internal;

internal static class DecodeResultHelpers {
    public delegate bool PixelDecodeString(byte[] rgba, int width, int height, CancellationToken token, out string text);

    public static bool TryGetImageInfo(ReadOnlySpan<byte> image, out ImageInfo info, out bool formatKnown) {
        formatKnown = ImageReader.TryDetectFormat(image, out var format);
        if (formatKnown && ImageReader.TryReadInfo(image, out info)) return true;
        info = formatKnown ? new ImageInfo(format, 0, 0) : default;
        return false;
    }

    public static ImageInfo EnsureDimensions(ImageInfo info, bool formatKnown, int width, int height) {
        if (info.IsValid) return info;
        var format = formatKnown ? info.Format : ImageFormat.Unknown;
        return new ImageInfo(format, width, height);
    }

    public static DecodeFailureReason FailureForImageRead(ReadOnlySpan<byte> image, bool formatKnown, CancellationToken token) {
        if (token.IsCancellationRequested) return DecodeFailureReason.Cancelled;
        if (image.IsEmpty) return DecodeFailureReason.InvalidInput;
        return formatKnown ? DecodeFailureReason.InvalidInput : DecodeFailureReason.UnsupportedFormat;
    }

    public static DecodeFailureReason FailureForDecode(CancellationToken token) {
        return token.IsCancellationRequested ? DecodeFailureReason.Cancelled : DecodeFailureReason.NoResult;
    }

    public static bool TryCheckImageLimits(ReadOnlySpan<byte> image, ImageDecodeOptions? options, out ImageInfo info, out bool formatKnown, out string? message) {
        message = null;
        _ = TryGetImageInfo(image, out info, out formatKnown);

        var maxBytes = options?.MaxBytes ?? 0;
        if (maxBytes <= 0) maxBytes = ImageReader.MaxImageBytes;
        if (maxBytes > 0 && image.Length > maxBytes) {
            message = "image payload exceeds size limits";
            return false;
        }

        var maxPixels = options?.MaxPixels ?? 0;
        if (maxPixels <= 0) maxPixels = ImageReader.MaxPixels;
        if (maxPixels > 0 && info.IsValid) {
            var pixels = (long)info.Width * info.Height;
            if (pixels > maxPixels) {
                message = "image dimensions exceed size limits";
                return false;
            }
        }

        return true;
    }

    public static DecodeResult<string> DecodeImageResult(ReadOnlySpan<byte> image, ImageDecodeOptions? options, CancellationToken cancellationToken, PixelDecodeString decode) {
        var stopwatch = Stopwatch.StartNew();
        if (!TryCheckImageLimits(image, options, out var info, out var formatKnown, out var limitMessage)) {
            return new DecodeResult<string>(DecodeFailureReason.InvalidInput, info, stopwatch.Elapsed, limitMessage);
        }

        var token = ImageDecodeHelper.ApplyBudget(cancellationToken, options, out var budgetCts, out var budgetScope);
        try {
            if (token.IsCancellationRequested) {
                return new DecodeResult<string>(DecodeFailureReason.Cancelled, info, stopwatch.Elapsed);
            }
            if (!ImageReader.TryDecodeRgba32(image, options, out var rgba, out var width, out var height)) {
                var imageFailure = FailureForImageRead(image, formatKnown, token);
                return new DecodeResult<string>(imageFailure, info, stopwatch.Elapsed);
            }

            info = EnsureDimensions(info, formatKnown, width, height);

            if (!ImageDecodeHelper.TryDownscale(ref rgba, ref width, ref height, options, token)) {
                return new DecodeResult<string>(DecodeFailureReason.Cancelled, info, stopwatch.Elapsed);
            }
            if (decode(rgba, width, height, token, out var text)) {
                return new DecodeResult<string>(text, info, stopwatch.Elapsed);
            }
            var failure = FailureForDecode(token);
            return new DecodeResult<string>(failure, info, stopwatch.Elapsed);
        } catch (Exception ex) {
            return new DecodeResult<string>(DecodeFailureReason.Error, info, stopwatch.Elapsed, ex.Message);
        } finally {
            budgetCts?.Dispose();
            budgetScope?.Dispose();
        }
    }

    public static DecodeResult<string> DecodeImageResult(Stream stream, ImageDecodeOptions? options, CancellationToken cancellationToken, PixelDecodeString decode) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (stream is MemoryStream memory && memory.TryGetBuffer(out var buffer)) {
            return DecodeImageResult(buffer.AsSpan(), options, cancellationToken, decode);
        }
        var maxBytes = options?.MaxBytes > 0 ? options.MaxBytes : ImageReader.MaxImageBytes;
        if (!RenderIO.TryReadBinary(stream, maxBytes, out var data)) {
            return new DecodeResult<string>(DecodeFailureReason.InvalidInput, default, TimeSpan.Zero, "image payload exceeds size limits");
        }
        return DecodeImageResult(data, options, cancellationToken, decode);
    }
}
