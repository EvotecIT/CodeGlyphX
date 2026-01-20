namespace CodeGlyphX;

public sealed partial class BarcodeDecodeOptions {
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
}
