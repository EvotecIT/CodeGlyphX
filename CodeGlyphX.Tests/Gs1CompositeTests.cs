using Xunit;
using CodeGlyphX.Gs1Composite;

namespace CodeGlyphX.Tests;

public sealed class Gs1CompositeTests {
    private static readonly string[] ZintGs1_128CcAFigure11 = {
        "0000000000000000000001101001000110100001000001101101011110111110010010001101010000010010000011101110100010000111011001010000000000000000000000000",
        "0000000000000000000001101011000110101111001100001111010001101100010010000101111000011001101011100101100001000110011001010000000000000000000000000",
        "0000000000000000000001101011100100011001100111101011000101110000010110000101001100110011110011011110011001110110111001010000000000000000000000000",
        "0000000000000000000001101011110111000111011011001110010001011100010111000101011000011100110010000100000100010110111101010000000000000000000000000",
        "0010110001100001010001001100100110110110011100100011011000100100010100010011101111010011001001000010110011011100010100001000100010010011100010100",
        "1101001110011110101110110011011001001001100011011100100111011011101011101100010000101100110110111101001100100011101011110111011101101100011101011"
    };

    private static readonly string[] ZintGs1_128CcB = {
        "0000000000000000000001100111010111011111011101001000001000010001010000101101111101101001111011101100100001100110011101010000000000000000000000000",
        "0000000000000000000001110111010111001011001000001110001101110100010000100101100011101111101011101000111000110111011101010000000000000000000000000",
        "0000000000000000000001110011010110011001000111101001010000111100010000110101011000101110000011111010011101000111001101010000000000000000000000000",
        "0000000000000000000001111011010101100011000010001100110110110000010001110101111101111001011011001000100001100111101101010000000000000000000000000",
        "0000000000000000000001111001010111010000110010001101000011100010010001100101000001001111010011100110001110010111100101010000000000000000000000000",
        "0000000000000000000001110001010110001010111110001001010011110000010001000101011111101111011011011111100110010111000101010000000000000000000000000",
        "0000000000000000000001100001010101000110001100001101100001000100010011000101011110011111011011000010001011000110000101010000000000000000000000000",
        "0000000000000000000001100011010110111000111110101111100010010011010011100101000011010111111011001100001111010110001101010000000000000000000000000",
        "0010110001100001010001001100100110100110001101110100111000111010010011110101100100001001010011000110010011100100010100001000101001110011100010100",
        "1101001110011110101110110011011001011001110010001011000111000101101100001010011011110110101100111001101100011011101011110111010110001100011101011"
    };

    private static readonly string[] ZintGs1_128CcC = {
        "1111111101010100011110101001111000111010011001111001110111110111010010000010000100010111110110100111101101001001100000011110101001111000111111101000101001",
        "1111111101010100011110101000001000111100010110110001110010011001000011111010010110000110101110000010001011001111100001011111101010011100111111101000101001",
        "1111111101010100011101010011111100101111000010100001011000100011100010000101000011110111010011111010001001110100110000010101000001111000111111101000101001",
        "1111111101010100011111010111111010110000101100111001110011110110010011111011110100110111001001000011101101000111001111011111010111111010111111101000101001",
        "1111111101010100011010111000000100110011000111101001001000111100100011001011101000000111001001101000001111001100001101011101011100011000111111101000101001",
        "1111111101010100011111010111100110110001100101111001101110000101110010010000100011110101100110011110001000111100001010011110101111100110111111101000101001",
        "1111111101010100011010011100111100111011100100001001101110011000100011100011000010110110010001011000001110110010110000011010011100111100111111101000101001",
        "1111111101010100011111010010011000111110100001001101001100111110001011110001100110100111011011100010001011100011111101010101111110011100111111101000101001",
        "1111111101010100011010011001111110111111001011000101001010001111000011111010000111010111101110101111101011011000011110010100111011111100111111101000101001",
        "1111111101010100010100011101110000111010000010111001100110000100001010011001100100000111001010111000001001100011100011010100011101110000111111101000101001",
        "1111111101010100011010011100000100111110010010110001010011110001000011110110001101000111110100011011101111110101001110011101001110011000111111101000101001",
        "1111111101010100010100010000011110101111100110100001000111100000101011011101001110000100000101000111101101011111101000010100011000111110111111101000101001",
        "1111111101010100010100000101000000110001000100001101000110001101000010010000001001000101101111011100001110000101000111010100000101000000111111101000101001",
        "1111111101010100011101000110100000110000110111100101001100011111001011100101100010000100100111110011001001100011111001011110100010010000111111101000101001",
        "0000000001011000110000101000100110010011001101101110011101000100010000101001001100110110010001101001110001001000111010100001000101011110011001110001010000",
        "0000000110100111001111010111011001101100110010010001100010111011101111010110110011001001101110010110001110110111000101011110111010100001100110001110101100"
    };

    [Fact]
    public void CcA_ComponentAndGeneralField_RoundTripIndependently() {
        var text = Gs1.ElementString("(21)ABC123");
        var bits = CompositeBitStreamCodec.Encode(text, Gs1CompositeComponent.CcA, 4, 145, out var columns, out var level);
        var modules = CompositeComponentCodec.Encode(bits, Gs1CompositeComponent.CcA, columns, level);

        Assert.True(CompositeComponentCodec.TryDecode(modules, out var recoveredBits, out var component));
        Assert.Equal(Gs1CompositeComponent.CcA, component);
        Assert.Equal(bits, recoveredBits);
        Assert.True(CompositeBitStreamCodec.TryDecode(recoveredBits, out var recoveredText));
        Assert.Equal(text, recoveredText);
    }

    [Fact]
    public void Gs1_128CcA_MatchesGs1Figure11AndIndependentZintMatrix() {
        var elementString = Gs1.ElementString("(21)A1B2C3D4E5F6G7H8");
        var bits = CompositeBitStreamCodec.Encode(elementString, Gs1CompositeComponent.CcA, 4, 145, out _, out _);
        Assert.Equal("000111110000100000001101000010011110001001000100011010011001000101010010101011100110011001001110110100100001",
            new string(System.Array.ConvertAll(bits, bit => bit ? '1' : '0')));
        var oracleComponent = new BitMatrix(99, 4);
        for (var y = 0; y < 4; y++) {
            for (var x = 0; x < 99; x++) oracleComponent[x, y] = ZintGs1_128CcAFigure11[y][x + 21] == '1';
        }
        Assert.True(CompositeComponentCodec.TryDecode(oracleComponent, out var oracleBits, out var oracleType));
        Assert.Equal(Gs1CompositeComponent.CcA, oracleType);
        Assert.Equal(bits, oracleBits);
        var symbol = Gs1CompositeEncoder.Encode("(01)03212345678906", "(21)A1B2C3D4E5F6G7H8",
            new Gs1CompositeEncodingOptions { Component = Gs1CompositeComponent.CcA });

        Assert.Equal(145, symbol.Modules.Width);
        Assert.Equal(ZintGs1_128CcAFigure11, ToRows(symbol.Modules));
    }

    [Fact]
    public void Gs1_128CcB_MatchesIndependentZintMatrix() {
        var compositeText = Gs1.ElementString("(91)12345678901234567890123456789012345678901");
        var bits = CompositeBitStreamCodec.Encode(compositeText, Gs1CompositeComponent.CcB, 4, 145, out _, out _);
        Assert.Equal("01101100001010101011011000101101110111010110010101010110110001011011101110101100101010101101100010110111011101011001010101011011000101101110111010110010",
            new string(System.Array.ConvertAll(bits, bit => bit ? '1' : '0')));
        var oracleComponent = new BitMatrix(99, 8);
        for (var y = 0; y < 8; y++) {
            for (var x = 0; x < 99; x++) oracleComponent[x, y] = ZintGs1_128CcB[y][x + 21] == '1';
        }
        Assert.True(CompositeComponentCodec.TryDecode(oracleComponent, out var oracleBits, out var oracleType));
        Assert.Equal(Gs1CompositeComponent.CcB, oracleType);
        Assert.Equal(bits, oracleBits);
        var symbol = Gs1CompositeEncoder.Encode("(01)12345678901231",
            "(91)12345678901234567890123456789012345678901",
            new Gs1CompositeEncodingOptions { Component = Gs1CompositeComponent.CcB });

        Assert.Equal(145, symbol.Modules.Width);
        Assert.Equal(ZintGs1_128CcB, ToRows(symbol.Modules));
        Assert.True(Gs1CompositeDecoder.TryDecode(symbol.Modules, out var decoded));
        Assert.Equal(Gs1.ElementString("(91)12345678901234567890123456789012345678901"), decoded.CompositeText);
    }

    [Fact]
    public void Gs1_128CcC_MatchesIndependentZintMatrix() {
        const string composite = "(91)ABC1234567890abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ-1234567890";
        var symbol = Gs1CompositeEncoder.Encode("(01)09506000134352", composite,
            new Gs1CompositeEncodingOptions { Component = Gs1CompositeComponent.CcC });

        Assert.Equal(154, symbol.Modules.Width);
        Assert.Equal(ZintGs1_128CcC, ToRows(symbol.Modules));
        Assert.True(Gs1CompositeDecoder.TryDecode(symbol.Modules, out var decoded));
        Assert.Equal(Gs1.ElementString(composite), decoded.CompositeText);
    }

    [Fact]
    public void UnifiedFacadesAndCapabilityCatalog_ExposeGs1Composite() {
        var modules = MatrixBarcodeEncoder.EncodeGs1Composite("(01)09506000134352", "(21)ABC123");

        Assert.True(MatrixBarcodeDecoder.TryDecode(BarcodeType.GS1Composite, modules, out var text));
        Assert.Equal(Gs1.ElementString("(21)ABC123"), text);
        Assert.True(MatrixBarcodeDecoder.TryDecodeGs1Composite(modules, out var detailed));
        Assert.Equal(Gs1.ElementString("(01)09506000134352"), detailed.LinearText);
        Assert.True(CodeGlyph.TryDecode(modules, out var decoded, expectedBarcode: BarcodeType.GS1Composite));
        Assert.Equal(CodeGlyphKind.Gs1Composite, decoded.Kind);
        Assert.NotNull(decoded.Gs1Composite);
        Assert.Equal(SymbolPayloadProfile.Gs1,
            new DetectedSymbol(SymbolFormat.Gs1Composite, decoded, new ImageRegion(0, 0, 1, 1)).PayloadProfile);

        var capability = SymbolCapabilities.Get(SymbolFormat.Gs1Composite);
        Assert.True(capability.CanEncode);
        Assert.True(capability.CanDecodeModules);
        Assert.True((capability.Operations & SymbolCapabilityFlags.Gs1Encode) != 0);
        Assert.Equal(BarcodeType.GS1Composite, capability.LegacyBarcodeType);
    }

    [Theory]
    [InlineData(Gs1CompositeComponent.CcA, "(21)ABC123")]
    [InlineData(Gs1CompositeComponent.CcB, "(91)ABC1234567890abcdefghijklmnopqrstuvwxyz")]
    [InlineData(Gs1CompositeComponent.CcC, "(91)ABC1234567890abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ-1234567890")]
    public void Gs1_128Components_RoundTripBothMessages(Gs1CompositeComponent component, string compositeText) {
        var symbol = Gs1CompositeEncoder.Encode("(01)09506000134352", compositeText,
            new Gs1CompositeEncodingOptions { Component = component });

        Assert.Equal(component, symbol.Component);
        Assert.True(Gs1CompositeDecoder.TryDecode(symbol.Modules, out var decoded));
        Assert.Equal(Gs1.ElementString("(01)09506000134352"), decoded.LinearText);
        Assert.Equal(Gs1.ElementString(compositeText), decoded.CompositeText);
        Assert.Equal(component, decoded.Component);
    }

    [Fact]
    public void Auto_SelectsSmallestFittingComponent() {
        var compact = Gs1CompositeEncoder.Encode("(01)09506000134352", "(21)ABC123");
        var larger = Gs1CompositeEncoder.Encode("(01)09506000134352",
            "(91)abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789abcdefghijklmnopqrstuvwxyz");

        Assert.Equal(Gs1CompositeComponent.CcA, compact.Component);
        Assert.NotEqual(Gs1CompositeComponent.CcA, larger.Component);
    }

    [Fact]
    public void InvalidEmptyComponents_AreRejected() {
        Assert.Throws<System.ArgumentException>(() => Gs1CompositeEncoder.Encode(string.Empty, "(21)ABC"));
        Assert.Throws<System.ArgumentException>(() => Gs1CompositeEncoder.Encode("(01)09506000134352", string.Empty));
        Assert.False(Gs1CompositeDecoder.TryDecode(new BitMatrix(20, 4), out _));
    }

    [Fact]
    public void NonAsciiDigits_AreRejectedInsteadOfMisencoded() {
        Assert.Throws<System.ArgumentException>(() =>
            Gs1CompositeEncoder.Encode("(01)09506000134352", "(91)\u0661\u0662\u0663"));
    }

    private static string[] ToRows(BitMatrix matrix) {
        var rows = new string[matrix.Height];
        for (var y = 0; y < matrix.Height; y++) {
            var values = new char[matrix.Width];
            for (var x = 0; x < matrix.Width; x++) values[x] = matrix[x, y] ? '1' : '0';
            rows[y] = new string(values);
        }
        return rows;
    }
}
