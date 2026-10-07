using System;
using CodeGlyphX.Rendering.Gif;
using CodeGlyphX.Rendering.Png;
using CodeGlyphX.Rendering.Webp;

namespace CodeGlyphX.Rendering;

internal static class RenderAnimationHelpers {
    internal static bool TryRenderMatrixWebp(OutputOptions? outputOptions, MatrixPngRenderOptions pngOptions, int quality, out byte[] webp) {
        var frames = outputOptions?.WebpFrames;
        if (frames is null || frames.Length == 0) {
            webp = Array.Empty<byte>();
            return false;
        }

        var durations = outputOptions?.AnimationDurationsMs;
        var animationOptions = outputOptions?.WebpAnimationOptions ?? default;
        if (durations is not null && durations.Length > 0) {
            webp = MatrixWebpRenderer.RenderAnimation(frames, pngOptions, durations, animationOptions, quality);
        } else {
            var duration = outputOptions?.AnimationDurationMs ?? 100;
            webp = MatrixWebpRenderer.RenderAnimation(frames, pngOptions, duration, animationOptions, quality);
        }
        return true;
    }

    internal static bool TryRenderMatrixGif(OutputOptions? outputOptions, MatrixPngRenderOptions pngOptions, out byte[] gif) {
        var frames = outputOptions?.GifFrames;
        if (frames is null || frames.Length == 0) {
            gif = Array.Empty<byte>();
            return false;
        }

        var durations = outputOptions?.AnimationDurationsMs;
        var animationOptions = outputOptions?.GifAnimationOptions ?? default;
        if (durations is not null && durations.Length > 0) {
            gif = MatrixGifRenderer.RenderAnimation(frames, pngOptions, durations, animationOptions);
        } else {
            var duration = outputOptions?.AnimationDurationMs ?? 100;
            gif = MatrixGifRenderer.RenderAnimation(frames, pngOptions, duration, animationOptions);
        }
        return true;
    }

    internal static bool TryRenderBarcodeWebp(OutputOptions? outputOptions, BarcodePngRenderOptions pngOptions, int quality, out byte[] webp) {
        var frames = outputOptions?.BarcodeWebpFrames;
        if (frames is null || frames.Length == 0) {
            webp = Array.Empty<byte>();
            return false;
        }

        var durations = outputOptions?.AnimationDurationsMs;
        var animationOptions = outputOptions?.WebpAnimationOptions ?? default;
        if (durations is not null && durations.Length > 0) {
            webp = BarcodeWebpRenderer.RenderAnimation(frames, pngOptions, durations, animationOptions, quality);
        } else {
            var duration = outputOptions?.AnimationDurationMs ?? 100;
            webp = BarcodeWebpRenderer.RenderAnimation(frames, pngOptions, duration, animationOptions, quality);
        }
        return true;
    }

    internal static bool TryRenderBarcodeGif(OutputOptions? outputOptions, BarcodePngRenderOptions pngOptions, out byte[] gif) {
        var frames = outputOptions?.BarcodeGifFrames;
        if (frames is null || frames.Length == 0) {
            gif = Array.Empty<byte>();
            return false;
        }

        var durations = outputOptions?.AnimationDurationsMs;
        var animationOptions = outputOptions?.GifAnimationOptions ?? default;
        if (durations is not null && durations.Length > 0) {
            gif = BarcodeGifRenderer.RenderAnimation(frames, pngOptions, durations, animationOptions);
        } else {
            var duration = outputOptions?.AnimationDurationMs ?? 100;
            gif = BarcodeGifRenderer.RenderAnimation(frames, pngOptions, duration, animationOptions);
        }
        return true;
    }

    internal static bool TryRenderQrWebp(OutputOptions? outputOptions, QrPngRenderOptions pngOptions, int quality, out byte[] webp) {
        var frames = outputOptions?.WebpFrames;
        if (frames is null || frames.Length == 0) {
            webp = Array.Empty<byte>();
            return false;
        }

        var durations = outputOptions?.AnimationDurationsMs;
        var animationOptions = outputOptions?.WebpAnimationOptions ?? default;
        if (durations is not null && durations.Length > 0) {
            webp = QrWebpRenderer.RenderAnimation(frames, pngOptions, durations, animationOptions, quality);
        } else {
            var duration = outputOptions?.AnimationDurationMs ?? 100;
            webp = QrWebpRenderer.RenderAnimation(frames, pngOptions, duration, animationOptions, quality);
        }
        return true;
    }

    internal static bool TryRenderQrGif(OutputOptions? outputOptions, QrPngRenderOptions pngOptions, out byte[] gif) {
        var frames = outputOptions?.GifFrames;
        if (frames is null || frames.Length == 0) {
            gif = Array.Empty<byte>();
            return false;
        }

        var durations = outputOptions?.AnimationDurationsMs;
        var animationOptions = outputOptions?.GifAnimationOptions ?? default;
        if (durations is not null && durations.Length > 0) {
            gif = QrGifRenderer.RenderAnimation(frames, pngOptions, durations, animationOptions);
        } else {
            var duration = outputOptions?.AnimationDurationMs ?? 100;
            gif = QrGifRenderer.RenderAnimation(frames, pngOptions, duration, animationOptions);
        }
        return true;
    }
}
