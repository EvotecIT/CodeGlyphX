namespace CodeGlyphX.Pdf417;

public static partial class Pdf417Decoder {
    private static bool HasInitialOffsetCodewords(ModuleGrid modules, int startOffset) {
        // Every candidate width has the same left indicator and first data character.
        // The module reader requires both exact codewords before error correction;
        // reject an impossible prefix once before allocating each candidate crop.
        // Earlier encoders emitted rows bottom-up, so preserve both entry rows.
        if (modules.Height == 0) return false;
        return ReadInitial(modules, startOffset, 0) || ReadInitial(modules, startOffset, modules.Height - 1);
    }

    private static bool ReadInitial(ModuleGrid modules, int startOffset, int row) =>
            TryReadCodeword(modules, row, startOffset + StartPatternWidth, 17, 0, out _) &&
            TryReadCodeword(modules, row, startOffset + StartPatternWidth + 17, 17, 0, out _);
}
