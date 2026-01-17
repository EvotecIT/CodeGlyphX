using System;
using System.Collections.Generic;

namespace CodeGlyphX.Aztec;

internal sealed class AztecBitBuffer {
    private readonly List<byte> _bytes = new();

    public int Size { get; private set; }

    public bool Get(int index) {
        if ((uint)index >= (uint)Size) throw new ArgumentOutOfRangeException(nameof(index));
        var byteIndex = index >> 3;
        var bitIndex = 7 - (index & 7);
        return ((_bytes[byteIndex] >> bitIndex) & 1) != 0;
    }

    public void AppendBit(bool bit) {
        var byteIndex = Size >> 3;
        if (byteIndex == _bytes.Count) _bytes.Add(0);

        if (bit) {
            var bitIndex = 7 - (Size & 7);
            _bytes[byteIndex] |= (byte)(1 << bitIndex);
        }

        Size++;
    }

    public void AppendBits(int value, int bitCount) {
        if (bitCount is < 0 or > 31) throw new ArgumentOutOfRangeException(nameof(bitCount));
        if ((value >> bitCount) != 0) throw new ArgumentOutOfRangeException(nameof(value));

        for (var i = bitCount - 1; i >= 0; i--) {
            AppendBit(((value >> i) & 1) != 0);
        }
    }

    public void Append(AztecBitBuffer other) {
        if (other is null) throw new ArgumentNullException(nameof(other));
        for (var i = 0; i < other.Size; i++) AppendBit(other.Get(i));
    }

    public byte[] ToByteArray() {
        if ((Size & 7) != 0) throw new InvalidOperationException("Buffer is not byte-aligned.");
        return _bytes.ToArray();
    }
}
