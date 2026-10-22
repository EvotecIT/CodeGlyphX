namespace CodeGlyphX.Aztec;

internal static partial class AztecEncoder {
    internal static BitMatrix EncodeRune(int value) {
        var bits = new AztecBitBuffer();
        bits.AppendBits(value, 8);
        var message = GenerateCheckWords(bits, 28, 4);
        var inverted = new AztecBitBuffer();
        // The Rune mode word inverts alternating bits, distinguishing it from
        // a compact Aztec layer/data-size message at the same bullseye.
        for (var i = 0; i < 28; i++) inverted.AppendBits(message.Get(i) ^ ((i & 1) == 0) ? 1 : 0, 1);
        var matrix = new BitMatrix(11, 11);
        DrawBullsEye(matrix, 5, 5);
        DrawModeMessage(matrix, true, 11, inverted);
        return matrix;
    }
}
