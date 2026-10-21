namespace CodeGlyphX;

/// <summary>
/// Options controlling how 1D barcode decoding behaves.
/// </summary>
public sealed partial class BarcodeDecodeOptions {
    /// <summary>Gets or sets explicit Mod16 validation for Codabar, including its start and stop characters.</summary>
    public OptionalChecksumPolicy CodabarChecksum { get; set; }
    /// <summary>Gets or sets explicit Mod10 validation for ITF and discrete 2 of 5. A padded leading zero is retained as data.</summary>
    public OptionalChecksumPolicy TwoOfFiveChecksum { get; set; }
    /// <summary>Gets or sets the convention for framed, checked BC412 symbols. Raw IBM modules use the specialist decoder.</summary>
    public Bc412.Bc412Variant Bc412Variant { get; set; } = Bc412.Bc412Variant.Semi;
    /// <summary>Interprets Code 39 shift pairs as Full ASCII (default). Set false for literal Code 39 application payloads such as HIBC.</summary>
    public bool Code39FullAscii { get; set; } = true;
    /// <summary>
    /// Controls how Code39 checksum characters are handled during decode.
    /// </summary>
    public Code39ChecksumPolicy Code39Checksum { get; set; } = Code39ChecksumPolicy.None;
    /// <summary>
    /// Controls how MSI checksum digits are handled during decode.
    /// </summary>
    public MsiChecksumPolicy MsiChecksum { get; set; } = MsiChecksumPolicy.None;
    /// <summary>Gets or sets an explicit MSI check algorithm. Null preserves the legacy Mod10/Mod10Mod10 interpretation.</summary>
    /// <remarks>Set MsiChecksum to RequireValid or StripIfValid to apply this selection. Mod11 check value ten can make
    /// the data length ambiguous; supply MsiExpectedDataLength when the application knows it.</remarks>
    public MsiChecksumType? MsiChecksumAlgorithm { get; set; }
    /// <summary>Gets or sets the known MSI data length before check digits. Requires an explicit check algorithm.</summary>
    public int? MsiExpectedDataLength { get; set; }
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

/// <summary>Explicit interpretation of a check that cannot be distinguished automatically from data.</summary>
public enum OptionalChecksumPolicy {
    /// <summary>Retain every data character without optional-check validation.</summary>
    None,
    /// <summary>Strip a valid check, retaining the input if validation fails.</summary>
    StripIfValid,
    /// <summary>Require and strip a valid check, otherwise fail decoding.</summary>
    RequireValid
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
