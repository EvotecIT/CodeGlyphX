namespace CodeGlyphX;

public sealed partial class BarcodeDecodeOptions {
    /// <summary>Selects Codabar optional Mod16 check handling.</summary>
    public BarcodeDecodeOptions WithCodabarChecksum(OptionalChecksumPolicy policy) { CodabarChecksum = policy; return this; }
    /// <summary>Selects optional Mod10 check handling for ITF and discrete 2 of 5.</summary>
    public BarcodeDecodeOptions WithTwoOfFiveChecksum(OptionalChecksumPolicy policy) { TwoOfFiveChecksum = policy; return this; }
    /// <summary>
    /// Sets Code39 checksum handling.
    /// </summary>
    public BarcodeDecodeOptions WithCode39Checksum(Code39ChecksumPolicy policy) {
        Code39Checksum = policy;
        return this;
    }

    /// <summary>
    /// Sets MSI checksum handling.
    /// </summary>
    public BarcodeDecodeOptions WithMsiChecksum(MsiChecksumPolicy policy) {
        MsiChecksum = policy;
        return this;
    }

    /// <summary>Selects an MSI algorithm and optional known data length without guessing between check conventions.</summary>
    public BarcodeDecodeOptions WithMsiChecksum(MsiChecksumPolicy policy, MsiChecksumType algorithm, int? expectedDataLength = null) {
        MsiChecksum = policy; MsiChecksumAlgorithm = algorithm; MsiExpectedDataLength = expectedDataLength;
        return this;
    }

    /// <summary>
    /// Sets Code 11 checksum handling.
    /// </summary>
    public BarcodeDecodeOptions WithCode11Checksum(Code11ChecksumPolicy policy) {
        Code11Checksum = policy;
        return this;
    }

    /// <summary>
    /// Sets Plessey CRC handling.
    /// </summary>
    public BarcodeDecodeOptions WithPlesseyChecksum(PlesseyChecksumPolicy policy) {
        PlesseyChecksum = policy;
        return this;
    }

    /// <summary>
    /// Enables or disables tile scanning for multiple barcodes.
    /// </summary>
    public BarcodeDecodeOptions WithTileScan(bool enabled = true, int tileGrid = 0) {
        EnableTileScan = enabled;
        TileGrid = tileGrid;
        return this;
    }
}
