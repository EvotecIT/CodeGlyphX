using System;
using System.Text;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class HanXinTests {
    private static readonly string[] ZintVersion1NumericRows = {
        "FE8AFE", "801202", "BE02FA", "A0380A", "AEEAEA", "AE00EA", "AE18EA", "002200", "150200", "18A008", "0000F0",
        "07A000", "000002", "D60168", "008150", "008400", "FE50EA", "0200EA", "FA82EA", "0A1C0A", "EAA0FA", "EA8002", "EA82FE"
    };
    private static readonly string[] ZintVersion1AutomaticMaskRows = {
        "FE20FE", "80C602", "BE2AFA", "A0EE0A", "AE42EA", "AED4EA", "AE30EA", "007600", "152980", "4DF55C", "AAAA5A",
        "52F554", "AAAAA8", "83543C", "032950", "00D000", "FE78EA", "0256EA", "FAA8EA", "0ACA0A", "EA88FA", "EAD602", "EA28FE"
    };

    [Fact]
    public void Version1Numeric_MatchesIndependentZintMatrix() {
        var symbol = HanXinEncoder.EncodeText("1234567890", new HanXinEncodingOptions {
            Mode = HanXinEncodingMode.Numeric, Version = 1, ErrorCorrectionLevel = 1, Mask = 0
        });

        Assert.Equal(ZintVersion1NumericRows, ToHexRows(symbol.Modules));
        Assert.True(HanXinDecoder.TryDecodeDetailed(symbol.Modules, out var decoded));
        Assert.Equal("1234567890", decoded.Text);
        Assert.Equal(1, decoded.Version);
        Assert.Equal(1, decoded.ErrorCorrectionLevel);
        Assert.Equal(0, decoded.Mask);
    }

    [Fact]
    public void AutomaticMask_MatchesIndependentZintSelection() {
        var symbol = HanXinEncoder.EncodeText("1234567890", new HanXinEncodingOptions {
            Mode = HanXinEncodingMode.Numeric, Version = 1, ErrorCorrectionLevel = 1
        });
        Assert.Equal(ZintVersion1AutomaticMaskRows, ToHexRows(symbol.Modules));
    }

    [Theory]
    [InlineData("Han Xin / Mixed CASE 42!")]
    [InlineData("\u0001ABC xyz[]{}")]
    public void AsciiText_RoundTripsTextCompaction(string value) {
        var symbol = HanXinEncoder.EncodeText(value, new HanXinEncodingOptions { Mode = HanXinEncodingMode.Text });
        Assert.True(HanXinDecoder.TryDecodeDetailed(symbol.Modules, out var decoded));
        Assert.Equal(value, decoded.Text);
    }

    [Theory]
    [InlineData("Ł")]
    [InlineData("\u001C")]
    [InlineData("\u001D")]
    [InlineData("\u001E")]
    [InlineData("\u001F")]
    public void ExplicitTextMode_RejectsCharactersOutsideTextCompaction(string value) {
        Assert.Throws<ArgumentException>(() => HanXinEncoder.EncodeText(value, new HanXinEncodingOptions {
            Mode = HanXinEncodingMode.Text
        }));
    }

    [Theory]
    [InlineData("A\u001CB")]
    [InlineData("A\u001DB")]
    [InlineData("A\u001EB")]
    [InlineData("A\u001FB")]
    public void AutomaticMode_RoutesUnsupportedAsciiControlsToBinary(string value) {
        var symbol = HanXinEncoder.EncodeText(value);

        Assert.True(HanXinDecoder.TryDecodeDetailed(symbol.Modules, out var decoded));
        Assert.Equal(value, decoded.Text);
    }

    [Fact]
    public void AllVersionsAndEccLevels_RoundTrip() {
        for (var version = 1; version <= 84; version++) {
            for (var ecc = 1; ecc <= 4; ecc++) {
                var symbol = HanXinEncoder.EncodeText("1", new HanXinEncodingOptions { Version = version, ErrorCorrectionLevel = ecc, Mask = version & 3 });
                Assert.True(HanXinDecoder.TryDecodeDetailed(symbol.Modules, out var decoded));
                Assert.Equal("1", decoded.Text);
                Assert.Equal(version, decoded.Version);
                Assert.Equal(ecc, decoded.ErrorCorrectionLevel);
            }
        }
    }

    [Fact]
    public void BinaryAndUtf8Eci_RoundTrip() {
        var binary = new byte[] { 0, 1, 2, 127, 128, 200, 255 };
        var binarySymbol = HanXinEncoder.EncodeBytes(binary, new HanXinEncodingOptions { Mask = 3 });
        Assert.True(HanXinDecoder.TryDecodeDetailed(binarySymbol.Modules, out var binaryDecoded));
        Assert.Equal(binary, binaryDecoded.Bytes);

        var utf8 = HanXinEncoder.EncodeText("汉字 / Łódź");
        Assert.True(HanXinDecoder.TryDecodeDetailed(utf8.Modules, out var utf8Decoded));
        Assert.Equal("汉字 / Łódź", utf8Decoded.Text);
        Assert.Equal(new[] { 26 }, utf8Decoded.EciAssignments);
    }

    [Fact]
    public void ExplicitUtf8Encoding_InfersEciAndRoundTrips() {
        var symbol = HanXinEncoder.EncodeText("汉字 / Łódź", new HanXinEncodingOptions {
            TextEncoding = Encoding.UTF8
        });

        Assert.True(HanXinDecoder.TryDecodeDetailed(symbol.Modules, out var decoded));
        Assert.Equal("汉字 / Łódź", decoded.Text);
        Assert.Equal(new[] { 26 }, decoded.EciAssignments);
    }

    [Fact]
    public void ExplicitEci_SelectsItsMatchingTextEncoding() {
        var symbol = HanXinEncoder.EncodeText("é", new HanXinEncodingOptions {
            Mode = HanXinEncodingMode.Binary,
            EciAssignmentNumber = 26
        });

        Assert.True(HanXinDecoder.TryDecodeDetailed(symbol.Modules, out var decoded));
        Assert.Equal("é", decoded.Text);
        Assert.Equal(new[] { 26 }, decoded.EciAssignments);
        Assert.Equal(Encoding.UTF8.GetBytes("é"), decoded.Bytes);
    }

    [Fact]
    public void ConflictingEncodingAndEci_AreRejected() {
        Assert.Throws<InvalidOperationException>(() => HanXinEncoder.EncodeText("é", new HanXinEncodingOptions {
            Mode = HanXinEncodingMode.Binary,
            TextEncoding = Encoding.Latin1,
            EciAssignmentNumber = 26
        }));
        Assert.Throws<InvalidOperationException>(() => HanXinEncoder.EncodeText("A", new HanXinEncodingOptions {
            Mode = HanXinEncodingMode.Binary,
            EciAssignmentNumber = 899
        }));
    }

    [Fact]
    public void ExplicitLossyEncoding_IsRejected() {
        Assert.Throws<ArgumentException>(() => HanXinEncoder.EncodeText("Ł", new HanXinEncodingOptions {
            TextEncoding = Encoding.Latin1
        }));
    }

    [Fact]
    public void InvalidOptionsAndEmptyMatrix_AreRejected() {
        Assert.Throws<ArgumentOutOfRangeException>(() => HanXinEncoder.EncodeText("A", new HanXinEncodingOptions { Version = 85 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => HanXinEncoder.EncodeText("A", new HanXinEncodingOptions { Mask = 4 }));
        Assert.Throws<InvalidOperationException>(() => HanXinEncoder.EncodeText("Ł", new HanXinEncodingOptions {
            Mode = HanXinEncodingMode.Binary,
            TextEncoding = Encoding.Unicode
        }));
        Assert.False(HanXinDecoder.TryDecodeDetailed(new BitMatrix(23, 23), out _));
    }

    [Fact]
    public void UnifiedFacadesAndCapabilityCatalog_ExposeHanXin() {
        var modules = MatrixBarcodeEncoder.Encode(BarcodeType.HanXin, "UNIFIED-HANXIN");
        Assert.True(MatrixBarcodeDecoder.TryDecode(BarcodeType.HanXin, modules, out var text));
        Assert.Equal("UNIFIED-HANXIN", text);
        Assert.True(CodeGlyph.TryDecode(modules, out var decoded, expectedBarcode: BarcodeType.HanXin));
        Assert.Equal(CodeGlyphKind.HanXin, decoded.Kind);
        Assert.NotNull(decoded.HanXin);

        var capability = SymbolCapabilities.Get(SymbolFormat.HanXin);
        Assert.True(capability.CanEncode);
        Assert.True(capability.CanDecodeModules);
        Assert.True((capability.Operations & SymbolCapabilityFlags.EciDecode) != 0);
    }

    private static string[] ToHexRows(BitMatrix matrix) {
        var rowChars = (matrix.Width + 3) / 4;
        var rows = new string[matrix.Height];
        for (var y = 0; y < matrix.Height; y++) {
            var chars = new char[rowChars];
            for (var nibble = 0; nibble < rowChars; nibble++) {
                var value = 0;
                for (var bit = 0; bit < 4; bit++) { var x = nibble * 4 + bit; if (x < matrix.Width && matrix[x, y]) value |= 1 << (3 - bit); }
                chars[nibble] = "0123456789ABCDEF"[value];
            }
            rows[y] = new string(chars);
        }
        return rows;
    }
}
