namespace CodeGlyphX.Pdf417;

/// <summary>
/// Options for Macro PDF417 encoding.
/// </summary>
public sealed class Pdf417MacroOptions {
    /// <summary>
    /// Segment index (0..809999).
    /// </summary>
    public int SegmentIndex { get; set; }

    /// <summary>
    /// File identifier as a numeric string (length must be a multiple of 3).
    /// </summary>
    public string FileId { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether this segment is the last segment.
    /// </summary>
    public bool IsLastSegment { get; set; }

    /// <summary>
    /// Optional total segment count (last segment only).
    /// </summary>
    public int? SegmentCount { get; set; }

    /// <summary>
    /// Optional file name (last segment only).
    /// </summary>
    public string? FileName { get; set; }

    /// <summary>
    /// Optional timestamp (last segment only).
    /// </summary>
    public long? Timestamp { get; set; }

    /// <summary>
    /// Optional sender (last segment only).
    /// </summary>
    public string? Sender { get; set; }

    /// <summary>
    /// Optional addressee (last segment only).
    /// </summary>
    public string? Addressee { get; set; }

    /// <summary>
    /// Optional file size (last segment only).
    /// </summary>
    public long? FileSize { get; set; }

    /// <summary>
    /// Optional checksum (last segment only).
    /// </summary>
    public int? Checksum { get; set; }
}
