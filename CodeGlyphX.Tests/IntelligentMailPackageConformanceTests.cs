using System;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class IntelligentMailPackageConformanceTests {
    [Fact]
    public void PackageCarriers_MatchIndependentBwippWithRoutingAndChannelVariants() {
        using var stream = typeof(IntelligentMailPackageConformanceTests).Assembly.GetManifestResourceStream(
            "CodeGlyphX.Tests.Fixtures.Conformance.bwipp-current.json");
        using var document = JsonDocument.Parse(stream!);
        var count = 0;
        foreach (var fixture in document.RootElement.GetProperty("fixtures").EnumerateArray()) {
            if (fixture.GetProperty("bcid").GetString() != "impb") continue;
            var pic = fixture.GetProperty("pic").GetString()!;
            var zip = fixture.GetProperty("zip").GetString()!;
            var expected = fixture.GetProperty("modules").GetString()!;
            var encoded = IntelligentMailPackage.Encode(pic, zip);
            Assert.Equal(expected, string.Concat(encoded.Segments.SelectMany(s => Enumerable.Repeat(s.IsBar ? '1' : '0', s.Modules))));
            Assert.True(IntelligentMailPackage.TryDecode(expected.Select(c => c == '1').ToArray(), out var decoded));
            Assert.Equal(pic, decoded.TrackingNumber);
            Assert.Equal(zip, decoded.RoutingZip);
            Assert.Equal(IntelligentMailPackage.Prepare(pic, zip), decoded.ElementString);
            Assert.Equal(pic, decoded.HumanReadable.Replace(" ", ""));
            count++;
        }
        Assert.Equal(6, count);
    }

    [Fact]
    public void UspsPublishedCheckExample_ExcludesRoutingAndRejectsInvalidConstructs() {
        Assert.Equal("9101123456789000000013", IntelligentMailPackage.Prepare("910112345678900000001"));
        var routed = IntelligentMailPackage.Prepare("910112345678900000001", "12345");
        Assert.Equal("42012345\u001d9101123456789000000013", routed);
        Assert.True(IntelligentMailPackage.TryParse(IntelligentMailPackage.CreateCommercial("123", "912345678", "1234567"), out var commercial));
        Assert.Equal("92", commercial.ChannelApplicationIdentifier);
        Assert.True(IntelligentMailPackage.TryParse(IntelligentMailPackage.CreateOnline("123", "12", "123456", "12345678"), out _));
        Assert.True(IntelligentMailPackage.TryParse(IntelligentMailPackage.CreateRetail("123", "1", "123456", "6123", "12345"), out _));
        Assert.False(IntelligentMailPackage.TryParse("9101123456789000000014", out _));
        Assert.False(IntelligentMailPackage.TryParse("420123459101123456789000000013", out _));
        Assert.Throws<ArgumentException>(() => IntelligentMailPackage.CreateCommercial("123", "912345678", "12345678901", "123456789"));
        Assert.Throws<ArgumentException>(() => IntelligentMailPackage.CreateCommercial("123", "123456789", "1234567"));
        Assert.Throws<ArgumentException>(() => IntelligentMailPackage.CreateRetail("123", "1", "123456", "6000", "12345"));
    }
}
