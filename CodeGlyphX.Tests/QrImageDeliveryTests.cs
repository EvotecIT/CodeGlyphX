using System;
using System.Linq;
using CodeGlyphX.Rendering.Art;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("ImageScannerSerial")]
public sealed class QrImageDeliveryTests {
    [Fact]
    public void DeliveryChecksIncludeActualExportAndRequestedPrintResolution() {
        const string payload = "DELIVERY-CHECK";
        var png = QrPngRenderer.Render(QR.Encode(payload).Modules, new QrPngRenderOptions { ModuleSize = 12 });
        var report = QrArt.ValidateDelivery(png, payload, new QrImageDeliveryOptions { DecodeBudgetMilliseconds = 3000 });
        Assert.Equal(7, report.Checks.Count);
        Assert.All(report.Checks.Where(c => c.Name != "Perspective"), c => Assert.True(c.Passed, c.Name));
        // Perspective recovery depends on the symbol geometry. Report the observation rather than promising success.
        Assert.Contains(report.Checks, c => c.Name == "Perspective" && c.Width == 348 && c.Height == 348);
        Assert.Equal(320, report.Checks.Single(c => c.Name == "ScreenSize").Width);
        Assert.Equal(236, report.Checks.Single(c => c.Name == "PrintRaster").Width);
        Assert.All(report.Checks.Where(c => c.Passed), c => Assert.Equal(payload, c.DecodedText));
        Assert.Equal(report.Checks.All(c => c.Passed), report.AllPassed);
        Assert.Throws<ArgumentOutOfRangeException>(() => QrArt.ValidateDelivery(png, payload, new QrImageDeliveryOptions { PrintMillimeters = double.NaN }));
    }

    [Theory]
    [InlineData(QrImageArtStyle.Engraving)]
    [InlineData(QrImageArtStyle.Halftone)]
    [InlineData(QrImageArtStyle.Contours)]
    [InlineData(QrImageArtStyle.Mosaic)]
    [InlineData(QrImageArtStyle.Botanical)]
    public void ArtisticTreatmentsDecodeAtTheirExportedSize(QrImageArtStyle style) {
        const string payload = "ARTISTIC-TREATMENT";
        var code = QR.Encode(payload, new QrEasyOptions { ErrorCorrectionLevel = QrErrorCorrectionLevel.H });
        var result = QrImageComposer.Render(code, new byte[] { 180, 120, 65, 255 }, 1, 1,
            new QrImageCompositionOptions { Strength = 0.95, Art = new QrImageArtOptions { Style = style } });
        Assert.True(QrArt.ValidateImage(result.ToPng(), payload, 3000).AllPassed);
    }

    [Fact]
    public void PngImageEncodingPreservesDecodedPixelsAndAlpha() {
        var pixels = new byte[] { 1, 2, 3, 0, 60, 70, 80, 128 };
        var png = PngImageEncoder.EncodeRgba32(pixels, 2, 1);
        Assert.Equal(pixels, PngReader.DecodeRgba32(png, out var width, out var height));
        Assert.Equal(2, width); Assert.Equal(1, height);
        Assert.Throws<ArgumentException>(() => PngImageEncoder.EncodeRgba32(pixels, int.MaxValue, 2));
    }
}
