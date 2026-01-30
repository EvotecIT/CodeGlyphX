using System;
using System.Collections.Generic;
using CodeGlyphX.Rendering.Webp;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("WebpTests")]

public sealed class WebpPrefixCodeTests {
    [Fact]
    public void PrefixCode_SimpleSingleSymbol_DecodesWithoutExtraBits() {
        var writer = new BitWriterLsb();
        writer.WriteBits(1, 1);  // simple code
        writer.WriteBits(0, 1);  // one symbol
        writer.WriteBits(1, 1);  // first symbol uses 8 bits
        writer.WriteBits(42, 8); // symbol0

        var data = writer.ToArray();
        var reader = new WebpBitReader(data);

        Assert.True(WebpPrefixCodeReader.TryReadPrefixCode(ref reader, alphabetSize: 256, out var code));
        var symbol = code.DecodeSymbol(ref reader);
        Assert.Equal(42, symbol);
    }

    [Fact]
    public void PrefixCode_SimpleTwoSymbols_DecodesExpectedSymbol() {
        var writer = new BitWriterLsb();
        writer.WriteBits(1, 1); // simple code
        writer.WriteBits(1, 1); // two symbols
        writer.WriteBits(1, 1); // first symbol uses 8 bits
        writer.WriteBits(5, 8);
        writer.WriteBits(7, 8);

        // Canonical codes for two 1-bit symbols: lowest symbol index gets 0,
        // next gets 1. We want to decode symbol 7, so append bit 1.
        writer.WriteBits(1, 1);

        var data = writer.ToArray();
        var reader = new WebpBitReader(data);

        Assert.True(WebpPrefixCodeReader.TryReadPrefixCode(ref reader, alphabetSize: 256, out var code));
        var symbol = code.DecodeSymbol(ref reader);
        Assert.Equal(7, symbol);
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

