using System;
using CodeGlyphX.Internal;
using CodeGlyphX.Pdf417;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("ImageScannerSerial")]
public sealed class Pdf417RightFramingTests {
    [Theory]
    [InlineData(0, false)]
    [InlineData(0, true)]
    [InlineData(90, false)]
    [InlineData(180, false)]
    [InlineData(270, true)]
    public void LeftCropWithinErasureCapacity_PreservesExactPayload(int rotation, bool invert) {
        const string expected = "SURVIVING-RIGHT-FRAME-0417";
        var cropped = CropLeft(CreateSymbol(expected), 34 + 2 * 17);
        if (rotation == 90) cropped = BitMatrixTransforms.Rotate90(cropped);
        if (rotation == 180) cropped = BitMatrixTransforms.Rotate180(cropped);
        if (rotation == 270) cropped = BitMatrixTransforms.Rotate270(cropped);
        var pixels = MatrixPngRenderer.RenderPixels(cropped,
            new MatrixPngRenderOptions { ModuleSize = 3, QuietZone = 4 }, out var width, out var height, out var stride);
        if (invert) for (var i = 0; i < pixels.Length; i += 4) {
            pixels[i] = (byte)(255 - pixels[i]);
            pixels[i + 1] = (byte)(255 - pixels[i + 1]);
            pixels[i + 2] = (byte)(255 - pixels[i + 2]);
        }
        var original = (byte[])pixels.Clone();
        using var deadline = DecodeBudget.Begin(TestBudget.Adjust(5000));

        Assert.True(Pdf417Decoder.TryDecode(pixels, width, height, stride, PixelFormat.Rgba32, out string text));
        Assert.Equal(expected, text);
        Assert.Equal(original, pixels);
    }

    [Fact]
    public void LeftCropBeyondErasureCapacity_ReturnsNoPayload() {
        // Fourteen rows lose three data columns: 42 erasures exceed the
        // 32 available parity characters. Framing alone cannot justify text.
        var cropped = CropLeft(CreateSymbol("PREFIX-ERASURES-EXCEED-CAPACITY"), 34 + 3 * 17);
        var pixels = MatrixPngRenderer.RenderPixels(cropped,
            new MatrixPngRenderOptions { ModuleSize = 3, QuietZone = 4 }, out var width, out var height, out var stride);
        using var deadline = DecodeBudget.Begin(TestBudget.Adjust(5000));

        Assert.False(Pdf417Decoder.TryDecode(pixels, width, height, stride, PixelFormat.Rgba32, out string text));
        Assert.Empty(text);
    }

    [Fact]
    public void CompleteSymbolBesideLeftCrop_KeepsOrdinaryReaderPriority() {
        const string expected = "COMPLETE-FRAMING-KEEPS-PRIORITY";
        var complete = CreateSymbol(expected);
        var cropped = CropLeft(CreateSymbol("CROPPED-NEIGHBOR-0417"), 34 + 2 * 17);
        var combined = new BitMatrix(cropped.Width + 12 + complete.Width, complete.Height);
        for (var y = 0; y < combined.Height; y++) {
            for (var x = 0; x < cropped.Width; x++) combined[x, y] = cropped[x, y];
            for (var x = 0; x < complete.Width; x++) combined[cropped.Width + 12 + x, y] = complete[x, y];
        }
        var pixels = MatrixPngRenderer.RenderPixels(combined,
            new MatrixPngRenderOptions { ModuleSize = 3, QuietZone = 4 }, out var width, out var height, out var stride);
        using var deadline = DecodeBudget.Begin(TestBudget.Adjust(5000));

        Assert.True(Pdf417Decoder.TryDecode(pixels, width, height, stride, PixelFormat.Rgba32, out string text));
        Assert.Equal(expected, text);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public void DamagedStartsAndMissingStop_PreserveVisibleRows(bool rotate, bool invert) {
        const string expected = "MIDDLE-ROWS-ESTABLISH-FRAMING-0417";
        var original = CreateSymbol(expected);
        var modules = new BitMatrix(original.Width - 18, original.Height);
        for (var y = 0; y < modules.Height; y++)
            for (var x = 0; x < modules.Width; x++)
                modules[x, y] = original[x, y] && (x >= 17 || y >= 5 && y <= 7);
        // Only three middle rows keep starts. Every row retains both indicators
        // and all data; the final indicator space merges with the quiet zone.
        if (rotate) modules = BitMatrixTransforms.Rotate90(modules);
        var pixels = MatrixPngRenderer.RenderPixels(modules,
            new MatrixPngRenderOptions { ModuleSize = 3, QuietZone = 4 }, out var width, out var height, out var stride);
        if (invert) for (var i = 0; i < pixels.Length; i += 4) {
            pixels[i] = (byte)(255 - pixels[i]);
            pixels[i + 1] = (byte)(255 - pixels[i + 1]);
            pixels[i + 2] = (byte)(255 - pixels[i + 2]);
        }
        using var deadline = DecodeBudget.Begin(TestBudget.Adjust(5000));

        Assert.True(Pdf417Decoder.TryDecode(pixels, width, height, stride, PixelFormat.Rgba32, out string text, out var diagnostics));
        Assert.Equal(expected, text);
        Assert.True(diagnostics.Success);
        Assert.Null(diagnostics.Failure);
    }

    private static BitMatrix CreateSymbol(string text) => Pdf417Encoder.EncodeSymbol(text, new Pdf417EncodeOptions {
        ErrorCorrectionLevel = 4, MinColumns = 6, MaxColumns = 6, MinRows = 14, MaxRows = 14
    }).Modules;

    private static BitMatrix CropLeft(BitMatrix source, int removedColumns) {
        var result = new BitMatrix(source.Width - removedColumns, source.Height);
        for (var y = 0; y < result.Height; y++)
            for (var x = 0; x < result.Width; x++) result[x, y] = source[x + removedColumns, y];
        return result;
    }
}
