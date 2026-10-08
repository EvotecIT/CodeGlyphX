using System;
using System.Text;
using System.Threading;
using CodeGlyphX.Pdf417;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class Pdf417FractionalSamplingTests {
    private const string Payload = "DISPATCH-漢字-1042";

    [Theory]
    [InlineData(true, 592, 124, 0)] // 216-point compact document rasterized at 216 dpi.
    [InlineData(true, 592, 124, 90)]
    [InlineData(true, 592, 124, 270)]
    [InlineData(false, 810, 122, 0)] // Rounded pitch 7 misses the regular width by 30 pixels.
    [InlineData(false, 810, 122, 90)]
    [InlineData(false, 810, 122, 270)]
    [InlineData(true, 516, 108, 0)] // Integer compact control.
    [InlineData(true, 516, 108, 90)]
    [InlineData(true, 516, 108, 270)]
    [InlineData(false, 720, 108, 0)] // Integer regular control.
    [InlineData(false, 720, 108, 90)]
    [InlineData(false, 720, 108, 270)]
    public void WholePageMacroRaster_DecodesExactTextAndMetadata(bool compact, int bodyWidth, int bodyHeight, int rotation) {
        var symbol = Pdf417Code.EncodeMacro(Payload, new Pdf417MacroOptions {
            FileId = "123456", SegmentIndex = 0, IsLastSegment = true, SegmentCount = 1, FileName = "dispatch.txt"
        }, new Pdf417EncodeOptions {
            Compact = compact, Compaction = Pdf417Compaction.Byte, TextEncoding = Encoding.UTF8,
            ErrorCorrectionLevel = 2, MinColumns = 3, MaxColumns = 3, MinRows = 18, MaxRows = 18
        });
        var png = RasterizeWholePage(symbol.Modules, bodyWidth, bodyHeight, rotation);
        var original = (byte[])png.Clone();

        Assert.True(Pdf417Code.TryDecodePng(png, Limits(), out string text));
        Assert.Equal(Payload, text);
        Assert.True(Pdf417Code.TryDecodePng(png, Limits(), out text, out var diagnostics));
        Assert.Equal(Payload, text);
        Assert.True(diagnostics.Success);
        Assert.Null(diagnostics.Failure);
        Assert.True(Pdf417Code.TryDecodePng(png, Limits(), out Pdf417Decoded decoded));
        Assert.Equal(Payload, decoded.Text);
        Assert.NotNull(decoded.Macro);
        Assert.Equal("123456", decoded.Macro!.FileId);
        Assert.Equal(0, decoded.Macro.SegmentIndex);
        Assert.True(decoded.Macro.IsLastSegment);
        Assert.Equal(1, decoded.Macro.SegmentCount);
        Assert.Equal("dispatch.txt", decoded.Macro.FileName);
        Assert.Equal(original, png);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(90)]
    [InlineData(270)]
    public void FractionalRaster_WithUncorrectableRows_DoesNotReturnPartialText(int rotation) {
        var modules = Pdf417Encoder.Encode("ASCII-CONTROL", new Pdf417EncodeOptions {
            Compaction = Pdf417Compaction.Text, ErrorCorrectionLevel = 0,
            MinColumns = 3, MaxColumns = 3, MinRows = 6, MaxRows = 6
        });
        var damaged = modules.Clone();
        for (var x = 0; x < modules.Width; x++) {
            damaged[x, 1] = modules[x, 4];
            damaged[x, 4] = modules[x, 1];
        }
        var png = RasterizeWholePage(damaged, 810, 41, rotation);

        Assert.False(Pdf417Code.TryDecodePng(png, Limits(), out string text, out var diagnostics));
        Assert.Empty(text);
        Assert.False(diagnostics.Success);
        Assert.False(Pdf417Code.TryDecodePng(png, Limits(), out Pdf417Decoded decoded));
        Assert.Null(decoded);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(90)]
    [InlineData(270)]
    public void FractionalRaster_PreCancelledDecode_DoesNotReturnTextOrMetadata(int rotation) {
        var modules = Pdf417Encoder.Encode(Payload, new Pdf417EncodeOptions { Compact = true });
        var png = RasterizeWholePage(modules, 592, 124, rotation);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.False(Pdf417Code.TryDecodePng(png, Limits(), cancellation.Token, out string text, out var diagnostics));
        Assert.Empty(text);
        Assert.False(diagnostics.Success);
        Assert.False(Pdf417Code.TryDecodePng(png, Limits(), cancellation.Token, out Pdf417Decoded decoded));
        Assert.Null(decoded);
    }

    private static ImageDecodeOptions Limits() => new() {
        MaxBytes = 4_000_000, MaxPixels = 1_000_000, MaxDecodedBytes = 4_000_000,
        RecognitionBudgetMilliseconds = TestBudget.Adjust(5000)
    };

    // Paint each vector module to its rounded device rectangle on a complete page. This
    // produces alternating integer bar widths when the physical pitch is fractional.
    private static byte[] RasterizeWholePage(BitMatrix modules, int bodyWidth, int bodyHeight, int rotation) {
        const int pageSize = 900, left = 40, top = 88;
        var stride = pageSize * 4;
        var pixels = new byte[pageSize * stride];
        for (var i = 0; i < pixels.Length; i++) pixels[i] = 255;
        for (var y = 0; y < modules.Height; y++) {
            var y0 = top + (int)Math.Round(y * bodyHeight / (double)modules.Height);
            var y1 = top + (int)Math.Round((y + 1) * bodyHeight / (double)modules.Height);
            for (var x = 0; x < modules.Width; x++) {
                if (!modules[x, y]) continue;
                var x0 = left + (int)Math.Round(x * bodyWidth / (double)modules.Width);
                var x1 = left + (int)Math.Round((x + 1) * bodyWidth / (double)modules.Width);
                for (var py = y0; py < y1; py++) for (var px = x0; px < x1; px++) {
                    var offset = py * stride + px * 4;
                    pixels[offset] = pixels[offset + 1] = pixels[offset + 2] = 0;
                }
            }
        }
        if (rotation == 0) return PngImageEncoder.EncodeRgba32(pixels, pageSize, pageSize);
        var rotated = new byte[pixels.Length];
        for (var y = 0; y < pageSize; y++) for (var x = 0; x < pageSize; x++) {
            var targetX = rotation == 90 ? pageSize - y - 1 : y;
            var targetY = rotation == 90 ? x : pageSize - x - 1;
            Buffer.BlockCopy(pixels, (y * pageSize + x) * 4, rotated, (targetY * pageSize + targetX) * 4, 4);
        }
        return PngImageEncoder.EncodeRgba32(rotated, pageSize, pageSize);
    }
}
