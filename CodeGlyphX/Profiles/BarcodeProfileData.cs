namespace CodeGlyphX;

/// <summary>A validated application identifier, its checked carrier text and profile-specific publishing metadata.</summary>
public sealed class BarcodeProfileData {
    /// <summary>Gets the explicitly selected application profile.</summary>
    public LinearBarcodeProfile Profile { get; }
    /// <summary>Gets the canonical identifier, including its application check character.</summary>
    public string Identifier { get; }
    /// <summary>Gets the complete decoded carrier payload, preserving the carrier's own check and application fields.</summary>
    public string CarrierText { get; }
    /// <summary>Gets the ISSN's two-digit issue variant, or null for other profiles.</summary>
    public string? IssueVariant { get; }
    /// <summary>Gets the application caption. An ISSN variant affects the bars rather than this caption.</summary>
    public string HumanReadable => BarcodeProfiles.HumanReadable(Profile, Identifier);

    internal BarcodeProfileData(LinearBarcodeProfile profile, string identifier, string carrierText, string? issueVariant) {
        Profile = profile; Identifier = identifier; CarrierText = carrierText; IssueVariant = issueVariant;
    }
}
