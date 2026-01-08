using CodeGlyphX;
using CodeGlyphX.Qr;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class QrPayloadTests {
    [Fact]
    public void QrDecode_DefaultsToLatin1WhenNoEci() {
        var bytes = new byte[] { 0x41, 0xC4, 0xE9 };
        var code = QrCodeEncoder.EncodeBytes(bytes);

        Assert.True(QrDecoder.TryDecode(code.Modules, out var decoded));
        Assert.Equal("AÄé", decoded.Text);
        Assert.Equal(bytes, decoded.Bytes);
    }

    [Fact]
    public void QrPayload_UsesEciPerSegment() {
        var bb = new QrBitBuffer();

        AppendEci(bb, 26); // UTF-8
        bb.AppendBits(0b0100, 4);
        bb.AppendBits(2, 8);
        bb.AppendBits(0xC5, 8);
        bb.AppendBits(0x81, 8); // Ł

        AppendEci(bb, 3); // Latin1
        bb.AppendBits(0b0100, 4);
        bb.AppendBits(1, 8);
        bb.AppendBits(0xE9, 8); // é

        bb.AppendBits(0, 4);
        while ((bb.LengthBits & 7) != 0) bb.AppendBit(false);

        var data = bb.ToByteArray();
        Assert.True(QrPayloadParser.TryParse(data, 1, out var payload, out var segments));
        Assert.Equal(new byte[] { 0xC5, 0x81, 0xE9 }, payload);
        Assert.Equal(2, segments.Length);
        Assert.Equal(QrTextEncoding.Utf8, segments[0].Encoding);
        Assert.Equal(QrTextEncoding.Latin1, segments[1].Encoding);
        Assert.Equal("Łé", QrDecoder.DecodeSegments(segments));
    }

    private static void AppendEci(QrBitBuffer bb, int assignmentNumber) {
        bb.AppendBits(0b0111, 4);
        if (assignmentNumber <= 0x7F) {
            bb.AppendBits(assignmentNumber, 8);
            return;
        }

        if (assignmentNumber <= 0x3FFF) {
            var first = 0b1000_0000 | ((assignmentNumber >> 8) & 0b0011_1111);
            bb.AppendBits(first, 8);
            bb.AppendBits(assignmentNumber & 0xFF, 8);
            return;
        }

        var head = 0b1100_0000 | ((assignmentNumber >> 16) & 0b0001_1111);
        bb.AppendBits(head, 8);
        bb.AppendBits((assignmentNumber >> 8) & 0xFF, 8);
        bb.AppendBits(assignmentNumber & 0xFF, 8);
    }
}
