namespace CodeGlyphX.Gs1Data;

/// <summary>Classification for a GS1 validation issue.</summary>
public enum Gs1ValidationIssueCode {
    /// <summary>The input representation is malformed.</summary>
    MalformedInput,

    /// <summary>The Application Identifier is not assigned in the bundled dictionary release.</summary>
    UnknownApplicationIdentifier,

    /// <summary>The data field is shorter than its GS1 format permits.</summary>
    DataTooShort,

    /// <summary>The data field is longer than its GS1 format permits.</summary>
    DataTooLong,

    /// <summary>The data field contains a character outside its assigned repertoire.</summary>
    InvalidCharacter,

    /// <summary>A semantic syntax rule such as a check digit, date, or code-list rule failed.</summary>
    InvalidData,

    /// <summary>A variable-length element was not terminated by FNC1 before another element.</summary>
    MissingFnc1Separator,

    /// <summary>An FNC1 separator was used after a predefined-length element.</summary>
    UnexpectedFnc1Separator,

    /// <summary>A mandatory associated AI or AI combination is absent.</summary>
    MissingRequiredApplicationIdentifier,

    /// <summary>Mutually exclusive AIs occur in the same message.</summary>
    MutuallyExclusiveApplicationIdentifiers
}
