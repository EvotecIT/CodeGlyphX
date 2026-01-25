using System;
using System.Collections.Generic;

namespace CodeGlyphX.Qr;

internal sealed class QrBitBuffer {
    private readonly List<byte> _bytes;

    public int LengthBits { get; private set; }

    public QrBitBuffer(int capacityBytes = 0) {
        _bytes = capacityBytes > 0 ? new List<byte>(capacityBytes) : new List<byte>();
    }

    public void AppendBits(int value, int bitCount) {
        if (bitCount is < 0 or > 31) throw new ArgumentOutOfRangeException(nameof(bitCount));
        if ((value >> bitCount) != 0) throw new ArgumentOutOfRangeException(nameof(value));

        if (bitCount == 0) return;

        while (bitCount > 0) {
            var byteIndex = LengthBits >> 3;
            var bitOffset = LengthBits & 7;
            if (byteIndex == _bytes.Count) _bytes.Add(0);
            var space = 8 - bitOffset;
            var take = bitCount < space ? bitCount : space;
            var shift = bitCount - take;
            var mask = (1 << take) - 1;
            var chunk = (value >> shift) & mask;
            var shiftInto = space - take;
            _bytes[byteIndex] |= (byte)(chunk << shiftInto);
            LengthBits += take;
            bitCount -= take;
        }
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
