namespace CodeGlyphX;

/// <summary>
/// Options controlling how 1D barcode decoding behaves.
/// </summary>
public sealed class BarcodeDecodeOptions {
    /// <summary>
    /// Controls how Code39 checksum characters are handled during decode.
    /// </summary>
    public Code39ChecksumPolicy Code39Checksum { get; set; } = Code39ChecksumPolicy.None;
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
