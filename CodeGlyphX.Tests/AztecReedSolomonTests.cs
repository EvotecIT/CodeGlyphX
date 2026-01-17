using CodeGlyphX.Aztec.Internal;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class AztecReedSolomonTests {
    [Fact]
    public void ReedSolomon_Roundtrip_Data6() {
        var field = GenericGf.AztecData6;
        var encoder = new ReedSolomonEncoder(field);
        var decoder = new ReedSolomonDecoder(field);
        var data = new int[12];
        for (var i = 0; i < 8; i++) data[i] = (i * 7 + 3) % field.Size;
        encoder.Encode(data, 4);
        decoder.Decode(data, 4);
    }
}
