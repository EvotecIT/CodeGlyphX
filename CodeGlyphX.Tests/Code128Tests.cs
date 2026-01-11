using CodeGlyphX.Code128;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class Code128Tests {
    [Theory]
    [InlineData("CODE128-12345")]
    [InlineData("1234567890")]
    public void Checksum_IsCorrect(string value) {
        var codes = Code128Encoder.EncodeCodeValues(value);
        Assert.True(codes.Length >= 3); // start, checksum, stop

        var checksumFromBarcode = codes[^2];
        var checksumExpected = ComputeChecksum(codes);
        Assert.Equal(checksumExpected, checksumFromBarcode);
    }

    [Theory]
    [InlineData("CODE128-12345", 167)]
    [InlineData("1234567890", 90)]
    public void TotalModules_IsStable(string value, int expected) {
        var barcode = BarcodeEncoder.Encode(BarcodeType.Code128, value);
        Assert.Equal(expected, barcode.TotalModules);
    }

    [Fact]
    public void Gs1_128_IncludesFnc1() {
        var elementText = "(01)09506000134352(10)ABC123(17)240101";
        var elementString = Gs1.ElementString(elementText);
        Assert.Contains(Gs1.GroupSeparator, elementString);

        var codes = Code128Encoder.EncodeCodeValues(elementString, gs1: true);
        var fnc1Count = 0;
        for (var i = 0; i < codes.Length; i++) {
            if (codes[i] == Code128Tables.Fnc1) fnc1Count++;
        }
        Assert.True(fnc1Count >= 2);
    }

    private static int ComputeChecksum(int[] codes) {
        // codes include: start, data..., checksum, stop
        var end = codes.Length - 2; // checksum is at end-1
        var sum = codes[0];
        for (var pos = 1; pos < end; pos++) sum = (sum + codes[pos] * pos) % 103;
        return sum;
    }
}
