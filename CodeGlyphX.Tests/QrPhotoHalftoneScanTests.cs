using CodeGlyphX.Rendering.Art;
using CodeGlyphX.Rendering.Png;
using Xunit;
using Xunit.Abstractions;

namespace CodeGlyphX.Tests;

/// <summary>
/// Strict scan matrix for <see cref="QrImageCompositionStyle.PhotoHalftone"/>: every screen, in photo colour and duotone,
/// with short and long payloads and with and without a logo, must decode with strict ZXing (no TryHarder, no rotation)
/// and the native decoder at 1024, 512 and 320 px, plain, rotated by two degrees and blurred. The long payload is version 11,
/// so 320 px gives about 4.6 px per module. Below about 4.5 px per module, blurred captures may not read.
/// </summary>
[Collection("ImageScannerSerial")]
public sealed class QrPhotoHalftoneScanTests {
    private const string ShortPayload = "https://evotec.xyz";
    private const string LongPayload = "https://evotec.xyz/products/codeglyphx/docs/getting-started?utm_source=print&utm_medium=flyer&utm_campaign=autumn-2026&ref=gallery-audit";
    private static readonly Lazy<byte[]> Photo = new(() => PngImageEncoder.EncodeRgba32(QrPhotoTreatmentScanTests.Landscape(512), 512, 512));
    private static readonly Lazy<byte[]> Logo = new(() => PngImageEncoder.EncodeRgba32(LogoMark(96), 96, 96));
    private readonly ITestOutputHelper _output;

    public QrPhotoHalftoneScanTests(ITestOutputHelper output) => _output = output;

    public static IEnumerable<object[]> Cases() {
        foreach (var screen in new[] { QrHalftoneScreen.Dots, QrHalftoneScreen.ErrorDiffusion, QrHalftoneScreen.Ordered })
            foreach (var duotone in new[] { false, true })
                foreach (var payload in new[] { "short", "long" })
                    foreach (var logo in new[] { false, true })
                        yield return new object[] { screen, duotone, payload, logo };
    }

    [Theory]
    [MemberData(nameof(Cases))]
    [Trait("Category", "CorpusTiming")]
    public void PhotoHalftone_DecodesWithStrictZxingAndLibrary_AfterResizingRotationAndBlur(QrHalftoneScreen screen, bool duotone, string payload, bool logo) {
        var text = payload == "short" ? ShortPayload : LongPayload;
        var options = new QrImageCompositionOptions {
            Style = QrImageCompositionStyle.PhotoHalftone,
            ModuleSize = 20,
            Duotone = duotone ? new QrImageDuotoneOptions() : null,
            Halftone = new QrImageHalftoneOptions { Screen = screen },
            Logo = logo ? new QrImageLogoOptions { Image = Logo.Value } : null,
        };
        var png = QrArt.Compose(text, Photo.Value, options).ToPng();
        var failures = new List<string>();
        var checks = 0;
        foreach (var observation in QrArtWave1ScanTests.ScanImageAt(png, text, 1024, 512, 320)) {
            checks++;
            _output.WriteLine($"{observation.Size}/{observation.Transform}: ZXing={observation.StrictPassed}; library={observation.LibraryPassed}");
            if (!observation.StrictPassed || !observation.LibraryPassed) failures.Add($"{observation.Size}/{observation.Transform}");
        }
        Assert.Equal(9, checks);
        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }

    // A two-colour rounded mark with a transparent surround, so the plate shows around it.
    private static byte[] LogoMark(int side) {
        var pixels = new byte[side * side * 4];
        for (var y = 0; y < side; y++) for (var x = 0; x < side; x++) {
            var u = (x + 0.5) / side - 0.5;
            var v = (y + 0.5) / side - 0.5;
            var r = Math.Sqrt(u * u + v * v);
            var p = (y * side + x) * 4;
            if (r > 0.48) continue;
            var inner = r < 0.22;
            pixels[p] = inner ? (byte)250 : (byte)18;
            pixels[p + 1] = inner ? (byte)200 : (byte)60;
            pixels[p + 2] = inner ? (byte)40 : (byte)120;
            pixels[p + 3] = 255;
        }
        return pixels;
    }
}
