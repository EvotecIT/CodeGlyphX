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
    /// Optional total segment count.
    /// </summary>
    public int? SegmentCount { get; set; }

    /// <summary>
    /// Optional file name.
    /// </summary>
    public string? FileName { get; set; }

    /// <summary>
    /// Optional timestamp.
    /// </summary>
    public long? Timestamp { get; set; }

    /// <summary>
    /// Optional sender.
    /// </summary>
    public string? Sender { get; set; }

    /// <summary>
    /// Optional addressee.
    /// </summary>
    public string? Addressee { get; set; }

    /// <summary>
    /// Optional file size.
    /// </summary>
    public long? FileSize { get; set; }

    /// <summary>
    /// Optional checksum.
    /// </summary>
    public int? Checksum { get; set; }
}
