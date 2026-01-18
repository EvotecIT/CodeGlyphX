using System;

namespace CodeGlyphX.Aztec;

internal sealed class AztecSimpleToken : AztecToken {
    private readonly int _value;
    private readonly int _bitCount;

    public AztecSimpleToken(AztecToken? previous, int value, int bitCount) : base(previous) {
        _value = value;
        _bitCount = bitCount;
    }

    public override void AppendTo(AztecBitBuffer buffer, byte[] text) {
        buffer.AppendBits(_value, _bitCount);
    }
}
