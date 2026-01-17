namespace CodeGlyphX.Aztec;

internal sealed class AztecDetectorResult {
    public BitMatrix Bits { get; }
    public bool Compact { get; }
    public int NbDataBlocks { get; }
    public int NbLayers { get; }

    public AztecDetectorResult(BitMatrix bits, bool compact, int nbDataBlocks, int nbLayers) {
        Bits = bits;
        Compact = compact;
        NbDataBlocks = nbDataBlocks;
        NbLayers = nbLayers;
    }
}
