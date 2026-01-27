using System;

namespace CodeGlyphX.Rendering.Webp;

/// <summary>
/// LSB-first bit reader used by the VP8L lossless bitstream.
/// </summary>
internal ref struct WebpBitReader {
    private readonly ReadOnlySpan<byte> _data;
    private ulong _bitBuffer;
    private int _bitCount;
    private int _byteOffset;
    private int _bitsConsumed;

    public WebpBitReader(ReadOnlySpan<byte> data) {
        _data = data;
        _bitBuffer = 0;
        _bitCount = 0;
        _byteOffset = 0;
        _bitsConsumed = 0;
    }

    /// <summary>
    /// Gets the number of bits consumed so far.
    /// </summary>
    public int BitsConsumed => _bitsConsumed;

    /// <summary>
    /// Attempts to read up to 32 bits (LSB-first). Returns -1 on truncation.
    /// </summary>
    public int ReadBits(int count) {
        if ((uint)count > 32) throw new ArgumentOutOfRangeException(nameof(count));
        if (!EnsureBits(count)) return -1;
        if (count == 0) return 0;

        var mask = count == 32 ? uint.MaxValue : (uint)((1UL << count) - 1UL);
        var value = (int)(_bitBuffer & mask);
        _bitBuffer >>= count;
        _bitCount -= count;
        _bitsConsumed += count;
        return value;
    }

    private bool EnsureBits(int count) {
        while (_bitCount < count && _byteOffset < _data.Length) {
            _bitBuffer |= (ulong)_data[_byteOffset++] << _bitCount;
            _bitCount += 8;
        }
        return _bitCount >= count;
    }
}

