using System;
using CodeGlyphX.Qr;
using CodeGlyphX.Rendering.Art;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("ImageScannerSerial")]
public sealed class QrExpressiveArtTests {
    private const string Payload = "https://example.com/art";

    [Theory]
    [InlineData(QrPngModuleShape.Rounded)]
    [InlineData(QrPngModuleShape.ConnectedRounded)]
    [InlineData(QrPngModuleShape.ConnectedSquircle)]
    [InlineData(QrPngModuleShape.Blob)]
    [InlineData(QrPngModuleShape.Leaf)]
    public void ShapedArtKeepsScanAnchorsAndDecodes(QrPngModuleShape shape) {
        var qr = QR.Encode(Payload, new QrEasyOptions { ErrorCorrectionLevel = QrErrorCorrectionLevel.H });
        var result = QrImageComposer.Render(qr, new byte[] { 215, 154, 73, 255 }, 1, 1,
            new QrImageCompositionOptions { Art = new QrImageArtOptions { Shape = shape }, Strength = 1 });
        var pixels = result.GetPixels();
        var functions = QrStructureAnalysis.BuildFunctionMask(qr.Version, qr.Size);
        for (var y = 0; y < qr.Size; y++) for (var x = 0; x < qr.Size; x++) {
            var p = (((y + 4) * 12 + 6) * result.Size + (x + 4) * 12 + 6) * 4;
            var luminance = pixels[p] * 0.299 + pixels[p + 1] * 0.587 + pixels[p + 2] * 0.114;
            Assert.True(qr.Modules[x, y] ? luminance < 32 : luminance > 223);
            if (functions[x, y]) Assert.Equal(qr.Modules[x, y] ? (byte)0 : (byte)255, pixels[p]);
        }
        var report = QrArt.ValidateImage(result.ToPng(), Payload, 3000);
        Assert.True(report.AllPassed);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(0.37, 0.64)]
    public void CanvasPlacementKeepsAnUnbrokenQuietZone(double x, double y) {
        var qr = QR.Encode(Payload);
        var options = new QrImageCompositionOptions {
            ModuleSize = 8,
            Art = new QrImageArtOptions { Shape = QrPngModuleShape.Blob, FunctionalForeground = new Rgba32(20, 30, 40), FunctionalBackground = new Rgba32(240, 230, 220) },
            Canvas = new QrImageCanvasOptions { PaddingModules = 8, PositionX = x, PositionY = y }
        };
        var result = QrImageComposer.Render(qr, new byte[] { 180, 60, 80, 255 }, 1, 1, options);
        Assert.Equal((qr.Size + 8) * 8, result.QrSize);
        Assert.Equal(result.QrSize + 128, result.Size);
        Assert.Equal((int)Math.Round(128 * x), result.QrOffsetX);
        Assert.Equal((int)Math.Round(128 * y), result.QrOffsetY);
        var pixels = result.GetPixels();
        for (var yy = 0; yy < result.QrSize; yy++) for (var xx = 0; xx < result.QrSize; xx++) {
            if (xx >= 32 && yy >= 32 && xx < result.QrSize - 32 && yy < result.QrSize - 32) continue;
            var p = ((yy + result.QrOffsetY) * result.Size + xx + result.QrOffsetX) * 4;
            Assert.Equal(240, pixels[p]);
            Assert.Equal(230, pixels[p + 1]);
            Assert.Equal(220, pixels[p + 2]);
            Assert.Equal(255, pixels[p + 3]);
        }
        Assert.True(QrArt.ValidateImage(result.ToPng(), Payload, 3000).AllPassed);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(40)]
    public void ArtGeometryNeverChangesFunctionalModulePixels(int version) {
        var size = version * 4 + 17;
        var modules = new BitMatrix(size, size);
        for (var y = 0; y < size; y++) for (var x = 0; x < size; x++) modules[x, y] = (x + y) % 2 == 0;
        var qr = new QrCode(version, QrErrorCorrectionLevel.H, 0, modules);
        var result = QrImageComposer.Render(qr, new byte[] { 190, 130, 210, 255 }, 1, 1,
            new QrImageCompositionOptions { ModuleSize = 6, Art = new QrImageArtOptions { Shape = QrPngModuleShape.ConnectedRounded, Scale = 0.65, DetailProtection = 1 } });
        var pixels = result.GetPixels();
        var functions = QrStructureAnalysis.BuildFunctionMask(version, size);
        for (var y = 0; y < size; y++) for (var x = 0; x < size; x++) {
            if (!functions[x, y]) continue;
            var expected = modules[x, y] ? (byte)0 : (byte)255;
            for (var yy = 0; yy < 6; yy++) for (var xx = 0; xx < 6; xx++) {
                var p = (((y + 4) * 6 + yy) * result.Size + (x + 4) * 6 + xx) * 4;
                Assert.Equal(expected, pixels[p]);
                Assert.Equal(expected, pixels[p + 1]);
                Assert.Equal(expected, pixels[p + 2]);
            }
        }
    }

    [Fact]
    public void ImageDetailChangesDecorationDeterministicallyWithoutMovingAnchors() {
        var source = new byte[23 * 17 * 4];
        for (var i = 0; i < source.Length; i += 4) {
            source[i] = (byte)(i * 37 % 256);
            source[i + 1] = (byte)(i * 13 % 256);
            source[i + 2] = (byte)(i * 61 % 256);
            source[i + 3] = 255;
        }
        var qr = QR.Encode(Payload);
        var options = new QrImageCompositionOptions { Art = new QrImageArtOptions { Shape = QrPngModuleShape.Leaf, DetailProtection = 1 } };
        var first = QrImageComposer.Render(qr, source, 23, 17, options).GetPixels();
        Assert.Equal(first, QrImageComposer.Render(qr, source, 23, 17, options).GetPixels());
        options.Art.DetailProtection = 0;
        var fixedShape = QrImageComposer.Render(qr, source, 23, 17, options).GetPixels();
        Assert.NotEqual(first, fixedShape);
        var side = (qr.Size + 8) * 12;
        for (var y = 0; y < qr.Size; y++) for (var x = 0; x < qr.Size; x++) {
            var p = (((y + 4) * 12 + 6) * side + (x + 4) * 12 + 6) * 4;
            for (var channel = 0; channel < 4; channel++) Assert.Equal(first[p + channel], fixedShape[p + channel]);
        }
    }

    [Fact]
    public void CropAlignmentSelectsTheRequestedSideOfAWideImage() {
        var image = new byte[] { 255, 0, 0, 255, 255, 0, 0, 255, 0, 0, 255, 255, 0, 0, 255, 255 };
        var left = new QrImageSampler(image, 4, 1, 100, new QrImageCompositionOptions { ImagePositionX = 0 });
        var right = new QrImageSampler(image, 4, 1, 100, new QrImageCompositionOptions { ImagePositionX = 1 });
        left.Sample(50, 50, out var lr, out _, out var lb);
        right.Sample(50, 50, out var rr, out _, out var rb);
        Assert.Equal(255, lr);
        Assert.Equal(0, lb);
        Assert.Equal(0, rr);
        Assert.Equal(255, rb);
    }

    [Fact]
    public void RejectsInvalidGeometryPlacementAndInk() {
        var qr = QR.Encode(Payload);
        void Render(QrImageCompositionOptions options) => QrImageComposer.Render(qr, new byte[4], 1, 1, options);
        Assert.Throws<ArgumentOutOfRangeException>(() => Render(new QrImageCompositionOptions { ImageZoom = double.NaN }));
        Assert.Throws<ArgumentOutOfRangeException>(() => Render(new QrImageCompositionOptions { ImagePositionX = double.PositiveInfinity }));
        Assert.Throws<ArgumentOutOfRangeException>(() => Render(new QrImageCompositionOptions { Canvas = new QrImageCanvasOptions { PositionY = -1 } }));
        Assert.Throws<ArgumentOutOfRangeException>(() => Render(new QrImageCompositionOptions { Art = new QrImageArtOptions { Scale = 0.4 } }));
        Assert.Throws<ArgumentException>(() => Render(new QrImageCompositionOptions { Art = new QrImageArtOptions { FunctionalForeground = Rgba32.White } }));
        Assert.Throws<ArgumentException>(() => Render(new QrImageCompositionOptions { Art = new QrImageArtOptions { FunctionalBackground = Rgba32.Black } }));
        Assert.Throws<ArgumentException>(() => Render(new QrImageCompositionOptions { Art = new QrImageArtOptions { FunctionalForeground = Rgba32.Transparent } }));
        Assert.Throws<ArgumentException>(() => Render(new QrImageCompositionOptions { ModuleSize = 64, Canvas = new QrImageCanvasOptions { PaddingModules = 64 } }));
    }
}
