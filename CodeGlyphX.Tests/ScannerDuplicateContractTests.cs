using System;
using System.Linq;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("GlobalState")]
public sealed class ScannerDuplicateContractTests {
    private const string Payload = "SAME-CODE";

    [Theory]
#if NET8_0_OR_GREATER
    [InlineData(SymbolFormat.QrCode, false)]
    [InlineData(SymbolFormat.QrCode, true)]
#endif
    [InlineData(SymbolFormat.Code128, false)]
    [InlineData(SymbolFormat.Code128, true)]
    public void Scanner_DeduplicationPolicyReachesMultiSymbolDecoders(SymbolFormat format, bool tiles) {
        var frame = CreatePair(format, Payload, Payload);

        var filtered = SymbolScanner.Scan(frame, Options(format, tiles, deduplicate: true));
        var observations = SymbolScanner.Scan(frame, Options(format, tiles, deduplicate: false));

        Assert.Equal(Payload, Assert.Single(filtered.Symbols).Text);
        // The scan preserves recognition observations, including repeated payloads. Threshold,
        // scanline, and tile retries do not promise one observation per physical symbol.
        Assert.True(observations.Symbols.Count > filtered.Symbols.Count);
        Assert.All(observations.Symbols, symbol => {
            Assert.Equal(format, symbol.Format);
            Assert.Equal(Payload, symbol.Text);
        });
    }

    [Fact]
    public void Scanner_LocatedLinearClassificationRetainsRepeatedObservationsWhenRequested() {
        const string gtin = "1234567890123";
        var frame = CreatePair(SymbolFormat.Gs1DataBarOmnidirectional, gtin, gtin);
        var formats = new[] { SymbolFormat.Gs1DataBarTruncated, SymbolFormat.Gs1DataBarOmnidirectional };
        var filteredOptions = Options(SymbolFormat.Gs1DataBarOmnidirectional, tiles: true, deduplicate: true);
        filteredOptions.Formats = formats;
        var observationOptions = Options(SymbolFormat.Gs1DataBarOmnidirectional, tiles: true, deduplicate: false);
        observationOptions.Formats = formats;

        var filtered = SymbolScanner.Scan(frame, filteredOptions);
        var observations = SymbolScanner.Scan(frame, observationOptions);

        Assert.Equal(SymbolFormat.Gs1DataBarOmnidirectional, Assert.Single(filtered.Symbols).Format);
        Assert.True(observations.Symbols.Count > filtered.Symbols.Count);
        Assert.All(observations.Symbols, symbol => {
            Assert.Equal(SymbolFormat.Gs1DataBarOmnidirectional, symbol.Format);
            Assert.Equal(gtin, symbol.Text);
        });
    }

    [Fact]
    public void Scanner_LegacyQrFallbackRetainsFullFrameAndTileObservationsWhenRequested() {
        var qr = Render(SymbolFormat.QrCode, Payload, out var qrWidth, out var qrHeight, out var qrStride);
        var width = (qrWidth + 48) * 2;
        var height = (qrHeight + 48) * 2;
        var pixels = Enumerable.Repeat((byte)255, width * height * 4).ToArray();
        Blit(qr, qrWidth, qrHeight, qrStride, pixels, width * 4, 24, 24);
        var frame = ImageFrame.Packed(pixels, width, height, PixelFormat.Rgba32);
#if NET8_0_OR_GREATER
        var previous = CodeGlyphXFeatures.ForceQrFallbackForTests;
        CodeGlyphXFeatures.ForceQrFallbackForTests = true;
#endif
        try {
            var filteredOptions = Options(SymbolFormat.QrCode, tiles: true, deduplicate: true);
            filteredOptions.Qr!.BudgetMilliseconds = TestBudget.Adjust(2000);
            var observationOptions = Options(SymbolFormat.QrCode, tiles: true, deduplicate: false);
            observationOptions.Qr!.BudgetMilliseconds = TestBudget.Adjust(2000);
            var filtered = SymbolScanner.Scan(frame, filteredOptions);
            var observations = SymbolScanner.Scan(frame, observationOptions);

            Assert.Equal(Payload, Assert.Single(filtered.Symbols).Text);
            Assert.True(observations.Symbols.Count > filtered.Symbols.Count);
            Assert.All(observations.Symbols, symbol => Assert.Equal(Payload, symbol.Text));
            var specialistOptions = QrOptions(tiles: true);
            specialistOptions.BudgetMilliseconds = TestBudget.Adjust(2000);
            Assert.True(QrImageDecoder.TryDecodeAll(pixels, width, height, width * 4, PixelFormat.Rgba32,
                specialistOptions, out var specialist));
            Assert.Equal(Payload, Assert.Single(specialist).Text);
        } finally {
#if NET8_0_OR_GREATER
            CodeGlyphXFeatures.ForceQrFallbackForTests = previous;
#endif
        }
    }

    [Theory]
#if NET8_0_OR_GREATER
    [InlineData(SymbolFormat.QrCode)]
#endif
    [InlineData(SymbolFormat.Code128)]
    public void Scanner_DeduplicationPreservesDistinctPayloads(SymbolFormat format) {
        const string otherPayload = "OTHER-CODE";
        var frame = CreatePair(format, Payload, otherPayload);

        var result = SymbolScanner.Scan(frame, Options(format, tiles: true, deduplicate: true));

        Assert.Equal(2, result.Symbols.Count);
        Assert.Contains(result.Symbols, symbol => symbol.Format == format && symbol.Text == Payload);
        Assert.Contains(result.Symbols, symbol => symbol.Format == format && symbol.Text == otherPayload);
    }

    [Theory]
#if NET8_0_OR_GREATER
    [InlineData(SymbolFormat.QrCode)]
#endif
    [InlineData(SymbolFormat.Code128)]
    public void SpecialistMultiDecoders_KeepTheirPayloadDeduplicationDefaults(SymbolFormat format) {
        var frame = CreatePair(format, Payload, Payload);
        var pixels = frame.Pixels.ToArray();
        if (format == SymbolFormat.QrCode) {
            Assert.True(QrImageDecoder.TryDecodeAll(pixels, frame.Width, frame.Height, frame.Stride,
                PixelFormat.Rgba32, QrOptions(tiles: true), out var arrayResults));
            Assert.Equal(Payload, Assert.Single(arrayResults).Text);
#if NET8_0_OR_GREATER
            Assert.True(QrImageDecoder.TryDecodeAll(pixels.AsSpan(), frame.Width, frame.Height, frame.Stride,
                PixelFormat.Rgba32, QrOptions(tiles: true), out var spanResults));
            Assert.Equal(Payload, Assert.Single(spanResults).Text);
#endif
        } else {
            var options = new BarcodeDecodeOptions { EnableTileScan = true, TileGrid = 2 };
            Assert.True(BarcodeDecoder.TryDecodeAll(pixels, frame.Width, frame.Height, frame.Stride,
                PixelFormat.Rgba32, out var arrayResults, BarcodeType.Code128, options));
            Assert.Equal(Payload, Assert.Single(arrayResults).Text);
#if NET8_0_OR_GREATER
            Assert.True(BarcodeDecoder.TryDecodeAll(pixels.AsSpan(), frame.Width, frame.Height, frame.Stride,
                PixelFormat.Rgba32, out var spanResults, BarcodeType.Code128, options));
            Assert.Equal(Payload, Assert.Single(spanResults).Text);
#endif
        }
    }

    private static ScanOptions Options(SymbolFormat format, bool tiles, bool deduplicate) {
        return new ScanOptions {
            Formats = new[] { format },
            Profile = ScanProfile.Fast,
            TimeoutMilliseconds = TestBudget.Adjust(10000),
            MaxSymbols = 0,
            Deduplicate = deduplicate,
            EnableTileScan = tiles,
            TileGrid = 2,
            Qr = QrOptions(tiles),
            Barcode = new BarcodeDecodeOptions { EnableTileScan = tiles, TileGrid = 2 }
        };
    }

    private static QrPixelDecodeOptions QrOptions(bool tiles) {
        return new QrPixelDecodeOptions {
            Profile = QrDecodeProfile.Fast,
            EnableTileScan = tiles,
            TileGrid = 2,
            MaxDimension = 0
        };
    }

    private static ImageFrame CreatePair(SymbolFormat format, string first, string second) {
        var a = Render(format, first, out var aw, out var ah, out var aStride);
        var b = Render(format, second, out var bw, out var bh, out var bStride);
        var cellWidth = Math.Max(aw, bw) + 48;
        var cellHeight = Math.Max(ah, bh) + 48;
        var width = cellWidth * 2;
        var height = cellHeight * 2;
        var canvas = Enumerable.Repeat((byte)255, width * height * 4).ToArray();
        Blit(a, aw, ah, aStride, canvas, width * 4, 24, 24);
        Blit(b, bw, bh, bStride, canvas, width * 4, cellWidth + 24, cellHeight + 24);
        return ImageFrame.Packed(canvas, width, height, PixelFormat.Rgba32);
    }

    private static byte[] Render(SymbolFormat format, string text, out int width, out int height, out int stride) {
        if (format == SymbolFormat.QrCode) {
            return QrPngRenderer.RenderPixels(QrCodeEncoder.EncodeText(text).Modules,
                new QrPngRenderOptions { ModuleSize = 5, QuietZone = 4 }, out width, out height, out stride);
        }
        var type = format == SymbolFormat.Code128 ? BarcodeType.Code128 : BarcodeType.GS1DataBarOmni;
        return BarcodePngRenderer.RenderPixels(BarcodeEncoder.Encode(type, text),
            new BarcodePngRenderOptions { ModuleSize = 3, QuietZone = 10, HeightModules = 40 },
            out width, out height, out stride);
    }

    private static void Blit(byte[] source, int width, int height, int stride,
        byte[] destination, int destinationStride, int offsetX, int offsetY) {
        for (var y = 0; y < height; y++) {
            Array.Copy(source, y * stride, destination, (y + offsetY) * destinationStride + offsetX * 4, width * 4);
        }
    }
}
