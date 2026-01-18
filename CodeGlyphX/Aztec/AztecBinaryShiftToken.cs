using System;

namespace CodeGlyphX.Aztec;

internal sealed class AztecBinaryShiftToken : AztecToken {
    private readonly int _start;
    private readonly int _byteCount;

    public AztecBinaryShiftToken(AztecToken? previous, int start, int byteCount) : base(previous) {
        _start = start;
        _byteCount = byteCount;
    }

    public override void AppendTo(AztecBitBuffer buffer, byte[] text) {
        for (var i = 0; i < _byteCount; i++) {
            if (i == 0 || (i == 31 && _byteCount <= 62)) {
                buffer.AppendBits(31, 5); // Binary shift latch
                if (_byteCount > 62) {
                    buffer.AppendBits(_byteCount - 31, 16);
                } else if (i == 0) {
                    buffer.AppendBits(Math.Min(_byteCount, 31), 5);
                } else {
                    buffer.AppendBits(_byteCount - 31, 5);
                }
            }
            buffer.AppendBits(text[_start + i], 8);
        }
    }
}
