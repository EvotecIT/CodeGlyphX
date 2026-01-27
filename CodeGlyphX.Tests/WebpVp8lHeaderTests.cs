using System;
using System.Collections.Generic;
using CodeGlyphX.Rendering.Webp;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class WebpVp8lHeaderTests {
    [Fact]
    public void Vp8l_Header_SubtractGreenOnly_Parses() {
        var payload = BuildVp8lPayload(width: 5, height: 3, transformType: 2);

        Assert.True(WebpVp8lDecoder.TryReadHeader(payload, out var header));
        Assert.Equal(5, header.Width);
        Assert.Equal(3, header.Height);
        Assert.Equal(40, header.BitsConsumed);
    }

    [Fact]
    public void Vp8l_Header_PredictorTransform_DoesNotBreakHeaderParse() {
        var payload = BuildVp8lPayload(width: 5, height: 3, transformType: 0);

        Assert.True(WebpVp8lDecoder.TryReadHeader(payload, out var header));
        Assert.Equal(5, header.Width);
        Assert.Equal(3, header.Height);
    }

    private static byte[] BuildVp8lPayload(int width, int height, int transformType) {
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));

        var writer = new BitWriterLsb();
        writer.WriteBits(0x2F, 8);
        writer.WriteBits(width - 1, 14);
        writer.WriteBits(height - 1, 14);
        writer.WriteBits(0, 1); // alpha hint
        writer.WriteBits(0, 3); // version

        writer.WriteBits(1, 1); // has transform
        writer.WriteBits(transformType, 2);
        writer.WriteBits(0, 1); // no more transforms

        writer.WriteBits(0, 1); // no color cache
        writer.WriteBits(0, 1); // no meta prefix codes

        return writer.ToArray();
    }

    private sealed class BitWriterLsb {
        private readonly List<byte> _bytes = new();
        private int _bitPos;

        public void WriteBits(int value, int count) {
            for (var i = 0; i < count; i++) {
                var bit = (value >> i) & 1;
                var byteIndex = _bitPos >> 3;
                var bitIndex = _bitPos & 7;
                if (byteIndex >= _bytes.Count) _bytes.Add(0);
                if (bit != 0) _bytes[byteIndex] = (byte)(_bytes[byteIndex] | (1 << bitIndex));
                _bitPos++;
            }
        }

        public byte[] ToArray() => _bytes.ToArray();
    }
}
