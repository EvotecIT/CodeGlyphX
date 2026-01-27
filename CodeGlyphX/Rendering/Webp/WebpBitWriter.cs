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
    /// Returns the written bytes.
    /// </summary>
    public byte[] ToArray() => _bytes.ToArray();
}

