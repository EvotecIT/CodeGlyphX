using CodeGlyphX.Rendering.Art;
using CodeGlyphX.Rendering.Png;
using Xunit;
using Xunit.Abstractions;
using ZXing;
using ZXing.Common;

namespace CodeGlyphX.Tests;

/// <summary>
/// Strict scan gate for the built-in photo treatments on a full-frame image. Every exported treatment must decode
/// with strict ZXing and the native decoder after independent resizing, a 2 degree rotation and a slight blur.
/// </summary>
[Collection("ImageScannerSerial")]
public sealed class QrPhotoTreatmentScanTests {
    private const string ShortPayload = "https://evotec.xyz";
    private const string LongPayload = "https://evotec.xyz/products/codeglyphx/docs/getting-started?utm_source=print&utm_medium=flyer&utm_campaign=autumn-2026&ref=gallery-audit";
    private static readonly Lazy<byte[]> Photo = new(() => PngImageEncoder.EncodeRgba32(Landscape(512), 512, 512));
    private readonly ITestOutputHelper _output;

    public QrPhotoTreatmentScanTests(ITestOutputHelper output) => _output = output;

    public static IEnumerable<object[]> TreatmentCases() {
        foreach (var treatment in new[] { "ColorModules", "ImageOverlay", "ImageBackground", "Halftone3x3", "Halftone3x3Duotone" })
            foreach (var payload in new[] { "short", "long" })
                foreach (var strength in new[] { 0.75, 1.0 })
                    yield return new object[] { treatment, payload, strength };
    }

    [Theory]
    [MemberData(nameof(TreatmentCases))]
    [Trait("Category", "CorpusTiming")]
    public void PhotoTreatments_DecodeWithStrictZxingAndLibrary_AfterResizingRotationAndBlur(string treatment, string payload, double strength) {
        var text = payload == "short" ? ShortPayload : LongPayload;
        var options = new QrImageCompositionOptions {
            Style = treatment.StartsWith("Halftone", StringComparison.Ordinal)
                ? QrImageCompositionStyle.Halftone3x3 : Enum.Parse<QrImageCompositionStyle>(treatment),
            Strength = strength,
            Duotone = treatment == "Halftone3x3Duotone" ? new QrImageDuotoneOptions() : null
        };
        var composition = QrArt.Compose(text, Photo.Value, options);
        var results = ScanMatrix(composition.GetPixels(), composition.Size, text).ToArray();
        foreach (var result in results) _output.WriteLine(result);
        Assert.Equal(9, results.Length);
        var failures = results.Where(r => !r.EndsWith("ZXing=True; library=True", StringComparison.Ordinal)).ToArray();
        Assert.True(failures.Length == 0, string.Join(Environment.NewLine, failures));
    }

    private static IEnumerable<string> ScanMatrix(byte[] rgba, int side, string expected) {
        foreach (var size in new[] { 1024, 512, 256 }) {
            var resized = ResizeArea(rgba, side, size);
            foreach (var transform in new[] { "plain", "rotate2deg", "blur121" }) {
                var pixels = transform == "rotate2deg" ? Rotate2Degrees(resized, size)
                    : transform == "blur121" ? Blur(resized, size) : resized;
                var reader = new BarcodeReaderGeneric {
                    AutoRotate = false,
                    Options = new DecodingOptions { TryHarder = false, PureBarcode = false, TryInverted = false,
                        PossibleFormats = new[] { BarcodeFormat.QR_CODE } }
                };
                var strict = reader.Decode(new RGBLuminanceSource(pixels, size, size, RGBLuminanceSource.BitmapFormat.RGBA32));
                var decoded = QrDecoder.TryDecode(pixels, size, size, size * 4, PixelFormat.Rgba32, out var library);
                yield return $"{size}/{transform}: ZXing={strict?.Text == expected}; library={decoded && library.Text == expected}";
            }
        }
    }

    // Deterministic full-frame landscape: sky gradient, sun, clouds, two ridges, a lake with reflection and a meadow.
    // Every region reaches the image edge, so edge, finder and quiet-zone effects are judged on real content.
    internal static byte[] Landscape(int size) {
        var pixels = new byte[size * size * 4];
        for (var y = 0; y < size; y++) for (var x = 0; x < size; x++) {
            var (r, g, b) = Scene(x / (double)size, y / (double)size);
            var noise = (Hash(x, y) - 0.5) * 10;
            var p = (y * size + x) * 4;
            pixels[p] = Clamp(r + noise); pixels[p + 1] = Clamp(g + noise); pixels[p + 2] = Clamp(b + noise); pixels[p + 3] = 255;
        }
        return pixels;

        static (double, double, double) Scene(double u, double v) {
            const double horizon = 0.62;
            if (v > horizon) {
                var meadowEdge = 0.80 + 0.06 * Math.Sin(u * 7 + 1.3) - 0.12 * u;
                if (v > meadowEdge) {
                    var t = (v - meadowEdge) / (1 - meadowEdge);
                    double r = 70 + 8 * Math.Sin(u * 40), g = 120 - 30 * t, b = 50;
                    int cx = (int)(u * 70), cy = (int)(v * 70);
                    var flower = Hash(cx, cy);
                    var fx = (cx + 0.2 + 0.6 * Hash(cy, cx)) / 70;
                    var fy = (cy + 0.2 + 0.6 * Hash(cx + 7, cy + 3)) / 70;
                    if (flower > 0.55 && Math.Pow(u - fx, 2) + Math.Pow(v - fy, 2) < Math.Pow(0.0035 + 0.003 * flower, 2)) {
                        var k = (int)(flower * 1000) % 3;
                        (r, g, b) = k == 0 ? (235, 70, 80) : k == 1 ? (250, 210, 60) : (240, 240, 245);
                    }
                    if (Math.Pow((u - 0.18) / 0.07, 2) + Math.Pow((v - 0.74) / 0.16, 2) < 1) (r, g, b) = (28, 62, 38);
                    return (r, g - 10 * t, b);
                }
                var mirrored = horizon - (v - horizon) * 0.9 + 0.006 * Math.Sin(u * 160 + v * 40);
                var (sr, sg, sb) = Scene(u, Math.Min(horizon - 0.001, Math.Max(0.01, mirrored)));
                return (sr * 0.62 + 10, sg * 0.66 + 18, sb * 0.72 + 30);
            }
            var near = horizon - 0.10 - 0.06 * Math.Sin(u * 9 + 0.5) - 0.03 * Math.Sin(u * 23);
            if (v > near) { var t = (v - near) / (horizon - near); return (54 + 20 * t, 72 + 10 * t, 58 + 6 * t); }
            var far = horizon - 0.22 - 0.08 * Math.Sin(u * 5 + 2) - 0.025 * Math.Sin(u * 17 + 1);
            if (v > far) return (112, 104, 150);
            var s = v / far;
            double red = 40 + 210 * s * s, green = 80 + 95 * s * s, blue = 160 - 60 * s * s;
            var d = Math.Sqrt(Math.Pow(u - 0.7, 2) + Math.Pow(v - 0.36, 2));
            if (d < 0.06) return (255, 236, 170);
            var glow = Math.Max(0, 1 - d / 0.25);
            red += 60 * glow; green += 40 * glow; blue += 10 * glow;
            var cloud = Math.Max(0, Math.Sin(u * 11 + v * 3) * Math.Sin(v * 30 + u * 4) - 0.55) * 2.2;
            if (v < 0.3) { red += (245 - red) * cloud; green += (240 - green) * cloud; blue += (245 - blue) * cloud; }
            return (red, green, blue);
        }
        static double Hash(int x, int y) {
            var h = unchecked((uint)(x * 374761393 + y * 668265263));
            h = unchecked((h ^ (h >> 13)) * 1274126177);
            return ((h ^ (h >> 16)) & 0xFFFFFF) / (double)0xFFFFFF;
        }
        static byte Clamp(double value) => (byte)Math.Max(0, Math.Min(255, Math.Round(value)));
    }

    // Independent pixel transforms, matching the wave 1 strict gate; they do not call the production scaler or filters.
    private static byte[] ResizeArea(byte[] source, int side, int size) {
        var target = new byte[size * size * 4];
        var scale = side / (double)size;
        for (var y = 0; y < size; y++) for (var x = 0; x < size; x++) {
            double x0 = x * scale, x1 = (x + 1) * scale, y0 = y * scale, y1 = (y + 1) * scale;
            for (var channel = 0; channel < 3; channel++) {
                var sum = 0.0;
                for (var sy = (int)y0; sy < Math.Ceiling(y1) && sy < side; sy++)
                    for (var sx = (int)x0; sx < Math.Ceiling(x1) && sx < side; sx++) {
                        var weight = (Math.Min(x1, sx + 1) - Math.Max(x0, sx)) * (Math.Min(y1, sy + 1) - Math.Max(y0, sy));
                        sum += source[(sy * side + sx) * 4 + channel] * weight;
                    }
                target[(y * size + x) * 4 + channel] = (byte)Math.Clamp((int)Math.Round(sum / (scale * scale)), 0, 255);
            }
            target[(y * size + x) * 4 + 3] = 255;
        }
        return target;
    }

    private static byte[] Rotate2Degrees(byte[] source, int size) {
        var target = new byte[source.Length];
        var radians = Math.PI / 90;
        var cos = Math.Cos(radians); var sin = Math.Sin(radians); var center = (size - 1) / 2.0;
        for (var y = 0; y < size; y++) for (var x = 0; x < size; x++) {
            var sx = cos * (x - center) + sin * (y - center) + center;
            var sy = -sin * (x - center) + cos * (y - center) + center;
            var ix = (int)Math.Floor(sx); var iy = (int)Math.Floor(sy);
            var fx = sx - ix; var fy = sy - iy;
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
        var target = new byte[source.Length];
        var weights = new[] { 1, 2, 1 };
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
