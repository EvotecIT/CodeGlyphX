using CodeGlyphX;
using CodeGlyphX.Qr;
using Xunit;

namespace CodeGlyphX.Tests;

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

        Assert.True(QrPayloadParser.TryParse(data, version: 1, out var payload, out var segments, out var structuredAppend, out var fnc1Mode));
        Assert.Equal(new byte[] { (byte)'a', (byte)'b', (byte)'c' }, payload);
        Assert.Single(segments);
        Assert.Equal(QrTextEncoding.Utf8, segments[0].Encoding);
        Assert.True(segments[0].Span.SequenceEqual(new byte[] { (byte)'a', (byte)'b', (byte)'c' }));
        Assert.False(structuredAppend.HasValue);
        Assert.Equal(QrFnc1Mode.None, fnc1Mode);
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

        Assert.True(QrPayloadParser.TryParse(data, version: 1, out var payload, out var segments, out var structuredAppend, out var fnc1Mode));
        Assert.Equal(new byte[] { (byte)'A', (byte)'1', (byte)'-' }, payload);
        Assert.Single(segments);
        Assert.Equal(QrTextEncoding.Latin1, segments[0].Encoding);
        Assert.True(segments[0].Span.SequenceEqual(new byte[] { (byte)'A', (byte)'1', (byte)'-' }));
        Assert.False(structuredAppend.HasValue);
        Assert.Equal(QrFnc1Mode.None, fnc1Mode);
    }

    [Fact]
    public void Parse_Fnc1_Alphanumeric_Gs1Escapes() {
        // FNC1 (0101) + ALPHANUMERIC (0010) + count (8) + "AB%CD%%E" + terminator
        var data = new byte[10];
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

        int AlnumIndex(char c) {
            const string table = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ $%*+-./:";
            return table.IndexOf(c);
        }

        WriteBits(0b0101, 4); // FNC1 first position
        WriteBits(0b0010, 4); // ALPHANUMERIC
        WriteBits(8, 9);      // count

        var text = "AB%CD%%E";
        var iChar = 0;
        while (iChar + 1 < text.Length) {
            var v = AlnumIndex(text[iChar]) * 45 + AlnumIndex(text[iChar + 1]);
            WriteBits(v, 11);
            iChar += 2;
        }
        if (iChar < text.Length) {
            WriteBits(AlnumIndex(text[iChar]), 6);
        }

        WriteBits(0, 4); // terminator

        Assert.True(QrPayloadParser.TryParse(data, version: 1, out var payload, out var segments, out var structuredAppend, out var fnc1Mode));
        Assert.Equal(QrFnc1Mode.FirstPosition, fnc1Mode);
        Assert.Single(segments);
        Assert.False(structuredAppend.HasValue);
        Assert.Equal(new byte[] { (byte)'A', (byte)'B', 0x1D, (byte)'C', (byte)'D', (byte)'%', (byte)'E' }, payload);
    }

    [Fact]
    public void Parse_DefaultLatin1_ByteMode_NoEci() {
        // BYTE (0100) + count (2) + 0x80 0xFF + terminator
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

        WriteBits(0b0100, 4);       // BYTE mode
        WriteBits(2, 8);            // count (version 1..9 => 8 bits)
        WriteBits(0x80, 8);
        WriteBits(0xFF, 8);
        WriteBits(0, 4);            // terminator

        Assert.True(QrPayloadParser.TryParse(data, version: 1, out var payload, out var segments, out var structuredAppend, out var fnc1Mode));
        Assert.Equal(new byte[] { 0x80, 0xFF }, payload);
        Assert.Single(segments);
        Assert.Equal(QrTextEncoding.Latin1, segments[0].Encoding);
        Assert.True(segments[0].Span.SequenceEqual(new byte[] { 0x80, 0xFF }));
        Assert.False(structuredAppend.HasValue);
        Assert.Equal(QrFnc1Mode.None, fnc1Mode);
    }

    [Fact]
    public void Parse_EciSwitch_Utf8_To_Latin1() {
        // ECI(UTF-8) + BYTE(count=2, 0xC2 0xA2) + ECI(Latin1) + BYTE(count=1, 0xA3) + terminator
        var data = new byte[9];
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
        WriteBits(0b00011010, 8);   // assignment 26 (UTF-8)
        WriteBits(0b0100, 4);       // BYTE mode
        WriteBits(2, 8);            // count
        WriteBits(0xC2, 8);
        WriteBits(0xA2, 8);
        WriteBits(0b0111, 4);       // ECI
        WriteBits(0b00000011, 8);   // assignment 3 (Latin1)
        WriteBits(0b0100, 4);       // BYTE mode
        WriteBits(1, 8);            // count
        WriteBits(0xA3, 8);
        WriteBits(0, 4);            // terminator

        Assert.True(QrPayloadParser.TryParse(data, version: 1, out var payload, out var segments, out var structuredAppend, out var fnc1Mode));
        Assert.Equal(new byte[] { 0xC2, 0xA2, 0xA3 }, payload);
        Assert.Equal(2, segments.Length);
        Assert.Equal(QrTextEncoding.Utf8, segments[0].Encoding);
        Assert.True(segments[0].Span.SequenceEqual(new byte[] { 0xC2, 0xA2 }));
        Assert.Equal(QrTextEncoding.Latin1, segments[1].Encoding);
        Assert.True(segments[1].Span.SequenceEqual(new byte[] { 0xA3 }));
        Assert.False(structuredAppend.HasValue);
        Assert.Equal(QrFnc1Mode.None, fnc1Mode);

        var text = QrDecoder.DecodeSegments(segments);
        Assert.Equal("\u00A2\u00A3", text);
    }

    [Fact]
    public void Parse_StructuredAppend_Metadata() {
        // STRUCTURED APPEND (0011) + seq(0x24 = index 2, total 4) + parity(0xAA)
        // + BYTE mode ("ok") + terminator
        var data = new byte[6];
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

        WriteBits(0b0011, 4); // structured append
        WriteBits(0x24, 8);   // index 2, total 4
        WriteBits(0xAA, 8);   // parity
        WriteBits(0b0100, 4); // BYTE mode
        WriteBits(2, 8);      // count
        WriteBits('o', 8);
        WriteBits('k', 8);
        WriteBits(0, 4);      // terminator

        Assert.True(QrPayloadParser.TryParse(data, version: 1, out var payload, out var segments, out var structuredAppend, out var fnc1Mode));
        Assert.Equal(new byte[] { (byte)'o', (byte)'k' }, payload);
        Assert.Single(segments);
        Assert.True(structuredAppend.HasValue);
        Assert.Equal(2, structuredAppend!.Value.Index);
        Assert.Equal(4, structuredAppend.Value.Total);
        Assert.Equal(0xAA, structuredAppend.Value.Parity);
        Assert.Equal(QrFnc1Mode.None, fnc1Mode);
    }
}
