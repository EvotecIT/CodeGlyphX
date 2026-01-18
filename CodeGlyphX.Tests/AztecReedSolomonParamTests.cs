using CodeGlyphX.Aztec.Internal;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class AztecReedSolomonParamTests {
    [Fact]
    public void ReedSolomon_Roundtrip_Param() {
        var field = GenericGf.AztecParam;
        var encoder = new ReedSolomonEncoder(field);
        var decoder = new ReedSolomonDecoder(field);
        var data = new int[7];
        for (var i = 0; i < 4; i++) data[i] = (i * 3 + 1) % field.Size;
        encoder.Encode(data, 3);
        decoder.Decode(data, 3);
    }
}
