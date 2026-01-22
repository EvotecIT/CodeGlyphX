using System.Text;

namespace CodeGlyphX.Pdf417;

/// <summary>
/// Options for MicroPDF417 encoding.
/// </summary>
public sealed class MicroPdf417EncodeOptions {
    /// <summary>
    /// Fixed number of data columns (1..4). Leave null to auto-select.
    /// </summary>
    public int? Columns { get; set; }

    /// <summary>
    /// Fixed number of rows (4..44). Leave null to auto-select.
    /// </summary>
    public int? Rows { get; set; }

    /// <summary>
    /// High-level compaction mode.
    /// </summary>
    public Pdf417Compaction Compaction { get; set; } = Pdf417Compaction.Auto;

    /// <summary>
    /// Text encoding used for byte compaction.
    /// </summary>
    public Encoding? TextEncoding { get; set; }
}
