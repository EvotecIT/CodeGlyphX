using System;
using CodeGlyphX.Qr;
using CodeGlyphX.Rendering.Art;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("ImageScannerSerial")]
public sealed class QrImageSubjectTests {
    [Fact]
    public void ProtectionRevealsMoreSourceColorWithoutMovingScanAnchors() {
        const string payload = "https://example.com/subject";
        var qr = QR.Encode(payload, new QrEasyOptions { ErrorCorrectionLevel = QrErrorCorrectionLevel.H });
        var source = new byte[] { 170, 140, 100, 255 };
        var options = new QrImageCompositionOptions { Strength = 1, Art = new QrImageArtOptions { Shape = QrPngModuleShape.Leaf } };
        var baseline = QrImageComposer.Render(qr, source, 1, 1, options);
        options.Art.Subject = new QrImageSubjectOptions { Mask = new QrImageProtectionMask(new byte[] { 255 }, 1, 1) };
        var protectedImage = QrImageComposer.Render(qr, source, 1, 1, options);
        var before = baseline.GetPixels();
        var after = protectedImage.GetPixels();
        var functions = QrStructureAnalysis.BuildFunctionMask(qr.Version, qr.Size);
        long beforeError = 0, afterError = 0;
        for (var y = 0; y < qr.Size; y++) for (var x = 0; x < qr.Size; x++) {
            var center = (((y + 4) * 12 + 6) * baseline.Size + (x + 4) * 12 + 6) * 4;
            for (var c = 0; c < 4; c++) Assert.Equal(before[center + c], after[center + c]);
            for (var yy = 0; yy < 12; yy++) for (var xx = 0; xx < 12; xx++) {
                var p = (((y + 4) * 12 + yy) * baseline.Size + (x + 4) * 12 + xx) * 4;
                for (var c = 0; c < 3; c++) {
                    if (functions[x, y]) Assert.Equal(before[p + c], after[p + c]);
                    else {
                        beforeError += Math.Abs(before[p + c] - source[c]);
                        afterError += Math.Abs(after[p + c] - source[c]);
                    }
                }
            }
        }
        Assert.True(afterError < beforeError * 0.8);
        var report = QrArt.ValidateImage(protectedImage.ToPng(), payload, TestBudget.Adjust(3000));
        Assert.All(report.Checks, check => Assert.True(check.Passed, check.Name));
    }

    [Fact]
    public void ProtectionMaskCopiesInputAndFollowsSourceCrop() {
        var values = new byte[] { 255, 255, 0, 0 };
        var mask = new QrImageProtectionMask(values, 4, 1);
        Array.Clear(values, 0, values.Length);
        var subject = new QrImageSubjectOptions { Mask = mask };
        var left = new QrImageSampler(new byte[16], 4, 1, 100, new QrImageCompositionOptions { ImagePositionX = 0 });
        var right = new QrImageSampler(new byte[16], 4, 1, 100, new QrImageCompositionOptions { ImagePositionX = 1 });
        Assert.Equal(1, left.Protection(50, 50, subject));
        Assert.Equal(0, right.Protection(50, 50, subject));
    }

    [Fact]
    public void FocalRegionUsesShortSideRadiusAndZeroStrengthIsIdentity() {
        var subject = new QrImageSubjectOptions { X = 0.25, Y = 0.5, Radius = 0.2 };
        Assert.Equal(1, subject.Sample(0.25, 0.5, 400, 100));
        Assert.Equal(0, subject.Sample(0.5, 0.5, 400, 100));
        subject.Strength = 0;
        Assert.Equal(0, subject.Sample(0.25, 0.5, 400, 100));
        Assert.Throws<ArgumentException>(() => new QrImageProtectionMask(new byte[3], 2, 2));
        subject.Radius = double.NaN;
        Assert.Throws<ArgumentOutOfRangeException>(() => subject.Validate());
    }
}
