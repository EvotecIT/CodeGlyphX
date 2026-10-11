using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CodeGlyphX.Code128;
using CodeGlyphX.Rendering;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class Code128ReaderInitializationTests {
    [Theory]
    [InlineData("abcdef", true, new[] { 104, 96, 65, 66, 67, 68, 69, 70 })]
    [InlineData("ab", true, new[] { 104, 65, 96, 66 })]
    [InlineData("a", true, new[] { 104, 65, 96 })]
    [InlineData("AB", true, new[] { 103, 96, 33, 34 })]
    [InlineData("\u00e1", true, new[] { 104, 100, 100, 96, 65 })]
    [InlineData("\u00e1", true, new[] { 104, 100, 96, 65 })]
    [InlineData("9612", false, new[] { 105, 96, 12 })]
    [InlineData("96A", true, new[] { 105, 96, 101, 96, 33 })]
    public void Decode_InitializationControl_PreservesContentAndCodeSet(string expected, bool readerInitialization, int[] codes) {
        Assert.True(BarcodeDecoder.TryDecode(Modules(codes), BarcodeType.Code128, out var decoded));
        Assert.Equal(BarcodeType.Code128, decoded.Type);
        Assert.Equal(expected, decoded.Text);
        Assert.Equal(readerInitialization, decoded.ReaderInitialization);
        Assert.Equal(readerInitialization, Assert.IsType<Code128SymbolMetadata>(SymbolResultFactory.From(decoded).Metadata).ReaderInitialization);
        Assert.True(BarcodeDecoder.TryDecode(Modules(codes), out var inferred));
        Assert.Equal(expected, inferred.Text);
        Assert.Equal(readerInitialization, inferred.ReaderInitialization);
    }

    [Theory]
    [InlineData(new[] { 104, 98, 96, 65 })]
    [InlineData(new[] { 104, 97, 65 })]
    [InlineData(new[] { 104, 100, 96 })]
    public void Decode_InitializationDoesNotRepairOtherInvalidControls(int[] codes) {
        Assert.False(BarcodeDecoder.TryDecode(Modules(codes), BarcodeType.Code128, out _));
    }

    [Fact]
    public void Decode_InitializationCannotServeAsCompositeDataCarrier() {
        Assert.False(BarcodeDecoder.TryDecodeGs1CompositeCarrier(Modules(new[] { 104, 102, 96, 16, 99 }), out _, out _));
    }

    [Fact]
    public void Decode_Gs1Initialization_PreservesFlagAcrossTypeHints() {
        var modules = Modules(new[] { 104, 102, 96, 33 });
        foreach (var hint in new BarcodeType?[] { null, BarcodeType.Code128, BarcodeType.GS1_128 }) {
            Assert.True(BarcodeDecoder.TryDecode(modules, hint, out var decoded));
            Assert.Equal(BarcodeType.GS1_128, decoded.Type);
            Assert.Equal("A", decoded.Text);
            Assert.True(decoded.ReaderInitialization);
            var symbol = SymbolResultFactory.From(decoded);
            Assert.Equal(SymbolPayloadProfile.Gs1, symbol.PayloadProfile);
            Assert.True(Assert.IsType<Code128SymbolMetadata>(symbol.Metadata).ReaderInitialization);
        }
    }

    [Fact]
    public void Decode_IndependentInitializationImage_PreservesContent() {
        var rgba = ImageReader.DecodeRgba32(Fixture(), out var width, out var height);
        Assert.True(BarcodeDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32,
            BarcodeType.Code128, out var decoded));
        Assert.Equal("abcdef", decoded.Text);
        Assert.True(decoded.ReaderInitialization);
        Assert.True(BarcodeDecoder.TryDecodeAll(rgba, width, height, width * 4, PixelFormat.Rgba32,
            out var all, BarcodeType.Code128));
        var single = Assert.Single(all);
        Assert.Equal("abcdef", single.Text);
        Assert.True(single.ReaderInitialization);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Scan_IndependentInitializationImage_PreservesContent(bool defaultFormats) {
        var options = ScanOptions.Robust(TestBudget.Adjust(5000));
        options.MaxSymbols = 1;
        if (!defaultFormats) options.Formats = new[] { SymbolFormat.Code128 };
        var symbol = Assert.Single(SymbolScanner.Scan(Fixture(), options).Symbols);
        Assert.Equal(SymbolFormat.Code128, symbol.Format);
        Assert.Equal("abcdef", symbol.Text);
        Assert.True(Assert.IsType<Code128SymbolMetadata>(symbol.Metadata).ReaderInitialization);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Decode_EqualTextDataAndInitialization_RemainDistinct(bool initializationFirst) {
        var ordinary = Modules(new[] { 104, 65 });
        var initialization = Modules(new[] { 104, 96, 65 });
        var width = (initialization.Length + 24) * 3;
        const int height = 180;
        var pixels = Enumerable.Repeat((byte)255, width * height * 4).ToArray();
        Paint(initializationFirst ? initialization : ordinary, 20, 100);
        Paint(initializationFirst ? ordinary : initialization, 110, 160);

        Assert.True(BarcodeDecoder.TryDecodeAll(pixels, width, height, width * 4, PixelFormat.Rgba32,
            out var all, BarcodeType.Code128));
        Assert.Equal(new[] { false, true }, all.Select(x => x.ReaderInitialization).OrderBy(x => x).ToArray());
        Assert.All(all, x => Assert.Equal("a", x.Text));
#if NET8_0_OR_GREATER
        Assert.True(BarcodeDecoder.TryDecodeAll((ReadOnlySpan<byte>)pixels, width, height, width * 4,
            PixelFormat.Rgba32, out var spanAll, BarcodeType.Code128));
        Assert.Equal(new[] { false, true }, spanAll.Select(x => x.ReaderInitialization).OrderBy(x => x).ToArray());
#endif
        foreach (var defaultFormats in new[] { false, true }) {
            var options = ScanOptions.Robust(TestBudget.Adjust(5000));
            options.MaxSymbols = 2;
            if (!defaultFormats) options.Formats = new[] { SymbolFormat.Code128 };
            var symbols = SymbolScanner.Scan(ImageFrame.Packed(pixels, width, height, PixelFormat.Rgba32), options).Symbols;
            Assert.Equal(new[] { false, true }, symbols.Select(x => Assert.IsType<Code128SymbolMetadata>(x.Metadata).ReaderInitialization).OrderBy(x => x).ToArray());
            Assert.All(symbols, x => Assert.Equal("a", x.Text));
        }

        void Paint(bool[] modules, int firstRow, int lastRow) {
            for (var y = firstRow; y < lastRow; y++) for (var x = 0; x < modules.Length * 3; x++) {
                if (!modules[x / 3]) continue;
                var offset = (y * width + x + 36) * 4;
                pixels[offset] = pixels[offset + 1] = pixels[offset + 2] = 0;
            }
        }
    }

    private static byte[] Fixture() {
        using var stream = typeof(Code128ReaderInitializationTests).Assembly.GetManifestResourceStream(
            "CodeGlyphX.Tests.Fixtures.Linear.zxing-code128-reader-init.png");
        Assert.NotNull(stream);
        using var buffer = new MemoryStream();
        stream!.CopyTo(buffer);
        return buffer.ToArray();
    }

    // Independently specified Code 128 codewords; the encoder under test does not choose these controls.
    private static bool[] Modules(int[] data) {
        var codes = data.ToList();
        var checksum = data[0];
        for (var i = 1; i < data.Length; i++) checksum = (checksum + data[i] * i) % 103;
        codes.Add(checksum);
        codes.Add(Code128Tables.Stop);
        var modules = new List<bool>();
        foreach (var code in codes) {
            var pattern = Code128Tables.GetPattern(code);
            var black = true;
            for (var run = code == Code128Tables.Stop ? 6 : 5; run >= 0; run--) {
                var width = (int)((pattern >> (run * 4)) & 15);
                modules.AddRange(Enumerable.Repeat(black, width));
                black = !black;
            }
        }
        return modules.ToArray();
    }
}
