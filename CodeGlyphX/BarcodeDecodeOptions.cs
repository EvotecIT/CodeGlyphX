namespace CodeGlyphX;

/// <summary>
/// Options controlling how 1D barcode decoding behaves.
/// </summary>
public sealed partial class BarcodeDecodeOptions {
    /// <summary>
    /// Controls how Code39 checksum characters are handled during decode.
    /// </summary>
    public Code39ChecksumPolicy Code39Checksum { get; set; } = Code39ChecksumPolicy.None;
    /// <summary>
    /// Controls how MSI checksum digits are handled during decode.
    /// </summary>
    public MsiChecksumPolicy MsiChecksum { get; set; } = MsiChecksumPolicy.None;
    /// <summary>
    /// Controls how Code 11 checksum characters are handled during decode.
    /// </summary>
    public Code11ChecksumPolicy Code11Checksum { get; set; } = Code11ChecksumPolicy.None;
    /// <summary>
    /// Controls whether Plessey CRC validation is required during decode.
    /// </summary>
    public PlesseyChecksumPolicy PlesseyChecksum { get; set; } = PlesseyChecksumPolicy.RequireValid;

    /// <summary>
    /// Enables tile-based scanning for multiple barcodes in one image.
    /// </summary>
    public bool EnableTileScan { get; set; } = false;

    /// <summary>
    /// Tile grid size for multi-scan (0 = auto, 2..4 recommended).
    /// </summary>
    public int TileGrid { get; set; } = 0;
}

/// <summary>
/// Policy for handling optional Code39 checksum characters.
/// </summary>
public enum Code39ChecksumPolicy {
    /// <summary>
    /// Do not strip checksum characters (default, avoids accidental data loss).
    /// </summary>
    None,
    /// <summary>
    /// Strip the trailing character if it matches a valid checksum.
    /// </summary>
    StripIfValid,
    /// <summary>
    /// Require a valid checksum and strip it; otherwise decoding fails.
    /// </summary>
    RequireValid
}

/// <summary>
/// Policy for handling optional MSI checksum digits.
/// </summary>
public enum MsiChecksumPolicy {
    /// <summary>
    /// Do not strip checksum digits (default, avoids accidental data loss).
    /// </summary>
    None,
    /// <summary>
    /// Strip trailing checksum digit(s) when valid.
    /// </summary>
    StripIfValid,
    /// <summary>
    /// Require valid checksum digit(s); otherwise decoding fails.
    /// </summary>
    RequireValid
}

/// <summary>
/// Policy for handling optional Code 11 checksum characters.
/// </summary>
public enum Code11ChecksumPolicy {
    /// <summary>
    /// Do not strip checksum characters (default, avoids accidental data loss).
    /// </summary>
    None,
    /// <summary>
    /// Strip trailing checksum character(s) when valid.
    /// </summary>
    StripIfValid,
    /// <summary>
    /// Require valid checksum character(s); otherwise decoding fails.
    /// </summary>
    RequireValid
}

/// <summary>
/// Policy for handling Plessey CRC validation.
/// </summary>
public enum PlesseyChecksumPolicy {
    /// <summary>
    /// Do not validate CRC.
    /// </summary>
    None,
    /// <summary>
    /// Validate CRC and ignore mismatches.
    /// </summary>
    StripIfValid,
    /// <summary>
    /// Require valid CRC; otherwise decoding fails.
    /// </summary>
    RequireValid
}
