using System;
using System.Buffers;
using System.Collections.Generic;
using CodeGlyphX.DataBar;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("ImageScannerSerial")]
public sealed class DataBarExpandedWorkspaceTests {
    [Theory]
    [InlineData(1, 0, false)]
    [InlineData(2, 1, true)]
    [InlineData(3, 2, false)]
    [InlineData(10, 3, true)]
    [InlineData(0, 0, false)]
    public void Scanner_PreservesMaximumAndShortRowsAfterDirtyPoolAndSequentialReuse(int columns, int turns, bool inverted) {
        // A maximum symbol exercises the 235-element single-row bound and the
        // same data in short, alternating physical rows. Each call then releases
        // its workspace before a smaller, differently oriented symbol uses it.
        foreach (var input in new[] { "(91)" + new string('A', 38), "(10)123", "(91)" + new string('A', 38) }) {
            DirtyIntegerPool();
            var matrix = EncodeRows(input, columns);
            var pixels = Render(matrix, out var width, out var height);
            for (var turn = 0; turn < turns; turn++) pixels = Rotate(pixels, ref width, ref height);
            if (inverted) for (var at = 0; at < pixels.Length; at += 4)
                pixels[at] = pixels[at + 1] = pixels[at + 2] = (byte)(255 - pixels[at]);
            var options = ScanOptions.Robust(TestBudget.Adjust(5000));
            options.Formats = new[] { SymbolFormat.Gs1DataBarExpandedStacked };
            options.MaxSymbols = 1;
            options.EnableTileScan = false;
            options.Image = new ImageDecodeOptions { MaxDimension = 0 };

            var result = SymbolScanner.Scan(ImageFrame.Packed(pixels, width, height, PixelFormat.Rgba32), options);

            var symbol = Assert.Single(result.Symbols);
            Assert.Equal(SymbolFormat.Gs1DataBarExpandedStacked, symbol.Format);
            Assert.Equal(Gs1.ElementString(input), symbol.Text);
            Assert.Equal(SymbolPayloadProfile.Gs1, symbol.PayloadProfile);
            Assert.Equal(ScanCompletionReason.SymbolLimitReached, result.CompletionReason);
        }
    }

    private static BitMatrix EncodeRows(string input, int columns) {
        if (columns > 0) return DataBarExpandedEncoder.EncodeExpandedStacked(input, columns);
        // Typed stacked recognition accepts the same physical single-row
        // Expanded encoding. Use it to reach all 235 widths through public APIs.
        var barcode = DataBarExpandedEncoder.EncodeExpanded(input);
        var matrix = new BitMatrix(barcode.TotalModules, 1);
        var x = 0;
        foreach (var segment in barcode.Segments)
            for (var i = 0; i < segment.Modules; i++) matrix[x++, 0] = segment.IsBar;
        return matrix;
    }

    private static void DirtyIntegerPool() {
        var buffers = new List<int[]>();
        foreach (var size in new[] { 32, 64, 128, 256, 512, 1024, 2048, 4096 }) {
            var buffer = ArrayPool<int>.Shared.Rent(size);
            for (var i = 0; i < buffer.Length; i++) buffer[i] = -1234567;
            buffers.Add(buffer);
        }
        foreach (var buffer in buffers) ArrayPool<int>.Shared.Return(buffer);
    }

    private static byte[] Render(BitMatrix matrix, out int width, out int height) {
        const int quiet = 4;
        width = matrix.Width * 2 + quiet * 2;
        height = quiet * 2;
        for (var y = 0; y < matrix.Height; y++) height += y % 4 == 0 ? 32 : 2;
        var pixels = new byte[width * height * 4];
        for (var i = 0; i < pixels.Length; i++) pixels[i] = 255;
        var top = quiet;
        for (var y = 0; y < matrix.Height; y++) {
            var rowHeight = y % 4 == 0 ? 32 : 2;
            for (var py = top; py < top + rowHeight; py++)
                for (var x = 0; x < matrix.Width; x++) if (matrix[x, y])
                    for (var dx = 0; dx < 2; dx++) {
                        var at = (py * width + quiet + x * 2 + dx) * 4;
                        pixels[at] = pixels[at + 1] = pixels[at + 2] = 0;
                    }
            top += rowHeight;
        }
        return pixels;
    }

    private static byte[] Rotate(byte[] pixels, ref int width, ref int height) {
        var rotated = new byte[pixels.Length];
        for (var y = 0; y < height; y++) for (var x = 0; x < width; x++)
            Buffer.BlockCopy(pixels, (y * width + x) * 4, rotated, (x * height + height - 1 - y) * 4, 4);
        var previousWidth = width; width = height; height = previousWidth;
        return rotated;
    }
}
