using System;
using System.Collections.Generic;
using System.Linq;
using CodeGlyphX.Rendering;

namespace CodeGlyphX.Benchmarks;

internal enum QrPackMode {
    Quick,
    Full
}

internal sealed class QrDecodeScenario {
    public QrDecodeScenario(
        string name,
        string pack,
        Func<QrDecodeScenarioData> createData,
        QrPixelDecodeOptions options,
        string[]? expectedTexts = null) {
        Name = name;
        Pack = pack;
        CreateData = createData;
        Options = options;
        ExpectedTexts = expectedTexts;
    }

    public string Name { get; }
    public string Pack { get; }
    public Func<QrDecodeScenarioData> CreateData { get; }
    public QrPixelDecodeOptions Options { get; }
    public string[]? ExpectedTexts { get; }
}

internal sealed class QrDecodeScenarioData {
    public QrDecodeScenarioData(byte[] rgba, int width, int height) {
        Rgba = rgba ?? throw new ArgumentNullException(nameof(rgba));
        Width = width;
        Height = height;
        Stride = checked(width * 4);
    }

    public byte[] Rgba { get; }
    public int Width { get; }
    public int Height { get; }
    public int Stride { get; }
}

internal static class QrDecodeScenarioPacks {
    public const string Ideal = "ideal";
    public const string Stress = "stress";
    public const string Art = "art";
    public const string Screenshot = "screenshot";
    public const string Multi = "multi";

    public static readonly string[] AllPacks = { Ideal, Stress, Art, Screenshot, Multi };

    public static List<QrDecodeScenario> GetScenarios(QrPackMode mode) {
        var scenarios = new List<QrDecodeScenario>(32);

        // Ideal pack: common "should always work" cases.
        scenarios.Add(Sample(Ideal, "clean-small", "Assets/DecodingSamples/qr-clean-small.png", IdealOptions(mode), ExpectedCleanSmall));
        scenarios.Add(Sample(Ideal, "clean-large", "Assets/DecodingSamples/qr-clean-large.png", IdealOptions(mode), ExpectedCleanLarge));
        scenarios.Add(Sample(Ideal, "generator-ui", "Assets/DecodingSamples/qr-generator-ui.png", IdealOptions(mode), "https://qrstud.io/qrmnky"));
        scenarios.Add(Sample(Ideal, "dot-aa", "Assets/DecodingSamples/qr-dot-aa.png", IdealOptions(mode), "DOT-AA"));

        // Stress pack: resampling, noise, quiet-zone removal, longer payloads.
        scenarios.Add(Sample(Stress, "noisy-ui", "Assets/DecodingSamples/qr-noisy-ui.png", StressOptions(mode), ExpectedNoisyUi));
        scenarios.Add(Generated(Stress, "resampled-generated", BuildResampledGenerated, StressOptions(mode), GeneratedPayload));
        scenarios.Add(Generated(Stress, "no-quiet-generated", BuildNoQuietGenerated, StressOptions(mode), GeneratedPayload));
        scenarios.Add(Generated(Stress, "long-payload-generated", BuildLongPayloadGenerated, StressOptions(mode), LongPayload));

        // Screenshot pack: UI captures with multiple elements.
        scenarios.Add(Sample(Screenshot, "screenshot-1", "Assets/DecodingSamples/qr-screenshot-1.png", ScreenshotOptions(mode), ExpectedCleanSmall));
        scenarios.Add(Sample(Screenshot, "screenshot-2", "Assets/DecodingSamples/qr-screenshot-2.png", ScreenshotOptions(mode), "What did I just tell you? Now we're both disappointed!"));
        scenarios.Add(Sample(Screenshot, "screenshot-3", "Assets/DecodingSamples/qr-screenshot-3.png", ScreenshotOptions(mode), ExpectedCleanSmall));

        // Multi pack: many codes in one image + screenshot-like degradation.
        scenarios.Add(Generated(Multi, "multi-8-screenshot-like", BuildMultiQrScreenshotLike, MultiOptions(mode), MultiPayloads));

        // Art pack: stylized QR art, including the known hard set.
        scenarios.Add(Sample(Art, "art-dots-variants", "Assets/DecodingSamples/qr-art-dots-variants.png", ArtOptions(mode), ExpectedJess3));
        scenarios.Add(Sample(Art, "art-jess3-grid", "Assets/DecodingSamples/qr-art-jess3-characters-grid.png", ArtOptions(mode), ExpectedJess3));
        scenarios.Add(Sample(Art, "art-jess3-splash", "Assets/DecodingSamples/qr-art-jess3-characters-splash.png", ArtOptions(mode), ExpectedJess3));
        scenarios.Add(Sample(Art, "art-jess3-splash-variant", "Assets/DecodingSamples/qr-art-jess3-characters-splash-variant.png", ArtOptions(mode), ExpectedJess3));

        if (mode == QrPackMode.Full) {
            scenarios.Add(Sample(Art, "art-facebook-splash-grid", "Assets/DecodingSamples/qr-art-facebook-splash-grid.png", ArtOptions(mode), ExpectedJess3));
            scenarios.Add(Sample(Art, "art-montage-grid", "Assets/DecodingSamples/qr-art-montage-grid.png", ArtOptions(mode), ExpectedJess3));
            scenarios.Add(Sample(Art, "art-stripe-eye-grid", "Assets/DecodingSamples/qr-art-stripe-eye-grid.png", ArtOptions(mode), ExpectedJess3));
            scenarios.Add(Sample(Art, "art-drip-variants", "Assets/DecodingSamples/qr-art-drip-variants.png", ArtOptions(mode), ExpectedJess3));
            scenarios.Add(Sample(Art, "art-solid-bg-grid", "Assets/DecodingSamples/qr-art-solid-bg-grid.png", ArtOptions(mode), ExpectedJess3));
            scenarios.Add(Sample(Art, "art-gear-illustration-grid", "Assets/DecodingSamples/qr-art-gear-illustration-grid.png", ArtOptions(mode), ExpectedJess3));
        }

        return scenarios;
    }

    private const string GeneratedPayload = QrDecodeSampleFactory.DefaultPayload;
    private const string ExpectedJess3 = "http://jess3.com";
    private const string ExpectedCleanLarge = "This is a quick test! 123#?";
    private const string ExpectedCleanSmall = "otpauth://totp/Evotec+Services+sp.+z+o.o.%3aprzemyslaw.klys%40evotec.pl?secret=jnll6mrqknd57pmn&issuer=Microsoft";
    private const string ExpectedNoisyUi = "otpauth://totp/Evotec+Services+sp.+z+o.o.%3aprzemyslaw.klys%40evotec.pl?secret=pqhjwcgzncvzykhd&issuer=Microsoft";
    private static readonly string LongPayload = BuildLongPayload();
    private static readonly string[] MultiPayloads = Enumerable.Range(1, 8).Select(i => $"SHOT-{i}").ToArray();

    private static QrDecodeScenario Sample(string pack, string name, string relativePath, QrPixelDecodeOptions options, string expectedText) {
        return new QrDecodeScenario(
            name,
            pack,
            () => QrDecodeSampleFactory.LoadSample(relativePath),
            options,
            new[] { expectedText });
    }

    private static QrDecodeScenario Generated(string pack, string name, Func<QrDecodeScenarioData> build, QrPixelDecodeOptions options, string expectedText) {
        return new QrDecodeScenario(
            name,
            pack,
            build,
            options,
            new[] { expectedText });
    }

    private static QrDecodeScenario Generated(string pack, string name, Func<QrDecodeScenarioData> build, QrPixelDecodeOptions options, string[] expectedTexts) {
        return new QrDecodeScenario(
            name,
            pack,
            build,
            options,
            expectedTexts);
    }

    private static QrPixelDecodeOptions IdealOptions(QrPackMode mode) {
        var quick = mode == QrPackMode.Quick;
        return new QrPixelDecodeOptions {
            Profile = QrDecodeProfile.Robust,
            MaxDimension = quick ? 1600 : 1800,
            BudgetMilliseconds = quick ? 1200 : 3000,
            AggressiveSampling = true,
            StylizedSampling = false,
            EnableTileScan = true
        };
    }

    private static QrPixelDecodeOptions StressOptions(QrPackMode mode) {
        var quick = mode == QrPackMode.Quick;
        return new QrPixelDecodeOptions {
            Profile = QrDecodeProfile.Robust,
            MaxDimension = 2200,
            BudgetMilliseconds = quick ? 2200 : 5000,
            AutoCrop = true,
            AggressiveSampling = true,
            StylizedSampling = false,
            EnableTileScan = true,
            TileGrid = 4
        };
    }

    private static QrPixelDecodeOptions ScreenshotOptions(QrPackMode mode) {
        var quick = mode == QrPackMode.Quick;
        return new QrPixelDecodeOptions {
            Profile = QrDecodeProfile.Robust,
            MaxDimension = 3200,
            BudgetMilliseconds = quick ? 4000 : 12000,
            AutoCrop = true,
            AggressiveSampling = true,
            StylizedSampling = false,
            EnableTileScan = true,
            TileGrid = 6
        };
    }

    private static QrPixelDecodeOptions MultiOptions(QrPackMode mode) {
        var quick = mode == QrPackMode.Quick;
        return new QrPixelDecodeOptions {
            Profile = QrDecodeProfile.Robust,
            MaxDimension = 3600,
            BudgetMilliseconds = quick ? 5000 : 15000,
            AutoCrop = true,
            AggressiveSampling = true,
            StylizedSampling = false,
            EnableTileScan = true,
            TileGrid = 4
        };
    }

    private static QrPixelDecodeOptions ArtOptions(QrPackMode mode) {
        var quick = mode == QrPackMode.Quick;
        return new QrPixelDecodeOptions {
            Profile = QrDecodeProfile.Robust,
            MaxDimension = 3200,
            BudgetMilliseconds = quick ? 4500 : 9000,
            AutoCrop = true,
            AggressiveSampling = true,
            StylizedSampling = true,
            EnableTileScan = true,
            TileGrid = 4
        };
    }

    private static QrDecodeScenarioData BuildResampledGenerated() {
        return QrDecodeSampleFactory.BuildResampledGenerated(GeneratedPayload);
    }

    private static QrDecodeScenarioData BuildNoQuietGenerated() {
        return QrDecodeSampleFactory.BuildNoQuietGenerated(GeneratedPayload);
    }

    private static QrDecodeScenarioData BuildLongPayloadGenerated() {
        return QrDecodeSampleFactory.BuildLongPayloadGenerated(LongPayload);
    }

    private static QrDecodeScenarioData BuildMultiQrScreenshotLike() {
        var renderOptions = new QrEasyOptions {
            ModuleSize = 14,
            QuietZone = 4,
            ErrorCorrectionLevel = QrErrorCorrectionLevel.H
        };

        var grid = 4;
        var pad = 28;
        var canvas = QrDecodeImageOps.BuildCompositeGrid(MultiPayloads, renderOptions, grid, pad, out var widthPx, out var heightPx, out var stridePx);

        // Simulate UI capture/resampling + light blur/noise.
        var downW = Math.Max(1, (int)Math.Round(widthPx * 0.68, MidpointRounding.AwayFromZero));
        var downH = Math.Max(1, (int)Math.Round(heightPx * 0.68, MidpointRounding.AwayFromZero));
        var down = QrDecodeImageOps.ResampleBilinear(canvas, widthPx, heightPx, downW, downH);

        var upW = downW + 180;
        var upH = downH + 120;
        var up = QrDecodeImageOps.ResampleBilinear(down, downW, downH, upW, upH);

        QrDecodeImageOps.ApplyBoxBlur(up, upW, upH, upW * 4, radius: 1);
        QrDecodeImageOps.ApplyDeterministicNoise(up, upW, upH, upW * 4, amplitude: 8, seed: 4242);

        return new QrDecodeScenarioData(up, upW, upH);
    }

    private static string BuildLongPayload() {
        var baseText = "CodeGlyphX benchmark long payload ";
        return string.Concat(Enumerable.Repeat(baseText, 24)).Trim();
    }
}
