using System;
using CodeGlyphX.Rendering;

namespace CodeGlyphX.Benchmarks;

internal static class QrDecodeSampleFactory {
    internal const string DefaultPayload = "https://github.com/EvotecIT/CodeGlyphX";

    public static QrDecodeScenarioData LoadSample(string relativePath) {
        var bytes = RepoFiles.ReadRepoFile(relativePath);
        if (!ImageReader.TryDecodeRgba32(bytes, out var rgba, out var width, out var height)) {
            throw new InvalidOperationException($"Failed to decode sample '{relativePath}'.");
        }
        return new QrDecodeScenarioData(rgba, width, height);
    }

    public static QrDecodeScenarioData BuildResampledGenerated(string payload = DefaultPayload) {
        var data = RenderQrImage(payload, OutputFormat.Png);
        var baseRgba = data.Rgba;
        var baseWidth = data.Width;
        var baseHeight = data.Height;

        var downW = Math.Max(1, (int)Math.Round(baseWidth * 0.62, MidpointRounding.AwayFromZero));
        var downH = Math.Max(1, (int)Math.Round(baseHeight * 0.62, MidpointRounding.AwayFromZero));
        var down = QrDecodeImageOps.ResampleBilinear(baseRgba, baseWidth, baseHeight, downW, downH);

        var upW = Math.Max(1, (int)Math.Round(baseWidth * 1.12, MidpointRounding.AwayFromZero));
        var upH = Math.Max(1, (int)Math.Round(baseHeight * 1.12, MidpointRounding.AwayFromZero));
        var up = QrDecodeImageOps.ResampleBilinear(down, downW, downH, upW, upH);

        return new QrDecodeScenarioData(up, upW, upH);
    }

    public static QrDecodeScenarioData BuildNoisyResampledGenerated(string payload = DefaultPayload, int noiseAmplitude = 12, int noiseSeed = 1337) {
        var data = BuildResampledGenerated(payload);
        QrDecodeImageOps.ApplyDeterministicNoise(data.Rgba, data.Width, data.Height, data.Stride, noiseAmplitude, noiseSeed);
        return data;
    }

    public static QrDecodeScenarioData BuildBlurredGenerated(string payload = DefaultPayload, int blurRadius = 1) {
        var data = RenderQrImage(payload, OutputFormat.Png);
        QrDecodeImageOps.ApplyBoxBlur(data.Rgba, data.Width, data.Height, data.Stride, blurRadius);
        return data;
    }

    public static QrDecodeScenarioData BuildGlareGenerated(string payload = DefaultPayload, byte alphaStart = 8, byte alphaEnd = 64) {
        var data = RenderQrImage(payload, OutputFormat.Png);
        QrDecodeImageOps.ApplyLinearGradientOverlay(data.Rgba, data.Width, data.Height, data.Stride, 255, 255, 255, alphaStart, alphaEnd);
        return data;
    }

    public static QrDecodeScenarioData BuildMotionBlurGenerated(string payload = DefaultPayload, int blurRadius = 6) {
        var data = RenderQrImage(payload, OutputFormat.Png);
        QrDecodeImageOps.ApplyMotionBlurHorizontal(data.Rgba, data.Width, data.Height, data.Stride, blurRadius);
        return data;
    }

    public static QrDecodeScenarioData BuildShearedGenerated(string payload = DefaultPayload, int maxShiftPx = 0) {
        var data = RenderQrImage(payload, OutputFormat.Png);
        var shift = maxShiftPx > 0 ? maxShiftPx : Math.Max(6, data.Width / 14);
        QrDecodeImageOps.ApplyHorizontalShear(data.Rgba, data.Width, data.Height, data.Stride, shift, 255, 255, 255, 255);
        return data;
    }

    public static QrDecodeScenarioData BuildLowContrastGenerated(string payload = DefaultPayload, double factor = 0.55, int bias = 0) {
        var data = RenderQrImage(payload, OutputFormat.Png);
        QrDecodeImageOps.ApplyContrast(data.Rgba, data.Width, data.Height, data.Stride, factor, bias);
        return data;
    }

    public static QrDecodeScenarioData BuildLowContrastGlareGenerated(string payload = DefaultPayload) {
        var data = RenderQrImage(payload, OutputFormat.Png);
        QrDecodeImageOps.ApplyContrast(data.Rgba, data.Width, data.Height, data.Stride, 0.55, 0);
        QrDecodeImageOps.ApplyLinearGradientOverlay(data.Rgba, data.Width, data.Height, data.Stride, 255, 255, 255, 16, 80);
        return data;
    }

    public static QrDecodeScenarioData BuildRotatedGenerated(string payload = DefaultPayload, double degrees = 8.0) {
        var data = RenderQrImage(payload, OutputFormat.Png);
        var rotated = QrDecodeImageOps.RotateBilinear(data.Rgba, data.Width, data.Height, data.Stride, degrees, 255, 255, 255, 255);
        return new QrDecodeScenarioData(rotated, data.Width, data.Height);
    }

    public static QrDecodeScenarioData BuildPartialQuietGenerated(string payload = DefaultPayload, int quietZoneModules = 4, int cropModules = 2) {
        var data = RenderQrImage(payload, OutputFormat.Png, options => options.QuietZone = quietZoneModules);
        var crop = Math.Max(1, cropModules) * 8;
        var maxCrop = Math.Min((data.Width - 1) / 2, (data.Height - 1) / 2);
        if (crop > maxCrop) crop = maxCrop;
        if (crop <= 0) return data;
        var cropped = QrDecodeImageOps.Crop(data.Rgba, data.Width, data.Height, data.Stride, crop, crop, crop, crop);
        return new QrDecodeScenarioData(cropped, data.Width - crop * 2, data.Height - crop * 2);
    }

    public static QrDecodeScenarioData BuildJpegCompressedGenerated(string payload = DefaultPayload, int quality = 60) {
        return RenderQrImage(payload, OutputFormat.Jpeg, options => options.JpegQuality = quality);
    }

    public static QrDecodeScenarioData BuildJpegBlurGenerated(string payload = DefaultPayload, int quality = 35, int blurRadius = 1) {
        var data = BuildJpegCompressedGenerated(payload, quality);
        QrDecodeImageOps.ApplyBoxBlur(data.Rgba, data.Width, data.Height, data.Stride, blurRadius);
        return data;
    }

    public static QrDecodeScenarioData BuildSaltPepperGenerated(string payload = DefaultPayload, double probability = 0.02, int seed = 1337) {
        var data = RenderQrImage(payload, OutputFormat.Png);
        QrDecodeImageOps.ApplySaltPepperNoise(data.Rgba, data.Width, data.Height, data.Stride, probability, seed);
        return data;
    }

    public static QrDecodeScenarioData BuildScreenshotLikeGenerated(string payload = DefaultPayload, int noiseAmplitude = 8, int blurRadius = 1) {
        var data = BuildResampledGenerated(payload);
        QrDecodeImageOps.ApplyBoxBlur(data.Rgba, data.Width, data.Height, data.Stride, blurRadius);
        QrDecodeImageOps.ApplyDeterministicNoise(data.Rgba, data.Width, data.Height, data.Stride, noiseAmplitude, 2025);
        QrDecodeImageOps.ApplyLinearGradientOverlay(data.Rgba, data.Width, data.Height, data.Stride, 255, 255, 255, 6, 36);
        QrDecodeImageOps.ApplyContrast(data.Rgba, data.Width, data.Height, data.Stride, 0.9, 0);
        return data;
    }

    public static QrDecodeScenarioData BuildKeystoneGenerated(string payload = DefaultPayload, double topScale = 0.78, double bottomScale = 1.0) {
        var data = RenderQrImage(payload, OutputFormat.Png);
        var keystone = QrDecodeImageOps.ApplyVerticalKeystone(data.Rgba, data.Width, data.Height, data.Stride, topScale, bottomScale, 255, 255, 255, 255);
        return new QrDecodeScenarioData(keystone, data.Width, data.Height);
    }

    public static QrDecodeScenarioData BuildNoQuietGenerated(string payload = DefaultPayload) {
        return RenderQrImage(payload, OutputFormat.Png, options => {
            options.QuietZone = 0;
            options.ErrorCorrectionLevel = QrErrorCorrectionLevel.H;
        });
    }

    public static QrDecodeScenarioData BuildFancyGenerated(string payload = DefaultPayload) {
        return RenderQrImage(payload, OutputFormat.Png, options => options.Style = QrRenderStyle.Fancy);
    }

    public static QrDecodeScenarioData BuildLongPayloadGenerated(string payload, int targetSizePx = 1400) {
        return RenderQrImage(payload, OutputFormat.Png, options => {
            options.ErrorCorrectionLevel = QrErrorCorrectionLevel.M;
            options.TargetSizePx = targetSizePx;
            options.TargetSizeIncludesQuietZone = true;
            options.QuietZone = 4;
        });
    }

    private static QrDecodeScenarioData RenderQrImage(string payload, OutputFormat format, Action<QrEasyOptions>? configure = null) {
        var options = new QrEasyOptions { ModuleSize = 8 };
        configure?.Invoke(options);
        var data = QrCode.Render(payload, format, options).Data;
        if (!ImageReader.TryDecodeRgba32(data, out var rgba, out var width, out var height)) {
            throw new InvalidOperationException($"Failed to decode generated QR {format} sample.");
        }
        return new QrDecodeScenarioData(rgba, width, height);
    }
}
