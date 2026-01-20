namespace CodeGlyphX.Pdf417;

/// <summary>
/// Macro PDF417 metadata payload.
/// </summary>
public sealed class Pdf417MacroMetadata {
    /// <summary>
    /// Gets the segment index.
    /// </summary>
    public int SegmentIndex { get; }

    /// <summary>
    /// Gets the file identifier.
    /// </summary>
    public string FileId { get; }

    /// <summary>
    /// Gets whether this segment is marked as the last segment.
    /// </summary>
    public bool IsLastSegment { get; }

    /// <summary>
    /// Gets the total number of segments, if present.
    /// </summary>
    public int? SegmentCount { get; }

    /// <summary>
    /// Gets the file name, if present.
    /// </summary>
    public string? FileName { get; }

    /// <summary>
    /// Gets the timestamp, if present.
    /// </summary>
    public long? Timestamp { get; }

    /// <summary>
    /// Gets the sender, if present.
    /// </summary>
    public string? Sender { get; }

    /// <summary>
    /// Gets the addressee, if present.
    /// </summary>
    public string? Addressee { get; }

    /// <summary>
    /// Gets the file size, if present.
    /// </summary>
    public long? FileSize { get; }

    /// <summary>
    /// Gets the checksum, if present.
    /// </summary>
    public int? Checksum { get; }

    internal Pdf417MacroMetadata(
        int segmentIndex,
        string fileId,
        bool isLastSegment,
        int? segmentCount,
        string? fileName,
        long? timestamp,
        string? sender,
        string? addressee,
        long? fileSize,
        int? checksum) {
        SegmentIndex = segmentIndex;
        FileId = fileId ?? string.Empty;
        IsLastSegment = isLastSegment;
        SegmentCount = segmentCount;
        FileName = fileName;
        Timestamp = timestamp;
        Sender = sender;
        Addressee = addressee;
        FileSize = fileSize;
        Checksum = checksum;
    }
}
