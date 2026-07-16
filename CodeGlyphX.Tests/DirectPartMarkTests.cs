using CodeGlyphX.DataMatrix;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class DirectPartMarkTests {
    [Fact]
    public void Scanner_RecoversLowContrastLaserEtch_AndReportsPreprocessing() {
        var modules = DataMatrixEncoder.Encode("DPM-LASER-ETCH-42");
        var frame = RenderLowContrast(modules, scale: 6, quiet: 4);

        var ordinary = SymbolScanner.Scan(frame, new ScanOptions { Formats = new[] { SymbolFormat.DataMatrix } });
        Assert.False(ordinary.IsSuccess);

        var recovered = SymbolScanner.Scan(frame, new ScanOptions {
            Formats = new[] { SymbolFormat.DataMatrix },
            DirectPartMarking = DirectPartMarkOptions.LaserEtch()
        });
        var symbol = Assert.Single(recovered.Symbols);
        Assert.Equal("DPM-LASER-ETCH-42", symbol.Text);
        Assert.True(symbol.WasDirectPartMarkPreprocessed);
        Assert.Equal(DirectPartMarkProfile.LaserEtch, symbol.DirectPartMarkProfile);
    }

    [Fact]
    public void RobustProfile_EnablesDirectPartMarkRecovery() {
        var modules = DataMatrixEncoder.Encode("DPM-ROBUST");
        var frame = RenderLowContrast(modules, scale: 5, quiet: 3);
        var options = ScanOptions.Robust(timeoutMilliseconds: 0);
        options.Formats = new[] { SymbolFormat.DataMatrix };

        var result = SymbolScanner.Scan(frame, options);

        Assert.Equal("DPM-ROBUST", Assert.Single(result.Symbols).Text);
    }

    [Fact]
    public void InvalidDirectPartMarkOptions_AreRejectedBeforeScanning() {
        var frame = ImageFrame.Packed(new byte[] { 255, 255, 255, 255 }, 1, 1, PixelFormat.Rgba32);
        Assert.Throws<System.ArgumentOutOfRangeException>(() => SymbolScanner.Scan(frame, new ScanOptions {
            DirectPartMarking = new DirectPartMarkOptions { MorphologyRadius = 4 }
        }));
    }

    private static ImageFrame RenderLowContrast(BitMatrix modules, int scale, int quiet) {
        var width = (modules.Width + quiet * 2) * scale;
        var height = (modules.Height + quiet * 2) * scale;
        var rgba = new byte[width * height * 4];
        for (var y = 0; y < height; y++) {
            for (var x = 0; x < width; x++) {
                var moduleX = x / scale - quiet;
                var moduleY = y / scale - quiet;
                var dark = moduleX >= 0 && moduleY >= 0 && moduleX < modules.Width && moduleY < modules.Height && modules[moduleX, moduleY];
                var illumination = x * 24 / width;
                var value = (byte)((dark ? 142 : 182) + illumination);
                var p = (y * width + x) * 4;
                rgba[p] = rgba[p + 1] = rgba[p + 2] = value;
                rgba[p + 3] = 255;
            }
        }
        return ImageFrame.Packed(rgba, width, height, PixelFormat.Rgba32);
    }
}
