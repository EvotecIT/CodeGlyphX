using System;
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
}
