using CodeMatrix.Qr;
using Xunit;

namespace CodeMatrix.Tests;

public sealed class QrPayloadParserTests {
    [Fact]
    public void Parse_EciUtf8_ThenByteMode() {
        // Build a minimal bit stream:
        //   ECI (0111) + assignment 26 (UTF-8) + BYTE (0100) + count (3) + "abc" + terminator (0000)
        var data = new byte[7];
        var bitPos = 0;

        void WriteBits(int value, int count) {
            for (var i = count - 1; i >= 0; i--) {
                var bit = (value >> i) & 1;
                var byteIndex = bitPos >> 3;
                var bitIndex = 7 - (bitPos & 7);
                if (bit != 0) data[byteIndex] |= (byte)(1 << bitIndex);
                bitPos++;
            }
        }

        WriteBits(0b0111, 4);       // ECI
        WriteBits(0b00011010, 8);   // assignment 26 (0 + 7-bit value)
        WriteBits(0b0100, 4);       // BYTE mode
        WriteBits(3, 8);            // count (version 1..9 => 8 bits)
        WriteBits('a', 8);
        WriteBits('b', 8);
        WriteBits('c', 8);
        WriteBits(0, 4);            // terminator

        Assert.True(QrPayloadParser.TryParse(data, version: 1, out var payload, out var enc));
        Assert.Equal(QrTextEncoding.Utf8, enc);
        Assert.Equal(new byte[] { (byte)'a', (byte)'b', (byte)'c' }, payload);
    }

    [Fact]
    public void Parse_Alphanumeric() {
        // ALPHANUMERIC (0010) + count (3) + "A1-" + terminator
        var data = new byte[5];
        var bitPos = 0;

        void WriteBits(int value, int count) {
            for (var i = count - 1; i >= 0; i--) {
                var bit = (value >> i) & 1;
                var byteIndex = bitPos >> 3;
                var bitIndex = 7 - (bitPos & 7);
                if (bit != 0) data[byteIndex] |= (byte)(1 << bitIndex);
                bitPos++;
            }
        }

        // mode
        WriteBits(0b0010, 4);
        // count bits (version 1..9 => 9)
        WriteBits(3, 9);
        // "A1" (pair): 10*45 + 1 = 451 => 11 bits
        WriteBits(451, 11);
        // "-" (single): 41 => 6 bits
        WriteBits(41, 6);
        // terminator
        WriteBits(0, 4);

        Assert.True(QrPayloadParser.TryParse(data, version: 1, out var payload, out var enc));
        Assert.Equal(QrTextEncoding.Utf8, enc);
        Assert.Equal(new byte[] { (byte)'A', (byte)'1', (byte)'-' }, payload);
    }
}
