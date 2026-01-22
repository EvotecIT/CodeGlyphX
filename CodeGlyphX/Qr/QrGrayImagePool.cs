using System;
using System.Buffers;

namespace CodeGlyphX.Qr;

internal sealed class QrGrayImagePool {
    private byte[][] _buffers;
    private int _count;

    public QrGrayImagePool(int initialCapacity = 16) {
        if (initialCapacity < 1) initialCapacity = 1;
        _buffers = new byte[initialCapacity][];
    }

    public byte[] Rent(int size) {
        var buffer = ArrayPool<byte>.Shared.Rent(size);
        if (_count == _buffers.Length) {
            Array.Resize(ref _buffers, _buffers.Length * 2);
        }
        _buffers[_count++] = buffer;
        return buffer;
    }

    public void ReturnAll() {
        for (var i = 0; i < _count; i++) {
            var buffer = _buffers[i];
            if (buffer is not null) {
                ArrayPool<byte>.Shared.Return(buffer, clearArray: false);
                _buffers[i] = Array.Empty<byte>();
            }
        }
        _count = 0;
    }
}
