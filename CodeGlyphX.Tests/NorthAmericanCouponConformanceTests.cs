using System;
using System.Text.Json;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class NorthAmericanCouponConformanceTests {
    [Fact]
    public void CouponProfiles_DecodeIndependentBwippAndUseSharedGs1Validation() {
        using var stream = typeof(NorthAmericanCouponConformanceTests).Assembly.GetManifestResourceStream(
            "CodeGlyphX.Tests.Fixtures.Conformance.bwipp-current.json");
        using var document = JsonDocument.Parse(stream!);
        var count = 0;
        foreach (var fixture in document.RootElement.GetProperty("fixtures").EnumerateArray()) {
            if (fixture.GetProperty("bcid").GetString() != "gs1northamericancoupon") continue;
            var text = fixture.GetProperty("text").GetString()!;
            var bits = fixture.GetProperty("modules").GetString()!;
            var matrix = new BitMatrix(fixture.GetProperty("width").GetInt32(), fixture.GetProperty("height").GetInt32());
            for (var y = 0; y < matrix.Height; y++)
                for (var x = 0; x < matrix.Width; x++) matrix[x, y] = bits[y * matrix.Width + x] == '1';
            Assert.True(NorthAmericanCoupon.TryDecode(matrix, out var coupon), text);
            Assert.Equal("123456-123456", coupon.HumanReadable);
            Assert.Equal(Gs1.ElementString(text), coupon.ElementString);
            var encoded = NorthAmericanCoupon.Encode(text);
            Assert.True(NorthAmericanCoupon.TryDecode(encoded.Modules, out coupon));
            Assert.Equal(Gs1.ElementString(text), coupon.ElementString);
            count++;
        }
        Assert.Equal(2, count);
        Assert.Equal("8110012345612345611110123", NorthAmericanCoupon.Prepare("123456", "123456", "1", "1", '0', "123"));
        Assert.Throws<ArgumentException>(() => NorthAmericanCoupon.Prepare("123456", "123456", "1", "1", '8', "123"));
        Assert.False(NorthAmericanCoupon.TryParse("(8110)012345612345611110123(10)OTHER", out _));
        Assert.False(NorthAmericanCoupon.TryParse("(8110)01234561234561111012332601014261231", out _));
    }
}
