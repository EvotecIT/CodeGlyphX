using System.Text;

namespace CodeGlyphX;

/// <summary>Controls Han Xin Code version, error correction, mask, ECI, and compaction.</summary>
public sealed class HanXinEncodingOptions {
    /// <summary>Gets or sets the compaction mode.</summary>
    public HanXinEncodingMode Mode { get; set; } = HanXinEncodingMode.Auto;
    /// <summary>Gets or sets an exact version from 1 through 84, or null for the smallest fitting version.</summary>
    public int? Version { get; set; }
    /// <summary>Gets or sets the error-correction level from 1 through 4.</summary>
    public int ErrorCorrectionLevel { get; set; } = 1;
    /// <summary>Gets or sets an exact data mask from 0 through 3, or null to minimize the standard penalty score.</summary>
    public int? Mask { get; set; }
    /// <summary>Gets or sets an optional ECI assignment from 0 through 999999.</summary>
    public int? EciAssignmentNumber { get; set; }
    /// <summary>Gets or sets the text encoding. UTF-8 with ECI 26 is selected automatically when binary text is required.</summary>
    public Encoding? TextEncoding { get; set; }

    internal HanXinEncodingOptions Clone() => new() {
        Mode = Mode,
        Version = Version,
        ErrorCorrectionLevel = ErrorCorrectionLevel,
        Mask = Mask,
        EciAssignmentNumber = EciAssignmentNumber,
        TextEncoding = TextEncoding
    };
}
