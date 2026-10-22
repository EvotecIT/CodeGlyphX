using CodeGlyphX.Internal;
using CodeGlyphX.Internal.ReedSolomon;

namespace CodeGlyphX.Aztec;

/// <summary>Reads the fixed Rune finder and its two data plus five parity words.</summary>
internal static class AztecRuneCodec {
    internal static bool TryDecode(BitMatrixView grid, out int value) {
        value = 0;
        if (grid.Width != 11 || grid.Height != 11) return false;
        for (var y = 0; y < 11; y++) {
            for (var x = 0; x < 11; x++) {
                if (IsMessageCell(x, y)) continue;
                var radius = System.Math.Max(System.Math.Abs(x - 5), System.Math.Abs(y - 5));
                var expected = radius < 5 ? (radius & 1) == 0
                    : x == 0 && y <= 1 || y == 0 && (x == 1 || x == 10) || x == 10 && (y == 1 || y == 9);
                if (grid.Get(x, y) != expected) return false;
            }
        }
        var bits = new bool[28];
        for (var i = 0; i < 7; i++) {
            bits[i] = grid.Get(2 + i, 0);
            bits[i + 7] = grid.Get(10, 2 + i);
            bits[20 - i] = grid.Get(2 + i, 10);
            bits[27 - i] = grid.Get(0, 2 + i);
        }
        var words = new int[7];
        for (var i = 0; i < bits.Length; i++) {
            var bit = bits[i] ^ ((i & 1) == 0);
            words[i / 4] = (words[i / 4] << 1) | (bit ? 1 : 0);
        }
        try {
            new ReedSolomonDecoder(GenericGf.AztecParam).Decode(words, 5);
        } catch (ReedSolomonException) {
            return false;
        }
        value = (words[0] << 4) | words[1];
        return true;
    }

    private static bool IsMessageCell(int x, int y) =>
        ((y == 0 || y == 10) && x >= 2 && x <= 8) ||
        ((x == 0 || x == 10) && y >= 2 && y <= 8);
}
