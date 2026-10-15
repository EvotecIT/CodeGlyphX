using System;
using CodeGlyphX.Aztec;
using CodeGlyphX.Internal;
using CodeGlyphX.Internal.ReedSolomon;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class AztecReaderInitializationTests {
    [Fact]
    public void IndependentReaderInitializationSymbol_DecodesPayload() {
        // Frozen ZXing black-box aztec-1/reader-init.png, sampled at module centers.
        // Full, one layer: the mode message declares 1034 words, including the
        // initialization flag. Its actual payload uses ten words.
        var rows = new[] {
            "0010010010000100101", "1111110011100000111", "1011000000100000110",
            "0011111111111111110", "1001000000000001001", "1001011111111101010",
            "0101010000000101101", "1101010111110101000", "1001010100010101011",
            "0101010101010101010", "1001010100010101111", "1001010111110101000",
            "1011010000000101010", "1011011111111101110", "1001000000000001000",
            "0001111111111111100", "0000010110011100011", "0111101111000110010",
            "1000001000011111010"
        };
        var modules = new BitMatrix(rows.Length, rows.Length);
        for (var y = 0; y < rows.Length; y++)
            for (var x = 0; x < rows.Length; x++) modules[x, y] = rows[y][x] == '1';

        Assert.True(AztecCode.TryDecode(modules, out var text));
        Assert.Equal("init-test", text);
        var pixels = MatrixPngRenderer.RenderPixels(modules, new MatrixPngRenderOptions {
            ModuleSize = 3, QuietZone = 2
        }, out var width, out var height, out var stride);
        Assert.True(AztecCode.TryDecode(pixels, width, height, stride, PixelFormat.Rgba32, out text));
        Assert.Equal("init-test", text);
    }

    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, 1)]
    [InlineData(false, 22)]
    public void InitializationFlag_DoesNotChangePayload(bool compact, int layers) {
        const string payload = "CONFIG";
        var modules = AztecCode.Encode(payload, new AztecEncodeOptions {
            Compact = compact, Layers = layers
        }).Modules.Clone();
        SetInitializationFlag(modules, compact);

        Assert.True(AztecCode.TryDecode(modules, out var text));
        Assert.Equal(payload, text);
    }

    [Fact]
    public void CompactTwoLayerSymbol_PreservesOrdinaryHighCountBit() {
        var payload = new string('A', 40);
        var modules = AztecCode.Encode(payload, new AztecEncodeOptions {
            Compact = true, Layers = 2, ErrorCorrectionPercent = 0
        }).Modules;
        Assert.True(AztecDetector.TryDetect(modules, out var detection));
        Assert.InRange(detection.NbDataBlocks, 33, 40);
        Assert.True(AztecCode.TryDecode(modules, out var text));
        Assert.Equal(payload, text);
    }

    [Fact]
    public void LargeFullSymbol_PreservesOrdinaryHighCountBit() {
        var bytes = new byte[1600];
        new Random(47).NextBytes(bytes);
        var modules = AztecCode.Encode(bytes, new AztecEncodeOptions {
            Compact = false, Layers = 32, ErrorCorrectionPercent = 0
        }).Modules;
        Assert.True(AztecDetector.TryDetect(modules, out var detection));
        Assert.True(detection.NbDataBlocks > 1024);
        Assert.True(AztecCode.TryDecode(modules, out var text));
        Assert.Equal(EncodingUtils.Latin1.GetString(bytes), text);
    }

    private static void SetInitializationFlag(BitMatrix modules, bool compact) {
        var center = modules.Width / 2;
        var side = compact ? 7 : 10;
        var radius = compact ? 5 : 7;
        var bits = new bool[side * 4];
        for (var i = 0; i < side; i++) {
            var offset = compact ? center - 3 + i : center - 5 + i + i / 5;
            bits[i] = modules[offset, center - radius];
            bits[i + side] = modules[center + radius, offset];
            bits[3 * side - 1 - i] = modules[offset, center + radius];
            bits[4 * side - 1 - i] = modules[center - radius, offset];
        }
        var words = new int[bits.Length / 4];
        for (var i = 0; i < bits.Length; i++) words[i / 4] = words[i / 4] * 2 + (bits[i] ? 1 : 0);
        words[compact ? 0 : 1] |= compact ? 2 : 4;
        new ReedSolomonEncoder(GenericGf.AztecParam).Encode(words, compact ? 5 : 6);
        for (var i = 0; i < bits.Length; i++) bits[i] = (words[i / 4] & (8 >> (i % 4))) != 0;
        for (var i = 0; i < side; i++) {
            var offset = compact ? center - 3 + i : center - 5 + i + i / 5;
            modules[offset, center - radius] = bits[i];
            modules[center + radius, offset] = bits[i + side];
            modules[offset, center + radius] = bits[3 * side - 1 - i];
            modules[center - radius, offset] = bits[4 * side - 1 - i];
        }
    }
}
