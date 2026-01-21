using CodeGlyphX;
using CodeGlyphX.Code39;
using CodeGlyphX.Code93;
using CodeGlyphX.Code25;
using CodeGlyphX.Code32;
using CodeGlyphX.DataBar;
using CodeGlyphX.Ean;
using CodeGlyphX.Itf;
using CodeGlyphX.PatchCode;
using CodeGlyphX.Pharmacode;
using CodeGlyphX.UpcA;
using CodeGlyphX.UpcE;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class Barcode1DTests {
    [Fact]
    public void Code39_TotalModules_NoChecksum() {
        var barcode = Code39Encoder.Encode("ABC", includeChecksum: false, fullAsciiMode: false);
        Assert.Equal(64, barcode.TotalModules);
    }

    [Fact]
    public void Code39_TotalModules_WithChecksum() {
        var barcode = Code39Encoder.Encode("ABC", includeChecksum: true, fullAsciiMode: false);
        Assert.Equal(77, barcode.TotalModules);
    }

    [Fact]
    public void Code93_TotalModules_NoChecksum() {
        var barcode = Code93Encoder.Encode("ABC", includeChecksum: false, fullAsciiMode: false);
        Assert.Equal(46, barcode.TotalModules);
    }

    [Fact]
    public void Code93_TotalModules_WithChecksum() {
        var barcode = Code93Encoder.Encode("ABC", includeChecksum: true, fullAsciiMode: false);
        Assert.Equal(64, barcode.TotalModules);
    }

    [Fact]
    public void Ean8_TotalModules() {
        var barcode = EanEncoder.Encode("1234567");
        Assert.Equal(67, barcode.TotalModules);
    }

    [Fact]
    public void Ean13_TotalModules() {
        var barcode = EanEncoder.Encode("590123412345");
        Assert.Equal(95, barcode.TotalModules);
    }

    [Fact]
    public void Ean13_TotalModules_WithAddOn2() {
        var barcode = EanEncoder.Encode("590123412345+12");
        Assert.Equal(116, barcode.TotalModules);
    }

    [Fact]
    public void Ean13_TotalModules_WithAddOn5() {
        var barcode = EanEncoder.Encode("590123412345+51234");
        Assert.Equal(143, barcode.TotalModules);
    }

    [Fact]
    public void UpcA_TotalModules() {
        var barcode = UpcAEncoder.Encode("03600029145");
        Assert.Equal(95, barcode.TotalModules);
    }

    [Fact]
    public void UpcE_TotalModules() {
        var barcode = UpcEEncoder.Encode("042100", UpcENumberSystem.Zero);
        Assert.Equal(51, barcode.TotalModules);
    }

    [Fact]
    public void UpcE_TotalModules_WithAddOn2() {
        var barcode = UpcEEncoder.Encode("042100+12", UpcENumberSystem.Zero);
        Assert.Equal(72, barcode.TotalModules);
    }

    [Fact]
    public void Gs1DataBarTruncated_TotalModules() {
        var barcode = DataBar14Encoder.EncodeTruncated("1234567890123");
        Assert.Equal(96, barcode.TotalModules);
    }

    [Fact]
    public void Gs1DataBarTruncated_RoundTrip_Modules() {
        var barcode = DataBar14Encoder.EncodeTruncated("0001234567890");
        Assert.True(DataBar14Decoder.TryDecodeTruncated(barcode, out var text));
        Assert.Equal("0001234567890", text);
    }

    [Fact]
    public void Gs1DataBarExpanded_RoundTrip_Modules() {
        var input = "(01)98898765432106(3103)000001";
        var barcode = DataBarExpandedEncoder.EncodeExpanded(input);
        Assert.True(DataBarExpandedDecoder.TryDecodeExpanded(barcode, out var text));
        Assert.Equal(Gs1.ElementString(input), text);
    }

    [Fact]
    public void Itf14_TotalModules() {
        var barcode = Itf14Encoder.Encode("1234567890123");
        Assert.Equal(135, barcode.TotalModules);
    }

    [Fact]
    public void Itf_TotalModules() {
        var barcode = ItfEncoder.Encode("123456");
        Assert.Equal(63, barcode.TotalModules);
    }

    [Fact]
    public void Matrix2of5_TotalModules() {
        var barcode = Matrix2of5Encoder.Encode("123456");
        Assert.Equal(107, barcode.TotalModules);
    }

    [Fact]
    public void Industrial2of5_TotalModules() {
        var barcode = Industrial2of5Encoder.Encode("123456");
        Assert.Equal(103, barcode.TotalModules);
    }

    [Fact]
    public void Iata2of5_TotalModules() {
        var barcode = Iata2of5Encoder.Encode("123456");
        Assert.Equal(97, barcode.TotalModules);
    }

    [Fact]
    public void PatchCode_TotalModules() {
        var barcode = PatchCodeEncoder.Encode("T");
        Assert.Equal(11, barcode.TotalModules);
    }

    [Fact]
    public void Pharmacode_TotalModules() {
        var barcode = PharmacodeEncoder.Encode(91);
        Assert.Equal(14, barcode.TotalModules);
    }

    [Fact]
    public void Code32_TotalModules() {
        var barcode = Code32Encoder.Encode("02608901");
        Assert.Equal(103, barcode.TotalModules);
    }
}
