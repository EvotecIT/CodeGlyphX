using System;
using CodeGlyphX.Rendering;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class GenericPngCompressionTests {
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void GenericRenderCompressionPreservesPixelsAndReducesSize(int family) {
        var stored = Render(family, 0);
        var compressed = Render(family, 6);
        var original = ImageReader.DecodeRgba32(stored, out var width, out var height);
        var decoded = ImageReader.DecodeRgba32(compressed, out var cw, out var ch);

        Assert.Equal(width, cw);
        Assert.Equal(height, ch);
        Assert.Equal(original, decoded);
        Assert.True(compressed.Length < stored.Length);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(10)]
    public void GenericRenderRejectsInvalidCompressionLevels(int level) {
        Assert.Throws<ArgumentOutOfRangeException>(() => Render(0, level));
        Assert.Throws<ArgumentOutOfRangeException>(() => Render(1, level));
        Assert.Throws<ArgumentOutOfRangeException>(() => Render(2, level));
    }

    private static byte[] Render(int family, int level) {
        var extras = new RenderExtras { PngCompressionLevel = level };
        if (family == 1)
            return DataMatrixCode.Render("COMPRESS-MATRIX", OutputFormat.Png,
                options: new MatrixOptions { ModuleSize = 8 }, extras: extras).Data;
        if (family == 2)
            return Barcode.Render(BarcodeType.Code128, "COMPRESS-BARCODE", OutputFormat.Png,
                new BarcodeOptions { ModuleSize = 3, HeightModules = 40 }, extras).Data;
        return QrCode.Render("COMPRESS-QR", OutputFormat.Png, new QrEasyOptions { ModuleSize = 8 }, extras).Data;
    }
}
