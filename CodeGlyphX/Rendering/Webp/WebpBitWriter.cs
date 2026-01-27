using System;
using System.Collections.Generic;

namespace CodeGlyphX.Rendering.Webp;

/// <summary>
/// LSB-first bit writer used by the managed VP8L encoder scaffold.
/// </summary>
internal sealed class WebpBitWriter {
    private readonly List<byte> _bytes = new();
    private int _bitPos;

    /// <summary>
    /// Gets the number of bits written so far.
    /// </summary>
    public int BitLength => _bitPos;

    /// <summary>
    /// Writes <paramref name="count"/> bits from <paramref name="value"/> LSB-first.
    /// </summary>
    public void WriteBits(int value, int count) {
        if (count < 0 || count > 31) throw new ArgumentOutOfRangeException(nameof(count));
        for (var i = 0; i < count; i++) {
            var bit = (value >> i) & 1;
            var byteIndex = _bitPos >> 3;
            var bitIndex = _bitPos & 7;
            if (byteIndex >= _bytes.Count) _bytes.Add(0);
            if (bit != 0) {
                _bytes[byteIndex] = (byte)(_bytes[byteIndex] | (1 << bitIndex));
            }
            _bitPos++;
        }
    }

    /// <summary>
    /// Appends the exact bit sequence from another writer.
    /// </summary>
    public void Append(WebpBitWriter other) {
        if (other is null) throw new ArgumentNullException(nameof(other));
        var bits = other._bitPos;
        for (var i = 0; i < bits; i++) {
            var byteIndex = i >> 3;
            var bitIndex = i & 7;
            var bit = (other._bytes[byteIndex] >> bitIndex) & 1;
            WriteBits(bit, 1);
        }
    }

    /// <summary>
    /// Returns the written bytes.
    /// </summary>
    public byte[] ToArray() => _bytes.ToArray();
}
