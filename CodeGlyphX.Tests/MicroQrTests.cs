using System.Text;
using CodeGlyphX;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class MicroQrTests {
    [Fact]
    public void MicroQr_EncodeDecode_ByteMode() {
        var data = Encoding.ASCII.GetBytes("HELLO");
        var code = MicroQrCodeEncoder.EncodeBytes(data, ecc: QrErrorCorrectionLevel.L, minVersion: 1, maxVersion: 4);

        Assert.True(MicroQrDecoder.TryDecode(code.Modules, out var decoded));
        Assert.Equal(data, decoded.Bytes);
        Assert.Equal("HELLO", decoded.Text);
    }

    [Fact]
    public void MicroQr_EncodeDecode_Numeric() {
        var code = MicroQrCodeEncoder.EncodeNumeric("1234", ecc: QrErrorCorrectionLevel.L, minVersion: 1, maxVersion: 4);

        Assert.Equal(1, code.Version);
        Assert.True(MicroQrDecoder.TryDecode(code.Modules, out var decoded));
        Assert.Equal("1234", decoded.Text);
    }
}
