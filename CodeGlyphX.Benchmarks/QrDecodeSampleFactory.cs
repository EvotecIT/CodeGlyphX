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
        var options = new QrEasyOptions { ModuleSize = 8 };
        var png = QrCode.Render(payload, OutputFormat.Png, options).Data;
        DecodePng(png, out var baseRgba, out var baseWidth, out var baseHeight);

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
        var options = new QrEasyOptions { ModuleSize = 8 };
        var png = QrCode.Render(payload, OutputFormat.Png, options).Data;
        DecodePng(png, out var rgba, out var width, out var height);
        QrDecodeImageOps.ApplyBoxBlur(rgba, width, height, width * 4, blurRadius);
        return new QrDecodeScenarioData(rgba, width, height);
    }

    public static QrDecodeScenarioData BuildGlareGenerated(string payload = DefaultPayload, byte alphaStart = 8, byte alphaEnd = 64) {
        var options = new QrEasyOptions { ModuleSize = 8 };
        var png = QrCode.Render(payload, OutputFormat.Png, options).Data;
        DecodePng(png, out var rgba, out var width, out var height);
        QrDecodeImageOps.ApplyLinearGradientOverlay(rgba, width, height, width * 4, 255, 255, 255, alphaStart, alphaEnd);
        return new QrDecodeScenarioData(rgba, width, height);
    }

    public static QrDecodeScenarioData BuildMotionBlurGenerated(string payload = DefaultPayload, int blurRadius = 6) {
        var options = new QrEasyOptions { ModuleSize = 8 };
        var png = QrCode.Render(payload, OutputFormat.Png, options).Data;
        DecodePng(png, out var rgba, out var width, out var height);
        QrDecodeImageOps.ApplyMotionBlurHorizontal(rgba, width, height, width * 4, blurRadius);
        return new QrDecodeScenarioData(rgba, width, height);
    }

    public static QrDecodeScenarioData BuildShearedGenerated(string payload = DefaultPayload, int maxShiftPx = 0) {
        var options = new QrEasyOptions { ModuleSize = 8 };
        var png = QrCode.Render(payload, OutputFormat.Png, options).Data;
        DecodePng(png, out var rgba, out var width, out var height);
        var shift = maxShiftPx > 0 ? maxShiftPx : Math.Max(6, width / 14);
        QrDecodeImageOps.ApplyHorizontalShear(rgba, width, height, width * 4, shift, 255, 255, 255, 255);
        return new QrDecodeScenarioData(rgba, width, height);
    }

    public static QrDecodeScenarioData BuildLowContrastGenerated(string payload = DefaultPayload, double factor = 0.55, int bias = 0) {
        var options = new QrEasyOptions { ModuleSize = 8 };
        var png = QrCode.Render(payload, OutputFormat.Png, options).Data;
        DecodePng(png, out var rgba, out var width, out var height);
        QrDecodeImageOps.ApplyContrast(rgba, width, height, width * 4, factor, bias);
        return new QrDecodeScenarioData(rgba, width, height);
    }

    public static QrDecodeScenarioData BuildLowContrastGlareGenerated(string payload = DefaultPayload) {
        var options = new QrEasyOptions { ModuleSize = 8 };
        var png = QrCode.Render(payload, OutputFormat.Png, options).Data;
        DecodePng(png, out var rgba, out var width, out var height);
        QrDecodeImageOps.ApplyContrast(rgba, width, height, width * 4, 0.55, 0);
        QrDecodeImageOps.ApplyLinearGradientOverlay(rgba, width, height, width * 4, 255, 255, 255, 16, 80);
        return new QrDecodeScenarioData(rgba, width, height);
    }

    public static QrDecodeScenarioData BuildRotatedGenerated(string payload = DefaultPayload, double degrees = 8.0) {
        var options = new QrEasyOptions { ModuleSize = 8 };
        var png = QrCode.Render(payload, OutputFormat.Png, options).Data;
        DecodePng(png, out var rgba, out var width, out var height);
        var rotated = QrDecodeImageOps.RotateBilinear(rgba, width, height, width * 4, degrees, 255, 255, 255, 255);
        return new QrDecodeScenarioData(rotated, width, height);
    }

    public static QrDecodeScenarioData BuildPartialQuietGenerated(string payload = DefaultPayload, int quietZoneModules = 4, int cropModules = 2) {
        var options = new QrEasyOptions { ModuleSize = 8, QuietZone = quietZoneModules };
        var png = QrCode.Render(payload, OutputFormat.Png, options).Data;
        DecodePng(png, out var rgba, out var width, out var height);
        var crop = Math.Max(1, cropModules) * options.ModuleSize;
        var maxCrop = Math.Min((width - 1) / 2, (height - 1) / 2);
        if (crop > maxCrop) crop = maxCrop;
        if (crop <= 0) return new QrDecodeScenarioData(rgba, width, height);
        var cropped = QrDecodeImageOps.Crop(rgba, width, height, width * 4, crop, crop, crop, crop);
        return new QrDecodeScenarioData(cropped, width - crop * 2, height - crop * 2);
    }

    public static QrDecodeScenarioData BuildJpegCompressedGenerated(string payload = DefaultPayload, int quality = 60) {
        var options = new QrEasyOptions { ModuleSize = 8, JpegQuality = quality };
        var jpeg = QrCode.Render(payload, OutputFormat.Jpeg, options).Data;
        if (!ImageReader.TryDecodeRgba32(jpeg, out var rgba, out var width, out var height)) {
            throw new InvalidOperationException("Failed to decode generated QR JPEG sample.");
        }
        return new QrDecodeScenarioData(rgba, width, height);
    }

    public static QrDecodeScenarioData BuildJpegBlurGenerated(string payload = DefaultPayload, int quality = 35, int blurRadius = 1) {
        var data = BuildJpegCompressedGenerated(payload, quality);
        QrDecodeImageOps.ApplyBoxBlur(data.Rgba, data.Width, data.Height, data.Stride, blurRadius);
        return data;
    }

    public static QrDecodeScenarioData BuildSaltPepperGenerated(string payload = DefaultPayload, double probability = 0.02, int seed = 1337) {
        var options = new QrEasyOptions { ModuleSize = 8 };
        var png = QrCode.Render(payload, OutputFormat.Png, options).Data;
        DecodePng(png, out var rgba, out var width, out var height);
        QrDecodeImageOps.ApplySaltPepperNoise(rgba, width, height, width * 4, probability, seed);
        return new QrDecodeScenarioData(rgba, width, height);
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
        var options = new QrEasyOptions { ModuleSize = 8 };
        var png = QrCode.Render(payload, OutputFormat.Png, options).Data;
        DecodePng(png, out var rgba, out var width, out var height);
        var keystone = QrDecodeImageOps.ApplyVerticalKeystone(rgba, width, height, width * 4, topScale, bottomScale, 255, 255, 255, 255);
        return new QrDecodeScenarioData(keystone, width, height);
    }

    public static QrDecodeScenarioData BuildNoQuietGenerated(string payload = DefaultPayload) {
        var options = new QrEasyOptions { QuietZone = 0, ErrorCorrectionLevel = QrErrorCorrectionLevel.H };
        var png = QrCode.Render(payload, OutputFormat.Png, options).Data;
        DecodePng(png, out var rgba, out var width, out var height);
        return new QrDecodeScenarioData(rgba, width, height);
    }

    public static QrDecodeScenarioData BuildFancyGenerated(string payload = DefaultPayload) {
        var options = new QrEasyOptions { Style = QrRenderStyle.Fancy };
        var png = QrCode.Render(payload, OutputFormat.Png, options).Data;
        DecodePng(png, out var rgba, out var width, out var height);
        return new QrDecodeScenarioData(rgba, width, height);
    }

    public static QrDecodeScenarioData BuildLongPayloadGenerated(string payload, int targetSizePx = 1400) {
        var options = new QrEasyOptions {
            ErrorCorrectionLevel = QrErrorCorrectionLevel.M,
            TargetSizePx = targetSizePx,
            TargetSizeIncludesQuietZone = true,
            QuietZone = 4
        };
        var png = QrCode.Render(payload, OutputFormat.Png, options).Data;
        DecodePng(png, out var rgba, out var width, out var height);
        return new QrDecodeScenarioData(rgba, width, height);
    }

    public static void DecodePng(byte[] png, out byte[] rgba, out int width, out int height) {
        if (!ImageReader.TryDecodeRgba32(png, out rgba, out width, out height)) {
            throw new InvalidOperationException("Failed to decode generated QR PNG sample.");
        }
    }
}
