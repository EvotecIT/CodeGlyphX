namespace CodeMatrix.Pdf417;

/// <summary>
/// Options for PDF417 encoding.
/// </summary>
public sealed class Pdf417EncodeOptions {
    /// <summary>Minimum number of data columns (1..30).</summary>
    public int MinColumns { get; set; } = 1;
    /// <summary>Maximum number of data columns (1..30).</summary>
    public int MaxColumns { get; set; } = 30;
    /// <summary>Minimum number of rows (3..90).</summary>
    public int MinRows { get; set; } = 3;
    /// <summary>Maximum number of rows (3..90).</summary>
    public int MaxRows { get; set; } = 90;
    /// <summary>Error correction level (0..8). Use -1 for auto.</summary>
    public int ErrorCorrectionLevel { get; set; } = -1;
    /// <summary>Target aspect ratio (width / height).</summary>
    public float TargetAspectRatio { get; set; } = 4f;
    /// <summary>Compact PDF417 (not recommended).</summary>
    public bool Compact { get; set; } = false;

    /// <summary>High-level compaction mode.</summary>
    public Pdf417Compaction Compaction { get; set; } = Pdf417Compaction.Auto;

    /// <summary>Text encoding used for byte compaction.</summary>
    public System.Text.Encoding? TextEncoding { get; set; }
}
