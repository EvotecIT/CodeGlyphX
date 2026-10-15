namespace CodeGlyphX.Internal;

// Decoder candidates are already bit-packed. Transform their bits directly,
// without repeating public coordinate and mutability checks for every cell.
internal static class BitMatrixTransforms {
    internal static BitMatrix Rotate90(BitMatrix source) =>
        Transform(source, source.Height, source.Width, source.Height - 1, -1, source.Height);

    internal static BitMatrix Rotate180(BitMatrix source) =>
        Transform(source, source.Width, source.Height, source.Width * source.Height - 1, -source.Width, -1);

    internal static BitMatrix Rotate270(BitMatrix source) =>
        Transform(source, source.Height, source.Width, (source.Width - 1) * source.Height, 1, -source.Height);

    internal static BitMatrix MirrorHorizontal(BitMatrix source) =>
        Transform(source, source.Width, source.Height, source.Width - 1, source.Width, -1);

    private static BitMatrix Transform(BitMatrix source, int width, int height,
        int first, int rowStep, int cellStep) {
        var result = new BitMatrix(width, height);
        var input = source.Words;
        var output = result.Words;
        var index = 0;
        for (var y = 0; y < source.Height; y++, first += rowStep) {
            var target = first;
            for (var x = 0; x < source.Width; x++, index++, target += cellStep) {
                if ((input[index >> 5] & (1u << (index & 31))) != 0)
                    output[target >> 5] |= 1u << (target & 31);
            }
        }
        return result;
    }
}
