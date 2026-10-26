using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Art;
using Xunit;
using Xunit.Abstractions;
using ZXing;
using ZXing.Common;

namespace CodeGlyphX.Tests;

/// <summary>Strict ZXing and native decoding of every built-in scene and pattern, independently of heuristics.</summary>
[Collection("ImageScannerSerial")]
public sealed class QrArtScenePatternScanTests {
    private const string ShortUrl = "https://evotec.xyz";
    private const string LongUrl = "https://evotec.xyz/products/codeglyphx/docs/getting-started?utm_source=print&utm_medium=flyer&utm_campaign=autumn-2026&ref=gallery-audit";
    private static readonly int[] Sizes = { 1024, 512, 256 };
    private static readonly string[] Transforms = { "plain", "rotate2deg", "blur121" };
    private readonly ITestOutputHelper _output;

    public QrArtScenePatternScanTests(ITestOutputHelper output) => _output = output;

    public static IEnumerable<object[]> SceneCases() =>
        Enum.GetValues<QrSceneStyle>().SelectMany(style => new[] { new object[] { style, ShortUrl }, new object[] { style, LongUrl } });

    public static IEnumerable<object[]> PatternCases() =>
        Enum.GetValues<QrArtPattern>().SelectMany(pattern => new[] { new object[] { pattern, ShortUrl }, new object[] { pattern, LongUrl } });

    // Scenes never resample their code: each delivery size is rendered on its own module grid. A scene needs at
    // least three pixels per module, so the long URL is gated at the sizes it can render (1024 and 512 pixels).
    [Theory]
    [MemberData(nameof(SceneCases))]
    public void Scenes_DecodeWithStrictZxingAndLibrary_AtEachRenderSize(QrSceneStyle style, string payload) {
        foreach (var size in Sizes) {
            var design = QrScenePresets.Create(style, size);
            if (payload == LongUrl && size == 256) {
                Assert.Throws<ArgumentException>(() => QrArt.ComposeScene(payload, design));
                continue;
            }
            var scene = QrArt.ComposeScene(payload, design);
            Assert.True(scene.Image.QrSize / (scene.QrVersion * 4 + 17 + 8) >= 3);
            AssertMatrix(scene.Image.GetPixels(), size, payload, $"{style}");
        }
    }

    // Patterns are rendered natively at 18, 12 and 6 pixels per module (6 is the API minimum), without resampling.
    [Theory]
    [MemberData(nameof(PatternCases))]
    public void Patterns_DecodeWithStrictZxingAndLibrary_AtEachModuleSize(QrArtPattern pattern, string payload) {
        foreach (var module in new[] { 18, 12, 6 }) {
            var result = QrArt.ComposePattern(payload, QrArtPatternPresets.CreatePatternOptions(pattern, 2026), QrArtPatternPresets.CreateCompositionOptions(pattern, module));
            AssertMatrix(result.GetPixels(), result.Size, payload, $"{pattern} m{module}");
        }
    }

    private void AssertMatrix(byte[] pixels, int size, string payload, string name) {
        foreach (var transform in Transforms) {
            var image = transform == "rotate2deg" ? Rotate2Degrees(pixels, size) : transform == "blur121" ? Blur(pixels, size) : pixels;
            var reader = new BarcodeReaderGeneric {
                AutoRotate = false,
                Options = new DecodingOptions { TryHarder = false, PureBarcode = false, TryInverted = false, PossibleFormats = new[] { BarcodeFormat.QR_CODE } }
            };
            var strict = reader.Decode(new RGBLuminanceSource(image, size, size, RGBLuminanceSource.BitmapFormat.RGBA32))?.Text;
            var native = QrDecoder.TryDecode(image, size, size, size * 4, PixelFormat.Rgba32, out var decoded) ? decoded.Text : null;
            _output.WriteLine($"{name} {size}/{transform}: ZXing={strict == payload}; library={native == payload}");
            Assert.Equal(payload, native);
            Assert.Equal(payload, strict);
        }
    }
    // Independent pixel transforms intentionally do not call the production scaler or filters.
    private static byte[] Rotate2Degrees(byte[] source, int size) {
        var target = new byte[source.Length];
        var radians = Math.PI / 90; var cos = Math.Cos(radians); var sin = Math.Sin(radians); var center = (size - 1) / 2.0;
        for (var y = 0; y < size; y++) for (var x = 0; x < size; x++) {
            var sx = cos * (x - center) + sin * (y - center) + center;
            var sy = -sin * (x - center) + cos * (y - center) + center;
            var ix = (int)Math.Floor(sx); var iy = (int)Math.Floor(sy); var fx = sx - ix; var fy = sy - iy;
            for (var c = 0; c < 3; c++) {
                var value = Sample(ix, iy, c) * (1 - fx) * (1 - fy) + Sample(ix + 1, iy, c) * fx * (1 - fy)
                    + Sample(ix, iy + 1, c) * (1 - fx) * fy + Sample(ix + 1, iy + 1, c) * fx * fy;
                target[(y * size + x) * 4 + c] = (byte)Math.Clamp((int)Math.Round(value), 0, 255);
            }
            target[(y * size + x) * 4 + 3] = 255;
        }
        return target;
        byte Sample(int x, int y, int channel) => x < 0 || y < 0 || x >= size || y >= size ? (byte)255 : source[(y * size + x) * 4 + channel];
    }

    private static byte[] Blur(byte[] source, int size) {
        var target = new byte[source.Length]; var weights = new[] { 1, 2, 1 };
        for (var y = 0; y < size; y++) for (var x = 0; x < size; x++) {
            for (var c = 0; c < 3; c++) {
                var sum = 0;
                for (var yy = -1; yy <= 1; yy++) for (var xx = -1; xx <= 1; xx++)
                    sum += source[(Math.Clamp(y + yy, 0, size - 1) * size + Math.Clamp(x + xx, 0, size - 1)) * 4 + c] * weights[yy + 1] * weights[xx + 1];
                target[(y * size + x) * 4 + c] = (byte)((sum + 8) / 16);
            }
            target[(y * size + x) * 4 + 3] = 255;
        }
        return target;
    }
}
