using System;
using System.Linq;
using CodeGlyphX.PosiCode;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("ImageScannerSerial")]
public sealed class AdditionalLinearScannerTests {
    [Theory]
    [InlineData(SymbolFormat.Bc412, "BRC412AB")]
    [InlineData(SymbolFormat.ChannelCode, "1234")]
    [InlineData(SymbolFormat.PosiCode, "ABC123")]
    [InlineData(SymbolFormat.Ean2, "12")]
    [InlineData(SymbolFormat.Ean5, "52495")]
    [InlineData(SymbolFormat.Coop2Of5, "0123456789")]
    [InlineData(SymbolFormat.Datalogic2Of5, "0123456789")]
    public void NewGuardedFormats_DecodeWithoutHintsFromModulesAndPixels(SymbolFormat format, string value) {
        var encoded = Barcode.Encode(format, value);
        var modules = encoded.Segments.SelectMany(s => Enumerable.Repeat(s.IsBar, s.Modules)).ToArray();
        Assert.True(BarcodeDecoder.TryDecode(modules, out var decoded));
        Assert.Equal(SymbolCapabilities.Get(format).LegacyBarcodeType, decoded.Type);
        Assert.Equal(value, decoded.Text);
        var pixels = BarcodePngRenderer.RenderPixels(encoded, new BarcodePngRenderOptions {
            ModuleSize = 3, QuietZone = 20, HeightModules = 30
        }, out var width, out var height, out var stride);
        Assert.True(BarcodeDecoder.TryDecode(pixels, width, height, stride, PixelFormat.Rgba32, out decoded));
        Assert.Equal(SymbolCapabilities.Get(format).LegacyBarcodeType, decoded.Type);
        Assert.Equal(value, decoded.Text);
    }

    [Theory]
    [InlineData(BarcodeType.IATA2of5, false)]
    [InlineData(BarcodeType.IATA2of5, true)]
    [InlineData(BarcodeType.Matrix2of5, false)]
    [InlineData(BarcodeType.Matrix2of5, true)]
    [InlineData(BarcodeType.Coop2of5, false)]
    [InlineData(BarcodeType.Coop2of5, true)]
    [InlineData(BarcodeType.Datalogic2of5, false)]
    [InlineData(BarcodeType.Datalogic2of5, true)]
    public void UncheckedPixelOverloads_ReturnCompletePayloadsThroughTheSameBoundaryPolicy(BarcodeType type, bool tiles) {
        const string value = "0123456789";
        var pixels = BarcodePngRenderer.RenderPixels(BarcodeEncoder.Encode(type, value), new BarcodePngRenderOptions {
            ModuleSize = 3, QuietZone = 20, HeightModules = 30
        }, out var width, out var height, out var stride);
        var options = new BarcodeDecodeOptions { EnableTileScan = tiles, TileGrid = 2 };
        Assert.True(BarcodeDecoder.TryDecodeAll(pixels, width, height, stride, PixelFormat.Rgba32, out var decoded, type, options));
        Assert.Equal(new[] { value }, decoded.Select(hit => hit.Text).ToArray());
        Assert.True(BarcodeDecoder.TryDecodeAll((ReadOnlySpan<byte>)pixels, width, height, stride,
            PixelFormat.Rgba32, out decoded, type, options));
        Assert.Equal(new[] { value }, decoded.Select(hit => hit.Text).ToArray());
    }

    [Fact]
    public void PosiCodeImageResults_RetainGs1AndReaderControlMetadata() {
        var samples = new[] {
            PosiCodeEncoder.Encode("ABC123", readerInitialization: true),
            PosiCodeEncoder.Encode("ABC123", messageAppend: true),
            PosiCodeEncoder.Encode("(01)09521234543213(10)ABC", isGs1: true)
        };
        for (var i = 0; i < samples.Length; i++) {
            var pixels = BarcodePngRenderer.RenderPixels(samples[i], new BarcodePngRenderOptions {
                ModuleSize = 3, QuietZone = 20, HeightModules = 30
            }, out var width, out var height, out var stride);
            Assert.True(BarcodeDecoder.TryDecode(pixels, width, height, stride, PixelFormat.Rgba32,
                BarcodeType.PosiCode, out var decoded));
            var metadata = Assert.IsType<PosiCodeSymbolMetadata>(decoded.Metadata);
            Assert.Equal(i == 0, metadata.ReaderInitialization);
            Assert.Equal(i == 1, metadata.MessageAppend);
            Assert.Equal(i == 2, metadata.IsGs1);
        }
    }

    [Fact]
    public void PosiCodeMultipleImages_DoNotDeduplicateDifferentMessageControls() {
        var render = new BarcodePngRenderOptions { ModuleSize = 3, QuietZone = 20, HeightModules = 30 };
        var normal = BarcodePngRenderer.RenderPixels(PosiCodeEncoder.Encode("ABC123"), render,
            out var firstWidth, out var firstHeight, out _);
        var append = BarcodePngRenderer.RenderPixels(PosiCodeEncoder.Encode("ABC123", messageAppend: true), render,
            out var secondWidth, out var secondHeight, out _);
        var width = Math.Max(firstWidth, secondWidth);
        var height = firstHeight + secondHeight + 60;
        var pixels = Enumerable.Repeat((byte)255, width * height * 4).ToArray();
        for (var y = 0; y < firstHeight; y++) Array.Copy(normal, y * firstWidth * 4, pixels, y * width * 4, firstWidth * 4);
        for (var y = 0; y < secondHeight; y++) Array.Copy(append, y * secondWidth * 4, pixels,
            (y + firstHeight + 60) * width * 4, secondWidth * 4);
        var result = SymbolScanner.Scan(ImageFrame.Packed(pixels, width, height, PixelFormat.Rgba32), new ScanOptions {
            Formats = new[] { SymbolFormat.PosiCode }, MaxSymbols = 0, EnableTileScan = true, TileGrid = 2,
            TimeoutMilliseconds = 5000
        });
        Assert.Equal(2, result.Symbols.Count);
        Assert.All(result.Symbols, symbol => Assert.Equal("ABC123", symbol.Text));
        Assert.Equal(new[] { false, true }, result.Symbols.Select(symbol =>
            Assert.IsType<PosiCodeSymbolMetadata>(symbol.Metadata).MessageAppend).OrderBy(value => value).ToArray());
    }
}
