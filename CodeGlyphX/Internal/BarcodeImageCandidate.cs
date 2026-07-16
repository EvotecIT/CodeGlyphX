namespace CodeGlyphX.Internal;

internal sealed class BarcodeImageCandidate {
    internal BarcodeDecoded Decoded { get; }
    internal BarcodeScanlineCandidate Scanline { get; }
    internal ImageRegion SearchRegion { get; }

    internal BarcodeImageCandidate(BarcodeDecoded decoded, BarcodeScanlineCandidate scanline, ImageRegion searchRegion) {
        Decoded = decoded;
        Scanline = scanline;
        SearchRegion = searchRegion;
    }
}
