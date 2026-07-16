using System;
using System.Text;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class MaxiCodeTests {
    private static readonly string[] ZintMode4Rows = {
        "1DF40ADC", "B51C40D8", "B8AF27EC", "55555550", "0000000C", "AAAAAAA8", "5555555C", "00000008", "AAAAAAAC",
        "55FF0150", "003D8804", "AAE03AA8", "56200D58", "02800808", "A8001AA4", "55000D58", "00800808", "AB000AA8",
        "56C0155C", "02000000", "A96006AC", "55600958", "0062C00C", "AA6E52A8", "55555C88", "000000B0", "AAAAAAB4",
        "63C85B90", "29C28F90", "E83AF150", "1CC81318", "82D808F8", "74404FA4"
    };

    [Fact]
    public void Mode4_MatchesIndependentZintMatrix() {
        var symbol = MaxiCodeEncoder.EncodeText("MaxiCode (19 chars)", new MaxiCodeEncodingOptions {
            Mode = MaxiCodeMode.Standard
        });

        Assert.Equal(30, symbol.Modules.Width);
        Assert.Equal(33, symbol.Modules.Height);
        Assert.Equal(ZintMode4Rows, ToHexRows(symbol.Modules));
        Assert.True(MaxiCodeDecoder.TryDecodeDetailed(symbol.Modules, out var decoded));
        Assert.Equal("MaxiCode (19 chars)", decoded.Text);
        Assert.Equal(MaxiCodeMode.Standard, decoded.Mode);
    }

    [Theory]
    [InlineData(MaxiCodeMode.Standard)]
    [InlineData(MaxiCodeMode.FullEcc)]
    [InlineData(MaxiCodeMode.ReaderProgramming)]
    public void UnstructuredModes_RoundTripCompaction(MaxiCodeMode mode) {
        const string text = "Mixed Case / 123456789 / punctuation!";
        var symbol = MaxiCodeEncoder.EncodeText(text, new MaxiCodeEncodingOptions { Mode = mode });

        Assert.True(MaxiCodeDecoder.TryDecodeDetailed(symbol.Modules, out var decoded));
        Assert.Equal(text, decoded.Text);
        Assert.Equal(mode, decoded.Mode);
        Assert.Equal(mode == MaxiCodeMode.ReaderProgramming, decoded.IsReaderProgramming);
    }

    [Fact]
    public void Mode2_RoundTripsCarrierPrimaryAndScmPrefix() {
        var symbol = MaxiCodeEncoder.EncodeText("SECONDARY", new MaxiCodeEncodingOptions {
            Mode = MaxiCodeMode.StructuredCarrierNumeric,
            PostalCode = "12345",
            CountryCode = 840,
            ServiceClass = 1,
            StructuredCarrierMessageVersion = 7
        });

        Assert.True(MaxiCodeDecoder.TryDecodeDetailed(symbol.Modules, out var decoded));
        Assert.Equal(MaxiCodeMode.StructuredCarrierNumeric, decoded.Mode);
        Assert.Equal("123450000", decoded.PostalCode);
        Assert.Equal(840, decoded.CountryCode);
        Assert.Equal(1, decoded.ServiceClass);
        Assert.Equal("[)>\u001e01\u001d07SECONDARY", decoded.Text);
        Assert.Equal("]U1", decoded.SymbologyIdentifier);
    }

    [Fact]
    public void Mode3_RoundTripsAlphanumericCarrierPrimary() {
        var symbol = MaxiCodeEncoder.EncodeText("SHIP", new MaxiCodeEncodingOptions {
            Mode = MaxiCodeMode.StructuredCarrierAlphanumeric,
            PostalCode = "EC1A1B",
            CountryCode = 826,
            ServiceClass = 12
        });

        Assert.True(MaxiCodeDecoder.TryDecodeDetailed(symbol.Modules, out var decoded));
        Assert.Equal("EC1A1B", decoded.PostalCode);
        Assert.Equal(826, decoded.CountryCode);
        Assert.Equal(12, decoded.ServiceClass);
        Assert.Equal("SHIP", decoded.Text);
    }

    [Fact]
    public void Utf8EciAndStructuredAppend_RoundTripMetadata() {
        var symbol = MaxiCodeEncoder.EncodeText("Łódź", new MaxiCodeEncodingOptions {
            Mode = MaxiCodeMode.FullEcc,
            StructuredAppendIndex = 2,
            StructuredAppendCount = 3
        });

        Assert.True(MaxiCodeDecoder.TryDecodeDetailed(symbol.Modules, out var decoded));
        Assert.Equal("Łódź", decoded.Text);
        Assert.Equal(new[] { 26 }, decoded.EciAssignments);
        Assert.Equal(2, decoded.StructuredAppendIndex);
        Assert.Equal(3, decoded.StructuredAppendCount);
    }

    [Fact]
    public void ExplicitUtf8Encoding_InfersEciAndRoundTrips() {
        var symbol = MaxiCodeEncoder.EncodeText("Łódź", new MaxiCodeEncodingOptions {
            TextEncoding = Encoding.UTF8
        });

        Assert.True(MaxiCodeDecoder.TryDecodeDetailed(symbol.Modules, out var decoded));
        Assert.Equal("Łódź", decoded.Text);
        Assert.Equal(new[] { 26 }, decoded.EciAssignments);
    }

    [Fact]
    public void Decoder_CorrectsPrimaryAndSecondaryModuleDamage() {
        var symbol = MaxiCodeEncoder.EncodeText("DAMAGE-CORRECTION-123456789");
        var damaged = Clone(symbol.Modules);
        Flip(damaged, 19, 15); // Primary codeword region.
        Flip(damaged, 0, 0);
        Flip(damaged, 4, 0);
        Flip(damaged, 8, 0);
        Flip(damaged, 12, 0);

        Assert.True(MaxiCodeDecoder.TryDecodeDetailed(damaged, out var decoded));
        Assert.Equal("DAMAGE-CORRECTION-123456789", decoded.Text);
    }

    [Fact]
    public void InvalidOptionsAndRandomMatrix_AreRejected() {
        Assert.Throws<InvalidOperationException>(() => MaxiCodeEncoder.EncodeText("X", new MaxiCodeEncodingOptions {
            Mode = MaxiCodeMode.StructuredCarrierNumeric,
            PostalCode = "ABC",
            CountryCode = 840
        }));
        Assert.Throws<InvalidOperationException>(() => MaxiCodeEncoder.EncodeText("X", new MaxiCodeEncodingOptions {
            StructuredAppendIndex = 2,
            StructuredAppendCount = 1
        }));
        Assert.Throws<InvalidOperationException>(() => MaxiCodeEncoder.EncodeText("X", new MaxiCodeEncodingOptions {
            TextEncoding = Encoding.Unicode
        }));
        Assert.False(MaxiCodeDecoder.TryDecodeDetailed(new BitMatrix(30, 33), out _));
    }

    [Fact]
    public void UnifiedFacadesAndCapabilityCatalog_ExposeMaxiCode() {
        var modules = MatrixBarcodeEncoder.Encode(BarcodeType.MaxiCode, "UNIFIED-MAXICODE");

        Assert.True(MatrixBarcodeDecoder.TryDecode(BarcodeType.MaxiCode, modules, out var text));
        Assert.Equal("UNIFIED-MAXICODE", text);
        Assert.True(CodeGlyph.TryDecode(modules, out var decoded, expectedBarcode: BarcodeType.MaxiCode));
        Assert.Equal(CodeGlyphKind.MaxiCode, decoded.Kind);
        Assert.Equal("UNIFIED-MAXICODE", decoded.Text);
        Assert.NotNull(decoded.MaxiCode);

        var capability = SymbolCapabilities.Get(SymbolFormat.MaxiCode);
        Assert.True(capability.CanEncode);
        Assert.True(capability.CanDecodeModules);
        Assert.True((capability.Operations & SymbolCapabilityFlags.EciEncode) != 0);
        Assert.True((capability.Operations & SymbolCapabilityFlags.StructuredAppendDecode) != 0);
    }

    private static string[] ToHexRows(BitMatrix matrix) {
        var rows = new string[matrix.Height];
        for (var y = 0; y < matrix.Height; y++) {
            var chars = new char[8];
            for (var nibble = 0; nibble < 8; nibble++) {
                var value = 0;
                for (var bit = 0; bit < 4; bit++) {
                    var x = nibble * 4 + bit;
                    if (x < matrix.Width && matrix[x, y]) value |= 1 << (3 - bit);
                }
                chars[nibble] = "0123456789ABCDEF"[value];
            }
            rows[y] = new string(chars);
        }
        return rows;
    }

    private static BitMatrix Clone(BitMatrix source) {
        var clone = new BitMatrix(source.Width, source.Height);
        for (var y = 0; y < source.Height; y++) {
            for (var x = 0; x < source.Width; x++) clone[x, y] = source[x, y];
        }
        return clone;
    }

    private static void Flip(BitMatrix matrix, int x, int y) => matrix[x, y] = !matrix[x, y];
}
