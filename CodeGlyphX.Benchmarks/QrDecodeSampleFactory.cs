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
