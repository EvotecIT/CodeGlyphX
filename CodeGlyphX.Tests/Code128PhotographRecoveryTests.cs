using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using CodeGlyphX.Code128;
using CodeGlyphX.Rendering;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class Code128PhotographRecoveryTests {
    [Theory]
    [InlineData("reversed-postal", "42094043", 0, false)]
    [InlineData("reversed-postal", "42094043", 1, true)]
    [InlineData("offcentre-label", "FGGQ6D1", 0, false)]
    [InlineData("offcentre-label", "FGGQ6D1", 1, true)]
    [InlineData("cutoff-label", "1PEF224A4", 0, false)]
    [InlineData("cutoff-label", "1PEF224A4", 1, true)]
    [InlineData("cutoff-reversed", "CNK8181G2C", 0, false)]
    [InlineData("cutoff-reversed", "CNK8181G2C", 1, true)]
    [InlineData("long-postal", "31001171800000017989625355702636", 0, false)]
    [InlineData("long-postal", "31001171800000017989625355702636", 1, true)]
    public void Decode_PhotographRecovery_PreservesPayloadAcrossPixelApis(string fixture, string expected, int turns, bool inverted) {
        var input = ImageReader.DecodeRgba32(Fixture(fixture), out var width, out var height);
        var pixels = Transform(input, ref width, ref height, turns, inverted);
        var before = (byte[])pixels.Clone();
        Assert.True(BarcodeDecoder.TryDecode(pixels, width, height, width * 4, PixelFormat.Rgba32,
            BarcodeType.Code128, out var decoded));
        Assert.Equal(expected, decoded.Text);
        Assert.False(decoded.ReaderInitialization);
#if NET8_0_OR_GREATER
        Assert.True(BarcodeDecoder.TryDecode((ReadOnlySpan<byte>)pixels, width, height, width * 4, PixelFormat.Rgba32,
            BarcodeType.Code128, null, CancellationToken.None, out var spanDecoded));
        Assert.Equal(expected, spanDecoded.Text);
#endif
        var options = ScanOptions.Robust(0);
        options.Formats = new[] { SymbolFormat.Code128, SymbolFormat.Gs1Code128 };
        options.EnableTileScan = false;
        options.MaxSymbols = 1;
        var result = SymbolScanner.Scan(ImageFrame.Packed(pixels, width, height, PixelFormat.Rgba32), options);
        Assert.Equal(expected, Assert.Single(result.Symbols).Text);
        Assert.Equal(before, pixels);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(3, false)]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    public void Decode_RecoveryBand_RequiresThreeAdjacentRows(int turns, bool inverted) {
        var barcode = BarcodeEncoder.Encode(BarcodeType.Code128, "OFFCENTRE-128");
        foreach (var rows in new[] { 1, 2, 3, 7 }) {
            var pixels = Band(Enumerable.Repeat(barcode, rows).ToArray(), out var width, out var height);
            pixels = Transform(pixels, ref width, ref height, turns, inverted);
            var found = BarcodeDecoder.TryDecode(pixels, width, height, width * 4, PixelFormat.Rgba32, BarcodeType.Code128, out var decoded);
            Assert.True(found == (rows >= 3), $"Unexpected recovery for {rows} physical rows, rotation {turns}, inversion {inverted}.");
            if (found) Assert.Equal("OFFCENTRE-128", decoded.Text);
        }
    }

    [Fact]
    public void Decode_RecoveryBand_RejectsIncorrectChecksum() {
        var invalid = FromCodes(new[] { 104, 33, 34 }, corruptChecksum: true);
        var pixels = Band(new[] { invalid, invalid, invalid, invalid, invalid }, out var width, out var height);
        Assert.False(BarcodeDecoder.TryDecode(pixels, width, height, width * 4, PixelFormat.Rgba32, BarcodeType.Code128, out _));
    }

    [Fact]
    public void Decode_RecoveryBand_DoesNotCombineReaderInitializationWithOrdinaryData() {
        var ordinary = FromCodes(new[] { 104, 33 });
        var initialization = FromCodes(new[] { 104, 96, 33 });
        var pixels = Band(new[] { initialization, ordinary, initialization }, out var width, out var height);
        Assert.False(BarcodeDecoder.TryDecode(pixels, width, height, width * 4, PixelFormat.Rgba32, BarcodeType.Code128, out _));
    }

    private static Barcode1D FromCodes(int[] data, bool corruptChecksum = false) {
        var checksum = data[0];
        for (var i = 1; i < data.Length; i++) checksum = (checksum + data[i] * i) % 103;
        if (corruptChecksum) checksum = (checksum + 1) % 103;
        var segments = new List<BarSegment>();
        foreach (var code in data.Concat(new[] { checksum, 106 })) {
            var count = code == 106 ? 7 : 6;
            var packed = Code128Tables.GetPattern(code);
            var widths = new int[count];
            for (var at = count - 1; at >= 0; at--) { widths[at] = (int)(packed & 15); packed >>= 4; }
            for (var at = 0; at < count; at++) segments.Add(new BarSegment((at & 1) == 0, widths[at]));
        }
        return new Barcode1D(segments);
    }

    private static byte[] Band(Barcode1D[] rows, out int width, out int height) {
        width = rows.Max(r => r.TotalModules) * 2 + 80;
        height = 480;
        var pixels = Enumerable.Repeat((byte)255, width * height * 4).ToArray();
        for (var row = 0; row < rows.Length; row++) {
            var x = 40;
            foreach (var segment in rows[row].Segments) {
                for (var at = 0; at < segment.Modules * 2; at++, x++) {
                    if (!segment.IsBar) continue;
                    var p = ((120 + row) * width + x) * 4;
                    pixels[p] = pixels[p + 1] = pixels[p + 2] = 0;
                }
            }
        }
        return pixels;
    }

    private static byte[] Transform(byte[] pixels, ref int width, ref int height, int turns, bool inverted) {
        var result = (byte[])pixels.Clone();
        if (inverted)
            for (var p = 0; p < result.Length; p += 4) {
                result[p] = (byte)(255 - result[p]);
                result[p + 1] = (byte)(255 - result[p + 1]);
                result[p + 2] = (byte)(255 - result[p + 2]);
            }
        for (var turn = 0; turn < turns; turn++) {
            var rotated = new byte[result.Length];
            for (var y = 0; y < height; y++)
                for (var x = 0; x < width; x++)
                    Buffer.BlockCopy(result, (y * width + x) * 4, rotated, (x * height + height - 1 - y) * 4, 4);
            result = rotated;
            var previousWidth = width; width = height; height = previousWidth;
        }
        return result;
    }

    private static byte[] Fixture(string label) {
        var assembly = typeof(Code128PhotographRecoveryTests).Assembly;
        var name = assembly.GetManifestResourceNames().Single(n => n.EndsWith(".zxing-code128-" + label + ".png", StringComparison.Ordinal));
        using var stream = assembly.GetManifestResourceStream(name)!;
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }
}
