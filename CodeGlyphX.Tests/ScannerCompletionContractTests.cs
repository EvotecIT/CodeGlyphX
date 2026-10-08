using System;
using System.Threading;
using CodeGlyphX.Rendering;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("ImageScannerSerial")]
public sealed class ScannerCompletionContractTests {
    [Theory]
    [InlineData(1)]
    [InlineData(32)]
    public void ShortQrAllowanceStillAttemptsReadableSymbol(int maximum) {
        const string text = "SHORT-ALLOWANCE";
        var pixels = QR.RenderPixels(text, out var width, out var height, out var stride,
            new QrRenderOptions { ModuleSize = 3 });
        // Warm the public recognizer so this protects allowance handling rather than JIT speed.
        Assert.True(QrImageDecoder.TryDecode(pixels, width, height, stride, PixelFormat.Rgba32, out _));
        var scan = SymbolScanner.Scan(ImageFrame.Packed(pixels, width, height, PixelFormat.Rgba32), new ScanOptions {
            Formats = new[] { SymbolFormat.QrCode }, TimeoutMilliseconds = 5000, MaxSymbols = maximum,
            EnableTileScan = false, Qr = new QrPixelDecodeOptions { BudgetMilliseconds = 100 }
        });
        Assert.Contains(scan.Symbols, symbol => symbol.Text == text);
    }

    [Fact]
    public void FastMixedFamilyScanRetainsCleanQr() {
        const string text = "FAST-QR";
        var pixels = QR.RenderPixels(text, out var width, out var height, out var stride,
            new QrRenderOptions { ModuleSize = 3 });
        var frame = ImageFrame.Packed(pixels, width, height, PixelFormat.Rgba32);
        // Exercise the profile with a generous total limit on every runtime. The legacy
        // recognizer needs longer than its share of a 150 ms mixed-family call.
        Assert.Contains(SymbolScanner.Scan(frame, ScanOptions.Fast(5000)).Symbols, symbol => symbol.Text == text);
#if NET8_0_OR_GREATER
        // The modern path formerly reserved 120 ms before doing any work, so even a
        // warmed clean QR could not use its share of the default Fast allowance.
        var scan = SymbolScanner.Scan(frame, ScanOptions.Fast());
        Assert.Contains(scan.Symbols, symbol => symbol.Text == text);
#endif
    }

    [Fact]
    public void ExpiredFamilyDoesNotCancelSiblingButMarksIncompleteRecognition() {
        using var complete = new ScanDeadline(CancellationToken.None, 5000);
        using (var attempt = complete.CreateAttempt(1, 2)) {
            Assert.True(SpinWait.SpinUntil(() => attempt.ShouldStop, 1000));
        }
        Assert.False(complete.ShouldStop);
        Assert.True(complete.RecognitionBudgetExceeded);
        using var sibling = complete.CreateAttempt(1, 1000);
        Assert.False(sibling.ShouldStop);
    }

    [Fact]
    public void NestedTypeAllowancePropagatesIncompleteRecognition() {
        using var complete = new ScanDeadline(CancellationToken.None, 5000);
        using (var family = complete.CreateAttempt(1, 1000)) {
            using (var type = family.CreateAttempt(1, 2)) {
                Assert.True(SpinWait.SpinUntil(() => type.ShouldStop, 1000));
            }
            Assert.False(family.ShouldStop);
        }
        Assert.False(complete.ShouldStop);
        Assert.True(complete.RecognitionBudgetExceeded);
    }

    [Fact]
    public void ExpiredRecognitionAllowanceIsReportedWithLiveTotalDeadline() {
        const int size = 2400;
        var pixels = new byte[size * size * 4];
        for (var i = 0; i < pixels.Length; i += 4) {
            var value = (byte)((i / 4 % 17) < 8 ? 0 : 255);
            pixels[i] = pixels[i + 1] = pixels[i + 2] = value;
            pixels[i + 3] = 255;
        }
        var scan = SymbolScanner.Scan(ImageFrame.Packed(pixels, size, size, PixelFormat.Rgba32), new ScanOptions {
            Formats = new[] { SymbolFormat.QrCode }, TimeoutMilliseconds = 5000, EnableTileScan = false,
            Qr = new QrPixelDecodeOptions { BudgetMilliseconds = 1, MaxScale = 1 }
        });
        Assert.Equal(ScanStatus.NoSymbolFound, scan.Status);
        Assert.Equal(ScanCompletionReason.RecognitionBudgetExceeded, scan.CompletionReason);
        Assert.True(scan.IsPartial);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CancellationOrDeadlineDuringCodecRejectionKeepsStructuredOutcome(bool timeout) {
        using var caller = new CancellationTokenSource();
        Action<ImageDecodeLimitViolation> handler = _ => {
            if (timeout) Thread.Sleep(20);
            else caller.Cancel();
        };
        ImageReader.LimitViolation += handler;
        try {
            var scan = SymbolScanner.Scan(new byte[2], new ScanOptions {
                CancellationToken = caller.Token, TimeoutMilliseconds = timeout ? 10 : 5000,
                Image = new ImageDecodeOptions { MaxBytes = 1 }
            });
            Assert.Equal(timeout ? ScanStatus.DeadlineExceeded : ScanStatus.Cancelled, scan.Status);
            Assert.Equal(timeout ? ScanCompletionReason.DeadlineExceeded : ScanCompletionReason.Cancelled, scan.CompletionReason);
        } finally {
            ImageReader.LimitViolation -= handler;
        }
    }
}
