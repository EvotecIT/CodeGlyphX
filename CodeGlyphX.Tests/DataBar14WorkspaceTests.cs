using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using CodeGlyphX.DataBar;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("ImageScannerSerial")]
public sealed class DataBar14WorkspaceTests {
    private static readonly string[] Values = {
        "0000000000000", "0000000000001", "0000000001596", "0000004537077",
        "0082193510642", "0200123456789", "1234567890123", "9999999999998", "9999999999999"
    };

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void ExactModules_PreserveCompletePolarityAndMissingLeadingGuardRules(bool inverted, bool trimmed) {
        foreach (var value in Values) {
            var barcode = DataBar14Encoder.EncodeTruncated(value);
            Assert.True(DataBar14Decoder.TryDecodeTruncated(barcode, out var text));
            Assert.Equal(value, text);
            Assert.True(DataBar14Decoder.TryDecodeOmnidirectional(barcode, out text));
            Assert.Equal(value, text);
            var modules = Expand(barcode).Skip(trimmed ? 1 : 0).Select(bit => bit != inverted).ToArray();
            var expected = !trimmed || !inverted;
            Assert.Equal(expected, DataBar14Decoder.TryDecodeTruncated(modules, out text));
            Assert.Equal(expected ? value : string.Empty, text);
            Assert.Equal(expected, DataBar14Decoder.TryDecodeOmnidirectional(modules, out text));
            Assert.Equal(expected ? value : string.Empty, text);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void StackedWidths_PreservePhysicalCharacterOrderAndChecksum(bool omni) {
        foreach (var value in Values) {
            var matrix = omni ? DataBar14Encoder.EncodeStackedOmnidirectional(value) : DataBar14Encoder.EncodeStacked(value);
            var success = omni ? DataBar14Decoder.TryDecodeStackedOmnidirectional(matrix, out var text)
                : DataBar14Decoder.TryDecodeStacked(matrix, out text);
            Assert.True(success);
            Assert.Equal(value, text);
            var top = RowRuns(matrix, 0);
            var bottom = RowRuns(matrix, matrix.Height - 1);
            Assert.Equal(25, top.Length);
            Assert.Equal(25, bottom.Length);
            Assert.True(DataBar14Decoder.TryDecodeStackedRows(top, bottom, out text));
            Assert.Equal(value, text);
        }
    }

    [Fact]
    public void SampledStackedRows_RejectIncompleteWidthsBeforeReadingTheWorkspace() {
        Assert.False(DataBar14Decoder.TryDecodeStackedRows(new int[24], new int[25], out var text));
        Assert.Equal(string.Empty, text);
        Assert.False(DataBar14Decoder.TryDecodeStackedRows(new int[25], new int[26], out text));
        Assert.Equal(string.Empty, text);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(44)]
    [InlineData(45)]
    [InlineData(46)]
    [InlineData(47)]
    [InlineData(8192)]
    public void InvalidModuleRunCountsAndCharacters_ReturnEmptyFailure(int length) {
        foreach (var first in new[] { false, true }) {
            var modules = Enumerable.Range(0, length).Select(i => (i % 2 == 0) == first).ToArray();
            Assert.False(DataBar14Decoder.TryDecodeTruncated(modules, out var text));
            Assert.Equal(string.Empty, text);
            Assert.False(DataBar14Decoder.TryDecodeOmnidirectional(modules, out text));
            Assert.Equal(string.Empty, text);
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void StackedImageScan_PreservesPayloadAfterDirtyIntegerPoolAndSequentialReuse(bool omni, bool inverted) {
        const string value = "0082193510642";
        var matrix = omni ? DataBar14Encoder.EncodeStackedOmnidirectional(value) : DataBar14Encoder.EncodeStacked(value);
        var pixels = Render(matrix, inverted, out var width, out var height);
        for (var attempt = 0; attempt < 2; attempt++) {
            var dirty = new List<int[]>();
            foreach (var size in new[] { 32, 64, 128, 256, 512, 1024 }) {
                var buffer = ArrayPool<int>.Shared.Rent(size);
                for (var i = 0; i < buffer.Length; i++) buffer[i] = -1234567 - attempt;
                dirty.Add(buffer);
            }
            foreach (var buffer in dirty) ArrayPool<int>.Shared.Return(buffer);
            var format = omni ? SymbolFormat.Gs1DataBarStackedOmnidirectional : SymbolFormat.Gs1DataBarStacked;
            var options = ScanOptions.Robust(TestBudget.Adjust(5000));
            options.Formats = new[] { format };
            options.MaxSymbols = 1;
            options.Image = new ImageDecodeOptions { MaxDimension = 0 };
            var result = SymbolScanner.Scan(ImageFrame.Packed(pixels, width, height, PixelFormat.Rgba32), options);
            var symbol = Assert.Single(result.Symbols);
            Assert.Equal(value, symbol.Text);
            Assert.Equal(format, symbol.Format);
        }
    }

    private static bool[] Expand(Barcode1D barcode) => barcode.Segments.SelectMany(s => Enumerable.Repeat(s.IsBar, s.Modules)).ToArray();

    private static int[] RowRuns(BitMatrix matrix, int row) {
        var runs = new List<int>();
        for (var first = 0; first < matrix.Width;) {
            var end = first + 1;
            while (end < matrix.Width && matrix[end, row] == matrix[first, row]) end++;
            runs.Add(end - first);
            first = end;
        }
        return runs.ToArray();
    }

    private static byte[] Render(BitMatrix matrix, bool inverted, out int width, out int height) {
        const int scale = 2, quiet = 4;
        var rows = matrix.Height == 5 ? new[] { 33, 1, 1, 1, 33 } : new[] { 5, 1, 7 };
        width = (matrix.Width + quiet * 2) * scale;
        height = (rows.Sum() + quiet * 2) * scale;
        var pixels = Enumerable.Repeat((byte)255, width * height * 4).ToArray();
        var top = quiet * scale;
        for (var row = 0; row < matrix.Height; row++) {
            for (var y = top; y < top + rows[row] * scale; y++) for (var x = 0; x < matrix.Width * scale; x++) {
                var at = (y * width + quiet * scale + x) * 4;
                if (matrix[x / scale, row]) pixels[at] = pixels[at + 1] = pixels[at + 2] = 0;
            }
            top += rows[row] * scale;
        }
        if (inverted) for (var i = 0; i < pixels.Length; i += 4) {
            pixels[i] = (byte)(255 - pixels[i]); pixels[i + 1] = (byte)(255 - pixels[i + 1]); pixels[i + 2] = (byte)(255 - pixels[i + 2]);
        }
        return pixels;
    }
}
