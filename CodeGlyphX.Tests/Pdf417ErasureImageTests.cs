using CodeGlyphX.Internal;
using System;
using System.IO;
using CodeGlyphX.Pdf417;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("ImageScannerSerial")]
public sealed class Pdf417ErasureImageTests {
    [Theory]
    [InlineData("zxing-damaged-strip.png", "This is PDF417")]
    [InlineData("zxing-occluded-rows.png", "A larger PDF417 barcode with text and error correction level 4.")]
    [InlineData("zxing-skewed-numeric.png", "1234567890")]
    [InlineData("zxing-damaged-right-frame.png", "A larger PDF417 barcode with text and error correction level 4.")]
    [InlineData("zxing-shallow-angle-priority.png", "A PDF 417 barcode with ASCII text")]
    [InlineData("zxing-left-cropped-start.png", "A larger PDF417 barcode with text and error correction level 8.")]
    [InlineData("zxing-left-cropped-indicator.png", "A larger PDF417 barcode with text and error correction level 8.")]
    [InlineData("zxing-left-cropped-data.png", "A larger PDF417 barcode with text and error correction level 8.")]
    [InlineData("zxing-missing-stop-and-starts.png", "This is PDF417")]
    public void IndependentDamagedAndSkewedImages_PreserveExactPayload(string name, string expected) {
        using var fixture = typeof(Pdf417ErasureImageTests).Assembly.GetManifestResourceStream("CodeGlyphX.Tests.Fixtures.Pdf417." + name);
        Assert.NotNull(fixture);
        using var buffer = new MemoryStream();
        fixture!.CopyTo(buffer);
        var options = ScanOptions.Robust(TestBudget.Adjust(5000));
        options.Formats = new[] { SymbolFormat.Pdf417 };
        options.MaxSymbols = 1;

        Assert.Equal(expected, Assert.Single(SymbolScanner.Scan(buffer.ToArray(), options).Symbols).Text);
    }

    [Theory]
    [InlineData(-0.025)]
    [InlineData(0.025)]
    public void ShallowSkewWithDamagedRightFraming_PreservesReadableDataRows(double slope) {
        const string payload = "RIGHT-FRAME-DAMAGE-KEEPS-VALID-DATA-0417";
        var modules = Pdf417Encoder.EncodeSymbol(payload, new Pdf417EncodeOptions {
            ErrorCorrectionLevel = 4, MinColumns = 6, MaxColumns = 6, MinRows = 14, MaxRows = 14
        }).Modules.Clone();
        // Keep three framed rows to establish the header independently. The
        // remaining rows retain every data character but lose both right-side
        // patterns, with no compact terminator to justify a different symbol.
        var tail = 34 + 6 * 17;
        for (var y = 3; y < modules.Height; y++)
            for (var x = tail; x < modules.Width; x++) {
                var offset = x - tail;
                modules[x, y] = offset < 2 || offset >= 3 && offset < 5 || offset >= 6;
            }
        var pixels = MatrixPngRenderer.RenderPixels(modules,
            new MatrixPngRenderOptions { ModuleSize = 3, QuietZone = 4 }, out var width, out var height, out var stride);
        var offsetY = (int)Math.Ceiling(Math.Abs(slope) * width);
        var skewed = new byte[stride * (height * 3 + offsetY)];
        for (var i = 0; i < skewed.Length; i++) skewed[i] = 255;
        for (var y = 0; y < height; y++) for (var repeat = 0; repeat < 3; repeat++) for (var x = 0; x < width; x++) {
            var shiftedY = y * 3 + repeat + (int)Math.Round(x * slope) + (slope < 0 ? offsetY : 0);
            Buffer.BlockCopy(pixels, y * stride + x * 4, skewed, shiftedY * stride + x * 4, 4);
        }
        pixels = skewed;
        height = height * 3 + offsetY;
        var original = (byte[])pixels.Clone();
        using var deadline = DecodeBudget.Begin(TestBudget.Adjust(5000));

        Assert.True(Pdf417Decoder.TryDecode(pixels, width, height, stride, PixelFormat.Rgba32, out string text));
        Assert.Equal(payload, text);
        Assert.Equal(original, pixels);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DamagedRowTails_PreserveReadablePrefixes(bool compact) {
        const string payload = "KEEP-READABLE-ROW-PREFIXES-0417";
        var symbol = Pdf417Encoder.EncodeSymbol(payload, new Pdf417EncodeOptions {
            Compact = compact, ErrorCorrectionLevel = 4,
            MinColumns = 6, MaxColumns = 6, MinRows = 14, MaxRows = 14
        });
        var modules = symbol.Modules.Clone();
        // Destroy the fifth payload character in eleven rows. Keeping the
        // readable four-character prefixes leaves 22 erasures, rather than
        // losing all 66 codewords in those rows.
        for (var y = 2; y <= 12; y++)
            for (var x = 34 + 4 * 17; x < 34 + 5 * 17; x++) modules[x, y] = x == 34 + 4 * 17;
        var pixels = MatrixPngRenderer.RenderPixels(modules,
            new MatrixPngRenderOptions { ModuleSize = 3, QuietZone = 4 }, out var width, out var height, out var stride);
        var original = (byte[])pixels.Clone();
        using var deadline = DecodeBudget.Begin(TestBudget.Adjust(5000));

        Assert.True(Pdf417Decoder.TryDecode(pixels, width, height, stride, PixelFormat.Rgba32, out string text));
        Assert.Equal(payload, text);
        Assert.Equal(original, pixels);
    }

    [Theory]
    [InlineData(false, 0)]
    [InlineData(false, 4)]
    [InlineData(true, 0)]
    [InlineData(true, 4)]
    public void MissingRowsWithinErasureCapacity_RecoverExactPayload(bool compact, int firstMissingRow) {
        const string payload = "DAMAGED-ROWS-RECOVER-EXACT-0417";
        var symbol = Pdf417Encoder.EncodeSymbol(payload, new Pdf417EncodeOptions {
            Compact = compact, ErrorCorrectionLevel = 4,
            MinColumns = 6, MaxColumns = 6, MinRows = 12, MaxRows = 12
        });
        var modules = symbol.Modules.Clone();
        // Four missing rows contain 24 codewords: beyond the 16-unknown-error
        // limit, but within the 32-known-erasure limit at correction level 4.
        for (var y = firstMissingRow; y < firstMissingRow + 4; y++)
            for (var x = 0; x < modules.Width; x++) modules[x, y] = false;
        var pixels = MatrixPngRenderer.RenderPixels(modules,
            new MatrixPngRenderOptions { ModuleSize = 3, QuietZone = 4 }, out var width, out var height, out var stride);
        var original = (byte[])pixels.Clone();
        using var deadline = DecodeBudget.Begin(TestBudget.Adjust(5000));

        Assert.True(Pdf417Decoder.TryDecode(pixels, width, height, stride, PixelFormat.Rgba32, out string text));
        Assert.Equal(payload, text);
        Assert.Equal(original, pixels);
    }
}
