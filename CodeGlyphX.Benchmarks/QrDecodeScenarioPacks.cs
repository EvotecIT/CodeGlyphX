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
        var idealOptions = IdealOptions(mode);
        var stressOptions = StressOptions(mode);
        var screenshotOptions = ScreenshotOptions(mode);
        var multiOptions = MultiOptions(mode);
        var artOptions = ArtOptions(mode);

        // Ideal pack: common "should always work" cases.
        AddSamples(
            scenarios,
            Ideal,
            idealOptions,
            ("clean-small", "Assets/DecodingSamples/qr-clean-small.png", ExpectedCleanSmall),
            ("clean-large", "Assets/DecodingSamples/qr-clean-large.png", ExpectedCleanLarge),
            ("generator-ui", "Assets/DecodingSamples/qr-generator-ui.png", "https://qrstud.io/qrmnky"),
            ("dot-aa", "Assets/DecodingSamples/qr-dot-aa.png", "DOT-AA"));

        // Stress pack: resampling, noise, quiet-zone removal, longer payloads.
        AddSamples(
            scenarios,
            Stress,
            stressOptions,
            ("noisy-ui", "Assets/DecodingSamples/qr-noisy-ui.png", ExpectedNoisyUi));
        AddGenerated(
            scenarios,
            Stress,
            stressOptions,
            ("resampled-generated", BuildResampledGenerated, GeneratedPayload),
            ("resampled-noisy-generated", BuildNoisyResampledGenerated, GeneratedPayload),
            ("screenshot-like-generated", BuildScreenshotLikeGenerated, GeneratedPayload),
            ("partial-quiet-generated", BuildPartialQuietGenerated, GeneratedPayload),
            ("jpeg-60-generated", BuildJpegCompressedGenerated, GeneratedPayload),
            ("no-quiet-generated", BuildNoQuietGenerated, GeneratedPayload),
            ("long-payload-generated", BuildLongPayloadGenerated, LongPayload));
        if (mode == QrPackMode.Full) {
            AddGenerated(
                scenarios,
                Stress,
                stressOptions,
                ("blurred-generated", BuildBlurredGenerated, GeneratedPayload),
                ("glare-generated", BuildGlareGenerated, GeneratedPayload),
                ("motion-blur-generated", BuildMotionBlurGenerated, GeneratedPayload),
                ("sheared-generated", BuildShearedGenerated, GeneratedPayload),
                ("low-contrast-generated", BuildLowContrastGenerated, GeneratedPayload),
                ("low-contrast-glare-generated", BuildLowContrastGlareGenerated, GeneratedPayload),
                ("rotated-generated", BuildRotatedGenerated, GeneratedPayload),
                ("jpeg-35-generated", BuildJpegCompressedLowGenerated, GeneratedPayload),
                ("keystone-generated", BuildKeystoneGenerated, GeneratedPayload),
                ("jpeg-35-blur-generated", BuildJpegBlurGenerated, GeneratedPayload),
                ("salt-pepper-generated", BuildSaltPepperGenerated, GeneratedPayload),
                ("resampled-noisy-strong-generated", () => QrDecodeSampleFactory.BuildNoisyResampledGenerated(GeneratedPayload, noiseAmplitude: 20, noiseSeed: 4242), GeneratedPayload),
                ("screenshot-like-strong-generated", () => QrDecodeSampleFactory.BuildScreenshotLikeGenerated(GeneratedPayload, noiseAmplitude: 14, blurRadius: 2), GeneratedPayload),
                ("partial-quiet-tight-generated", () => QrDecodeSampleFactory.BuildPartialQuietGenerated(GeneratedPayload, quietZoneModules: 2, cropModules: 3), GeneratedPayload),
                ("low-contrast-strong-generated", () => QrDecodeSampleFactory.BuildLowContrastGenerated(GeneratedPayload, factor: 0.40, bias: 0), GeneratedPayload));
        }

        // Screenshot pack: UI captures with multiple elements.
        AddSamples(
            scenarios,
            Screenshot,
            screenshotOptions,
            ("screenshot-1", "Assets/DecodingSamples/qr-screenshot-1.png", ExpectedCleanSmall),
            ("screenshot-2", "Assets/DecodingSamples/qr-screenshot-2.png", "What did I just tell you? Now we're both disappointed!"),
            ("screenshot-3", "Assets/DecodingSamples/qr-screenshot-3.png", ExpectedCleanSmall));

        // Multi pack: many codes in one image + screenshot-like degradation.
        AddGeneratedMulti(
            scenarios,
            Multi,
            multiOptions,
            ("multi-8-screenshot-like", BuildMultiQrScreenshotLike, MultiPayloads));

        // Art pack: stylized QR art, including the known hard set.
        AddSamples(
            scenarios,
            Art,
            artOptions,
            ("art-dots-variants", "Assets/DecodingSamples/qr-art-dots-variants.png", ExpectedJess3),
            ("art-jess3-grid", "Assets/DecodingSamples/qr-art-jess3-characters-grid.png", ExpectedJess3),
            ("art-jess3-splash", "Assets/DecodingSamples/qr-art-jess3-characters-splash.png", ExpectedJess3),
            ("art-jess3-splash-variant", "Assets/DecodingSamples/qr-art-jess3-characters-splash-variant.png", ExpectedJess3));

        if (mode == QrPackMode.Full) {
            AddSamples(
                scenarios,
                Art,
                artOptions,
                ("art-facebook-splash-grid", "Assets/DecodingSamples/qr-art-facebook-splash-grid.png", ExpectedFacebookJess3),
                ("art-montage-grid", "Assets/DecodingSamples/qr-art-montage-grid.png", ExpectedZip2Montage),
                ("art-stripe-eye-grid", "Assets/DecodingSamples/qr-art-stripe-eye-grid.png", ExpectedJess3),
                ("art-drip-variants", "Assets/DecodingSamples/qr-art-drip-variants.png", ExpectedJess3),
                ("art-solid-bg-grid", "Assets/DecodingSamples/qr-art-solid-bg-grid.png", ExpectedJess3),
                ("art-gear-illustration-grid", "Assets/DecodingSamples/qr-art-gear-illustration-grid.png", ExpectedJess3));
        }

        return scenarios;
    }

    private const string GeneratedPayload = QrDecodeSampleFactory.DefaultPayload;
    private const string ExpectedJess3 = "http://jess3.com"; // NOSONAR
    private const string ExpectedFacebookJess3 = "http://www.facebook.com/JESS3"; // NOSONAR
    private const string ExpectedZip2Montage = "http://zip2.it/brqr#793618522169349375768512169081277855174341298682677390128336508746685343206314554540757831128512012765565340457163802334869373954682682661341296002959959682672725341202682890853371864448104673341141687721219362682984020043955491325341340146702703341956113384000533341141"; // NOSONAR
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

    private static void AddSamples(List<QrDecodeScenario> scenarios, string pack, QrPixelDecodeOptions options, params (string Name, string Path, string Expected)[] items) {
        for (var i = 0; i < items.Length; i++) {
            var item = items[i];
            scenarios.Add(Sample(pack, item.Name, item.Path, options, item.Expected));
        }
    }

    private static void AddGenerated(List<QrDecodeScenario> scenarios, string pack, QrPixelDecodeOptions options, params (string Name, Func<QrDecodeScenarioData> Build, string Expected)[] items) {
        for (var i = 0; i < items.Length; i++) {
            var item = items[i];
            scenarios.Add(Generated(pack, item.Name, item.Build, options, item.Expected));
        }
    }

    private static void AddGeneratedMulti(List<QrDecodeScenario> scenarios, string pack, QrPixelDecodeOptions options, params (string Name, Func<QrDecodeScenarioData> Build, string[] Expected)[] items) {
        for (var i = 0; i < items.Length; i++) {
            var item = items[i];
            scenarios.Add(Generated(pack, item.Name, item.Build, options, item.Expected));
        }
    }

    private static QrPixelDecodeOptions IdealOptions(QrPackMode mode) {
        var quick = mode == QrPackMode.Quick;
        return BuildOptions(
            maxDimension: quick ? 1600 : 1800,
            budgetMs: quick ? 1200 : 3000,
            autoCrop: false,
            stylized: false,
            tileGrid: 0);
    }

    private static QrPixelDecodeOptions StressOptions(QrPackMode mode) {
        var quick = mode == QrPackMode.Quick;
        return BuildOptions(
            maxDimension: 2200,
            budgetMs: quick ? 2200 : 5000,
            autoCrop: true,
            stylized: false,
            tileGrid: 4);
    }

    private static QrPixelDecodeOptions ScreenshotOptions(QrPackMode mode) {
        var quick = mode == QrPackMode.Quick;
        return BuildOptions(
            maxDimension: 3200,
            budgetMs: quick ? 4000 : 12000,
            autoCrop: true,
            stylized: false,
            tileGrid: 6);
    }

    private static QrPixelDecodeOptions MultiOptions(QrPackMode mode) {
        var quick = mode == QrPackMode.Quick;
        return BuildOptions(
            maxDimension: 3600,
            budgetMs: quick ? 5000 : 15000,
            autoCrop: true,
            stylized: false,
            tileGrid: 4);
    }

    private static QrPixelDecodeOptions ArtOptions(QrPackMode mode) {
        var quick = mode == QrPackMode.Quick;
        return BuildOptions(
            maxDimension: 3200,
            budgetMs: quick ? 4500 : 9000,
            autoCrop: true,
            stylized: true,
            tileGrid: 4);
    }

    private static QrPixelDecodeOptions BuildOptions(int maxDimension, int budgetMs, bool autoCrop, bool stylized, int tileGrid) {
        return new QrPixelDecodeOptions {
            Profile = QrDecodeProfile.Robust,
            MaxDimension = maxDimension,
            BudgetMilliseconds = budgetMs,
            AutoCrop = autoCrop,
            AggressiveSampling = true,
            StylizedSampling = stylized,
            EnableTileScan = true,
            TileGrid = tileGrid
        };
    }

    private static QrDecodeScenarioData BuildResampledGenerated()
        => QrDecodeSampleFactory.BuildResampledGenerated(GeneratedPayload);

    private static QrDecodeScenarioData BuildNoisyResampledGenerated()
        => QrDecodeSampleFactory.BuildNoisyResampledGenerated(GeneratedPayload);

    private static QrDecodeScenarioData BuildScreenshotLikeGenerated()
        => QrDecodeSampleFactory.BuildScreenshotLikeGenerated(GeneratedPayload);

    private static QrDecodeScenarioData BuildBlurredGenerated()
        => QrDecodeSampleFactory.BuildBlurredGenerated(GeneratedPayload);

    private static QrDecodeScenarioData BuildGlareGenerated()
        => QrDecodeSampleFactory.BuildGlareGenerated(GeneratedPayload);

    private static QrDecodeScenarioData BuildMotionBlurGenerated()
        => QrDecodeSampleFactory.BuildMotionBlurGenerated(GeneratedPayload);

    private static QrDecodeScenarioData BuildShearedGenerated()
        => QrDecodeSampleFactory.BuildShearedGenerated(GeneratedPayload);

    private static QrDecodeScenarioData BuildLowContrastGenerated()
        => QrDecodeSampleFactory.BuildLowContrastGenerated(GeneratedPayload);

    private static QrDecodeScenarioData BuildLowContrastGlareGenerated()
        => QrDecodeSampleFactory.BuildLowContrastGlareGenerated(GeneratedPayload);

    private static QrDecodeScenarioData BuildRotatedGenerated()
        => QrDecodeSampleFactory.BuildRotatedGenerated(GeneratedPayload);

    private static QrDecodeScenarioData BuildPartialQuietGenerated()
        => QrDecodeSampleFactory.BuildPartialQuietGenerated(GeneratedPayload);

    private static QrDecodeScenarioData BuildJpegCompressedGenerated()
        => QrDecodeSampleFactory.BuildJpegCompressedGenerated(GeneratedPayload, 60);

    private static QrDecodeScenarioData BuildJpegCompressedLowGenerated()
        => QrDecodeSampleFactory.BuildJpegCompressedGenerated(GeneratedPayload, 35);

    private static QrDecodeScenarioData BuildKeystoneGenerated()
        => QrDecodeSampleFactory.BuildKeystoneGenerated(GeneratedPayload);

    private static QrDecodeScenarioData BuildJpegBlurGenerated()
        => QrDecodeSampleFactory.BuildJpegBlurGenerated(GeneratedPayload);

    private static QrDecodeScenarioData BuildSaltPepperGenerated()
        => QrDecodeSampleFactory.BuildSaltPepperGenerated(GeneratedPayload);

    private static QrDecodeScenarioData BuildNoQuietGenerated()
        => QrDecodeSampleFactory.BuildNoQuietGenerated(GeneratedPayload);

    private static QrDecodeScenarioData BuildLongPayloadGenerated()
        => QrDecodeSampleFactory.BuildLongPayloadGenerated(LongPayload);

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
