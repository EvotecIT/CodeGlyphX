namespace CodeGlyphX;

/// <summary>Controls Code One size and application encoding.</summary>
public sealed class CodeOneEncodingOptions {
    /// <summary>Gets or sets the fixed size, or Auto for the smallest A-H size.</summary>
    public CodeOneVersion Version { get; set; }
    /// <summary>Gets or sets whether the text is a GS1 message.</summary>
    public bool IsGs1 { get; set; }
    /// <summary>Gets or sets an ECI assignment. Unicode text outside Latin-1 uses UTF-8 ECI 26 by default.</summary>
    public int? EciAssignmentNumber { get; set; }
}
