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

internal readonly record struct QrPackInfo(string Name, string Category, string Description, string Guidance);

internal static class QrDecodeScenarioPacks {
    public const string Ideal = "ideal";
    public const string Stress = "stress";
    public const string Art = "art";
    public const string Screenshot = "screenshot";
    public const string Multi = "multi";

    public static readonly string[] AllPacks = { Ideal, Stress, Art, Screenshot, Multi };
    private static readonly IReadOnlyDictionary<string, QrPackInfo> PackInfo = new Dictionary<string, QrPackInfo>(StringComparer.OrdinalIgnoreCase) {
        {
            Ideal,
            new QrPackInfo(
                Ideal,
                "ideal",
                "Clean renders and baseline samples that should always decode.",
                "Expect ~100% decode/expected; failures are regressions.")
        },
        {
            Stress,
            new QrPackInfo(
                Stress,
                "stress/realism",
                "Resampling, noise, glare, missing quiet zone, and long payloads.",
                "Track progress; occasional misses are expected but should trend down.")
        },
        {
            Screenshot,
            new QrPackInfo(
                Screenshot,
                "realism",
                "UI screenshots with surrounding clutter and scaling artifacts.",
                "Decode rate should improve over time; latency is secondary.")
        },
        {
            Multi,
            new QrPackInfo(
                Multi,
                "realism",
                "Multiple codes in one image with screenshot-like degradation.",
                "Expect higher latency; focus on correctness and recall.")
        },
        {
            Art,
            new QrPackInfo(
                Art,
                "stylized",
                "Illustrated/stylized QR art from the hard sample set.",
                "Decode% is a reliability goal; track changes carefully.")
        }
    };

    public static QrPackInfo GetPackInfo(string pack) {
        return PackInfo.TryGetValue(pack, out var info)
            ? info
            : new QrPackInfo(pack, "unknown", string.Empty, string.Empty);
    }

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
        scenarios.Add(Generated(Stress, "resampled-noisy-generated", BuildNoisyResampledGenerated, StressOptions(mode), GeneratedPayload));
        scenarios.Add(Generated(Stress, "screenshot-like-generated", BuildScreenshotLikeGenerated, StressOptions(mode), GeneratedPayload));
        scenarios.Add(Generated(Stress, "partial-quiet-generated", BuildPartialQuietGenerated, StressOptions(mode), GeneratedPayload));
        scenarios.Add(Generated(Stress, "jpeg-60-generated", BuildJpegCompressedGenerated, StressOptions(mode), GeneratedPayload));
        scenarios.Add(Generated(Stress, "no-quiet-generated", BuildNoQuietGenerated, StressOptions(mode), GeneratedPayload));
        scenarios.Add(Generated(Stress, "long-payload-generated", BuildLongPayloadGenerated, StressOptions(mode), LongPayload));
        if (mode == QrPackMode.Full) {
            scenarios.Add(Generated(Stress, "blurred-generated", BuildBlurredGenerated, StressOptions(mode), GeneratedPayload));
            scenarios.Add(Generated(Stress, "glare-generated", BuildGlareGenerated, StressOptions(mode), GeneratedPayload));
            scenarios.Add(Generated(Stress, "motion-blur-generated", BuildMotionBlurGenerated, StressOptions(mode), GeneratedPayload));
            scenarios.Add(Generated(Stress, "sheared-generated", BuildShearedGenerated, StressOptions(mode), GeneratedPayload));
            scenarios.Add(Generated(Stress, "low-contrast-generated", BuildLowContrastGenerated, StressOptions(mode), GeneratedPayload));
            scenarios.Add(Generated(Stress, "low-contrast-glare-generated", BuildLowContrastGlareGenerated, StressOptions(mode), GeneratedPayload));
            scenarios.Add(Generated(Stress, "rotated-generated", BuildRotatedGenerated, StressOptions(mode), GeneratedPayload));
            scenarios.Add(Generated(Stress, "jpeg-35-generated", BuildJpegCompressedLowGenerated, StressOptions(mode), GeneratedPayload));
            scenarios.Add(Generated(Stress, "keystone-generated", BuildKeystoneGenerated, StressOptions(mode), GeneratedPayload));
            scenarios.Add(Generated(Stress, "jpeg-35-blur-generated", BuildJpegBlurGenerated, StressOptions(mode), GeneratedPayload));
            scenarios.Add(Generated(Stress, "salt-pepper-generated", BuildSaltPepperGenerated, StressOptions(mode), GeneratedPayload));
        }

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
            scenarios.Add(Sample(Art, "art-facebook-splash-grid", "Assets/DecodingSamples/qr-art-facebook-splash-grid.png", ArtOptions(mode), ExpectedFacebookJess3));
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
    private const string ExpectedFacebookJess3 = "http://www.facebook.com/JESS3";
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

    private static QrDecodeScenarioData BuildNoisyResampledGenerated() {
        return QrDecodeSampleFactory.BuildNoisyResampledGenerated(GeneratedPayload);
    }

    private static QrDecodeScenarioData BuildScreenshotLikeGenerated() {
        return QrDecodeSampleFactory.BuildScreenshotLikeGenerated(GeneratedPayload);
    }

    private static QrDecodeScenarioData BuildBlurredGenerated() {
        return QrDecodeSampleFactory.BuildBlurredGenerated(GeneratedPayload);
    }

    private static QrDecodeScenarioData BuildGlareGenerated() {
        return QrDecodeSampleFactory.BuildGlareGenerated(GeneratedPayload);
    }

    private static QrDecodeScenarioData BuildMotionBlurGenerated() {
        return QrDecodeSampleFactory.BuildMotionBlurGenerated(GeneratedPayload);
    }

    private static QrDecodeScenarioData BuildShearedGenerated() {
        return QrDecodeSampleFactory.BuildShearedGenerated(GeneratedPayload);
    }

    private static QrDecodeScenarioData BuildLowContrastGenerated() {
        return QrDecodeSampleFactory.BuildLowContrastGenerated(GeneratedPayload);
    }

    private static QrDecodeScenarioData BuildLowContrastGlareGenerated() {
        return QrDecodeSampleFactory.BuildLowContrastGlareGenerated(GeneratedPayload);
    }

    private static QrDecodeScenarioData BuildRotatedGenerated() {
        return QrDecodeSampleFactory.BuildRotatedGenerated(GeneratedPayload);
    }

    private static QrDecodeScenarioData BuildPartialQuietGenerated() {
        return QrDecodeSampleFactory.BuildPartialQuietGenerated(GeneratedPayload);
    }

    private static QrDecodeScenarioData BuildJpegCompressedGenerated() {
        return QrDecodeSampleFactory.BuildJpegCompressedGenerated(GeneratedPayload, 60);
    }

    private static QrDecodeScenarioData BuildJpegCompressedLowGenerated() {
        return QrDecodeSampleFactory.BuildJpegCompressedGenerated(GeneratedPayload, 35);
    }

    private static QrDecodeScenarioData BuildKeystoneGenerated() {
        return QrDecodeSampleFactory.BuildKeystoneGenerated(GeneratedPayload);
    }

    private static QrDecodeScenarioData BuildJpegBlurGenerated() {
        return QrDecodeSampleFactory.BuildJpegBlurGenerated(GeneratedPayload);
    }

    private static QrDecodeScenarioData BuildSaltPepperGenerated() {
        return QrDecodeSampleFactory.BuildSaltPepperGenerated(GeneratedPayload);
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
