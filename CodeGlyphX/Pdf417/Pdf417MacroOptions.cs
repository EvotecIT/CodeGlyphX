namespace CodeGlyphX.Pdf417;

/// <summary>
/// Options for Macro PDF417 encoding.
/// </summary>
public sealed class Pdf417MacroOptions {
    /// <summary>
    /// Segment index (0..99998), encoded as five decimal digits in two codewords.
    /// </summary>
    public int SegmentIndex { get; set; }

    /// <summary>
    /// File identifier as a numeric string (length must be a multiple of 3).
    /// </summary>
    public string FileId { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether this segment is the last segment. When <see cref="SegmentCount"/> is set,
    /// this must be true exactly when <see cref="SegmentIndex"/> equals the count minus one.
    /// Index 99998 must be marked as last even when the count is omitted.
    /// </summary>
    public bool IsLastSegment { get; set; }

    /// <summary>
    /// Optional total segment count (1..99999), greater than <see cref="SegmentIndex"/>.
    /// When used, set it to the same value on every segment of the file.
    /// </summary>
    public int? SegmentCount { get; set; }

    /// <summary>
    /// Optional file name. When repeated across segments, use the same value for the file.
    /// </summary>
    public string? FileName { get; set; }

    /// <summary>
    /// Optional timestamp. When repeated across segments, use the same value for the file.
    /// </summary>
    public long? Timestamp { get; set; }

    /// <summary>
    /// Optional sender. When repeated across segments, use the same value for the file.
    /// </summary>
    public string? Sender { get; set; }

    /// <summary>
    /// Optional addressee. When repeated across segments, use the same value for the file.
    /// </summary>
    public string? Addressee { get; set; }

    /// <summary>
    /// Optional file size. When repeated across segments, use the same value for the file.
    /// </summary>
    public long? FileSize { get; set; }

    /// <summary>
    /// Optional checksum. When repeated across segments, use the same value for the file.
    /// </summary>
    public int? Checksum { get; set; }
}
