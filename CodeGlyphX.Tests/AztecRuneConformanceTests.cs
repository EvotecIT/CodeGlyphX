using System;
using System.Globalization;
using System.IO;
using System.Linq;
using CodeGlyphX.Internal;
using CodeGlyphX.Rendering;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class AztecRuneConformanceTests {
    [Fact]
    public void AllValues_MatchAndDecodeIndependentBwippModules() {
        using var stream = Open("Conformance.bwipp-aztec-runes.txt");
        using var reader = new StreamReader(stream);
        var values = reader.ReadToEnd().Split('\n').Where(line => line.Length > 0 && line[0] != '#').ToArray();
        Assert.Equal(256, values.Length);
        foreach (var fixture in values) {
            var parts = fixture.Split(':');
            var value = int.Parse(parts[0], CultureInfo.InvariantCulture);
            var bits = parts[1].TrimEnd('\r');
            var independent = Grid(bits);
            var encoded = AztecRune.Encode(value);
            Assert.Equal(SymbolFormat.AztecRune, encoded.Format);
            Assert.Equal(bits, Bits(encoded.Modules));
            Assert.True(AztecRune.TryDecode(independent, out var decoded), value.ToString(CultureInfo.InvariantCulture));
            Assert.Equal(value, decoded);
            var unified = SymbolDecoder.Decode(independent, SymbolFormat.AztecRune);
            Assert.Equal(value.ToString(CultureInfo.InvariantCulture), unified.Text);
            Assert.Equal(new[] { (byte)value }, unified.RawBytes.ToArray());
            Assert.Equal(value, Assert.IsType<AztecRuneSymbolMetadata>(unified.Metadata).Value);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(37)]
    [InlineData(255)]
    public void RotationsAndReflections_PreserveNumericValue(int value) {
        var matrix = AztecRune.Encode(value).Modules;
        for (var mirror = 0; mirror < 2; mirror++) {
            var grid = mirror == 0 ? matrix : BitMatrixTransforms.MirrorHorizontal(matrix);
            for (var turn = 0; turn < 4; turn++) {
                Assert.True(AztecRune.TryDecode(grid, out var decoded));
                Assert.Equal(value, decoded);
                grid = BitMatrixTransforms.Rotate90(grid);
            }
        }
    }

    [Fact]
    public void TwoDamagedModeWords_AreCorrected() {
        var grid = AztecRune.Encode(37).Modules.Clone();
        grid[2, 0] = !grid[2, 0]; // word zero, bit zero
        grid[10, 3] = !grid[10, 3]; // word two, bit eight
        Assert.True(AztecRune.TryDecode(grid, out var value));
        Assert.Equal(37, value);
    }

    [Fact]
    public void FinderAndSize_MustIdentifyARune() {
        Assert.False(AztecRune.TryDecode(new BitMatrix(11, 11), out _));
        Assert.False(AztecRune.TryDecode(AztecCode.Encode("37").Modules, out _));
        var grid = AztecRune.Encode(37).Modules.Clone();
        grid[5, 5] = false;
        Assert.False(AztecRune.TryDecode(grid, out _));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(37)]
    [InlineData(255)]
    public void IndependentPng_IsRecognizedWithValueAndGeometry(int value) {
        using var stream = Open("Aztec.bwipp-rune-" + value.ToString(CultureInfo.InvariantCulture) + ".png");
        using var bytes = new MemoryStream();
        stream.CopyTo(bytes);
        var result = SymbolScanner.Scan(bytes.ToArray(), new ScanOptions {
            Formats = new[] { SymbolFormat.AztecRune }, MaxSymbols = 1, TimeoutMilliseconds = 5000
        });
        var symbol = Assert.Single(result.Symbols);
        Assert.Equal(SymbolFormat.AztecRune, symbol.Format);
        Assert.Equal(value.ToString(CultureInfo.InvariantCulture), symbol.Text);
        Assert.Equal(value, Assert.IsType<AztecRuneSymbolMetadata>(symbol.Metadata).Value);
        Assert.NotNull(symbol.Geometry);
        Assert.Empty(result.UnsupportedFormats);
    }

    [Fact]
    public void AllValues_ImageRecognitionPreservesNumericValue() {
        for (var value = 0; value < 256; value++) {
            var image = AztecRune.Encode(value).Render(OutputFormat.Png,
                new MatrixOptions { ModuleSize = 5, QuietZone = 3 }).ToArray();
            var result = SymbolScanner.Scan(image, new ScanOptions {
                Formats = new[] { SymbolFormat.AztecRune }, MaxSymbols = 1, TimeoutMilliseconds = 5000
            });
            Assert.True(result.Symbols.Count == 1, "Missing Rune value " + value.ToString(CultureInfo.InvariantCulture));
            Assert.Equal(value.ToString(CultureInfo.InvariantCulture), Assert.Single(result.Symbols).Text);
        }
    }

    [Theory]
    [InlineData(0, false, false)]
    [InlineData(1, false, false)]
    [InlineData(2, false, false)]
    [InlineData(3, false, false)]
    [InlineData(0, true, false)]
    [InlineData(1, true, false)]
    [InlineData(2, true, false)]
    [InlineData(3, true, false)]
    [InlineData(0, false, true)]
    [InlineData(1, false, true)]
    [InlineData(2, false, true)]
    [InlineData(3, false, true)]
    public void BlankOuterRow_IsRestoredInRotatedReflectedAndInvertedImages(int rotation, bool mirrored, bool inverted) {
        var source = AztecRune.Encode(68).Modules;
        var modules = new BitMatrixView(source, rotation, mirrored).ToMatrix();
        var options = new MatrixOptions { ModuleSize = 5, QuietZone = 3 };
        if (inverted) { options.Foreground = Rgba32.White; options.Background = Rgba32.Black; }
        var image = new MatrixSymbol(SymbolFormat.AztecRune, modules).Render(OutputFormat.Png, options).ToArray();
        var result = SymbolScanner.Scan(image, new ScanOptions {
            Formats = new[] { SymbolFormat.AztecRune }, MaxSymbols = 1, TimeoutMilliseconds = 5000
        });
        var symbol = Assert.Single(result.Symbols);
        Assert.Equal("68", symbol.Text);
        Assert.NotNull(symbol.Geometry);
        Assert.Equal(inverted, symbol.IsInverted);
    }

    [Theory]
    [InlineData("A")]
    [InlineData("1234567890")]
    [InlineData("A123BJC5D6E71")]
    public void OrdinaryAztecImages_AreNotMisidentifiedAsRunes(string text) {
        var image = AztecCode.Encode(text).Render(OutputFormat.Png, new MatrixOptions { ModuleSize = 5 }).ToArray();
        var result = SymbolScanner.Scan(image, new ScanOptions {
            Formats = new[] { SymbolFormat.AztecRune }, MaxSymbols = 1, TimeoutMilliseconds = 5000
        });
        Assert.Empty(result.Symbols);
    }

    [Fact]
    public void MatrixFacade_EncodesTheNumericContractAndRendersToImage() {
        var symbol = MatrixBarcode.Encode(SymbolFormat.AztecRune, "255");
        var result = SymbolScanner.Scan(symbol.Render(OutputFormat.Png,
            new MatrixOptions { ModuleSize = 5, QuietZone = 2 }).ToArray(),
            new ScanOptions { Formats = new[] { SymbolFormat.AztecRune }, MaxSymbols = 1, TimeoutMilliseconds = 5000 });
        Assert.Equal("255", Assert.Single(result.Symbols).Text);
        Assert.Throws<ArgumentOutOfRangeException>(() => AztecRune.Encode(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => AztecRune.Encode(256));
        Assert.Throws<ArgumentException>(() => MatrixBarcode.Encode(SymbolFormat.AztecRune, "256"));
        Assert.Throws<ArgumentException>(() => MatrixBarcode.Encode(SymbolFormat.AztecRune, "-1"));
        Assert.Throws<ArgumentException>(() => MatrixBarcode.Encode(SymbolFormat.AztecRune, "A"));
        Assert.Throws<ArgumentNullException>(() => AztecRune.TryDecode(null!, out _));
    }

    private static Stream Open(string name) => typeof(AztecRuneConformanceTests).Assembly
        .GetManifestResourceStream("CodeGlyphX.Tests.Fixtures." + name)
        ?? throw new InvalidOperationException("Missing independent Rune fixture.");

    private static BitMatrix Grid(string bits) {
        var result = new BitMatrix(11, 11);
        for (var y = 0; y < 11; y++)
            for (var x = 0; x < 11; x++) result[x, y] = bits[y * 11 + x] == '1';
        return result;
    }

    private static string Bits(BitMatrix modules) => string.Concat(Enumerable.Range(0, 121)
        .Select(i => modules[i % 11, i / 11] ? '1' : '0'));
}
