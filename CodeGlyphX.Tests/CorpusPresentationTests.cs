using CodeGlyphX.Benchmarks;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class CorpusPresentationTests {
    [Theory]
    [InlineData(MaxiCodeMode.StructuredCarrierNumeric, "12345", "123450000", true)]
    [InlineData(MaxiCodeMode.StructuredCarrierAlphanumeric, "EC1A1B", "EC1A1B", false)]
    public void CarrierComparison_PreservesAllFieldsAndSecondaryText(MaxiCodeMode mode, string postal, string decodedPostal, bool envelope) {
        var matrix = MaxiCodeEncoder.EncodeText("SHIP", new MaxiCodeEncodingOptions {
            Mode = mode, PostalCode = postal, CountryCode = 840, ServiceClass = 1,
            StructuredCarrierMessageVersion = envelope ? 96 : null
        }).Modules;
        Assert.True(SymbolDecoder.TryDecode(matrix, out var symbol, format: SymbolFormat.MaxiCode));
        var primary = decodedPostal + "\u001d840\u001d001\u001d";
        Assert.Equal(envelope ? "[)>\u001e01\u001d96" + primary + "SHIP" : primary + "SHIP", CorpusPresentation.Text(symbol));
        Assert.Equal(envelope ? "[)>\u001e01\u001d96SHIP" : "SHIP", symbol.Text);
    }

    [Fact]
    public void UnstructuredAndOtherFormats_KeepExactText() {
        Assert.True(SymbolDecoder.TryDecode(MaxiCodeEncoder.EncodeText("[)>\u001e01\u001d96DATA").Modules,
            out var symbol, format: SymbolFormat.MaxiCode));
        Assert.Equal(symbol.Text, CorpusPresentation.Text(symbol));
        Assert.Equal("123", CorpusPresentation.Text(new DetectedSymbol(SymbolFormat.Code128, "123")));
    }
}
