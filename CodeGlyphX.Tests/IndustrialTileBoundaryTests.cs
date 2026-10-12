using System;
using System.IO;
using System.Linq;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("ImageScannerSerial")]
public sealed class IndustrialTileBoundaryTests {
    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(3, false)]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    public void Scan_InvertedPerforatedTexture_RejectsIndustrialTileFragments(int quarterTurns, bool typed) {
        var pixels = ReadInvertedTexture(quarterTurns, out var width, out var height);
        var options = ScanOptions.Robust(TestBudget.Adjust(5000));
        options.MaxSymbols = 32;
        options.Image = new ImageDecodeOptions { MaxDimension = 0 };
        if (typed) options.Formats = new[] { SymbolFormat.Industrial2Of5 };

        var result = SymbolScanner.Scan(ImageFrame.Packed(pixels, width, height, PixelFormat.Rgba32), options);

        // An unrelated format can yield an inconclusive recognition attempt.
        // False acceptance is separate from the fixed-deadline corpus contract.
        Assert.Contains(result.CompletionReason, new[] { ScanCompletionReason.Completed, ScanCompletionReason.RecognitionBudgetExceeded });
        Assert.Empty(result.Symbols);
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, true)]
    [InlineData(false, true, true)]
    [InlineData(true, false, true)]
    [InlineData(true, true, true)]
    public void Scan_CompleteSingleDigitSymbols_PreservesTypedAndDefaultTileResults(bool inverted, bool vertical, bool typed) {
        var render = new BarcodePngRenderOptions { ModuleSize = 3, QuietZone = 20, HeightModules = 30 };
        var left = BarcodePngRenderer.RenderPixels(BarcodeEncoder.Encode(BarcodeType.Industrial2of5, "2"),
            render, out var lw, out var lh, out _);
        var right = BarcodePngRenderer.RenderPixels(BarcodeEncoder.Encode(BarcodeType.Industrial2of5, "8"),
            render, out var rw, out var rh, out _);
        var width = lw + rw; var height = Math.Max(lh, rh);
        var pixels = Enumerable.Repeat((byte)255, width * height * 4).ToArray();
        for (var y = 0; y < lh; y++) Array.Copy(left, y * lw * 4, pixels, y * width * 4, lw * 4);
        for (var y = 0; y < rh; y++) Array.Copy(right, y * rw * 4, pixels, (y * width + lw) * 4, rw * 4);
        if (inverted) for (var at = 0; at < pixels.Length; at += 4)
            for (var c = 0; c < 3; c++) pixels[at + c] = (byte)(255 - pixels[at + c]);
        if (vertical) Rotate(ref pixels, ref width, ref height);

        var options = ScanOptions.Robust(TestBudget.Adjust(5000));
        options.EnableTileScan = true; options.TileGrid = 2; options.MaxSymbols = 0;
        if (typed) options.Formats = new[] { SymbolFormat.Industrial2Of5 };
        var result = SymbolScanner.Scan(ImageFrame.Packed(pixels, width, height, PixelFormat.Rgba32), options);
        Assert.Equal(new[] { "2", "8" }, result.Symbols.Select(hit => hit.Text).OrderBy(text => text, StringComparer.Ordinal).ToArray());
        Assert.All(result.Symbols, hit => Assert.Equal(SymbolFormat.Industrial2Of5, hit.Format));
    }

    private static byte[] ReadInvertedTexture(int quarterTurns, out int width, out int height) {
        using var fixture = typeof(IndustrialTileBoundaryTests).Assembly.GetManifestResourceStream(
            "CodeGlyphX.Tests.Fixtures.Linear.zxing-perforated-negative-13.png");
        Assert.NotNull(fixture);
        using var buffer = new MemoryStream(); fixture!.CopyTo(buffer);
        var pixels = ImageReader.DecodeRgba32(buffer.ToArray(), out width, out height);
        for (var turn = 0; turn < quarterTurns; turn++) Rotate(ref pixels, ref width, ref height);
        for (var at = 0; at < pixels.Length; at += 4)
            for (var c = 0; c < 3; c++) pixels[at + c] = (byte)(255 - pixels[at + c]);
        return pixels;
    }

    private static void Rotate(ref byte[] pixels, ref int width, ref int height) {
        var rotated = new byte[pixels.Length];
        for (var y = 0; y < height; y++) for (var x = 0; x < width; x++)
            Buffer.BlockCopy(pixels, (y * width + x) * 4, rotated, (x * height + height - 1 - y) * 4, 4);
        pixels = rotated; (width, height) = (height, width);
    }
}
