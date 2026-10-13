using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using CodeGlyphX.DataBar;
using CodeGlyphX.Internal;
using CodeGlyphX.Rendering;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("ImageScannerSerial")]
public sealed class DataBar14CharacterRecoveryTests {
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void Decode_IndependentCharacterRoundingPhotograph_PreservesPayloadAcrossPixelEntrypoints(bool vertical, bool inverted) {
        var pixels = Load(out var width, out var height);
        if (vertical) {
            var rotated = new byte[pixels.Length];
            for (var y = 0; y < height; y++) for (var x = 0; x < width; x++)
                Buffer.BlockCopy(pixels, (y * width + x) * 4, rotated, (x * height + height - 1 - y) * 4, 4);
            pixels = rotated; (width, height) = (height, width);
        }
        if (inverted) for (var at = 0; at < pixels.Length; at += 4)
            for (var channel = 0; channel < 3; channel++) pixels[at + channel] = (byte)(255 - pixels[at + channel]);

        Assert.True(BarcodeDecoder.TryDecode(pixels, width, height, width * 4, PixelFormat.Rgba32,
            BarcodeType.GS1DataBarOmni, out var typed));
        AssertPayload(typed);
        Assert.True(BarcodeDecoder.TryDecode(pixels, width, height, width * 4, PixelFormat.Rgba32,
            BarcodeType.GS1DataBarOmni, null, default, out var diagnosed, out var diagnostics));
        AssertPayload(diagnosed);
        Assert.True(diagnostics.Success);
        // Untyped single-result decoding retains its existing format priority.
        // The all-result path must also expose the corroborated DataBar symbol.
        Assert.True(BarcodeDecoder.TryDecodeAll(pixels, width, height, width * 4, PixelFormat.Rgba32, out var untyped));
        Assert.Contains(untyped, result => (result.Type == BarcodeType.GS1DataBarOmni || result.Type == BarcodeType.GS1DataBarTruncated)
            && result.Text == "0200123456789");
        Assert.True(BarcodeDecoder.TryDecodeAll(pixels, width, height, width * 4, PixelFormat.Rgba32,
            out var all, BarcodeType.GS1DataBarOmni));
        AssertPayload(Assert.Single(all));
#if NET8_0_OR_GREATER
        Assert.True(BarcodeDecoder.TryDecode(pixels.AsSpan(), width, height, width * 4, PixelFormat.Rgba32,
            BarcodeType.GS1DataBarOmni, out var span));
        AssertPayload(span);
        Assert.True(BarcodeDecoder.TryDecodeAll(pixels.AsSpan(), width, height, width * 4, PixelFormat.Rgba32,
            out var spanAll, BarcodeType.GS1DataBarOmni));
        AssertPayload(Assert.Single(spanAll));
#endif
        var scan = SymbolScanner.Scan(ImageFrame.Packed(pixels, width, height, PixelFormat.Rgba32), new ScanOptions {
            Formats = new[] { SymbolFormat.Gs1DataBarOmnidirectional }, TimeoutMilliseconds = TestBudget.Adjust(5000)
        });
        var symbol = Assert.Single(scan.Symbols);
        Assert.Equal("0200123456789", symbol.Text);
        Assert.Equal(SymbolFormat.Gs1DataBarOmnidirectional, symbol.Format);
    }

    [Theory]
    [InlineData(2, false)]
    [InlineData(3, true)]
    public void Recovery_RequiresThreeAdjacentPhysicalRows(int rows, bool expected) {
        var source = Load(out var width, out var height);
        var pixels = new byte[source.Length]; for (var i = 0; i < pixels.Length; i++) pixels[i] = 255;
        for (var row = 0; row < rows; row++)
            Buffer.BlockCopy(source, 137 * width * 4, pixels, (135 + row) * width * 4, width * 4);
        Assert.Equal(expected, BarcodeDecoder.TryDecode(pixels, width, height, width * 4, PixelFormat.Rgba32,
            BarcodeType.GS1DataBarOmni, out var decoded));
        if (expected) AssertPayload(decoded);
    }

    [Fact]
    public void Recovery_DoesNotJoinRowsAtDifferentHorizontalLocations() {
        var source = Load(out var width, out var height);
        var pixels = new byte[source.Length]; for (var i = 0; i < pixels.Length; i++) pixels[i] = 255;
        for (var row = 0; row < 3; row++)
            Buffer.BlockCopy(source, 137 * width * 4, pixels, ((135 + row) * width + row * 20) * 4, (width - row * 20) * 4);
        Assert.False(BarcodeDecoder.TryDecode(pixels, width, height, width * 4, PixelFormat.Rgba32,
            BarcodeType.GS1DataBarOmni, out _));
    }

    [Fact]
    public void Recovery_ReadsBgraPixelsWithRowPadding() {
        var source = Load(out var width, out var height);
        var stride = width * 4 + 12;
        var pixels = new byte[stride * height];
        for (var y = 0; y < height; y++) for (var x = 0; x < width; x++) {
            var input = (y * width + x) * 4; var output = y * stride + x * 4;
            pixels[output] = source[input + 2]; pixels[output + 1] = source[input + 1];
            pixels[output + 2] = source[input]; pixels[output + 3] = source[input + 3];
        }
        Assert.True(BarcodeDecoder.TryDecode(pixels, width, height, stride, PixelFormat.Bgra32,
            BarcodeType.GS1DataBarOmni, out var decoded));
        AssertPayload(decoded);
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(3, true)]
    public void Recovery_RejectsConflictingOverlappingWindowsButKeepsSeparateBands(int centerDistance, bool expected) {
        // Each observation has already passed the complete DataBar decoder.
        // Exercise selection directly because threshold ambiguity can produce
        // different valid payloads at the same physical bounds in shared rows.
        var observationType = typeof(BarcodeScanline).GetNestedType("DataBarRowObservation", BindingFlags.NonPublic)!;
        var rowType = typeof(List<>).MakeGenericType(observationType);
        var rows = (IDictionary)Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(typeof(int), rowType))!;
        AddBand(100, "0200123456789");
        AddBand(100 + centerDistance, "0001234567890");
        var candidates = new List<BarcodeScanlineCandidate>();
        var select = typeof(BarcodeScanline).GetMethod("CollectCorroboratedDataBarRows", BindingFlags.NonPublic | BindingFlags.Static)!;
        select.Invoke(null, new object[] { rows, CancellationToken.None, candidates });
        if (!expected) { Assert.Empty(candidates); return; }
        Assert.Equal(2, candidates.Count);
        var payloads = new List<string>();
        foreach (var candidate in candidates) {
            Assert.True(DataBar14Decoder.TryDecodeOmnidirectional(candidate.Modules, out var content));
            payloads.Add(content);
        }
        Assert.Contains("0200123456789", payloads);
        Assert.Contains("0001234567890", payloads);

        void AddBand(int center, string content) {
            var barcode = DataBar14Encoder.EncodeOmnidirectional(content);
            var modules = new bool[barcode.TotalModules]; var at = 0;
            foreach (var segment in barcode.Segments)
                for (var i = 0; i < segment.Modules; i++) modules[at++] = segment.IsBar;
            Assert.True(DataBar14Decoder.TryDecodeOmnidirectional(modules, out var verified));
            Assert.Equal(content, verified);
            for (var position = center - 1; position <= center + 1; position++) {
                if (!rows.Contains(position)) rows.Add(position, Activator.CreateInstance(rowType));
                var candidate = new BarcodeScanlineCandidate(modules, position, false);
                var observation = Activator.CreateInstance(observationType, BindingFlags.NonPublic | BindingFlags.Instance,
                    null, new object[] { candidate, content, 10.0, 106.0, 1.0, false, false }, null)!;
                ((IList)rows[position]!).Add(observation);
            }
        }
    }

    private static byte[] Load(out int width, out int height) {
        using var fixture = typeof(DataBar14CharacterRecoveryTests).Assembly.GetManifestResourceStream(
            "CodeGlyphX.Tests.Fixtures.Linear.zxing-databar14-character-recovery.png");
        Assert.NotNull(fixture);
        using var buffer = new MemoryStream(); fixture!.CopyTo(buffer);
        return ImageReader.DecodeRgba32(buffer.ToArray(), out width, out height);
    }

    private static void AssertPayload(BarcodeDecoded decoded) {
        Assert.Equal("0200123456789", decoded.Text);
        Assert.Equal(BarcodeType.GS1DataBarOmni, decoded.Type);
    }
}
