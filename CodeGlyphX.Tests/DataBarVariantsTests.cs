using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CodeGlyphX.DataBar;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("ImageScannerSerial")]
public sealed class DataBarVariantsTests {
    public static IEnumerable<object[]> ZintLimitedVectors() {
        yield return Vector("1234567890123", "0100110011110010100010011101011010101100100101001010100101000001110001110100000");
        yield return Vector("0000000000000", "0101010101010000001000000111010111010100100101010101010100000010000001110100000");
        yield return Vector("0000000183064", "0101010101010000001000000111001010101011100101010101010100011110000011110100000");
        yield return Vector("0000000820064", "0101010101010000001000000111011010110100100101010101010101111110001111110100000");
        yield return Vector("0000001000776", "0101010101010000001000000111010111010101000101010101010100000110000011110100000");
        yield return Vector("0000001491021", "0101010101010000001000000111010100101011001101010101010100111110000111110100000");
        yield return Vector("0000001979845", "0101010101010000001000000111010111010010100101010101010100000010000000010100000");
        yield return Vector("0000001996939", "0101010101010000001000000111011011010101000101010101010101111110111111110100000");
        yield return Vector("1999999999999", "0100111100110110101101111101010101101011000101010000101110001101011110010100000");
    }

    [Theory]
    [MemberData(nameof(ZintLimitedVectors))]
    public void Limited_MatchesIndependentZintModules(string value, string expectedModules) {
        var barcode = DataBarLimitedEncoder.Encode(value);

        Assert.Equal(79, barcode.TotalModules);
        Assert.Equal(expectedModules, ToModuleString(barcode));
        Assert.True(DataBarLimitedDecoder.TryDecode(barcode, out var decoded));
        Assert.Equal(value, decoded);
        Assert.True(DataBarLimitedDecoder.TryDecode(ToModules(expectedModules), out decoded));
        Assert.Equal(value, decoded);
    }

    [Fact]
    public void Limited_LinearFacade_RoundTripsAndReportsType() {
        const string value = "1234567890123";
        var barcode = BarcodeEncoder.Encode(BarcodeType.GS1DataBarLimited, value);
        var modules = ExpandModules(barcode);

        Assert.True(BarcodeDecoder.TryDecode(modules, BarcodeType.GS1DataBarLimited, out var decoded));
        Assert.Equal(BarcodeType.GS1DataBarLimited, decoded.Type);
        Assert.Equal(value, decoded.Text);
        Assert.True(BarcodeDecoder.TryDecode(modules, out decoded));
        Assert.Equal(BarcodeType.GS1DataBarLimited, decoded.Type);
    }

    [Fact]
    public void Limited_UnifiedImageScanner_IsReachableThroughCapabilityCatalog() {
        const string value = "1234567890123";
        var barcode = DataBarLimitedEncoder.Encode(value);
        var pixels = Rendering.Png.BarcodePngRenderer.RenderPixels(
            barcode,
            new Rendering.Png.BarcodePngRenderOptions { ModuleSize = 4, QuietZone = 10, HeightModules = 40 },
            out var width,
            out var height,
            out _);

        var result = SymbolScanner.Scan(ImageFrame.Packed(pixels, width, height, PixelFormat.Rgba32), new ScanOptions {
            Formats = new[] { SymbolFormat.Gs1DataBarLimited },
            TimeoutMilliseconds = TestBudget.Adjust(5000)
        });

        var capability = SymbolCapabilities.Get(SymbolFormat.Gs1DataBarLimited);
        Assert.True(capability.CanScanImages);
        Assert.True(capability.CanScanMultiple);
        Assert.Empty(result.UnsupportedFormats);
        Assert.Equal(ScanStatus.Success, result.Status);
        var symbol = Assert.Single(result.Symbols);
        Assert.Equal(SymbolFormat.Gs1DataBarLimited, symbol.Format);
        Assert.Equal(value, symbol.Text);
    }

    [Fact]
    public void Limited_AcceptsValidGtinAndRejectsInvalidInput() {
        Assert.Equal(ToModuleString(DataBarLimitedEncoder.Encode("1234567890123")),
            ToModuleString(DataBarLimitedEncoder.Encode("(01)12345678901231")));
        Assert.Throws<InvalidOperationException>(() => DataBarLimitedEncoder.Encode("12345678901232"));
        Assert.Throws<InvalidOperationException>(() => DataBarLimitedEncoder.Encode("2000000000000"));
        Assert.Throws<InvalidOperationException>(() => DataBarLimitedEncoder.Encode("ABC"));
    }

    [Fact]
    public void Omnidirectional_IsLinearAndUsesExpectedTypeForPhysicalIdentity() {
        const string value = "1234567890123";
        var omni = BarcodeEncoder.Encode(BarcodeType.GS1DataBarOmni, value);
        var truncated = BarcodeEncoder.Encode(BarcodeType.GS1DataBarTruncated, value);
        var modules = ExpandModules(omni);

        Assert.Equal(ToModuleString(truncated), ToModuleString(omni));
        Assert.True(BarcodeDecoder.TryDecode(modules, BarcodeType.GS1DataBarOmni, out var decoded));
        Assert.Equal(BarcodeType.GS1DataBarOmni, decoded.Type);
        Assert.Equal(value, decoded.Text);
    }

    [Fact]
    public void Omnidirectional_UnifiedImageScanner_IsReachableThroughCapabilityCatalog() {
        const string value = "1234567890123";

        var result = SymbolScanner.Scan(RenderOmnidirectionalFrame(value), new ScanOptions {
            Formats = new[] { SymbolFormat.Gs1DataBarOmnidirectional },
            TimeoutMilliseconds = TestBudget.Adjust(5000)
        });

        var capability = SymbolCapabilities.Get(SymbolFormat.Gs1DataBarOmnidirectional);
        Assert.True(capability.CanScanImages);
        Assert.True(capability.CanScanMultiple);
        Assert.Empty(result.UnsupportedFormats);
        Assert.Equal(ScanStatus.Success, result.Status);
        var symbol = Assert.Single(result.Symbols);
        Assert.Equal(SymbolFormat.Gs1DataBarOmnidirectional, symbol.Format);
        Assert.Equal(value, symbol.Text);
    }

    [Fact]
    public void Omnidirectional_UnifiedImageScanner_IsDispatchedWithOtherLinearFormats() {
        const string value = "1234567890123";
        var frame = RenderOmnidirectionalFrame(value);

        var result = SymbolScanner.Scan(frame, new ScanOptions {
            Formats = new[] { SymbolFormat.Gs1DataBarOmnidirectional, SymbolFormat.Code128 },
            TimeoutMilliseconds = TestBudget.Adjust(5000)
        });

        Assert.Equal(ScanStatus.Success, result.Status);
        var symbol = Assert.Single(result.Symbols);
        Assert.Equal(SymbolFormat.Gs1DataBarOmnidirectional, symbol.Format);
        Assert.Equal(value, symbol.Text);
    }

    [Theory]
    [InlineData(13, SymbolFormat.Gs1DataBarTruncated)]
    [InlineData(40, SymbolFormat.Gs1DataBarOmnidirectional)]
    public void DefaultUnifiedImageScan_UsesPhysicalDataBar14Identity(int heightModules, SymbolFormat expectedFormat) {
        const string value = "1234567890123";

        var result = SymbolScanner.Scan(RenderDataBarFrame(DataBar14Encoder.EncodeTruncated(value), heightModules));

        Assert.Equal(ScanStatus.Success, result.Status);
        var symbol = Assert.Single(result.Symbols);
        Assert.Equal(expectedFormat, symbol.Format);
        Assert.Equal(value, symbol.Text);
    }

    [Theory]
    [InlineData(13, SymbolFormat.Gs1DataBarTruncated)]
    [InlineData(40, SymbolFormat.Gs1DataBarOmnidirectional)]
    public void ExplicitMixedDataBar14ImageScan_UsesPhysicalIdentity(int heightModules, SymbolFormat expectedFormat) {
        const string value = "1234567890123";
        var result = SymbolScanner.Scan(RenderDataBarFrame(DataBar14Encoder.EncodeTruncated(value), heightModules), new ScanOptions {
            Formats = new[] {
                SymbolFormat.Gs1DataBarTruncated,
                SymbolFormat.Gs1DataBarOmnidirectional,
                SymbolFormat.Code128
            },
            TimeoutMilliseconds = TestBudget.Adjust(5000)
        });

        Assert.Equal(ScanStatus.Success, result.Status);
        var symbol = Assert.Single(result.Symbols);
        Assert.Equal(expectedFormat, symbol.Format);
        Assert.Equal(value, symbol.Text);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MultiSymbolImageScan_ClassifiesEachDataBar14CandidateIndependently(bool explicitFormats) {
        const string value = "1234567890123";
        var truncated = RenderDataBarFrame(DataBar14Encoder.EncodeTruncated(value), heightModules: 13, moduleSize: 2);
        var omni = RenderDataBarFrame(DataBar14Encoder.EncodeOmnidirectional(value), heightModules: 40, moduleSize: 2);
        var frame = StackForIndependentScanlines(truncated, truncatedY: 20, omni, omniY: 50);
        var options = new ScanOptions { TimeoutMilliseconds = TestBudget.Adjust(10000) };
        if (explicitFormats) {
            options.Formats = new[] {
                SymbolFormat.Gs1DataBarTruncated,
                SymbolFormat.Gs1DataBarOmnidirectional,
                SymbolFormat.Code128
            };
        }

        var result = SymbolScanner.Scan(frame, options);

        Assert.Equal(ScanStatus.Success, result.Status);
        var dataBarSymbols = result.Symbols
            .Where(symbol => symbol.Format == SymbolFormat.Gs1DataBarTruncated
                             || symbol.Format == SymbolFormat.Gs1DataBarOmnidirectional)
            .ToArray();
        Assert.Equal(2, dataBarSymbols.Length);
        Assert.Contains(dataBarSymbols, symbol => symbol.Format == SymbolFormat.Gs1DataBarTruncated && symbol.Text == value);
        Assert.Contains(dataBarSymbols, symbol => symbol.Format == SymbolFormat.Gs1DataBarOmnidirectional && symbol.Text == value);
    }

    [Fact]
    public void Truncated_MixedFormatImageScan_PreservesExplicitIdentity() {
        const string value = "1234567890123";
        var barcode = DataBar14Encoder.EncodeTruncated(value);

        var result = SymbolScanner.Scan(RenderDataBarFrame(barcode, heightModules: 13), new ScanOptions {
            Formats = new[] { SymbolFormat.Gs1DataBarTruncated, SymbolFormat.Code128 },
            TimeoutMilliseconds = TestBudget.Adjust(5000)
        });

        Assert.Equal(ScanStatus.Success, result.Status);
        var symbol = Assert.Single(result.Symbols);
        Assert.Equal(SymbolFormat.Gs1DataBarTruncated, symbol.Format);
        Assert.Equal(value, symbol.Text);
    }

    [Fact]
    public void StackedOmnidirectional_HasAccurateFirstClassTypeAndCompatibilityAlias() {
        const string value = "1234567890123";
        var modules = MatrixBarcodeEncoder.Encode(BarcodeType.GS1DataBarStackedOmni, value);
        var compatibility = DataBar14Encoder.EncodeOmni(value);

        Assert.Equal(50, modules.Width);
        Assert.Equal(5, modules.Height);
        AssertMatrixEqual(compatibility, modules);
        Assert.True(MatrixBarcodeDecoder.TryDecode(BarcodeType.GS1DataBarStackedOmni, modules, out var decoded));
        Assert.Equal(value, decoded);
        Assert.True(MatrixBarcodeDecoder.TryDecodeAny(modules, out var any));
        Assert.Equal(BarcodeType.GS1DataBarStackedOmni, any.Type);
    }

    [Fact]
    public void Omnidirectional_MatrixFacadeUsesOneRow() {
        const string value = "1234567890123";
        var modules = MatrixBarcodeEncoder.Encode(BarcodeType.GS1DataBarOmni, value);

        Assert.Equal(96, modules.Width);
        Assert.Equal(1, modules.Height);
        Assert.True(MatrixBarcodeDecoder.TryDecode(BarcodeType.GS1DataBarOmni, modules, out var decoded));
        Assert.Equal(value, decoded);
    }

    private static object[] Vector(string value, string modules) => new object[] { value, modules };

    private static string ToModuleString(Barcode1D barcode) {
        var builder = new StringBuilder(barcode.TotalModules);
        for (var i = 0; i < barcode.Segments.Count; i++) {
            var segment = barcode.Segments[i];
            builder.Append(segment.IsBar ? '1' : '0', segment.Modules);
        }
        return builder.ToString();
    }

    private static bool[] ExpandModules(Barcode1D barcode) => ToModules(ToModuleString(barcode));

    private static ImageFrame RenderOmnidirectionalFrame(string value) {
        return RenderDataBarFrame(DataBar14Encoder.EncodeOmnidirectional(value), heightModules: 40);
    }

    private static ImageFrame RenderDataBarFrame(Barcode1D barcode, int heightModules, int moduleSize = 4) {
        var pixels = Rendering.Png.BarcodePngRenderer.RenderPixels(
            barcode,
            new Rendering.Png.BarcodePngRenderOptions { ModuleSize = moduleSize, QuietZone = 10, HeightModules = heightModules },
            out var width,
            out var height,
            out _);
        return ImageFrame.Packed(pixels, width, height, PixelFormat.Rgba32);
    }

    private static ImageFrame StackForIndependentScanlines(ImageFrame truncated, int truncatedY, ImageFrame omni, int omniY) {
        var width = Math.Max(truncated.Width, omni.Width);
        var height = Math.Max(truncatedY + truncated.Height, omniY + omni.Height);
        var stride = width * 4;
        var pixels = new byte[stride * height];
        for (var i = 0; i < pixels.Length; i += 4) {
            pixels[i] = 255;
            pixels[i + 1] = 255;
            pixels[i + 2] = 255;
            pixels[i + 3] = 255;
        }
        CopyFrame(truncated, truncatedY, pixels, stride);
        CopyFrame(omni, omniY, pixels, stride);
        return ImageFrame.Packed(pixels, width, height, PixelFormat.Rgba32);
    }

    private static void CopyFrame(ImageFrame source, int targetY, byte[] target, int targetStride) {
        var sourcePixels = source.Pixels.ToArray();
        for (var y = 0; y < source.Height; y++) {
            Buffer.BlockCopy(sourcePixels, y * source.Stride, target, (targetY + y) * targetStride, source.Stride);
        }
    }

    private static bool[] ToModules(string modules) {
        var result = new bool[modules.Length];
        for (var i = 0; i < modules.Length; i++) result[i] = modules[i] == '1';
        return result;
    }

    private static void AssertMatrixEqual(BitMatrix expected, BitMatrix actual) {
        Assert.Equal(expected.Width, actual.Width);
        Assert.Equal(expected.Height, actual.Height);
        for (var y = 0; y < expected.Height; y++) {
            for (var x = 0; x < expected.Width; x++) Assert.Equal(expected[x, y], actual[x, y]);
        }
    }
}
