#if NET8_0_OR_GREATER
using System;
using System.IO;
using System.Threading;
using CodeGlyphX.Rendering;
using CodeGlyphX.Tests.TestHelpers;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("ImageScannerSerial")]
public sealed class QrExactGridRecoveryTests {
    [Theory]
    [InlineData(18, false)]
    [InlineData(18, true)]
    [InlineData(19, false)]
    [InlineData(19, true)]
    public void IndependentVersion40Raster_PreservesExactPayload(int fixture, bool scanner) {
        var name = "zxing-exact-grid-" + fixture;
        var expected = File.ReadAllText(FixturePaths.Get("Qr", name + ".txt"));
        Assert.True(ImageReader.TryDecodeRgba32(File.ReadAllBytes(FixturePaths.Get("Qr", name + ".png")),
            out var pixels, out var width, out var height));
        if (scanner) {
            var scan = SymbolScanner.Scan(ImageFrame.Packed(pixels, width, height, PixelFormat.Rgba32),
                ScanOptions.Robust(TestBudget.Adjust(1500)));
            var decoded = Assert.Single(scan.Symbols);
            Assert.Equal(SymbolFormat.QrCode, decoded.Format);
            Assert.Equal(expected, decoded.Text);
        } else {
            Assert.True(QrDecoder.TryDecode(pixels, width, height, width * 4, PixelFormat.Rgba32,
                out var decoded, Options()));
            Assert.Equal(expected, decoded.Text);
        }
    }

    [Theory]
    [InlineData(1, 1, 1, 0, false, 0)]
    [InlineData(11, 1, 1, 0, false, 0)]
    [InlineData(40, 1, 1, 3, true, 1)]
    [InlineData(40, 2, 2, 4, false, 2)]
    [InlineData(12, 2, 3, 5, true, 3)]
    [InlineData(40, 1, 2, 0, false, 1)]
    public void IntegerRaster_PreservesVersionSpacingOrientationAndPolarity(int version, int pitchX, int pitchY,
        int padding, bool invert, int turns) {
        var expected = "EXACT-GRID-" + version;
        var matrix = QrCodeEncoder.EncodeText(expected, new QrEncodingOptions { MinVersion = version, MaxVersion = version }).Modules;
        for (var i = 0; i < turns; i++) matrix = CodeGlyphX.Internal.BitMatrixTransforms.Rotate90(matrix);
        var width = matrix.Width * pitchX + padding * 2;
        var height = matrix.Height * pitchY + padding * 2;
        var pixels = new byte[width * height * 4];
        for (var y = 0; y < height; y++) {
            for (var x = 0; x < width; x++) {
                var mx = (x - padding) / pitchX;
                var my = (y - padding) / pitchY;
                var dark = x >= padding && y >= padding && x < width - padding && y < height - padding && matrix[mx, my];
                var value = (byte)(dark != invert ? 0 : 255);
                var at = (y * width + x) * 4;
                pixels[at] = pixels[at + 1] = pixels[at + 2] = value;
                pixels[at + 3] = 255;
            }
        }
        Assert.True(QrDecoder.TryDecode(pixels, width, height, width * 4, PixelFormat.Rgba32,
            out var decoded, Options()));
        Assert.Equal(expected, decoded.Text);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExactRaster_DoesNotRestartCancelledOrExpiredRecognition(bool ambientDeadline) {
        Assert.True(ImageReader.TryDecodeRgba32(File.ReadAllBytes(FixturePaths.Get("Qr", "zxing-exact-grid-18.png")),
            out var pixels, out var width, out var height));
        using var cancel = new CancellationTokenSource();
        using var scope = CodeGlyphX.Internal.DecodeBudget.Begin(ambientDeadline ? 1 : 0);
        if (ambientDeadline) {
            Assert.True(SpinWait.SpinUntil(() => CodeGlyphX.Internal.DecodeBudget.IsExpired, TimeSpan.FromSeconds(1)));
        } else {
            cancel.Cancel();
        }
        Assert.False(QrDecoder.TryDecode(pixels, width, height, width * 4, PixelFormat.Rgba32,
            out _, Options(), cancel.Token));
    }

    [Fact]
    public void ExactRaster_StillRequiresValidCodewords() {
        Assert.True(ImageReader.TryDecodeRgba32(File.ReadAllBytes(FixturePaths.Get("Qr", "zxing-exact-grid-18.png")),
            out var pixels, out var width, out var height));
        // Keep the finder borders and integer pitch, but destroy most payload
        // and correction bits. Geometry alone must never produce a payload.
        for (var y = 12; y < height - 12; y++) {
            for (var x = 12; x < width - 12; x++) {
                var at = (y * width + x) * 4;
                pixels[at] = pixels[at + 1] = pixels[at + 2] = 255;
            }
        }
        Assert.False(QrDecoder.TryDecode(pixels, width, height, width * 4, PixelFormat.Rgba32, out _, Options()));
    }

    [Fact]
    public void ExactRaster_RespectsTheExistingAcceptancePredicate() {
        Assert.True(ImageReader.TryDecodeRgba32(File.ReadAllBytes(FixturePaths.Get("Qr", "zxing-exact-grid-18.png")),
            out var pixels, out var width, out var height));
        var offered = false;
        Assert.False(CodeGlyphX.Qr.QrPixelDecoder.TryDecode(pixels, width, height, width * 4, PixelFormat.Rgba32,
            Options(), decoded => { offered = true; return false; }, CancellationToken.None, out _, out _));
        Assert.True(offered);
    }

    private static QrPixelDecodeOptions Options() => new() {
        Profile = QrDecodeProfile.Robust, BudgetMilliseconds = TestBudget.Adjust(1500), MaxDimension = 4096
    };
}
#endif
