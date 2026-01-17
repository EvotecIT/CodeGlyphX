using System;

namespace CodeGlyphX.Aztec;

internal abstract class AztecToken {
    public static AztecToken Empty { get; } = new AztecSimpleToken(null, 0, 0);

    public AztecToken? Previous { get; }

    protected AztecToken(AztecToken? previous) {
        Previous = previous;
    }

    public AztecToken Add(int value, int bitCount) => new AztecSimpleToken(this, value, bitCount);

    public AztecToken AddBinaryShift(int start, int byteCount) => new AztecBinaryShiftToken(this, start, byteCount);

    public abstract void AppendTo(AztecBitBuffer buffer, byte[] text);
}
