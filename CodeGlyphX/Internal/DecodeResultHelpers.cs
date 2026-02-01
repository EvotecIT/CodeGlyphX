using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Internal;

internal static class DecodeResultHelpers {
    public delegate bool PixelDecodeString(byte[] rgba, int width, int height, CancellationToken token, out string text);
    public delegate bool PixelDecodeWithDiagnostics<TDiag>(byte[] rgba, int width, int height, CancellationToken token, out string text, out TDiag diagnostics);

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

    public static int ResolveMaxBytes(ImageDecodeOptions? options) {
        if (options is not null && options.MaxBytes > 0) return options.MaxBytes;
        return ImageReader.MaxImageBytes;
    }

    public static bool TryReadBinary(string path, ImageDecodeOptions? options, out byte[] data) {
        return RenderIO.TryReadBinary(path, ResolveMaxBytes(options), out data);
    }

    public static bool TryReadBinary(Stream stream, ImageDecodeOptions? options, out byte[] data) {
        return RenderIO.TryReadBinary(stream, ResolveMaxBytes(options), out data);
    }

    public static bool TryReadImageBytes(Stream stream, ImageDecodeOptions? options, CancellationToken cancellationToken, out byte[] data, out bool cancelled) {
        data = Array.Empty<byte>();
        cancelled = false;
        if (cancellationToken.IsCancellationRequested) {
            cancelled = true;
            return false;
        }
        return TryReadBinary(stream, options, out data);
    }

    public static bool TryDecodeImage(ReadOnlySpan<byte> image, ImageDecodeOptions? options, CancellationToken cancellationToken, PixelDecodeString decode, out string text) {
        var token = ImageDecodeHelper.ApplyBudget(cancellationToken, options, out var budgetCts, out var budgetScope);
        try {
            if (token.IsCancellationRequested) { text = string.Empty; return false; }
            if (!ImageReader.TryDecodeRgba32(image, options, out var rgba, out var width, out var height)) { text = string.Empty; return false; }
            if (!ImageDecodeHelper.TryDownscale(ref rgba, ref width, ref height, options, token)) { text = string.Empty; return false; }
            return decode(rgba, width, height, token, out text);
        } finally {
            budgetCts?.Dispose();
            budgetScope?.Dispose();
        }
    }

    public static bool TryDecodeImage(Stream stream, ImageDecodeOptions? options, CancellationToken cancellationToken, PixelDecodeString decode, out string text) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        var token = ImageDecodeHelper.ApplyBudget(cancellationToken, options, out var budgetCts, out var budgetScope);
        try {
            if (token.IsCancellationRequested) { text = string.Empty; return false; }
            if (!TryReadBinary(stream, options, out var data)) { text = string.Empty; return false; }
            if (!ImageReader.TryDecodeRgba32(data, options, out var rgba, out var width, out var height)) { text = string.Empty; return false; }
            if (!ImageDecodeHelper.TryDownscale(ref rgba, ref width, ref height, options, token)) { text = string.Empty; return false; }
            return decode(rgba, width, height, token, out text);
        } finally {
            budgetCts?.Dispose();
            budgetScope?.Dispose();
        }
    }

    public static bool TryDecodePngWithDiagnostics<TDiag>(
        byte[] png,
        ImageDecodeOptions? options,
        CancellationToken cancellationToken,
        string failureInvalid,
        string failureCancelled,
        string failureDownscale,
        string failureNoDecoded,
        PixelDecodeWithDiagnostics<TDiag> decode,
        Func<TDiag> createDiagnostics,
        Func<TDiag, string?> getFailure,
        Action<TDiag, string> setFailure,
        out string text,
        out TDiag diagnostics) {
        diagnostics = createDiagnostics();
        if (png is null) throw new ArgumentNullException(nameof(png));
        var token = ImageDecodeHelper.ApplyBudget(cancellationToken, options, out var budgetCts, out var budgetScope);
        try {
            if (token.IsCancellationRequested) {
                text = string.Empty;
                setFailure(diagnostics, failureCancelled);
                return false;
            }
            if (!TryCheckImageLimits(png, options, out _, out _, out var limitMessage)) {
                text = string.Empty;
                setFailure(diagnostics, limitMessage ?? failureInvalid);
                return false;
            }
            var rgba = PngReader.DecodeRgba32(png, out var width, out var height);
            if (!ImageDecodeHelper.TryDownscale(ref rgba, ref width, ref height, options, token)) {
                text = string.Empty;
                setFailure(diagnostics, token.IsCancellationRequested ? failureCancelled : failureDownscale);
                return false;
            }
            if (decode(rgba, width, height, token, out text, out var diag)) {
                diagnostics = diag;
                return true;
            }
            diagnostics = diag;
            if (getFailure(diagnostics) is null) setFailure(diagnostics, failureNoDecoded);
            text = string.Empty;
            return false;
        } finally {
            budgetCts?.Dispose();
            budgetScope?.Dispose();
        }
    }

    public static bool TryDecodeImageWithDiagnostics<TDiag>(
        byte[] image,
        ImageDecodeOptions? options,
        CancellationToken cancellationToken,
        string failureCancelled,
        string failureDownscale,
        string failureUnsupported,
        string failureNoDecoded,
        PixelDecodeWithDiagnostics<TDiag> decode,
        Func<TDiag> createDiagnostics,
        Func<TDiag, string?> getFailure,
        Action<TDiag, string> setFailure,
        out string text,
        out TDiag diagnostics) {
        diagnostics = createDiagnostics();
        if (image is null) throw new ArgumentNullException(nameof(image));
        var token = ImageDecodeHelper.ApplyBudget(cancellationToken, options, out var budgetCts, out var budgetScope);
        try {
            if (token.IsCancellationRequested) {
                text = string.Empty;
                setFailure(diagnostics, failureCancelled);
                return false;
            }
            if (!ImageReader.TryDecodeRgba32(image, options, out var rgba, out var width, out var height)) {
                text = string.Empty;
                setFailure(diagnostics, failureUnsupported);
                return false;
            }
            if (!ImageDecodeHelper.TryDownscale(ref rgba, ref width, ref height, options, token)) {
                text = string.Empty;
                setFailure(diagnostics, token.IsCancellationRequested ? failureCancelled : failureDownscale);
                return false;
            }
            if (decode(rgba, width, height, token, out text, out var diag)) {
                diagnostics = diag;
                return true;
            }
            diagnostics = diag;
            if (getFailure(diagnostics) is null) setFailure(diagnostics, failureNoDecoded);
            text = string.Empty;
            return false;
        } finally {
            budgetCts?.Dispose();
            budgetScope?.Dispose();
        }
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
