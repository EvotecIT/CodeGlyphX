using System;

namespace CodeMatrix;

public sealed class BitMatrix {
    private readonly uint[] _words;

    public int Width { get; }
    public int Height { get; }

    public BitMatrix(int width, int height) {
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));

        Width = width;
        Height = height;

        var bitCount = checked(width * height);
        _words = new uint[(bitCount + 31) / 32];
    }

    private BitMatrix(int width, int height, uint[] words) {
        Width = width;
        Height = height;
        _words = words;
    }

    public bool this[int x, int y] {
        get => Get(x, y);
        set => Set(x, y, value);
    }

    public bool Get(int x, int y) {
        if ((uint)x >= (uint)Width) throw new ArgumentOutOfRangeException(nameof(x));
        if ((uint)y >= (uint)Height) throw new ArgumentOutOfRangeException(nameof(y));

        var bitIndex = (y * Width) + x;
        var wordIndex = bitIndex >> 5;
        var bitMask = 1u << (bitIndex & 31);
        return (_words[wordIndex] & bitMask) != 0;
    }

    public void Set(int x, int y, bool value) {
        if ((uint)x >= (uint)Width) throw new ArgumentOutOfRangeException(nameof(x));
        if ((uint)y >= (uint)Height) throw new ArgumentOutOfRangeException(nameof(y));

        var bitIndex = (y * Width) + x;
        var wordIndex = bitIndex >> 5;
        var bitMask = 1u << (bitIndex & 31);
        if (value) _words[wordIndex] |= bitMask;
        else _words[wordIndex] &= ~bitMask;
    }

    public void Clear() => Array.Clear(_words, 0, _words.Length);

    public BitMatrix Clone() => new(Width, Height, (uint[])_words.Clone());

    public byte[] ToPackedBytes() {
        var totalBits = Width * Height;
        var byteCount = (totalBits + 7) / 8;
        var bytes = new byte[byteCount];

        for (var i = 0; i < totalBits; i++) {
            var wordIndex = i >> 5;
            var bitMask = 1u << (i & 31);
            var isSet = (_words[wordIndex] & bitMask) != 0;
            if (!isSet) continue;

            // Pack row-major, left-to-right, top-to-bottom. Bit 7 is earliest.
            var byteIndex = i >> 3;
            var bitInByte = 7 - (i & 7);
            bytes[byteIndex] |= (byte)(1 << bitInByte);
        }

        return bytes;
    }
}

