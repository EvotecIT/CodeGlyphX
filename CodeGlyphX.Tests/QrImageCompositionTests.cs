using System;
using System.Linq;
using System.Threading;
using CodeGlyphX.Qr;
using CodeGlyphX.Rendering.Art;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("ImageScannerSerial")]
public sealed class QrImageCompositionTests {
    private const string Payload = "https://example.com/art";

    [Theory]
    [InlineData(QrImageCompositionStyle.ColorModules, 6, 1.0)]
    [InlineData(QrImageCompositionStyle.ImageOverlay, 7, 1.0)]
    [InlineData(QrImageCompositionStyle.ImageOverlay, 12, 0.75)]
    public void ExportedCompositionDecodesAfterResizingAndBlur(QrImageCompositionStyle style, int moduleSize, double strength) {
        var qr = QR.Encode(Payload, new QrEncodingOptions { ErrorCorrectionLevel = QrErrorCorrectionLevel.H });
        var source = Artwork(73, 41);
        var original = (byte[])source.Clone();
        var result = QrImageComposer.Render(qr, source, 73, 41,
            new QrImageCompositionOptions { Style = style, ModuleSize = moduleSize, Strength = strength });
        Assert.Equal(original, source);
        Assert.Equal((qr.Size + 8) * moduleSize, result.Size);
        var report = QrArt.ValidateImage(result.ToPng(), Payload, budgetMilliseconds: 3000);
        Assert.True(report.AllPassed, string.Join(", ", report.Checks.Select(c => $"{c.Name}: {c.Passed}")));
        Assert.All(report.Checks, c => Assert.Equal(Payload, c.DecodedText));
        Assert.Equal(3, report.Checks.Count);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(40)]
    public void SolidFunctionPatternsKeepEveryFunctionalPixelAndTheQuietZoneSolid(int version) {
        var size = version * 4 + 17;
        var modules = new BitMatrix(size, size);
        for (var y = 0; y < size; y++) for (var x = 0; x < size; x++) modules[x, y] = (x + y) % 2 == 0;
        var qr = new QrCode(version, QrErrorCorrectionLevel.H, 0, modules);
        var result = QrImageComposer.Render(qr, Artwork(19, 13), 19, 13,
            new QrImageCompositionOptions { ModuleSize = 6, Style = QrImageCompositionStyle.ImageOverlay, Strength = 1, SolidFunctionPatterns = true });
        var pixels = result.GetPixels();
        var functions = QrStructureAnalysis.BuildFunctionMask(version, size);
        for (var y = 0; y < result.Size; y++) for (var x = 0; x < result.Size; x++) {
            var mx = x / 6 - 4;
            var my = y / 6 - 4;
            var border = mx < 0 || my < 0 || mx >= size || my >= size;
            var p = (y * result.Size + x) * 4;
            Assert.Equal(255, pixels[p + 3]);
            if (!border && !functions[mx, my]) continue;
            var expected = !border && modules[mx, my] ? (byte)0 : (byte)255;
            Assert.Equal(expected, pixels[p]);
            Assert.Equal(expected, pixels[p + 1]);
            Assert.Equal(expected, pixels[p + 2]);
        }
    }

    [Fact]
    public void ZeroStrengthMatchesPlainRendererAndCopiesPixels() {
        var qr = QR.Encode(Payload);
        var result = QrImageComposer.Render(qr, Artwork(7, 9), 7, 9, new QrImageCompositionOptions { Strength = 0 });
        var expected = QrPngRenderer.RenderPixels(qr.Modules, new QrPngRenderOptions { ModuleSize = 12 }, out _, out _, out _);
        Assert.Equal(expected, result.GetPixels());
        var copy = result.GetPixels();
        copy[0] = 0;
        Assert.Equal(255, result.GetPixels()[0]);
    }

    [Fact]
    public void SourceAlphaIsFlattenedBeforeInterpolation() {
        var qr = QR.Encode(Payload);
        var transparent = new byte[] { 255, 0, 0, 0, 0, 255, 0, 0 };
        var white = Enumerable.Repeat((byte)255, 8).ToArray();
        Assert.Equal(QrImageComposer.Render(qr, white, 2, 1).GetPixels(),
            QrImageComposer.Render(qr, transparent, 2, 1).GetPixels());
    }

    [Fact]
    public void ContainPreservesLetterboxAndCoverFillsIt() {
        var qr = QR.Encode(Payload);
        var red = new byte[] { 255, 0, 0, 255, 255, 0, 0, 255, 255, 0, 0, 255, 255, 0, 0, 255 };
        var contain = QrImageComposer.Render(qr, red, 4, 1, new QrImageCompositionOptions { Fit = QrImageFit.Contain });
        var cover = QrImageComposer.Render(qr, red, 4, 1);
        // Data cell above the contained image, outside the top-left finder and timing patterns.
        var offset = ((4 * 12 + 6) * contain.Size + (4 + 10) * 12 + 6) * 4;
        var contained = contain.GetPixels();
        var covered = cover.GetPixels();
        Assert.Equal(contained[offset], contained[offset + 1]);
        Assert.True(covered[offset] > covered[offset + 1]);
    }

    [Fact]
    public void EncodedImageConvenienceAndValidationUseExportedPayload() {
        var png = QrPngRenderer.Render(QR.Encode(Payload).Modules, new QrPngRenderOptions());
        var composition = QrArt.Compose(Payload, png);
        var report = QrArt.ValidateImage(composition.ToPng(), "different", 3000);
        Assert.False(report.AllPassed);
        Assert.All(report.Checks, c => { Assert.False(c.Passed); Assert.Equal(Payload, c.DecodedText); });
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        Assert.Throws<OperationCanceledException>(() => QrArt.ValidateImage(png, Payload, cancellationToken: cts.Token));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    public void RejectsInvalidStrength(double strength) {
        Assert.Throws<ArgumentOutOfRangeException>(() => QrImageComposer.Render(QR.Encode(Payload), new byte[4], 1, 1,
            new QrImageCompositionOptions { Strength = strength }));
    }

    [Fact]
    public void RejectsMalformedSourcesAndExcessiveOutputBeforeAllocation() {
        var qr = QR.Encode(Payload);
        Assert.Throws<ArgumentException>(() => QrImageComposer.Render(qr, new byte[4], int.MaxValue, int.MaxValue));
        Assert.Throws<ArgumentException>(() => QrImageComposer.Render(qr, new byte[3], 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => QrImageComposer.Render(qr, new byte[4], 1, 1, new QrImageCompositionOptions { ModuleSize = 5 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => QrImageComposer.Render(qr, new byte[4], 1, 1, new QrImageCompositionOptions { QuietZone = 3 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => QrImageComposer.Render(qr, new byte[4], 1, 1, new QrImageCompositionOptions { CenterSize = double.NaN }));
        Assert.Throws<ArgumentOutOfRangeException>(() => QrImageComposer.Render(qr, new byte[4], 1, 1, new QrImageCompositionOptions { Fit = (QrImageFit)9 }));
        var huge = new QrCode(40, QrErrorCorrectionLevel.H, 0, new BitMatrix(177, 177));
        Assert.Throws<ArgumentException>(() => QrImageComposer.Render(huge, new byte[4], 1, 1, new QrImageCompositionOptions { ModuleSize = 64 }));
    }

    [Theory]
    [InlineData(QrImageCompositionStyle.ColorModules)]
    [InlineData(QrImageCompositionStyle.ImageOverlay)]
    public void FlatImageRegionsKeepAWideModuleContrast(QrImageCompositionStyle style) {
        var qr = QR.Encode(Payload);
        var result = QrImageComposer.Render(qr, new byte[] { 128, 128, 128, 255 }, 1, 1,
            new QrImageCompositionOptions { ModuleSize = 8, Style = style, Strength = 1 });
        var pixels = result.GetPixels();
        var functions = QrStructureAnalysis.BuildFunctionMask(qr.Version, qr.Size);
        int brightestDark = 0, dimmestLight = 255;
        for (var y = 0; y < qr.Size; y++) for (var x = 0; x < qr.Size; x++) {
            if (functions[x, y]) continue;
            var p = (((y + 4) * 8 + 4) * result.Size + (x + 4) * 8 + 4) * 4;
            var value = pixels[p];
            if (qr.Modules[x, y]) brightestDark = Math.Max(brightestDark, value);
            else dimmestLight = Math.Min(dimmestLight, value);
        }
        // Image tints replace pure black and white, but every module centre stays well inside its band.
        Assert.InRange(brightestDark, 1, 64);
        Assert.InRange(dimmestLight, 191, 254);
        Assert.True(dimmestLight - brightestDark >= 130, $"Module contrast {dimmestLight - brightestDark} is too low.");
    }

    [Fact]
    public void ColorModulesTintEveryPixelOfAModule_WithoutCentreDots() {
        var qr = QR.Encode(Payload);
        var result = QrImageComposer.Render(qr, new byte[] { 40, 120, 200, 255 }, 1, 1,
            new QrImageCompositionOptions { ModuleSize = 10, Style = QrImageCompositionStyle.ColorModules, Strength = 1 });
        var pixels = result.GetPixels();
        var functions = QrStructureAnalysis.BuildFunctionMask(qr.Version, qr.Size);
        for (var my = 0; my < qr.Size; my++) for (var mx = 0; mx < qr.Size; mx++) {
            var centre = Pixel(mx, my, 5, 5);
            // Blue stays blue in both polarities, including function patterns: no grey veil and no white blocks.
            Assert.True(centre.B > centre.R + 20, $"Module ({mx},{my}) lost the image hue: {centre}.");
            if (InRings(mx, my)) continue;
            // A flat image yields a flat module: no centre dot or mesh is drawn inside it. Separators, timing and
            // format modules share this treatment, so no light gutters or black-and-white stripes cross the image.
            for (var y = 0; y < 10; y++) for (var x = 0; x < 10; x++) Assert.Equal(centre, Pixel(mx, my, x, y));
            if (functions[mx, my]) Assert.InRange(Luma(centre), qr.Modules[mx, my] ? 0 : 130, qr.Modules[mx, my] ? 125 : 255);
        }
        // Finders are rounded image-tinted rings: deep ball, light gap.
        Assert.True(Luma(Pixel(3, 3, 5, 5)) <= 45);
        Assert.True(Luma(Pixel(1, 3, 5, 5)) >= 130);

        bool InRings(int mx, int my) => (mx < 7 && my < 7) || (mx >= qr.Size - 7 && my < 7) || (mx < 7 && my >= qr.Size - 7)
            || (qr.Version > 1 && Math.Abs(mx - (qr.Size - 7)) <= 2 && Math.Abs(my - (qr.Size - 7)) <= 2);

        (int R, int G, int B) Pixel(int mx, int my, int x, int y) {
            var p = (((my + 4) * 10 + y) * result.Size + (mx + 4) * 10 + x) * 4;
            return (pixels[p], pixels[p + 1], pixels[p + 2]);
        }
        static double Luma((int R, int G, int B) c) => 0.299 * c.R + 0.587 * c.G + 0.114 * c.B;
    }

    [Fact]
    public void ImageOverlayTintsFunctionPatternsByDefault_AndSolidPatternsAreOptIn() {
        var qr = QR.Encode(Payload);
        var image = new byte[] { 200, 150, 90, 255 };
        var options = new QrImageCompositionOptions { ModuleSize = 12, Style = QrImageCompositionStyle.ImageOverlay };
        var tinted = QrImageComposer.Render(qr, image, 1, 1, options).GetPixels();
        options.SolidFunctionPatterns = true;
        var solid = QrImageComposer.Render(qr, image, 1, 1, options);
        var pixels = solid.GetPixels();
        // Timing module (9,6) and the separator corner (7,7) are light in every QR.
        foreach (var (mx, my) in new[] { (9, 6), (7, 7) }) {
            var p = (((my + 4) * 12 + 6) * solid.Size + (mx + 4) * 12 + 6) * 4;
            Assert.Equal(255, pixels[p]); Assert.Equal(255, pixels[p + 1]); Assert.Equal(255, pixels[p + 2]);
            Assert.True(tinted[p] != tinted[p + 2], $"Default overlay module ({mx},{my}) should carry the image hue.");
        }
    }

    [Fact]
    public void ImageOverlayFadesToTheImageAtEveryModuleCorner_SoNoGridIsDrawn() {
        var qr = QR.Encode(Payload);
        var image = new byte[] { 200, 150, 90, 255 };
        var result = QrImageComposer.Render(qr, image, 1, 1,
            new QrImageCompositionOptions { ModuleSize = 12, Style = QrImageCompositionStyle.ImageOverlay, Strength = 0.75 });
        var pixels = result.GetPixels();
        var functions = QrStructureAnalysis.BuildFunctionMask(qr.Version, qr.Size);
        for (var my = 0; my < qr.Size; my++) for (var mx = 0; mx < qr.Size; mx++) {
            if (functions[mx, my]) continue;
            // Corner pixels of both polarities are the untreated image, so neighbouring modules join seamlessly.
            foreach (var (x, y) in new[] { (0, 0), (11, 0), (0, 11), (11, 11) }) {
                var p = (((my + 4) * 12 + y) * result.Size + (mx + 4) * 12 + x) * 4;
                Assert.InRange(pixels[p] - image[0], -2, 2);
                Assert.InRange(pixels[p + 1] - image[1], -2, 2);
                Assert.InRange(pixels[p + 2] - image[2], -2, 2);
            }
        }
    }

    [Fact]
    public void ValidationRejectsHiddenResizingAndHonorsInputLimits() {
        var png = QrPngRenderer.Render(QR.Encode(Payload).Modules, new QrPngRenderOptions());
        Assert.Throws<ArgumentException>(() => QrArt.ValidateImage(png, Payload, imageOptions: new ImageDecodeOptions { MaxDimension = 100 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => QrArt.ValidateImage(png, Payload, budgetMilliseconds: 0));
        Assert.Throws<FormatException>(() => QrArt.Compose(Payload, png, imageOptions: new ImageDecodeOptions { MaxBytes = 8 }));
    }

    [Fact]
    public void UnicodeAndVersionInformationSurviveComposition() {
        const string payload = "Zażółć gęślą jaźń — 東京";
        var qr = QR.Encode(payload, new QrEncodingOptions { ErrorCorrectionLevel = QrErrorCorrectionLevel.H, MinVersion = 7 });
        var image = QrImageComposer.Render(qr, Artwork(23, 47), 23, 47, new QrImageCompositionOptions { Style = QrImageCompositionStyle.ImageOverlay });
        Assert.True(QrArt.ValidateImage(image.ToPng(), payload, 3000).AllPassed);
    }

    private static byte[] Artwork(int width, int height) {
        var pixels = new byte[width * height * 4];
        for (var y = 0; y < height; y++) for (var x = 0; x < width; x++) {
            var p = (y * width + x) * 4;
            pixels[p] = (byte)(x * 255 / width);
            pixels[p + 1] = (byte)(y * 255 / height);
            pixels[p + 2] = (byte)((x * 37 + y * 17) % 256);
            pixels[p + 3] = 255;
        }
        return pixels;
    }
}
