using System;
using System.Linq;
using System.Threading;
using CodeGlyphX.Rendering;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed partial class SpecialistImageScannerTests {
    [Theory]
    [MemberData(nameof(ExternalImages))]
    public void ExternalPixels_DecodeFractionalScaleLowContrastAndSeparateCaption(SymbolFormat format, string expected) {
        Assert.True(ImageReader.TryDecodeRgba32(Resource("ImageSamples.bwipp-" + format + ".png"),
            out var pixels, out var width, out var height));
        var scaledWidth = (int)(width * 1.25); var scaledHeight = (int)(height * 1.6);
        var outputWidth = scaledWidth + 40; var outputHeight = scaledHeight + 55;
        var output = Enumerable.Repeat((byte)255, outputWidth * outputHeight * 4).ToArray();
        for (var y = 0; y < scaledHeight; y++) for (var x = 0; x < scaledWidth; x++) {
            var source = ((int)(y / 1.6) * width + (int)(x / 1.25)) * 4;
            var at = ((y + 15) * outputWidth + x + 20) * 4;
            for (var channel = 0; channel < 3; channel++) output[at + channel] = (byte)(70 + pixels[source + channel] * .55 + 40 * x / scaledWidth);
        }
        // Separate text-like marks must not enlarge the sampled barcode grid.
        for (var y = outputHeight - 18; y < outputHeight - 10; y++) for (var x = 30; x < 110; x++) {
            if (x % 9 > 5) continue;
            var at = (y * outputWidth + x) * 4;
            output[at] = output[at + 1] = output[at + 2] = 0;
        }
        var result = SymbolScanner.Scan(ImageFrame.Packed(output, outputWidth, outputHeight, PixelFormat.Rgba32), Options(format));
        Assert.True(result.IsSuccess, $"{format}: {result.Status}/{result.CompletionReason}");
        Assert.Equal(expected, Assert.Single(result.Symbols).Text);
    }

    [Theory]
    [InlineData(SymbolFormat.Code16K, "HELLO", 7)]
    [InlineData(SymbolFormat.Code49, "ABC123", -9)]
    [InlineData(SymbolFormat.CodablockF, "AB1234567890xy", 7)]
    [InlineData(SymbolFormat.MicroPdf417, "ABC123", -9)]
    [InlineData(SymbolFormat.RmQrCode, "RMQR-42", 7)]
    [InlineData(SymbolFormat.HanXin, "1234567890", -9)]
    [InlineData(SymbolFormat.DotCode, "ABC123", 7)]
    [InlineData(SymbolFormat.UspsIntelligentMail, "01234567094987654321-01234567891", -9)]
    public void ExternalPixels_DecodeSmallArbitraryRotations(SymbolFormat format, string expected, double angle) {
        Assert.True(ImageReader.TryDecodeRgba32(Resource("ImageSamples.bwipp-" + format + ".png"),
            out var pixels, out var width, out var height));
        var cosine = Math.Cos(angle * Math.PI / 180); var sine = Math.Sin(angle * Math.PI / 180);
        var targetWidth = (int)Math.Ceiling(width * Math.Abs(cosine) + height * Math.Abs(sine)) + 40;
        var targetHeight = (int)Math.Ceiling(height * Math.Abs(cosine) + width * Math.Abs(sine)) + 40;
        var rotated = Enumerable.Repeat((byte)255, targetWidth * targetHeight * 4).ToArray();
        for (var y = 0; y < targetHeight; y++) for (var x = 0; x < targetWidth; x++) {
            var dx = x + .5 - targetWidth / 2d; var dy = y + .5 - targetHeight / 2d;
            var sx = (int)Math.Floor(cosine * dx + sine * dy + width / 2d);
            var sy = (int)Math.Floor(-sine * dx + cosine * dy + height / 2d);
            if ((uint)sx >= (uint)width || (uint)sy >= (uint)height) continue;
            Array.Copy(pixels, (sy * width + sx) * 4, rotated, (y * targetWidth + x) * 4, 4);
        }
        var result = SymbolScanner.Scan(ImageFrame.Packed(rotated, targetWidth, targetHeight, PixelFormat.Rgba32), Options(format));
        Assert.True(result.IsSuccess, $"{format}: {angle} {result.Status}/{result.CompletionReason}");
        Assert.Equal(expected, Assert.Single(result.Symbols).Text);
    }

    [Theory]
    [MemberData(nameof(CheckedImages))]
    public void DefaultSelection_RecognizesCheckedSpecialistsWithoutWeakFormatFalsePositives(SymbolFormat format, string expected) {
        var result = SymbolScanner.Scan(Resource("ImageSamples.bwipp-" + format + ".png"), new ScanOptions {
            TimeoutMilliseconds = 10000, EnableTileScan = false
        });
        Assert.Contains(result.Symbols, symbol => symbol.Format == format && symbol.Text == expected);
        Assert.DoesNotContain(result.Symbols, symbol => symbol.Format == SymbolFormat.Daft || symbol.Format == SymbolFormat.Flattermarken
            || symbol.Format == SymbolFormat.PharmacodeTwoTrack || symbol.Format == SymbolFormat.KixCode || symbol.Format == SymbolFormat.FacingIdentificationMark);
    }

    public static System.Collections.Generic.IEnumerable<object[]> CheckedImages() =>
        ExternalImages().Where(row => SymbolCapabilities.Get((SymbolFormat)row[0]).IsDefaultScanFormat);
    public static System.Collections.Generic.IEnumerable<object[]> CheckedFormats() =>
        CheckedImages().Select(row => new[] { row[0] });

    [Fact]
    public void IndependentCollationImage_RetainsLeadingAndTrailingWhiteDigits() {
        var png = Resource("ImageSamples.bwipp-Flattermarken.png");
        Assert.True(ImageReader.TryDecodeRgba32(png, out _, out var width, out var height));
        var options = Options(SymbolFormat.Flattermarken);
        options.Region = new ImageRegion(36, 36, 243, height - 72);
        options.SamplingGrid = new ImageSamplingGrid(81, 1);
        var result = SymbolScanner.Scan(png, options);
        Assert.Equal("001109900", Assert.Single(result.Symbols).Text);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(90)]
    [InlineData(180)]
    [InlineData(270)]
    public void RawBarStates_UseKnownOrientationRatherThanGuessingAnotherValidMessage(int rotation) {
        var modules = CodeGlyphX.Postal.DaftCode.Encode("FATDAFTD").Modules;
        var grid = modules;
        for (var i = 0; i < rotation / 90; i++) grid = CodeGlyphX.Internal.BitMatrixTransforms.Rotate90(grid);
        var png = CodeGlyphX.Rendering.Png.MatrixPngRenderer.Render(grid,
            new CodeGlyphX.Rendering.Png.MatrixPngRenderOptions { ModuleSize = 5, QuietZone = 0 });
        var options = Options(SymbolFormat.Daft);
        options.SamplingGrid = new ImageSamplingGrid(modules.Width, modules.Height, rotation);
        Assert.Equal("FATDAFTD", Assert.Single(SymbolScanner.Scan(png, options).Symbols).Text);
    }

    [Theory]
    [InlineData("000")]
    [InlineData("00000")]
    public void ExplicitCollationCalibration_CanRepresentAnEntirelyWhiteMessage(string expected) {
        var options = Options(SymbolFormat.Flattermarken);
        options.SamplingGrid = new ImageSamplingGrid(expected.Length * 9, 1);
        var width = expected.Length * 27;
        var rgba = Enumerable.Repeat((byte)255, width * 20 * 4).ToArray();
        Assert.Equal(expected, Assert.Single(SymbolScanner.Scan(ImageFrame.Packed(rgba, width, 20, PixelFormat.Rgba32), options).Symbols).Text);
    }

    [Theory]
    [MemberData(nameof(CheckedFormats))]
    public void CorruptedExternalSymbols_DoNotProduceUncheckedReplacementPayloads(SymbolFormat format) {
        Assert.True(ImageReader.TryDecodeRgba32(Resource("ImageSamples.bwipp-" + format + ".png"),
            out var rgba, out var width, out var height));
        for (var y = 0; y < height; y++) for (var x = width / 4; x < width * 3 / 4; x++) {
            var p = (y * width + x) * 4;
            rgba[p] = rgba[p + 1] = rgba[p + 2] = 255;
        }
        var result = SymbolScanner.Scan(ImageFrame.Packed(rgba, width, height, PixelFormat.Rgba32), Options(format));
        Assert.Empty(result.Symbols);
    }

    [Fact]
    public void SpecialistRecognition_ObservesCancellationAndDoesNotClaimUniformImages() {
        using var source = new CancellationTokenSource(); source.Cancel();
        var options = Options(SymbolFormat.HanXin); options.CancellationToken = source.Token;
        var result = SymbolScanner.Scan(Resource("ImageSamples.bwipp-HanXin.png"), options);
        Assert.Equal(ScanCompletionReason.Cancelled, result.CompletionReason);
        Assert.Empty(result.Symbols);
        var white = Enumerable.Repeat((byte)255, 120 * 60 * 4).ToArray();
        foreach (var format in ExternalImages().Select(row => (SymbolFormat)row[0])) {
            result = SymbolScanner.Scan(ImageFrame.Packed(white, 120, 60, PixelFormat.Rgba32), Options(format));
            Assert.Equal(ScanStatus.NoSymbolFound, result.Status);
        }
    }

    [Fact]
    public void CalibratedGrid_ReusesTheScannerRegionAndResultContractForOrdinaryFormats() {
        var qr = QR.Encode("CALIBRATED-QR");
        var png = QR.Render("CALIBRATED-QR", OutputFormat.Png, new QrRenderOptions { ModuleSize = 5, QuietZone = 4 }).ToArray();
        var options = Options(SymbolFormat.QrCode);
        options.Region = new ImageRegion(20, 20, qr.Modules.Width * 5, qr.Modules.Height * 5);
        options.SamplingGrid = new ImageSamplingGrid(qr.Modules.Width, qr.Modules.Height);
        var result = SymbolScanner.Scan(png, options);
        var hit = Assert.Single(result.Symbols);
        Assert.Equal("CALIBRATED-QR", hit.Text);
        Assert.Equal(ScanCompletionReason.SymbolLimitReached, result.CompletionReason);
        Assert.Equal(20, hit.Geometry!.Bounds.X);
        Assert.Equal(20, hit.Geometry.Bounds.Y);
        Assert.Equal(qr.Modules.Width * 5, hit.Geometry.Bounds.Width);
        options.Region = new ImageRegion(20, 20, qr.Modules.Width * 5 + 100, qr.Modules.Height * 5);
        Assert.Equal(ScanStatus.InvalidImage, SymbolScanner.Scan(png, options).Status);
        options.Formats = null;
        Assert.Throws<ArgumentException>(() => SymbolScanner.Scan(png, options));
    }
}
