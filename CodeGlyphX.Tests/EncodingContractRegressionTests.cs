using System;
using System.Text;
using CodeGlyphX.Code128;
using CodeGlyphX.Code39;
using CodeGlyphX.DataBar;
using CodeGlyphX.Pdf417;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class EncodingContractRegressionTests {
    [Theory]
    [InlineData(QrTextEncoding.Latin1, "漢")]
    [InlineData(QrTextEncoding.Ascii, "é")]
    [InlineData(QrTextEncoding.Utf8, "漢")]
    public void MicroQrText_RejectsLossyOrUndeclaredTextEncoding(QrTextEncoding encoding, string text) {
        Assert.Throws<ArgumentException>(() => MicroQrCodeEncoder.EncodeText(text, encoding, minVersion: 4, maxVersion: 4));
    }

    [Theory]
    [InlineData(QrTextEncoding.Latin1, "café")]
    [InlineData(QrTextEncoding.Ascii, "ABC")]
    [InlineData(QrTextEncoding.Utf8, "ABC")]
    public void MicroQrText_PreservesRepresentableByteModeText(QrTextEncoding encoding, string text) {
        var symbol = MicroQrCodeEncoder.EncodeText(text, encoding, minVersion: 4, maxVersion: 4);
        Assert.True(MicroQrDecoder.TryDecode(symbol.Modules, out var decoded));
        Assert.Equal(text, decoded.Text);
    }

    [Fact]
    public void Pdf417_MinimumRowsAreSatisfiedByPadding() {
        var symbol = Pdf417Encoder.EncodeSymbol("A", new Pdf417EncodeOptions {
            Compaction = Pdf417Compaction.Text,
            ErrorCorrectionLevel = 0,
            MinColumns = 3,
            MaxColumns = 3,
            MinRows = 3,
            MaxRows = 3
        });
        Assert.Equal(3, symbol.Columns);
        Assert.Equal(3, symbol.Rows);
        Assert.Equal(0, symbol.ErrorCorrectionLevel);
        Assert.True(Pdf417Decoder.TryDecode(symbol.Modules, out string decoded));
        Assert.Equal("A", decoded);
    }

    [Fact]
    public void Pdf417_PaddingCannotExceedTotalCodewordLimit() {
        Assert.Throws<ArgumentException>(() => Pdf417Encoder.Encode("A", new Pdf417EncodeOptions {
            Compaction = Pdf417Compaction.Text,
            ErrorCorrectionLevel = 0,
            MinColumns = 30,
            MaxColumns = 30,
            MinRows = 90,
            MaxRows = 90
        }));
    }

    [Theory]
    [InlineData(false, "漢é")]
    [InlineData(true, "漢é")]
    [InlineData(false, "ABC")]
    [InlineData(true, "ABC")]
    [InlineData(false, "ABCDE漢字Z")]
    [InlineData(true, "ABCDE漢字Z")]
    public void Pdf417Family_Utf16BigEndianTextRoundTrips(bool micro, string text) {
        var modules = micro
            ? MicroPdf417Encoder.Encode(text, new MicroPdf417EncodeOptions { Compaction = Pdf417Compaction.Byte, TextEncoding = Encoding.BigEndianUnicode })
            : Pdf417Encoder.Encode(text, new Pdf417EncodeOptions { Compaction = Pdf417Compaction.Byte, TextEncoding = Encoding.BigEndianUnicode });
        var success = micro ? MicroPdf417Decoder.TryDecode(modules, out var decoded) : Pdf417Decoder.TryDecode(modules, out decoded);
        Assert.True(success);
        Assert.Equal(text, decoded);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Pdf417Family_RejectsEncodingWithoutKnownEci(bool micro) {
        if (micro) {
            Assert.Throws<InvalidOperationException>(() => MicroPdf417Encoder.Encode("漢", new MicroPdf417EncodeOptions { TextEncoding = Encoding.Unicode }));
        } else {
            Assert.Throws<InvalidOperationException>(() => Pdf417Encoder.Encode("漢", new Pdf417EncodeOptions { TextEncoding = Encoding.Unicode }));
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Pdf417Family_RejectsReplacementFallback(bool micro) {
        if (micro) {
            Assert.Throws<ArgumentException>(() => MicroPdf417Encoder.Encode("漢", new MicroPdf417EncodeOptions { TextEncoding = Encoding.ASCII }));
        } else {
            Assert.Throws<ArgumentException>(() => Pdf417Encoder.Encode("漢", new Pdf417EncodeOptions { TextEncoding = Encoding.ASCII }));
        }
    }

    [Theory]
    [InlineData(false, "ABCDEF")]
    [InlineData(true, "ABCDEF")]
    [InlineData(false, "ABCD漢字XYZ")]
    [InlineData(true, "ABCD漢字XYZ")]
    public void Pdf417Family_ByteCompactionPreservesSixByteGroupsAndMultibyteBoundaries(bool micro, string text) {
        var bytes = Encoding.UTF8.GetBytes(text);
        var modules = micro ? MicroPdf417Encoder.EncodeBytes(bytes) : Pdf417Encoder.EncodeBytes(bytes);
        var success = micro ? MicroPdf417Decoder.TryDecode(modules, out var decoded) : Pdf417Decoder.TryDecode(modules, out decoded);
        Assert.True(success);
        Assert.Equal(text, decoded);
    }

    [Theory]
    [InlineData(Pdf417Compaction.Auto, "AB漢CéDEF")]
    [InlineData(Pdf417Compaction.Byte, "ABCD漢字XYZ")]
    [InlineData(Pdf417Compaction.Text, "Hello-World")]
    [InlineData(Pdf417Compaction.Numeric, "1234567890123456789012345")]
    public void Pdf417_DefaultEncodingAndCompactionRoundTrip(Pdf417Compaction compaction, string text) {
        var modules = Pdf417Encoder.Encode(text, new Pdf417EncodeOptions { Compaction = compaction });
        Assert.True(Pdf417Decoder.TryDecode(modules, out string decoded));
        Assert.Equal(text, decoded);
    }

    [Fact]
    public void MacroPdf417_TextEncodingRoundTripsWithMetadata() {
        var modules = Pdf417Encoder.EncodeMacro("漢é", new Pdf417MacroOptions { FileId = "123", IsLastSegment = true },
            new Pdf417EncodeOptions { Compaction = Pdf417Compaction.Byte, TextEncoding = Encoding.BigEndianUnicode });
        Assert.True(Pdf417Decoder.TryDecode(modules, out Pdf417Decoded decoded));
        Assert.Equal("漢é", decoded.Text);
        Assert.NotNull(decoded.Macro);
        Assert.Equal("123", decoded.Macro!.FileId);
        Assert.True(decoded.Macro.IsLastSegment);
    }

    [Fact]
    public void Pdf417_Utf16BigEndianDeclaresKnownCharsetCodewords() {
        var codewords = Pdf417HighLevelEncoder.Encode("漢", Pdf417Compaction.Byte, Encoding.BigEndianUnicode);
        // Charset ECI 927/25 is UTF-16BE; U+6F22 is the byte pair 0x6F, 0x22.
        Assert.Equal(new[] { 927, 25, 901, 0x6F, 0x22 }, codewords);
    }

    [Theory]
    [InlineData(Pdf417Compaction.Byte, "ABC")]
    [InlineData(Pdf417Compaction.Auto, "HELLO")]
    public void MicroPdf417_DefaultAsciiPreservesConstrainedCapacity(Pdf417Compaction compaction, string text) {
        var modules = MicroPdf417Encoder.Encode(text, new MicroPdf417EncodeOptions { Columns = 1, Rows = 11, Compaction = compaction });
        Assert.True(MicroPdf417Decoder.TryDecode(modules, out var decoded));
        Assert.Equal(text, decoded);
    }

    [Fact]
    public void Pdf417_DefaultAsciiByteCompactionPreservesConstrainedCapacity() {
        var modules = Pdf417Encoder.Encode("ABC", new Pdf417EncodeOptions {
            Compaction = Pdf417Compaction.Byte,
            ErrorCorrectionLevel = 0,
            MinColumns = 2,
            MaxColumns = 2,
            MinRows = 4,
            MaxRows = 4
        });
        Assert.True(Pdf417Decoder.TryDecode(modules, out string decoded));
        Assert.Equal("ABC", decoded);
        Assert.Equal(new[] { 901, 65, 66, 67 }, Pdf417HighLevelEncoder.Encode("ABC", Pdf417Compaction.Byte, null));
        Assert.Equal(new[] { 214, 341, 449 }, Pdf417HighLevelEncoder.Encode("HELLO", Pdf417Compaction.Auto, null));
        Assert.Equal(new[] { 927, 26, 901, 0xE6, 0xBC, 0xA2 }, Pdf417HighLevelEncoder.Encode("漢", Pdf417Compaction.Byte, null));
    }

    [Fact]
    public void Pdf417_DeclaredCharsetControlsIndependentPayloadBytes() {
        Assert.Equal("漢", Pdf417DecodedBitStreamParser.Decode(new[] { 927, 25, 901, 0x6F, 0x22 }));
        Assert.Equal("éé", Pdf417DecodedBitStreamParser.Decode(new[] { 927, 3, 901, 0xE9, 927, 26, 901, 0xC3, 0xA9 }));
        Assert.Equal("aab", Pdf417DecodedBitStreamParser.Decode(new[] { 810, 927, 3, 1 }));
        Assert.Null(Pdf417DecodedBitStreamParser.Decode(new[] { 927 }));
        Assert.Null(Pdf417DecodedBitStreamParser.Decode(new[] { 927, 899, 901, 65 }));
        Assert.Null(Pdf417DecodedBitStreamParser.Decode(new[] { 927, 26, 901, 255 }));
    }

    [Fact]
    public void Pdf417_ImageDecoderHonorsDeclaredCharset() {
        var modules = Pdf417Encoder.Encode("ABCDE漢字Z", new Pdf417EncodeOptions {
            Compaction = Pdf417Compaction.Byte,
            TextEncoding = Encoding.BigEndianUnicode
        });
        var pixels = MatrixPngRenderer.RenderPixels(modules, new MatrixPngRenderOptions { ModuleSize = 3, QuietZone = 2 },
            out var width, out var height, out var stride);
        Assert.True(Pdf417Decoder.TryDecode(pixels, width, height, stride, PixelFormat.Rgba32, out string decoded));
        Assert.Equal("ABCDE漢字Z", decoded);
    }

    [Fact]
    public void MicroPdf417_FourColumnsDecodeAcrossTheCenterMarker() {
        var modules = MicroPdf417Encoder.Encode("ABCD漢字XYZ", new MicroPdf417EncodeOptions {
            Columns = 4,
            Rows = 8,
            Compaction = Pdf417Compaction.Byte
        });
        Assert.True(MicroPdf417Decoder.TryDecode(modules, out var decoded));
        Assert.Equal("ABCD漢字XYZ", decoded);
    }

    [Theory]
    [InlineData(BarcodeType.Code128, "ABC", SymbolFormat.Code128)]
    [InlineData(BarcodeType.GS1_128, "(01)09501101530003", SymbolFormat.Gs1Code128)]
    [InlineData(BarcodeType.Code39, "ABC", SymbolFormat.Code39)]
    [InlineData(BarcodeType.Code93, "ABC", SymbolFormat.Code93)]
    [InlineData(BarcodeType.EAN, "1234567+12", SymbolFormat.Ean)]
    [InlineData(BarcodeType.UPCA, "12345678901", SymbolFormat.UpcA)]
    [InlineData(BarcodeType.UPCE, "123456", SymbolFormat.UpcE)]
    [InlineData(BarcodeType.ITF14, "1234567890123", SymbolFormat.Itf14)]
    [InlineData(BarcodeType.ITF, "1234", SymbolFormat.Itf)]
    [InlineData(BarcodeType.Industrial2of5, "1234", SymbolFormat.Industrial2Of5)]
    [InlineData(BarcodeType.Matrix2of5, "1234", SymbolFormat.Matrix2Of5)]
    [InlineData(BarcodeType.IATA2of5, "1234", SymbolFormat.Iata2Of5)]
    [InlineData(BarcodeType.PatchCode, "2", SymbolFormat.PatchCode)]
    [InlineData(BarcodeType.Codabar, "1234", SymbolFormat.Codabar)]
    [InlineData(BarcodeType.MSI, "1234", SymbolFormat.Msi)]
    [InlineData(BarcodeType.Code11, "1234", SymbolFormat.Code11)]
    [InlineData(BarcodeType.Plessey, "1234", SymbolFormat.Plessey)]
    [InlineData(BarcodeType.Telepen, "ABC", SymbolFormat.Telepen)]
    [InlineData(BarcodeType.Pharmacode, "1234", SymbolFormat.Pharmacode)]
    [InlineData(BarcodeType.Code32, "12345678", SymbolFormat.Code32)]
    [InlineData(BarcodeType.GS1DataBarTruncated, "0950110153000", SymbolFormat.Gs1DataBarTruncated)]
    [InlineData(BarcodeType.GS1DataBarOmni, "0950110153000", SymbolFormat.Gs1DataBarOmnidirectional)]
    [InlineData(BarcodeType.GS1DataBarExpanded, "(01)09501101530003", SymbolFormat.Gs1DataBarExpanded)]
    [InlineData(BarcodeType.GS1DataBarLimited, "0950110153000", SymbolFormat.Gs1DataBarLimited)]
    public void GenericLinearEncoders_IdentifyTheirPhysicalFormat(BarcodeType type, string text, SymbolFormat format) {
        Assert.Equal(format, BarcodeEncoder.Encode(type, text).Format);
    }

    [Fact]
    public void LinearEncoders_PreserveFormatIdentityAcrossEntryPoints() {
        Assert.Equal(SymbolFormat.Code128, BarcodeEncoder.Encode(BarcodeType.Code128, "ABC").Format);
        Assert.Equal(SymbolFormat.Code128, Code128Encoder.Encode("ABC").Format);
        Assert.Equal(SymbolFormat.Gs1Code128, Code128Encoder.EncodeGs1("0109501101530003").Format);
        Assert.Equal(SymbolFormat.Code39, BarcodeEncoder.EncodeCode39("ABC").Format);
        Assert.Equal(SymbolFormat.Code39, Code39Encoder.Encode("ABC").Format);
        Assert.Equal(SymbolFormat.Code32, BarcodeEncoder.EncodeCode32("12345678").Format);
        Assert.Equal(SymbolFormat.Gs1DataBarTruncated, DataBar14Encoder.EncodeTruncated("0950110153000").Format);
        Assert.Equal(SymbolFormat.Gs1DataBarOmnidirectional, DataBar14Encoder.EncodeOmnidirectional("0950110153000").Format);
        Assert.Null(new Barcode1D(new[] { new BarSegment(true, 1) }).Format);
    }
}
