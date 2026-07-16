using System;
using System.Text;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class DotCodeTests {
    private static readonly string[] AimFigure5Mask0 = {
        "10101000100010000000001", "01000101010001010000000", "00100010001000101000100", "01010001010000000101010",
        "00001000100010100010101", "00000101010101010000010", "00100010101000000010001", "00010100000101000100010",
        "00001000001000001010101", "01010101010001000001010", "10100000100010001000101", "01000100000100010101000",
        "10000010000010100010001", "00010000010100010101010", "10101000001000101010001", "01000001010101010000010"
    };

    [Fact]
    public void Gs1Mask0_MatchesAimAndIndependentZintMatrix() {
        var symbol = DotCodeEncoder.EncodeGs1("(17)070620(10)ABC123456", new DotCodeEncodingOptions { Width = 23, Mask = 0 });

        Assert.Equal(23, symbol.Modules.Width);
        Assert.Equal(16, symbol.Modules.Height);
        Assert.Equal(AimFigure5Mask0, ToRows(symbol.Modules));
        Assert.True(DotCodeDecoder.TryDecodeDetailed(symbol.Modules, out var decoded));
        Assert.Equal("1707062010ABC123456", decoded.Text.Replace(Gs1.GroupSeparator.ToString(), string.Empty));
    }

    [Theory]
    [InlineData("A")]
    [InlineData("Mixed Case / 123456789 / punctuation!")]
    [InlineData("1712345610")]
    [InlineData("[)>\u001e05\u001dA\u001e\u0004")]
    public void TextAndMacroPayloads_RoundTrip(string text) {
        var symbol = DotCodeEncoder.EncodeText(text);

        Assert.True(DotCodeDecoder.TryDecodeDetailed(symbol.Modules, out var decoded));
        Assert.Equal(text, decoded.Text);
    }

    [Fact]
    public void BinaryPayload_RoundTripsRadix259Compaction() {
        var bytes = new byte[] { 0, 1, 127, 128, 129, 200, 255, 50, 51, 52 };
        var symbol = DotCodeEncoder.EncodeBytes(bytes);

        Assert.True(DotCodeDecoder.TryDecodeDetailed(symbol.Modules, out var decoded));
        Assert.Equal(bytes, decoded.Bytes);
    }

    [Fact]
    public void Utf8EciAndStructuredAppend_RoundTripMetadata() {
        var symbol = DotCodeEncoder.EncodeText("Łódź", new DotCodeEncodingOptions {
            StructuredAppendIndex = 2,
            StructuredAppendCount = 3
        });

        Assert.True(DotCodeDecoder.TryDecodeDetailed(symbol.Modules, out var decoded));
        Assert.Equal("Łódź", decoded.Text);
        Assert.Equal(new[] { 26 }, decoded.EciAssignments);
        Assert.Equal(2, decoded.StructuredAppendIndex);
        Assert.Equal(3, decoded.StructuredAppendCount);
    }

    [Fact]
    public void ExplicitUtf8Encoding_InfersEciAndRoundTrips() {
        var symbol = DotCodeEncoder.EncodeText("Łódź", new DotCodeEncodingOptions {
            TextEncoding = Encoding.UTF8
        });

        Assert.True(DotCodeDecoder.TryDecodeDetailed(symbol.Modules, out var decoded));
        Assert.Equal("Łódź", decoded.Text);
        Assert.Equal(new[] { 26 }, decoded.EciAssignments);
    }

    [Fact]
    public void Decoder_CorrectsSeveralDamagedDots() {
        var symbol = DotCodeEncoder.EncodeText("DOTCODE-DAMAGE-123456789", new DotCodeEncodingOptions { Width = 35, Mask = 2 });
        var damaged = symbol.Modules.Clone();
        damaged[0, 0] = !damaged[0, 0];
        damaged[2, 0] = !damaged[2, 0];
        damaged[4, 0] = !damaged[4, 0];

        Assert.True(DotCodeDecoder.TryDecodeDetailed(damaged, out var decoded));
        Assert.Equal("DOTCODE-DAMAGE-123456789", decoded.Text);
    }

    [Fact]
    public void InvalidOptionsAndEmptyGrid_AreRejected() {
        Assert.Throws<ArgumentOutOfRangeException>(() => DotCodeEncoder.EncodeText("A", new DotCodeEncodingOptions { Width = 4 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => DotCodeEncoder.EncodeText("A", new DotCodeEncodingOptions { Mask = 8 }));
        Assert.Throws<InvalidOperationException>(() => DotCodeEncoder.EncodeText("A", new DotCodeEncodingOptions { TextEncoding = Encoding.Unicode }));
        Assert.False(DotCodeDecoder.TryDecodeDetailed(new BitMatrix(13, 10), out _));
    }

    [Fact]
    public void UnifiedFacadesAndCapabilityCatalog_ExposeDotCode() {
        var modules = MatrixBarcodeEncoder.Encode(BarcodeType.DotCode, "UNIFIED-DOTCODE");

        Assert.True(MatrixBarcodeDecoder.TryDecode(BarcodeType.DotCode, modules, out var text));
        Assert.Equal("UNIFIED-DOTCODE", text);
        Assert.True(CodeGlyph.TryDecode(modules, out var decoded, expectedBarcode: BarcodeType.DotCode));
        Assert.Equal(CodeGlyphKind.DotCode, decoded.Kind);
        Assert.NotNull(decoded.DotCode);
        Assert.Equal("UNIFIED-DOTCODE", decoded.Text);

        var capability = SymbolCapabilities.Get(SymbolFormat.DotCode);
        Assert.True(capability.CanEncode);
        Assert.True(capability.CanDecodeModules);
        Assert.True((capability.Operations & SymbolCapabilityFlags.Gs1Encode) != 0);
        Assert.True((capability.Operations & SymbolCapabilityFlags.StructuredAppendDecode) != 0);
    }

    private static string[] ToRows(BitMatrix matrix) {
        var rows = new string[matrix.Height];
        for (var y = 0; y < matrix.Height; y++) {
            var chars = new char[matrix.Width];
            for (var x = 0; x < matrix.Width; x++) chars[x] = matrix[x, y] ? '1' : '0';
            rows[y] = new string(chars);
        }
        return rows;
    }
}
