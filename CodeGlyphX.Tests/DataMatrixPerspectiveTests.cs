using System;
using System.IO;
using CodeGlyphX.DataMatrix;
using CodeGlyphX.Rendering;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("ImageScannerSerial")]
public sealed class DataMatrixPerspectiveTests {
    [Theory]
    [InlineData("zxing-perspective-20.png", "http://google.com/m")]
    [InlineData("zxing-perspective-40.png", "This is a test of our DataMatrix support using a longer piece of text, and therefore a more dense barcode.")]
    [InlineData("zxing-perspective-lowcontrast.png", "This is a test of our DataMatrix support using a longer piece of text, and therefore a more dense barcode.")]
    public void IndependentPhotographs_DecodeExactPayload(string name, string expected) {
        using var stream = typeof(DataMatrixPerspectiveTests).Assembly.GetManifestResourceStream(
            "CodeGlyphX.Tests.Fixtures.DataMatrix." + name);
        Assert.NotNull(stream);
        using var buffer = new MemoryStream();
        stream!.CopyTo(buffer);
        var options = ScanOptions.Robust(0);
        options.Formats = new[] { SymbolFormat.DataMatrix };
        var result = SymbolScanner.Scan(buffer.ToArray(), options);
        Assert.Equal(expected, Assert.Single(result.Symbols).Text);
    }

    [Theory]
    [InlineData(0, false, false)]
    [InlineData(1, false, false)]
    [InlineData(2, false, false)]
    [InlineData(3, false, false)]
    [InlineData(0, true, false)]
    [InlineData(0, false, true)]
    public void ProjectivePixels_PreserveGs1Metadata(int rotation, bool mirrored, bool inverted) {
        const string payload = "010123456789012810LOT42";
        var modules = DataMatrixEncoder.Encode(payload, new DataMatrixEncodingOptions {
            Rows = 24, Columns = 24, IsGs1 = true
        });
        var pixels = Warp(Orient(modules, rotation, mirrored), inverted);
        Assert.True(DataMatrixDecoder.TryDecodeDetailed(pixels, 240, 240, 960, PixelFormat.Rgba32, out var decoded));
        Assert.Equal(payload, decoded.Text);
        Assert.True(decoded.IsGs1);
        Assert.Empty(decoded.EciAssignments);
        Assert.Equal(24, decoded.Rows);
        Assert.Equal(24, decoded.Columns);
        Assert.False(decoded.IsDmre);
        Assert.True(DataMatrixDecoder.TryDecode(pixels, 240, 240, 960, PixelFormat.Rgba32, out string text, out var diagnostics));
        Assert.Equal(payload, text);
        Assert.True(diagnostics.Success);
    }

    [Fact]
    public void ProjectiveUnicodeSymbol_PreservesEciMetadata() {
        const string payload = "製品-42-żółć";
        var modules = DataMatrixEncoder.Encode(payload, new DataMatrixEncodingOptions {
            Rows = 24, Columns = 24, EciAssignmentNumber = 26
        });
        var pixels = Warp(modules, false);
        Assert.True(DataMatrixDecoder.TryDecodeDetailed(pixels, 240, 240, 960, PixelFormat.Rgba32, out var decoded));
        Assert.Equal(payload, decoded.Text);
        Assert.Equal(new[] { 26 }, decoded.EciAssignments);
        Assert.False(decoded.IsGs1);
    }

    [Fact]
    public void ProjectiveRectangularSymbol_PreservesDmreMetadata() {
        var modules = DataMatrixEncoder.Encode("DMRE-42", new DataMatrixEncodingOptions { Rows = 8, Columns = 48 });
        var pixels = Warp(modules, false);
        Assert.True(DataMatrixDecoder.TryDecodeDetailed(pixels, 240, 240, 960, PixelFormat.Rgba32, out var decoded));
        Assert.Equal("DMRE-42", decoded.Text);
        Assert.True(decoded.IsDmre);
        Assert.Equal(8, decoded.Rows);
        Assert.Equal(48, decoded.Columns);
    }

    [Fact]
    public void ProjectiveSymbolWithMissingInternalClock_IsRejected() {
        var modules = DataMatrixEncoder.Encode("FRAME-VALIDATION", new DataMatrixEncodingOptions { Rows = 40, Columns = 40 }).Clone();
        for (var x = 0; x < modules.Width; x++) modules[x, 20] = false;
        var pixels = Warp(modules, false);
        Assert.False(DataMatrixDecoder.TryDecodeDetailed(pixels, 240, 240, 960, PixelFormat.Rgba32, out _));
    }

    private static BitMatrix Orient(BitMatrix modules, int rotation, bool mirrored) {
        var oriented = new BitMatrix(modules.Width, modules.Height);
        for (var y = 0; y < modules.Height; y++)
            for (var x = 0; x < modules.Width; x++) {
                var sx = mirrored ? modules.Width - 1 - x : x;
                var ox = rotation == 1 ? modules.Height - 1 - y : rotation == 2 ? modules.Width - 1 - x : rotation == 3 ? y : x;
                var oy = rotation == 1 ? x : rotation == 2 ? modules.Height - 1 - y : rotation == 3 ? modules.Width - 1 - x : y;
                oriented[ox, oy] = modules[sx, y];
            }
        return oriented;
    }

    // Inverse of x=(4u+.25v+30)/(1+.001u+.001v),
    // y=(-.2u+4v+45)/(1+.001u+.001v). This fixture does not use
    // the decoder's transform or its corner/dimension estimates.
    private static byte[] Warp(BitMatrix modules, bool inverted) {
        var pixels = new byte[240 * 240 * 4];
        for (var y = 0; y < 240; y++)
            for (var x = 0; x < 240; x++) {
                var a = .001 * x - 4;
                var b = .001 * x - .25;
                var c = .001 * y + .2;
                var d = .001 * y - 4;
                var det = a * d - b * c;
                var u = ((30 - x) * d - b * (45 - y)) / det;
                var v = (a * (45 - y) - (30 - x) * c) / det;
                var dark = u >= 0 && v >= 0 && u < modules.Width && v < modules.Height && modules[(int)u, (int)v];
                var luminance = (byte)((dark != inverted ? 24 : 224) + x / 16);
                var at = (y * 240 + x) * 4;
                pixels[at] = pixels[at + 1] = pixels[at + 2] = luminance;
                pixels[at + 3] = 255;
            }
        return pixels;
    }
}
