using System.Text;

namespace CodeGlyphX;

/// <summary>Controls DotCode dimensions, masking, GS1, ECI, and structured append.</summary>
public sealed class DotCodeEncodingOptions {
    /// <summary>Gets or sets an optional symbol width from 5 through 200 modules.</summary>
    public int? Width { get; set; }
    /// <summary>Gets or sets an optional mask from 0 through 7. Masks 4 through 7 force the six orientation corners.</summary>
    public int? Mask { get; set; }
    /// <summary>Gets or sets whether the value is a GS1 AI string.</summary>
    public bool IsGs1 { get; set; }
    /// <summary>Gets or sets whether to emit the reader-initialization (FNC3) control.</summary>
    public bool ReaderInitialization { get; set; }
    /// <summary>Gets or sets an optional ECI assignment from 0 through 811799.</summary>
    public int? EciAssignmentNumber { get; set; }
    /// <summary>Gets or sets the text encoding. UTF-8 with ECI 26 is selected automatically when required.</summary>
    public Encoding? TextEncoding { get; set; }
    /// <summary>Gets or sets the one-based structured-append symbol index.</summary>
    public int? StructuredAppendIndex { get; set; }
    /// <summary>Gets or sets the structured-append count from 2 through 35.</summary>
    public int? StructuredAppendCount { get; set; }

    internal DotCodeEncodingOptions Clone() => new() {
        Width = Width,
        Mask = Mask,
        IsGs1 = IsGs1,
        ReaderInitialization = ReaderInitialization,
        EciAssignmentNumber = EciAssignmentNumber,
        TextEncoding = TextEncoding,
        StructuredAppendIndex = StructuredAppendIndex,
        StructuredAppendCount = StructuredAppendCount
    };
}
