using System;
using CodeGlyphX.Rendering.Gif;
using CodeGlyphX.Rendering.Png;
using CodeGlyphX.Rendering.Webp;

namespace CodeGlyphX.Rendering;

internal static class RenderAnimationHelpers {
    internal static bool TryRenderMatrixWebp(RenderExtras? extras, MatrixPngRenderOptions pngOptions, int quality, out byte[] webp) {
        var frames = extras?.WebpFrames;
        if (frames is null || frames.Length == 0) {
            webp = Array.Empty<byte>();
            return false;
        }

        var durations = extras?.AnimationDurationsMs;
        var animationOptions = extras?.WebpAnimationOptions ?? default;
        if (durations is not null && durations.Length > 0) {
            webp = MatrixWebpRenderer.RenderAnimation(frames, pngOptions, durations, animationOptions, quality);
        } else {
            var duration = extras?.AnimationDurationMs ?? 100;
            webp = MatrixWebpRenderer.RenderAnimation(frames, pngOptions, duration, animationOptions, quality);
        }
        return true;
    }

    internal static bool TryRenderMatrixGif(RenderExtras? extras, MatrixPngRenderOptions pngOptions, out byte[] gif) {
        var frames = extras?.GifFrames;
        if (frames is null || frames.Length == 0) {
            gif = Array.Empty<byte>();
            return false;
        }

        var durations = extras?.AnimationDurationsMs;
        var animationOptions = extras?.GifAnimationOptions ?? default;
        if (durations is not null && durations.Length > 0) {
            gif = MatrixGifRenderer.RenderAnimation(frames, pngOptions, durations, animationOptions);
        } else {
            var duration = extras?.AnimationDurationMs ?? 100;
            gif = MatrixGifRenderer.RenderAnimation(frames, pngOptions, duration, animationOptions);
        }
        return true;
    }

    internal static bool TryRenderBarcodeWebp(RenderExtras? extras, BarcodePngRenderOptions pngOptions, int quality, out byte[] webp) {
        var frames = extras?.BarcodeWebpFrames;
        if (frames is null || frames.Length == 0) {
            webp = Array.Empty<byte>();
            return false;
        }

        var durations = extras?.AnimationDurationsMs;
        var animationOptions = extras?.WebpAnimationOptions ?? default;
        if (durations is not null && durations.Length > 0) {
            webp = BarcodeWebpRenderer.RenderAnimation(frames, pngOptions, durations, animationOptions, quality);
        } else {
            var duration = extras?.AnimationDurationMs ?? 100;
            webp = BarcodeWebpRenderer.RenderAnimation(frames, pngOptions, duration, animationOptions, quality);
        }
        return true;
    }

    internal static bool TryRenderBarcodeGif(RenderExtras? extras, BarcodePngRenderOptions pngOptions, out byte[] gif) {
        var frames = extras?.BarcodeGifFrames;
        if (frames is null || frames.Length == 0) {
            gif = Array.Empty<byte>();
            return false;
        }

        var durations = extras?.AnimationDurationsMs;
        var animationOptions = extras?.GifAnimationOptions ?? default;
        if (durations is not null && durations.Length > 0) {
            gif = BarcodeGifRenderer.RenderAnimation(frames, pngOptions, durations, animationOptions);
        } else {
            var duration = extras?.AnimationDurationMs ?? 100;
            gif = BarcodeGifRenderer.RenderAnimation(frames, pngOptions, duration, animationOptions);
        }
        return true;
    }

    internal static bool TryRenderQrWebp(RenderExtras? extras, QrPngRenderOptions pngOptions, int quality, out byte[] webp) {
        var frames = extras?.WebpFrames;
        if (frames is null || frames.Length == 0) {
            webp = Array.Empty<byte>();
            return false;
        }

        var durations = extras?.AnimationDurationsMs;
        var animationOptions = extras?.WebpAnimationOptions ?? default;
        if (durations is not null && durations.Length > 0) {
            webp = QrWebpRenderer.RenderAnimation(frames, pngOptions, durations, animationOptions, quality);
        } else {
            var duration = extras?.AnimationDurationMs ?? 100;
            webp = QrWebpRenderer.RenderAnimation(frames, pngOptions, duration, animationOptions, quality);
        }
        return true;
    }

    internal static bool TryRenderQrGif(RenderExtras? extras, QrPngRenderOptions pngOptions, out byte[] gif) {
        var frames = extras?.GifFrames;
        if (frames is null || frames.Length == 0) {
            gif = Array.Empty<byte>();
            return false;
        }

        var durations = extras?.AnimationDurationsMs;
        var animationOptions = extras?.GifAnimationOptions ?? default;
        if (durations is not null && durations.Length > 0) {
            gif = QrGifRenderer.RenderAnimation(frames, pngOptions, durations, animationOptions);
        } else {
            var duration = extras?.AnimationDurationMs ?? 100;
            gif = QrGifRenderer.RenderAnimation(frames, pngOptions, duration, animationOptions);
        }
        return true;
    }
}
