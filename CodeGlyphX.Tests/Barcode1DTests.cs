using CodeGlyphX.Code39;
using CodeGlyphX.Code93;
using CodeGlyphX.Ean;
using CodeGlyphX.Itf;
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
    public void Itf14_TotalModules() {
        var barcode = Itf14Encoder.Encode("1234567890123");
        Assert.Equal(135, barcode.TotalModules);
    }
}
