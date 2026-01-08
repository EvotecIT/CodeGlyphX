using System.Collections.Generic;
using CodeGlyphX.Pdf417;
using CodeGlyphX.Pdf417.Ec;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class Pdf417ErrorCorrectionTests {
    [Fact]
    public void ErrorCorrection_CorrectsSingleError() {
        var data = new List<int> { 3, 65, 66 };
        var ecc = Pdf417ErrorCorrection.GenerateErrorCorrection(data, 2); // 8 codewords
        var received = new int[data.Count + ecc.Length];
        data.CopyTo(received, 0);
        ecc.CopyTo(received, data.Count);

        received[1] = (received[1] + 7) % 929;

        var ec = new ErrorCorrection();
        Assert.True(ec.Decode(received, ecc.Length));
        Assert.Equal(65, received[1]);
    }
}
