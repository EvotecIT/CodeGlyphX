using System;
using System.Threading;
using CodeGlyphX.Rendering.Png;
using CodeGlyphX;

namespace CodeGlyphX.Rendering;

internal static class ImageDecodeHelper {
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

    public static CancellationToken ApplyBudget(CancellationToken cancellationToken, ImageDecodeOptions? options, out CancellationTokenSource? budgetCts, out IDisposable? budgetScope) {
        budgetCts = null;
        budgetScope = null;
        if (options is null || options.MaxMilliseconds <= 0) return cancellationToken;
        budgetScope = Internal.DecodeBudget.Begin(options.MaxMilliseconds);
        if (cancellationToken.CanBeCanceled) {
            budgetCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            budgetCts.CancelAfter(options.MaxMilliseconds);
            return budgetCts.Token;
        }
        budgetCts = new CancellationTokenSource(options.MaxMilliseconds);
        return budgetCts.Token;
    }
}
