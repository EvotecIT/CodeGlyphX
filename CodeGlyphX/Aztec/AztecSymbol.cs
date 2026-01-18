namespace CodeGlyphX.Aztec;

internal sealed class AztecSymbol {
    public bool Compact { get; }
    public int Size { get; }
    public int Layers { get; }
    public int CodeWords { get; }
    public BitMatrix Matrix { get; }

    public AztecSymbol(bool compact, int size, int layers, int codeWords, BitMatrix matrix) {
        Compact = compact;
        Size = size;
        Layers = layers;
        CodeWords = codeWords;
        Matrix = matrix;
    }
}
