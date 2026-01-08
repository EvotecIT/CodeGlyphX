using System;
using System.Collections.Generic;

namespace CodeGlyphX.Qr;

internal sealed class QrBitBuffer {
    private readonly List<byte> _bytes = new();

    public int LengthBits { get; private set; }

    public void AppendBits(int value, int bitCount) {
        if (bitCount is < 0 or > 31) throw new ArgumentOutOfRangeException(nameof(bitCount));
        if ((value >> bitCount) != 0) throw new ArgumentOutOfRangeException(nameof(value));

        for (var i = bitCount - 1; i >= 0; i--) AppendBit(((value >> i) & 1) != 0);
    }

    public void AppendBit(bool bit) {
        var byteIndex = LengthBits >> 3;
        if (byteIndex == _bytes.Count) _bytes.Add(0);

        if (bit) {
            var bitIndexInByte = 7 - (LengthBits & 7);
            _bytes[byteIndex] |= (byte)(1 << bitIndexInByte);
        }

        LengthBits++;
    }

    public byte[] ToByteArray() {
        if ((LengthBits & 7) != 0) throw new InvalidOperationException("Buffer is not byte-aligned.");
        return _bytes.ToArray();
    }
}

