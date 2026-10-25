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
    [InlineData(QrImageCompositionStyle.ImageBackground, 0.65)]
    [InlineData(QrImageCompositionStyle.ImageBackground, 1.0)]
    public void PhotoTreatments_RetainFunctionalPixelsAndExactScanPayload(QrImageCompositionStyle style, double strength) {
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
