using System;
using System.IO;
using System.Threading;
using CodeGlyphX.Rendering;

namespace CodeGlyphX.Internal;

internal static class DecodeResultHelpers {
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

        var maxBytes = Math.Max(0, options?.MaxBytes ?? ImageReader.MaxImageBytes);
        if (maxBytes > 0 && image.Length > maxBytes) {
            message = GuardMessages.ForBytes("image payload exceeds size limits", image.Length, maxBytes);
            return false;
        }

        var maxPixels = Math.Max(0, options?.MaxPixels ?? ImageReader.MaxPixels);
        if (maxPixels > 0 && info.IsValid) {
            var pixels = (long)info.Width * info.Height;
            if (pixels > maxPixels) {
                message = GuardMessages.ForPixels("image dimensions exceed size limits", info.Width, info.Height, pixels, maxPixels);
                return false;
            }
        }

        return true;
    }

    public static int ResolveMaxBytes(ImageDecodeOptions? options) {
        return Math.Max(0, options?.MaxBytes ?? ImageReader.MaxImageBytes);
    }

    public static bool TryReadBinary(string path, ImageDecodeOptions? options, out byte[] data) {
        return RenderIO.TryReadBinary(path, ResolveMaxBytes(options), out data);
    }

    public static bool TryReadBinary(Stream stream, ImageDecodeOptions? options, out byte[] data) {
        return RenderIO.TryReadBinary(stream, ResolveMaxBytes(options), out data);
    }
}
