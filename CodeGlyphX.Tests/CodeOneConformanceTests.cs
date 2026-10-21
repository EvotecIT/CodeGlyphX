using System;
using System.Globalization;
using System.IO;
using System.Linq;
using CodeGlyphX.Internal;
using CodeGlyphX.Rendering;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class CodeOneConformanceTests {
    [Fact]
    public void IndependentSizesAndEncodations_DecodePayloadAndVersion() {
        using var stream = typeof(CodeOneConformanceTests).Assembly.GetManifestResourceStream(
            "CodeGlyphX.Tests.Fixtures.Conformance.bwipp-code-one.txt");
        Assert.NotNull(stream);
        using var reader = new StreamReader(stream!);
        var records = reader.ReadToEnd().Split('\n').Where(line => line.Length > 0 && line[0] != '#').ToArray();
        Assert.Equal(96, records.Length);
        foreach (var record in records) {
            var parts = record.TrimEnd('\r').Split('|');
            var version = (CodeOneVersion)int.Parse(parts[0], CultureInfo.InvariantCulture);
            var expected = Bytes(parts[1]);
            var width = int.Parse(parts[2], CultureInfo.InvariantCulture);
            var height = int.Parse(parts[3], CultureInfo.InvariantCulture);
            var grid = new BitMatrix(width, height);
            for (var y = 0; y < height; y++)
                for (var x = 0; x < width; x++) grid[x, y] = parts[4][y * width + x] == '1';
            Assert.True(CodeOneDecoder.TryDecode(grid, out var value), parts[0] + ":" + parts[1]);
            Assert.Equal(version, value.Version);
            Assert.Equal(expected, value.Bytes);
            var gs1 = parts.Length > 5 && parts[5] == "1";
            var eci = parts.Length > 6 && parts[6] == "26";
            Assert.Equal(gs1, value.IsGs1);
            Assert.Equal(eci ? System.Text.Encoding.UTF8.GetString(expected) : EncodingUtils.Latin1.GetString(expected), value.Text);
            Assert.Equal(eci ? new[] { 26 } : Array.Empty<int>(), value.EciAssignments);
            var own = CodeOneEncoder.EncodeBytes(expected, new CodeOneEncodingOptions { Version = version, IsGs1 = gs1, EciAssignmentNumber = eci ? 26 : (int?)null });
            Assert.True(CodeOneDecoder.TryDecode(own.Modules, out var recovered), parts[0] + ":" + parts[1]);
            Assert.Equal(expected, recovered.Bytes);
            Assert.Equal(value.Text, recovered.Text);
            if (parts.Length == 5 && (version is >= CodeOneVersion.S10 and <= CodeOneVersion.S30 || parts[1] == "48454c4c4f"
                || parts[1] == "8081828384" || expected.Length is 13 or 22 or 26 or 27 or 2218 or 3550))
                Assert.True(parts[4] == Modules(own.Modules), "Independent output differs: version=" + version
                    + "; bytes=" + expected.Length + "; hex=" + parts[1].Substring(0, Math.Min(parts[1].Length, 32)));
        }
    }

    [Fact]
    public void ErrorCorrectionRepairsDataButRequiresRecognitionPatterns() {
        var finderRows = new[] { 6, 8, 11, 16, 22, 31, 47, 69, 5, 5, 5, 11, 11, 11 };
        for (var version = CodeOneVersion.A; version <= CodeOneVersion.T48; version++) {
            var text = version is >= CodeOneVersion.S10 and <= CodeOneVersion.S30 ? "37" : "Data";
            var encoded = CodeOneEncoder.EncodeText(text, new CodeOneEncodingOptions { Version = version });
            var damaged = encoded.Modules.Clone();
            damaged[0, 0] = !damaged[0, 0];
            Assert.True(CodeOneDecoder.TryDecode(damaged, out var decoded));
            Assert.Equal(text, decoded.Text);
            var finder = encoded.Modules.Clone();
            finder[0, finderRows[(int)version - 1]] = false;
            Assert.False(CodeOneDecoder.TryDecode(finder, out _));
        }
        Assert.False(CodeOneDecoder.TryDecode(new BitMatrix(148, 134), out _));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(10)]
    [InlineData(23)]
    [InlineData(60)]
    [InlineData(249)]
    [InlineData(250)]
    [InlineData(3550)]
    public void NumericCompactionBoundaries_RetainAllDigits(int count) {
        var text = string.Concat(Enumerable.Range(0, count).Select(i => (char)('0' + i % 10)));
        var symbol = CodeOneEncoder.EncodeText(text);
        Assert.True(CodeOneDecoder.TryDecode(symbol.Modules, out var decoded));
        Assert.Equal(text, decoded.Text);
    }

    [Fact]
    public void PublicWorkflow_PreservesExactBytesAndControlInterpretation() {
        var bytes = Enumerable.Range(0, 256).Select(value => (byte)value).ToArray();
        var encoded = CodeOneEncoder.EncodeBytes(bytes);
        Assert.True(CodeOneDecoder.TryDecode(encoded.Modules, out var decoded));
        Assert.Equal(bytes, decoded.Bytes);
        Assert.True(SymbolDecoder.TryDecode(encoded.Modules, out var symbol, SymbolFormat.CodeOne));
        Assert.Equal(bytes, symbol.RawBytes.ToArray());
        Assert.Equal(encoded.Version, Assert.IsType<CodeOneSymbolMetadata>(symbol.Metadata).Version);
        const string text = "Case \\ text: Zażółć 世界";
        var unicode = CodeOneEncoder.EncodeText(text);
        Assert.True(CodeOneDecoder.TryDecode(unicode.Modules, out decoded));
        Assert.Equal(text, decoded.Text);
        Assert.Equal(new[] { 26 }, decoded.EciAssignments);
        var gs1 = CodeOneEncoder.EncodeText("(01)03212345678906(10)LOT", new CodeOneEncodingOptions { IsGs1 = true });
        Assert.True(CodeOneDecoder.TryDecode(gs1.Modules, out decoded));
        Assert.Equal(Gs1.ElementString("(01)03212345678906(10)LOT"), decoded.Text);
        Assert.True(decoded.IsGs1);
    }

    [Fact]
    public void NumericS_PreservesNumericValueAndRejectsOverflow() {
        var symbol = CodeOneEncoder.EncodeText("000037", new CodeOneEncodingOptions { Version = CodeOneVersion.S10 });
        Assert.True(CodeOneDecoder.TryDecode(symbol.Modules, out var decoded));
        Assert.Equal("37", decoded.Text);
        Assert.Equal(37UL, decoded.NumericValue);
        Assert.Throws<ArgumentException>(() => CodeOneEncoder.EncodeText("1000000", new CodeOneEncodingOptions { Version = CodeOneVersion.S10 }));
        Assert.Throws<ArgumentException>(() => CodeOneEncoder.EncodeText("A", new CodeOneEncodingOptions { Version = CodeOneVersion.S10 }));
    }

    [Fact]
    public void EveryFixedSize_ScansItsRenderedImageAndModuleOrientations() {
        for (var version = CodeOneVersion.A; version <= CodeOneVersion.T48; version++) {
            var text = version is >= CodeOneVersion.S10 and <= CodeOneVersion.S30 ? "37" : "Hello";
            var symbol = CodeOneEncoder.EncodeText(text, new CodeOneEncodingOptions { Version = version });
            var grid = symbol.Modules;
            for (var turn = 0; turn < 4; turn++) {
                Assert.True(CodeOneDecoder.TryDecode(grid, out var decoded));
                Assert.Equal(text, decoded.Text);
                Assert.True(CodeOneDecoder.TryDecode(BitMatrixTransforms.MirrorHorizontal(grid), out decoded));
                Assert.Equal(text, decoded.Text);
                grid = BitMatrixTransforms.Rotate90(grid);
            }
            var image = symbol.Render(OutputFormat.Png, new MatrixOptions { ModuleSize = 4, QuietZone = 3 }).ToArray();
            var scan = SymbolScanner.Scan(image, new ScanOptions { Formats = new[] { SymbolFormat.CodeOne }, MaxSymbols = 1 });
            var result = Assert.Single(scan.Symbols);
            Assert.Equal(text, result.Text);
            Assert.NotNull(result.Geometry);
            Assert.Equal(version, Assert.IsType<CodeOneSymbolMetadata>(result.Metadata).Version);
        }
    }

    private static byte[] Bytes(string hex) => Enumerable.Range(0, hex.Length / 2)
        .Select(i => byte.Parse(hex.Substring(i * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture)).ToArray();
    private static string Modules(BitMatrix grid) => string.Concat(Enumerable.Range(0, grid.Height)
        .SelectMany(y => Enumerable.Range(0, grid.Width).Select(x => grid[x, y] ? '1' : '0')));
}
