using System;
using System.Collections.Generic;
using CodeGlyphX.Rendering;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class ValueContractTests {
    [Fact]
    public void DefaultDecodeResultIsUninitializedRatherThanSuccessful() {
        var result = default(DecodeResult<string>);

        Assert.False(result.IsSuccess);
        Assert.Equal(DecodeFailureReason.Uninitialized, result.Failure);
        Assert.Null(result.Value);
        Assert.False(string.IsNullOrEmpty(result.Message));
    }

    [Fact]
    public void DecodeResultRequiresAValueForSuccessAndAReasonForFailure() {
        Assert.Throws<ArgumentNullException>(() => new DecodeResult<string>(null!, default, TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(() => new DecodeResult<string>(DecodeFailureReason.None, default, TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(() => new DecodeResult<string>((DecodeFailureReason)99, default, TimeSpan.Zero));

        var image = new ImageInfo(ImageFormat.Png, 12, 34);
        var elapsed = TimeSpan.FromMilliseconds(5);
        var success = new DecodeResult<int>(0, image, elapsed);
        Assert.True(success.IsSuccess);
        Assert.Equal(DecodeFailureReason.None, success.Failure);
        Assert.Equal(0, success.Value);
        Assert.Equal(string.Empty, success.Message);
        Assert.Equal(image, success.Image);
        Assert.Equal(elapsed, success.Elapsed);

        var failure = new DecodeResult<string>(DecodeFailureReason.NoResult, image, elapsed, "No QR code found.");
        Assert.False(failure.IsSuccess);
        Assert.Null(failure.Value);
        Assert.Equal(DecodeFailureReason.NoResult, failure.Failure);
        Assert.Equal("No QR code found.", failure.Message);
    }

    [Fact]
    public void BarcodeOwnsAReadOnlySnapshotOfSegments() {
        var segments = new[] { new BarSegment(true, 2), new BarSegment(false, 3) };
        var barcode = new Barcode1D(segments);
        segments[0] = new BarSegment(true, 9);

        Assert.Equal(2, barcode.Segments[0].Modules);
        Assert.Equal(5, barcode.TotalModules);
        var exposed = Assert.IsAssignableFrom<IList<BarSegment>>(barcode.Segments);
        Assert.Throws<NotSupportedException>(() => exposed[0] = new BarSegment(true, 9));
        Assert.Equal(5, barcode.Segments[0].Modules + barcode.Segments[1].Modules);
    }

    [Fact]
    public void BarcodeRejectsZeroWidthDefaultSegments() {
        Assert.Throws<ArgumentException>(() => new Barcode1D(new[] { default(BarSegment) }));
    }

    [Theory]
    [InlineData(1, 21)]
    [InlineData(40, 177)]
    public void QrCodeDimensionsAgreeWithVersion(int version, int size) {
        var qr = new QrCode(version, QrErrorCorrectionLevel.H, 7, new BitMatrix(size, size));

        Assert.Equal(version, qr.Version);
        Assert.Equal(size, qr.Size);
        Assert.Equal(QrErrorCorrectionLevel.H, qr.ErrorCorrectionLevel);
        Assert.Equal(7, qr.Mask);
        Assert.Throws<ArgumentException>(() => new QrCode(version, QrErrorCorrectionLevel.H, 0, new BitMatrix(size - 1, size)));
        Assert.Throws<ArgumentException>(() => new QrCode(version, QrErrorCorrectionLevel.H, 0, new BitMatrix(1, 1)));
    }

    [Fact]
    public void QrCodeRejectsInvalidMetadata() {
        var modules = new BitMatrix(21, 21);

        Assert.Throws<ArgumentOutOfRangeException>(() => new QrCode(0, QrErrorCorrectionLevel.L, 0, modules));
        Assert.Throws<ArgumentOutOfRangeException>(() => new QrCode(41, QrErrorCorrectionLevel.L, 0, modules));
        Assert.Throws<ArgumentOutOfRangeException>(() => new QrCode(1, (QrErrorCorrectionLevel)(-1), 0, modules));
        Assert.Throws<ArgumentOutOfRangeException>(() => new QrCode(1, (QrErrorCorrectionLevel)4, 0, modules));
        Assert.Throws<ArgumentOutOfRangeException>(() => new QrCode(1, QrErrorCorrectionLevel.L, -1, modules));
        Assert.Throws<ArgumentOutOfRangeException>(() => new QrCode(1, QrErrorCorrectionLevel.L, 8, modules));
    }
}
