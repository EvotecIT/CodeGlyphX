using System;
using CodeGlyphX.Qr;
using CodeGlyphX.Rendering.Art;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("ImageScannerSerial")]
public sealed class QrPhotoTreatmentTests {
    private const string Payload = "https://example.com/photo";

    [Theory]
    [InlineData(QrImageCompositionStyle.Halftone3x3, 0.45)]
    [InlineData(QrImageCompositionStyle.Halftone3x3, 0.65)]
    [InlineData(QrImageCompositionStyle.Halftone3x3, 0.85)]
    [InlineData(QrImageCompositionStyle.Halftone3x3, 1.0)]
    public void Halftone_RetainsFunctionalPixelsAndExactScanPayload(QrImageCompositionStyle style, double strength) {
        var qr = QR.Encode(Payload, new QrEncodingOptions { ErrorCorrectionLevel = QrErrorCorrectionLevel.H });
        var source = Artwork(96);
        var result = QrImageComposer.Render(qr, source, 96, 96, new QrImageCompositionOptions {
            Style = style, ModuleSize = 18, Strength = strength, CenterSize = 0.7
        });
        var pixels = result.GetPixels();
        var functions = QrStructureAnalysis.BuildFunctionMask(qr.Version, qr.Size);
        for (var y = 0; y < result.Size; y++) for (var x = 0; x < result.Size; x++) {
            var mx = x / 18 - 4; var my = y / 18 - 4;
            var inside = mx >= 0 && my >= 0 && mx < qr.Size && my < qr.Size;
            var value = pixels[(y * result.Size + x) * 4];
            if (!inside) Assert.Equal(255, value);
            else if (functions[mx, my]) Assert.Equal(qr.Modules[mx, my] ? 0 : 255, value);
            else if (x % 18 >= 6 && x % 18 < 12 && y % 18 >= 6 && y % 18 < 12)
                Assert.Equal(qr.Modules[mx, my] ? 0 : 255, value);
        }
        Assert.True(QrArt.ValidateImage(result.ToPng(), Payload, 3000).AllPassed);
        Assert.Equal(Payload, QrLogoPlacementTests.IndependentDecode(result.ToPng()));
    }

    [Fact]
    public void HalftoneUsesUniformBinarySubCells_AndSourceDetailChangesTheImage() {
        var qr = QR.Encode(Payload);
        var options = new QrImageCompositionOptions { Style = QrImageCompositionStyle.Halftone3x3, ModuleSize = 12 };
        var result = QrImageComposer.Render(qr, Artwork(96), 96, 96, options);
        var pixels = result.GetPixels();
        for (var y = 0; y < result.Size; y += 4) for (var x = 0; x < result.Size; x += 4) {
            var value = pixels[(y * result.Size + x) * 4];
            Assert.True(value == 0 || value == 255);
            for (var dy = 0; dy < 4; dy++) for (var dx = 0; dx < 4; dx++)
                Assert.Equal(value, pixels[((y + dy) * result.Size + x + dx) * 4]);
        }
        Assert.NotEqual(Convert.ToBase64String(result.ToPng()), Convert.ToBase64String(
            QrImageComposer.Render(qr, new byte[] { 255, 255, 255, 255 }, 1, 1, options).ToPng()));
        options.ModuleSize = 7;
        Assert.Throws<ArgumentException>(() => QrImageComposer.Render(qr, Artwork(96), 96, 96, options));
    }

    [Theory]
    [InlineData(0.65)]
    [InlineData(1.0)]
    public void ImageBackground_TintsModulesAndFunctionPatterns_AndDecodes(double strength) {
        var qr = QR.Encode(Payload, new QrEncodingOptions { ErrorCorrectionLevel = QrErrorCorrectionLevel.H });
        const int module = 18;
        var result = QrImageComposer.Render(qr, Artwork(96), 96, 96, new QrImageCompositionOptions {
            Style = QrImageCompositionStyle.ImageBackground, ModuleSize = module, Strength = strength, CenterSize = 0.7
        });
        var pixels = result.GetPixels();
        var functions = QrStructureAnalysis.BuildFunctionMask(qr.Version, qr.Size);
        var band = 20 + 60 * strength;
        for (var y = 0; y < result.Size; y++) for (var x = 0; x < result.Size; x++) {
            var mx = x / module - 4; var my = y / module - 4;
            var p = (y * result.Size + x) * 4;
            if (mx < 0 || my < 0 || mx >= qr.Size || my >= qr.Size) {
                Assert.Equal(255, pixels[p]); Assert.Equal(255, pixels[p + 1]); Assert.Equal(255, pixels[p + 2]);
                continue;
            }
            if (functions[mx, my] || x % module != module / 2 || y % module != module / 2) continue;
            // Data dot centres carry an image tint inside their luminance band instead of pure black or white.
            var luma = Luma(pixels, p);
            if (qr.Modules[mx, my]) Assert.InRange(luma, 0, band + 1);
            else Assert.InRange(luma, 255 - band - 1, 255);
        }
        // The finder ball uses the deep function band; the light gap is lifted image colour.
        Assert.InRange(Luma(pixels, Offset(3.5, 3.5)), 0, 45);
        Assert.InRange(Luma(pixels, Offset(1.5, 3.5)), 160, 255);
        Assert.True(QrArt.ValidateImage(result.ToPng(), Payload, 3000).AllPassed);
        Assert.Equal(Payload, QrLogoPlacementTests.IndependentDecode(result.ToPng()));

        int Offset(double mx, double my) => ((int)((4 + my) * module) * result.Size + (int)((4 + mx) * module)) * 4;
    }

    [Fact]
    public void ImageBackground_ContinuesTheImageUnderReservedZones_WithoutWhiteBlocks() {
        var qr = QR.Encode(Payload, new QrEncodingOptions { ErrorCorrectionLevel = QrErrorCorrectionLevel.H, MinVersion = 7 });
        const int module = 12;
        var green = new byte[] { 90, 160, 60, 255 };
        var result = QrImageComposer.Render(qr, green, 1, 1, new QrImageCompositionOptions {
            Style = QrImageCompositionStyle.ImageBackground, ModuleSize = module, Strength = 0.75
        });
        var pixels = result.GetPixels();
        // The outer corner of each finder separator shows the image itself.
        foreach (var (mx, my) in new[] { (7.95, 7.95), (qr.Size - 7.95, 7.95), (7.95, qr.Size - 7.95) }) {
            var p = Offset(mx, my);
            Assert.InRange(pixels[p] - green[0], -2, 2);
            Assert.InRange(pixels[p + 1] - green[1], -2, 2);
            Assert.InRange(pixels[p + 2] - green[2], -2, 2);
        }
        // Light finder gaps, the separator band and the alignment gap are lifted image colour, not white.
        foreach (var (mx, my) in new[] { (1.5, 3.5), (7.3, 3.5), (qr.Size - 7.3, 3.5), (QrAlignment(qr) - 0.5, QrAlignment(qr) + 0.5) }) {
            var p = Offset(mx, my);
            Assert.True(Luma(pixels, p) >= 160, $"Light function pixel at ({mx},{my}) is not light.");
            Assert.True(pixels[p + 1] > pixels[p + 2] + 15, $"Light function pixel at ({mx},{my}) lost the image hue.");
        }

        int Offset(double mx, double my) => ((int)((4 + my) * module) * result.Size + (int)((4 + mx) * module)) * 4;
        static int QrAlignment(QrCode code) => code.Size - 7;
    }

    [Fact]
    public void HalftoneDuotone_UsesOnlyInkAndPaper_IncludingTheQuietZone() {
        var qr = QR.Encode(Payload, new QrEncodingOptions { ErrorCorrectionLevel = QrErrorCorrectionLevel.H });
        var duotone = new QrImageDuotoneOptions();
        var result = QrImageComposer.Render(qr, Artwork(96), 96, 96, new QrImageCompositionOptions {
            Style = QrImageCompositionStyle.Halftone3x3, ModuleSize = 12, Duotone = duotone
        });
        var pixels = result.GetPixels();
        int ink = 0, paper = 0;
        for (var p = 0; p < pixels.Length; p += 4) {
            if (pixels[p] == duotone.Ink.R && pixels[p + 1] == duotone.Ink.G && pixels[p + 2] == duotone.Ink.B) ink++;
            else if (pixels[p] == duotone.Paper.R && pixels[p + 1] == duotone.Paper.G && pixels[p + 2] == duotone.Paper.B) paper++;
            else Assert.Fail($"Unexpected colour at pixel {p / 4}.");
        }
        Assert.True(ink > 0 && paper > 0);
        Assert.Equal(duotone.Paper.R, pixels[0]);
        Assert.True(QrArt.ValidateImage(result.ToPng(), Payload, 3000).AllPassed);

        Assert.Throws<ArgumentException>(() => QrImageComposer.Render(qr, Artwork(8), 8, 8, new QrImageCompositionOptions {
            Style = QrImageCompositionStyle.Halftone3x3, Duotone = new QrImageDuotoneOptions { Ink = new CodeGlyphX.Rendering.Rgba32(120, 120, 120) }
        }));
        Assert.Throws<ArgumentException>(() => QrImageComposer.Render(qr, Artwork(8), 8, 8, new QrImageCompositionOptions {
            Style = QrImageCompositionStyle.Halftone3x3, Duotone = new QrImageDuotoneOptions { Paper = new CodeGlyphX.Rendering.Rgba32(150, 150, 150) }
        }));
        Assert.Throws<ArgumentException>(() => QrImageComposer.Render(qr, Artwork(8), 8, 8, new QrImageCompositionOptions {
            Style = QrImageCompositionStyle.ImageBackground, Duotone = new QrImageDuotoneOptions()
        }));
    }

    [Fact]
    public void Compose_RaisesShortPayloadsToTheMinimumVersion_AndVerifiesTheResult() {
        const string shortPayload = "https://evotec.xyz";
        var image = CodeGlyphX.Rendering.Png.PngImageEncoder.EncodeRgba32(Artwork(96), 96, 96);
        var smallest = QR.Encode(shortPayload, new QrEncodingOptions { ErrorCorrectionLevel = QrErrorCorrectionLevel.H }).Version;
        Assert.True(smallest < 6);
        foreach (var style in new[] { QrImageCompositionStyle.ColorModules, QrImageCompositionStyle.ImageOverlay,
            QrImageCompositionStyle.ImageBackground, QrImageCompositionStyle.Halftone3x3 }) {
            var options = new QrImageCompositionOptions { Style = style, ModuleSize = 12 };
            var raised = QrArt.Compose(shortPayload, image, options);
            Assert.Equal((6 * 4 + 17 + 8) * 12, raised.Size);
            // Compose has exercised its fixed native guard. Check its exported payload independently;
            // transformed-art recovery belongs to the complete strict photo matrix, not version selection.
            var reader = new ZXing.BarcodeReaderGeneric { AutoRotate = false, Options = new ZXing.Common.DecodingOptions {
                TryHarder = false, TryInverted = false, PossibleFormats = new[] { ZXing.BarcodeFormat.QR_CODE }
            } };
            var decoded = reader.Decode(new ZXing.RGBLuminanceSource(raised.GetPixels(), raised.Size, raised.Size,
                ZXing.RGBLuminanceSource.BitmapFormat.RGBA32));
            Assert.Equal(shortPayload, decoded?.Text);
            options.MinimumVersion = 1;
            Assert.Equal((smallest * 4 + 17 + 8) * 12, QrArt.Compose(shortPayload, image, options).Size);
        }
        // Long payloads keep the larger version they need.
        var longPayload = new string('x', 120);
        var needed = QR.Encode(longPayload, new QrEncodingOptions { ErrorCorrectionLevel = QrErrorCorrectionLevel.H }).Version;
        Assert.Equal((needed * 4 + 17 + 8) * 6, QrArt.Compose(longPayload, image, new QrImageCompositionOptions { ModuleSize = 6 }).Size);
        Assert.Throws<ArgumentOutOfRangeException>(() => QrArt.Compose(shortPayload, image, new QrImageCompositionOptions { MinimumVersion = 0 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => QrArt.Compose(shortPayload, image, new QrImageCompositionOptions { MinimumVersion = 41 }));
    }

    [Fact]
    public void ComposeVerification_RejectsPixelsThatDoNotCarryTheEncodedPayload() {
        var image = CodeGlyphX.Rendering.Png.PngImageEncoder.EncodeRgba32(Artwork(96), 96, 96);
        var composition = QrArt.Compose(Payload, image);
        var other = QR.Encode("https://example.com/other", new QrEncodingOptions { ErrorCorrectionLevel = QrErrorCorrectionLevel.H });
        var error = Assert.Throws<InvalidOperationException>(() => QrArt.VerifyComposition(other, composition));
        Assert.Contains("VerifyDecode", error.Message);
    }

    private static double Luma(byte[] pixels, int p) => 0.299 * pixels[p] + 0.587 * pixels[p + 1] + 0.114 * pixels[p + 2];

    private static byte[] Artwork(int side) {
        var pixels = new byte[side * side * 4];
        for (var y = 0; y < side; y++) for (var x = 0; x < side; x++) {
            var p = (y * side + x) * 4;
            pixels[p] = (byte)(x * 255 / (side - 1)); pixels[p + 1] = (byte)(y * 255 / (side - 1));
            pixels[p + 2] = (byte)(Math.Sqrt((x - side / 2) * (x - side / 2) + (y - side / 2) * (y - side / 2)) * 3);
            pixels[p + 3] = 255;
        }
        return pixels;
    }
}
