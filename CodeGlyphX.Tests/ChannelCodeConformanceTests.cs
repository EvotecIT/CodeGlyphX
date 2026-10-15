using System;
using System.Linq;
using System.Text.Json;
using CodeGlyphX.ChannelCode;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class ChannelCodeConformanceTests {
    [Fact]
    public void RankedChannelWidths_MatchIndependentBwippAtEveryChannelCapacity() {
        using var stream = typeof(ChannelCodeConformanceTests).Assembly.GetManifestResourceStream(
            "CodeGlyphX.Tests.Fixtures.Conformance.bwipp-linear.json");
        using var document = JsonDocument.Parse(stream!);
        var count = 0;
        foreach (var fixture in document.RootElement.GetProperty("fixtures").EnumerateArray()) {
            if (fixture.GetProperty("bcid").GetString() != "channelcode") continue;
            var value = fixture.GetProperty("text").GetString()!;
            var bits = fixture.GetProperty("modules").GetString()!;
            var barcode = Barcode.Encode(SymbolFormat.ChannelCode, value);
            Assert.Equal(bits, string.Concat(barcode.Segments.Select(s => new string(s.IsBar ? '1' : '0', s.Modules))));
            Assert.True(BarcodeDecoder.TryDecode(bits.Select(bit => bit == '1').ToArray(), BarcodeType.ChannelCode, out var decoded));
            Assert.Equal(value, decoded.Text);
            count++;
        }
        Assert.Equal(7, count);
    }

    [Theory]
    [InlineData("7742863")]
    [InlineData("12345678")]
    [InlineData("-1")]
    [InlineData("")]
    public void ChannelCapacity_RejectsOverflowAndNonNumericValues(string value) {
        Assert.Throws<ArgumentException>(() => ChannelCodeEncoder.Encode(value));
    }
}
