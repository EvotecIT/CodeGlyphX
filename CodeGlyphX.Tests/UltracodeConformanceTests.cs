using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using CodeGlyphX.Ultracode;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class UltracodeConformanceTests {
    [Fact]
    public void IndependentColourCellsQualifyAllLevelsAndBothDccRevisions() {
        using var stream = typeof(UltracodeConformanceTests).Assembly.GetManifestResourceStream(
            "CodeGlyphX.Tests.Fixtures.Conformance.bwipp-ultracode.txt");
        Assert.NotNull(stream);
        using var reader = new StreamReader(stream!);
        var records = reader.ReadToEnd().Split('\n').Where(line => line.Length > 0 && line[0] != '#').ToArray();
        Assert.Equal(70, records.Length);
        var heights = new System.Collections.Generic.HashSet<int>();
        foreach (var record in records) {
            var parts = record.TrimEnd('\r').Split('|');
            var level = (UltracodeErrorCorrectionLevel)int.Parse(parts[0], CultureInfo.InvariantCulture);
            var bytes = Enumerable.Range(0, parts[2].Length / 2)
                .Select(i => byte.Parse(parts[2].Substring(i * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture)).ToArray();
            var grid = new UltracodeGrid(int.Parse(parts[3], CultureInfo.InvariantCulture), int.Parse(parts[4], CultureInfo.InvariantCulture));
            heights.Add(grid.Height);
            for (var y = 0; y < grid.Height; y++) for (var x = 0; x < grid.Width; x++)
                grid[x, y] = (UltracodeColor)(parts[5][y * grid.Width + x] - '0');
            Assert.True(UltracodeDecoder.TryDecode(grid, out var result), "Independent decode: " + string.Join("|", parts.Take(5)));
            Assert.Equal(bytes, result!.Bytes);
            Assert.Equal(level, result.ErrorCorrectionLevel);
            Assert.Equal(0, result.CorrectedCodewords);
            if (parts[1] == "2") {
                var encoded = UltracodeEncoder.EncodeBytes(bytes, level);
                Assert.Equal(grid.Width, encoded.Cells.Width);
                Assert.Equal(grid.Height, encoded.Cells.Height);
                Assert.Equal(parts[5], Cells(encoded.Cells));
            }
        }
        Assert.Equal(new[] { 13, 19, 25, 31 }, heights.OrderBy(height => height));
    }

    [Theory]
    [InlineData(UltracodeErrorCorrectionLevel.EC1, 263)]
    [InlineData(UltracodeErrorCorrectionLevel.EC2, 252)]
    [InlineData(UltracodeErrorCorrectionLevel.EC3, 234)]
    [InlineData(UltracodeErrorCorrectionLevel.EC4, 220)]
    [InlineData(UltracodeErrorCorrectionLevel.EC5, 202)]
    public void CapacityAndInputRepertoireAreExplicit(UltracodeErrorCorrectionLevel level, int capacity) {
        var symbol = UltracodeEncoder.EncodeBytes(new byte[capacity], level);
        Assert.True(UltracodeDecoder.TryDecode(symbol.Cells, out var result));
        Assert.Equal(new byte[capacity], result!.Bytes);
        Assert.Throws<ArgumentException>(() => UltracodeEncoder.EncodeBytes(new byte[capacity + 1], level));
        Assert.Throws<ArgumentException>(() => UltracodeEncoder.EncodeText("世界", level));
    }

    [Fact]
    public void CorrectsUnknownTilesAndSubstitutedTilesThroughThePrimeFieldEngine() {
        const string text = "Case 123!";
        var encoded = UltracodeEncoder.EncodeText(text, UltracodeErrorCorrectionLevel.EC5);
        var cells = encoded.Cells.Clone();
        var messageCount = text.Length + 3;
        var parityCount = UltracodeLayout.ParityCount(messageCount, UltracodeErrorCorrectionLevel.EC5);
        var layout = new UltracodeLayout(messageCount, parityCount);
        layout.TilePosition(2, out var x, out var y);
        cells[x, y] = UltracodeColor.Unknown;
        layout.TilePosition(parityCount + 5, out x, out y);
        for (var row = 0; row < 5; row++) cells[x, y + row] = encoded.Cells[5, 1 + row];
        Assert.True(UltracodeDecoder.TryDecode(cells, out var decoded));
        Assert.Equal(text, decoded!.Text);
        Assert.Equal(2, decoded.CorrectedCodewords);
        var cancelled = new CancellationToken(canceled: true);
        Assert.False(UltracodeDecoder.TryDecode(cells, out _, cancelled));
        Assert.True(encoded.Cells.IsReadOnly);
        Assert.Throws<InvalidOperationException>(() => encoded.Cells[0, 0] = UltracodeColor.White);
    }

    private static string Cells(UltracodeGrid grid) {
        var values = new char[grid.Width * grid.Height];
        for (var y = 0; y < grid.Height; y++) for (var x = 0; x < grid.Width; x++) values[y * grid.Width + x] = (char)('0' + (int)grid[x, y]);
        return new string(values);
    }

    [Fact]
    public void ProtectedHeaderTilesAreRecoveredAndTooManyErasuresAreRejected() {
        const string text = "Protected headers";
        var symbol = UltracodeEncoder.EncodeText(text, UltracodeErrorCorrectionLevel.EC3);
        var parity = UltracodeLayout.ParityCount(text.Length + 3, UltracodeErrorCorrectionLevel.EC3);
        var layout = new UltracodeLayout(text.Length + 3, parity);
        var damaged = symbol.Cells.Clone();
        foreach (var tile in new[] { 0, 1, parity + 4 }) {
            layout.TilePosition(tile, out var x, out var y);
            damaged[x, y] = UltracodeColor.Unknown;
        }
        Assert.True(UltracodeDecoder.TryDecode(damaged, out var restored));
        Assert.Equal(text, restored!.Text);
        Assert.Equal(3, restored.CorrectedCodewords);
        for (var tile = 2; tile < parity; tile++) {
            layout.TilePosition(tile, out var x, out var y);
            damaged[x, y] = UltracodeColor.Unknown;
        }
        Assert.False(UltracodeDecoder.TryDecode(damaged, out _));
    }
}
