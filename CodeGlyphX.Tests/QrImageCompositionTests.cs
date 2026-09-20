using System;
using System.Linq;
using System.Threading;
using CodeGlyphX.Qr;
using CodeGlyphX.Rendering.Art;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class QrImageCompositionTests {
    private const string Payload = "https://example.com/art";

    [Theory]
    [InlineData(QrImageCompositionStyle.ColorModules, 6, 1.0)]
    [InlineData(QrImageCompositionStyle.ImageOverlay, 7, 1.0)]
    [InlineData(QrImageCompositionStyle.ImageOverlay, 12, 0.75)]
    public void ExportedCompositionDecodesAfterResizingAndBlur(QrImageCompositionStyle style, int moduleSize, double strength) {
        var qr = QrCode.Encode(Payload, new QrEasyOptions { ErrorCorrectionLevel = QrErrorCorrectionLevel.H });
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
    public void AllFunctionalPixelsAndQuietZoneRemainSolid(int version) {
        var size = version * 4 + 17;
        var modules = new BitMatrix(size, size);
        for (var y = 0; y < size; y++) for (var x = 0; x < size; x++) modules[x, y] = (x + y) % 2 == 0;
        var qr = new QrCode(version, QrErrorCorrectionLevel.H, 0, modules);
        var result = QrImageComposer.Render(qr, Artwork(19, 13), 19, 13,
            new QrImageCompositionOptions { ModuleSize = 6, Style = QrImageCompositionStyle.ImageOverlay, Strength = 1 });
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
        var qr = QrCode.Encode(Payload);
        var result = QrImageComposer.Render(qr, Artwork(7, 9), 7, 9, new QrImageCompositionOptions { Strength = 0 });
        var expected = QrPngRenderer.RenderPixels(qr.Modules, new QrPngRenderOptions { ModuleSize = 12 }, out _, out _, out _);
        Assert.Equal(expected, result.GetPixels());
        var copy = result.GetPixels();
        copy[0] = 0;
        Assert.Equal(255, result.GetPixels()[0]);
    }

    [Fact]
    public void SourceAlphaIsFlattenedBeforeInterpolation() {
        var qr = QrCode.Encode(Payload);
        var transparent = new byte[] { 255, 0, 0, 0, 0, 255, 0, 0 };
        var white = Enumerable.Repeat((byte)255, 8).ToArray();
        Assert.Equal(QrImageComposer.Render(qr, white, 2, 1).GetPixels(),
            QrImageComposer.Render(qr, transparent, 2, 1).GetPixels());
    }

    [Fact]
    public void ContainPreservesLetterboxAndCoverFillsIt() {
        var qr = QrCode.Encode(Payload);
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
        Assert.Throws<ArgumentOutOfRangeException>(() => QrImageComposer.Render(QrCode.Encode(Payload), new byte[4], 1, 1,
            new QrImageCompositionOptions { Strength = strength }));
    }

    [Fact]
    public void RejectsMalformedSourcesAndExcessiveOutputBeforeAllocation() {
        var qr = QrCode.Encode(Payload);
        Assert.Throws<ArgumentException>(() => QrImageComposer.Render(qr, new byte[4], int.MaxValue, int.MaxValue));
        Assert.Throws<ArgumentException>(() => QrImageComposer.Render(qr, new byte[3], 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => QrImageComposer.Render(qr, new byte[4], 1, 1, new QrImageCompositionOptions { ModuleSize = 5 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => QrImageComposer.Render(qr, new byte[4], 1, 1, new QrImageCompositionOptions { QuietZone = 3 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => QrImageComposer.Render(qr, new byte[4], 1, 1, new QrImageCompositionOptions { CenterSize = double.NaN }));
        Assert.Throws<ArgumentOutOfRangeException>(() => QrImageComposer.Render(qr, new byte[4], 1, 1, new QrImageCompositionOptions { Fit = (QrImageFit)9 }));
        var huge = new QrCode(40, QrErrorCorrectionLevel.H, 0, new BitMatrix(177, 177));
        Assert.Throws<ArgumentException>(() => QrImageComposer.Render(huge, new byte[4], 1, 1, new QrImageCompositionOptions { ModuleSize = 64 }));
        var inconsistent = new QrCode(40, QrErrorCorrectionLevel.H, 0, qr.Modules);
        Assert.Throws<ArgumentException>(() => QrImageComposer.Render(inconsistent, new byte[4], 1, 1));
    }

    [Theory]
    [InlineData(QrImageCompositionStyle.ColorModules)]
    [InlineData(QrImageCompositionStyle.ImageOverlay)]
    public void FlatImageRegionsRetainStrongModuleCenters(QrImageCompositionStyle style) {
        var qr = QrCode.Encode(Payload);
        var result = QrImageComposer.Render(qr, new byte[] { 128, 128, 128, 255 }, 1, 1,
            new QrImageCompositionOptions { ModuleSize = 8, Style = style, Strength = 1 });
        var pixels = result.GetPixels();
        for (var y = 0; y < qr.Size; y++) for (var x = 0; x < qr.Size; x++) {
            var p = (((y + 4) * 8 + 4) * result.Size + (x + 4) * 8 + 4) * 4;
            var value = pixels[p];
            Assert.True(qr.Modules[x, y] ? value < 32 : value > 223);
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
        var qr = QrCode.Encode(payload, new QrEasyOptions { ErrorCorrectionLevel = QrErrorCorrectionLevel.H, MinVersion = 7 });
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
