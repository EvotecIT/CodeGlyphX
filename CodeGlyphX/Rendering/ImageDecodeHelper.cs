using System;
using System.Threading;
using CodeGlyphX;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering;

internal static class ImageDecodeHelper {
    /// <summary>Consistent diagnostic text for malformed or caller-rejected PNG input.</summary>
    internal const string InvalidPngFailure = "Invalid PNG image or image limit exceeded.";

    /// <summary>
    /// Attempts to decode a PNG raster while honoring caller image limits and preserving non-throwing Try* facade semantics.
    /// </summary>
    public static bool TryDecodePngRgba32(ReadOnlySpan<byte> png, ImageDecodeOptions? options, out byte[] rgba, out int width, out int height) {
        rgba = Array.Empty<byte>();
        width = 0;
        height = 0;
        return ImageReader.TryDetectFormat(png, out var format)
            && format == ImageFormat.Png
            && ImageReader.TryDecodeRgba32(png, options, out rgba, out width, out height);
    }

    public static bool TryDownscale(ref byte[] rgba, ref int width, ref int height, ImageDecodeOptions? options, CancellationToken cancellationToken) {
        if (options is null) return true;
        return TryDownscale(ref rgba, ref width, ref height, options.MaxDimension, cancellationToken);
    }

    public static bool TryDownscale(ref byte[] rgba, ref int width, ref int height, int maxDimension, CancellationToken cancellationToken) {
        if (maxDimension <= 0) return true;
        if (width <= 0 || height <= 0) return true;
        var maxSide = Math.Max(width, height);
        if (maxSide <= maxDimension) return true;
        if (cancellationToken.IsCancellationRequested) return false;

        var scale = maxDimension / (double)maxSide;
        var dstWidth = Math.Max(1, (int)Math.Round(width * scale));
        var dstHeight = Math.Max(1, (int)Math.Round(height * scale));
        if (dstWidth == width && dstHeight == height) return true;

        var scaled = ImageScaler.ResizeToFitBox(
            rgba,
            width,
            height,
            width * 4,
            dstWidth,
            dstHeight,
            Rgba32.White,
            preserveAspectRatio: false);

        rgba = scaled;
        width = dstWidth;
        height = dstHeight;
        return true;
    }

    public static RecognitionBudgetScope BeginRecognitionBudget(CancellationToken cancellationToken, ImageDecodeOptions? options, out CancellationToken token) {
        return BeginRecognitionBudget(cancellationToken, options?.RecognitionBudgetMilliseconds ?? 0, out token);
    }

    public static RecognitionBudgetScope BeginRecognitionBudget(CancellationToken cancellationToken, int milliseconds, out CancellationToken token) {
        if (milliseconds <= 0) {
            token = cancellationToken;
            return new RecognitionBudgetScope(null, null);
        }

        var decodeScope = Internal.DecodeBudget.Begin(milliseconds);
        try {
            var cancellationSource = cancellationToken.CanBeCanceled
                ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)
                : new CancellationTokenSource();
            cancellationSource.CancelAfter(milliseconds);
            token = cancellationSource.Token;
            return new RecognitionBudgetScope(cancellationSource, decodeScope);
        } catch {
            decodeScope?.Dispose();
            throw;
        }
    }
}

internal readonly struct RecognitionBudgetScope : IDisposable {
    private readonly CancellationTokenSource? _cancellationSource;
    private readonly IDisposable? _decodeScope;

    public RecognitionBudgetScope(CancellationTokenSource? cancellationSource, IDisposable? decodeScope) {
        _cancellationSource = cancellationSource;
        _decodeScope = decodeScope;
    }

    public void Dispose() {
        _cancellationSource?.Dispose();
        _decodeScope?.Dispose();
    }
}
