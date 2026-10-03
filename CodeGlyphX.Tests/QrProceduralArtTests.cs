using System;
using System.Threading;
using System.Linq;
using CodeGlyphX.Qr;
using CodeGlyphX.Rendering.Art;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("ImageScannerSerial")]
public sealed class QrProceduralArtTests {
    private const string Payload = "https://example.com/art";

    [Theory]
    [InlineData(QrArtPattern.Marble, QrImageArtStyle.ModuleShape, QrImageFinderStyle.Rounded)]
    [InlineData(QrArtPattern.Waves, QrImageArtStyle.Ribbons, QrImageFinderStyle.Circular)]
    [InlineData(QrArtPattern.Sunburst, QrImageArtStyle.CrossStitch, QrImageFinderStyle.Chamfered)]
    [InlineData(QrArtPattern.Geometric, QrImageArtStyle.Mosaic, QrImageFinderStyle.Chamfered)]
    [InlineData(QrArtPattern.Botanical, QrImageArtStyle.Weave, QrImageFinderStyle.Circular)]
    [InlineData(QrArtPattern.Circuit, QrImageArtStyle.Circuit, QrImageFinderStyle.Rounded)]
    public void PatternTreatmentsProtectStructureAndRecoverPayload(QrArtPattern pattern, QrImageArtStyle style, QrImageFinderStyle finders) {
        var result = QrArt.ComposePattern(Payload, new QrArtPatternOptions { Pattern = pattern, Seed = 17, RotationDegrees = 23 },
            new QrImageCompositionOptions {
                // Qualify these busy treatments at nine pixels per module after downsampling.
                ModuleSize = 18, Strength = 0.95,
                Canvas = new QrImageCanvasOptions { PaddingModules = 6 },
                Art = new QrImageArtOptions { Style = style, Finders = finders, Scale = 0.95 }
            });
        AssertProtectedStructure(result);
#if !NET472
        // The legacy image recognizer does not support these artistic raster treatments.
        var report = QrArt.ValidateImage(result.ToPng(), Payload, 3000);
        Assert.True(report.AllPassed, string.Join(", ", report.Checks.Select(c => c.Name + "=" + c.Passed)));
#endif
    }

    [Fact]
    public void SeedReproducesPixelsAndCallerPaletteRemainsUnchanged() {
        var pattern = new QrArtPatternOptions { Pattern = QrArtPattern.Geometric, Seed = 123 };
        var palette = (Rgba32[])pattern.Colors.Clone();
        var first = QrArt.ComposePattern(Payload, pattern).GetPixels();
        Assert.Equal(first, QrArt.ComposePattern(Payload, pattern).GetPixels());
        pattern.Seed++;
        Assert.NotEqual(first, QrArt.ComposePattern(Payload, pattern).GetPixels());
        Assert.Equal(palette, pattern.Colors);
    }

    [Fact]
    public void CircuitScaleChangesStrokeWidthWithoutChangingProtectedStructure() {
        var pattern = new QrArtPatternOptions { Pattern = QrArtPattern.Circuit };
        var options = new QrImageCompositionOptions {
            ModuleSize = 18, Strength = 1,
            Art = new QrImageArtOptions { Style = QrImageArtStyle.Circuit, Scale = 0.65 }
        };
        var thin = QrArt.ComposePattern(Payload, pattern, options);
        options.Art.Scale = 1;
        var thick = QrArt.ComposePattern(Payload, pattern, options);
        Assert.NotEqual(thin.GetPixels(), thick.GetPixels());
        AssertProtectedStructure(thin);
        AssertProtectedStructure(thick);
    }

    [Fact]
    public void PatternRejectsUnrenderableInputsAndHonorsCancellation() {
        Assert.Throws<ArgumentOutOfRangeException>(() => QrArt.ComposePattern(Payload, new QrArtPatternOptions { Scale = double.NaN }));
        Assert.Throws<ArgumentOutOfRangeException>(() => QrArt.ComposePattern(Payload, new QrArtPatternOptions { RotationDegrees = double.PositiveInfinity }));
        Assert.Throws<ArgumentOutOfRangeException>(() => QrArt.ComposePattern(Payload, new QrArtPatternOptions { Pattern = (QrArtPattern)999 }));
        Assert.Throws<ArgumentException>(() => QrArt.ComposePattern(Payload, new QrArtPatternOptions { Colors = new[] { Rgba32.Black } }));
        Assert.Throws<ArgumentException>(() => QrArt.ComposePattern(Payload, new QrArtPatternOptions { Colors = new[] { Rgba32.Black, Rgba32.Transparent } }));
        Assert.Throws<OperationCanceledException>(() => QrArt.ComposePattern(Payload, cancellationToken: new CancellationToken(true)));
    }

    private static void AssertProtectedStructure(QrImageComposition result) {
        const int module = 18, border = 4 * module;
        var qr = QR.Encode(Payload, new QrEasyOptions { ErrorCorrectionLevel = QrErrorCorrectionLevel.H });
        var functions = QrStructureAnalysis.BuildFunctionMask(qr.Version, qr.Size);
        var pixels = result.GetPixels();
        for (var y = 0; y < result.QrSize; y++) for (var x = 0; x < result.QrSize; x++) {
            if (x >= border && y >= border && x < result.QrSize - border && y < result.QrSize - border) continue;
            var p = ((result.QrOffsetY + y) * result.Size + result.QrOffsetX + x) * 4;
            Assert.Equal(255, pixels[p]);
            Assert.Equal(255, pixels[p + 1]);
            Assert.Equal(255, pixels[p + 2]);
        }
        for (var y = 0; y < qr.Size; y++) for (var x = 0; x < qr.Size; x++) {
            // Finder silhouettes intentionally change; timing, alignment, format and version cells do not.
            if (!functions[x, y] || (x < 7 && (y < 7 || y >= qr.Size - 7)) || (y < 7 && x >= qr.Size - 7)) continue;
            var p = ((result.QrOffsetY + border + y * module + module / 2) * result.Size
                + result.QrOffsetX + border + x * module + module / 2) * 4;
            var expected = qr.Modules[x, y] ? (byte)0 : (byte)255;
            Assert.Equal(expected, pixels[p]);
            Assert.Equal(expected, pixels[p + 1]);
            Assert.Equal(expected, pixels[p + 2]);
        }
    }
}
