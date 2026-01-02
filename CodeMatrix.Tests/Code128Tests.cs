using CodeMatrix.Code128;
using Xunit;

namespace CodeMatrix.Tests;

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

    private static int ComputeChecksum(int[] codes) {
        // codes include: start, data..., checksum, stop
        var end = codes.Length - 2; // checksum is at end-1
        var sum = codes[0];
        for (var pos = 1; pos < end; pos++) sum = (sum + codes[pos] * pos) % 103;
        return sum;
    }
}
