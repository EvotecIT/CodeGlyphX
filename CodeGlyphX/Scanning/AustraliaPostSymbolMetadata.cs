using CodeGlyphX.AustraliaPost;

namespace CodeGlyphX;

/// <summary>Australia Post fields, checked parity and possible customer-information interpretations.</summary>
public sealed class AustraliaPostSymbolMetadata : SymbolMetadata {
    /// <summary>Gets the decoded postal fields and application-selected customer interpretation.</summary>
    public AustraliaPostDecoded Data { get; }
    internal AustraliaPostSymbolMetadata(AustraliaPostDecoded data) => Data = data;
}
