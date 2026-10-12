using CodeGlyphX.Qr;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class QrPhaseSelectionTests {
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void UniformScoresRetainTheFirstFineCandidate(bool adaptive, bool invert) {
        const int size = 224;
        var pixels = new byte[size * size * 4];
        for (var i = 0; i < pixels.Length; i += 4) {
            pixels[i] = pixels[i + 1] = pixels[i + 2] = 127;
            pixels[i + 3] = 255;
        }
        Assert.True(QrGrayImage.TryCreate(pixels, size, size, size * 4, PixelFormat.Rgba32,
            1, minContrast: 0, out var image));
        image = adaptive ? image.WithHybridThreshold(8, 24, 0) : image.WithThreshold(128);

        QrPixelSampling.RefinePhase(image, invert, 13, 13, 1, 0.025, -0.025, 1, 41, out var x, out var y);

        // All candidates tie. The original fine traversal replaces the coarse
        // winner with its first equal candidate, then keeps that candidate.
        Assert.Equal(-0.35 - 0.10, x);
        Assert.Equal(-0.35 - 0.10, y);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SymbolPhasePreservesTheQualifiedSamplingChoice(bool invert) {
        const int size = 224;
        var qr = QrCodeEncoder.EncodeText("PHASE", QrErrorCorrectionLevel.M,
            minVersion: 6, maxVersion: 6, forceMask: 5);
        var pixels = new byte[size * size * 4];
        System.Array.Fill(pixels, (byte)255);
        for (var y = 0; y < qr.Modules.Height; y++) for (var x = 0; x < qr.Modules.Width; x++) {
            var offset = ((y + 10) * size + x + 10) * 4;
            var value = qr.Modules[x, y] ? (byte)0 : (byte)255;
            pixels[offset] = pixels[offset + 1] = pixels[offset + 2] = value;
        }
        Assert.True(QrGrayImage.TryCreate(pixels, size, size, size * 4, PixelFormat.Rgba32,
            1, minContrast: 0, out var image));

        QrPixelSampling.RefinePhase(image.WithThreshold(128), invert, 13, 13,
            1, 0.025, -0.025, 1, 41, out var phaseX, out var phaseY);

        // Exact previous-core selection for this module image and affine estimate.
        Assert.Equal(0.25, phaseX);
        Assert.Equal(-0.35, phaseY);
    }
}
