namespace CodeGlyphX.Gs1Data;

/// <summary>
/// Character repertoire assigned to a GS1 Application Identifier data component.
/// </summary>
public enum Gs1DataCharacterSet {
    /// <summary>Digits 0 through 9.</summary>
    Numeric,

    /// <summary>GS1 AI encodable character set 82.</summary>
    CharacterSet82,

    /// <summary>GS1 AI encodable character set 39.</summary>
    CharacterSet39,

    /// <summary>RFC 4648 base64url characters.</summary>
    Base64Url
}
