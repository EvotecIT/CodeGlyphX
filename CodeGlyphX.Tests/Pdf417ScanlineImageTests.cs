using System;
using CodeGlyphX.Internal;
using CodeGlyphX.Pdf417;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("ImageScannerSerial")]
public sealed class Pdf417ScanlineImageTests {
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void CompactModuleWidths_DecodeExactPayload(int columns) {
        const string payload = "COMPACT-WIDTH-0417";
        var modules = Pdf417Encoder.Encode(payload, new Pdf417EncodeOptions {
            Compact = true, ErrorCorrectionLevel = 2, MinColumns = columns, MaxColumns = columns
        });

        Assert.Equal(columns * 17 + 35, modules.Width);
        Assert.True(Pdf417Decoder.TryDecode(modules, out string text));
        Assert.Equal(payload, text);
    }

    [Theory]
    [InlineData(false, 1, 1, true)]
    [InlineData(false, 2, 3, true)]
    [InlineData(true, 1, 1, true)]
    [InlineData(true, 1, 3, true)]
    [InlineData(true, 2, 0, false)]
    [InlineData(false, 2, 2, false)]
    [InlineData(true, 2, 2, true)]
    public void RotatedAndInvertedRows_PreserveExactPayload(bool compact, int columns, int rotation, bool inverted) {
        const string payload = "SCANLINE-REUSE-0417";
        var modules = Pdf417Encoder.Encode(payload, new Pdf417EncodeOptions {
            Compact = compact, ErrorCorrectionLevel = 2, MinColumns = columns, MaxColumns = columns
        });
        modules = rotation == 0 ? modules : rotation == 1 ? BitMatrixTransforms.Rotate90(modules)
            : rotation == 2 ? BitMatrixTransforms.Rotate180(modules) : BitMatrixTransforms.Rotate270(modules);
        var pixels = MatrixPngRenderer.RenderPixels(modules,
            new MatrixPngRenderOptions { ModuleSize = 3, QuietZone = 4 }, out var width, out var height, out var stride);
        if (inverted) for (var i = 0; i < pixels.Length; i += 4) {
            pixels[i] = (byte)(255 - pixels[i]);
            pixels[i + 1] = (byte)(255 - pixels[i + 1]);
            pixels[i + 2] = (byte)(255 - pixels[i + 2]);
        }
        var original = (byte[])pixels.Clone();
        using var deadline = DecodeBudget.Begin(TestBudget.Adjust(5000));

        Assert.True(Pdf417Decoder.TryDecode(pixels, width, height, stride, PixelFormat.Rgba32, out string text));
        Assert.Equal(payload, text);
        Assert.Equal(original, pixels);
    }
}
