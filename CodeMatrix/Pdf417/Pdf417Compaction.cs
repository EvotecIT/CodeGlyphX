namespace CodeMatrix.Pdf417;

/// <summary>
/// High-level compaction mode selection for PDF417.
/// </summary>
public enum Pdf417Compaction {
    /// <summary>Choose compaction automatically.</summary>
    Auto,
    /// <summary>Use text compaction.</summary>
    Text,
    /// <summary>Use byte compaction.</summary>
    Byte,
    /// <summary>Use numeric compaction.</summary>
    Numeric
}
