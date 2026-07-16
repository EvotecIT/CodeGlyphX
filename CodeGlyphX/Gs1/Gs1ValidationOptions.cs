namespace CodeGlyphX.Gs1Data;

/// <summary>Controls GS1 parsing and validation strictness.</summary>
public sealed class Gs1ValidationOptions {
    /// <summary>
    /// Gets or sets whether unassigned AIs are accepted in bracketed input as variable-length fields.
    /// Raw element strings cannot safely infer the length of an unknown AI.
    /// </summary>
    public bool AllowUnknownApplicationIdentifiers { get; set; }

    /// <summary>Gets or sets whether mandatory and mutually-exclusive AI association rules are evaluated.</summary>
    public bool ValidateAssociations { get; set; } = true;

    /// <summary>Gets or sets whether GS1 semantic linters such as check-digit and date validation are evaluated.</summary>
    public bool ValidateSemanticRules { get; set; } = true;

    /// <summary>Gets or sets whether data characters are checked against the official N, X, Y, and Z repertoires.</summary>
    public bool ValidateCharacterSets { get; set; } = true;

    /// <summary>
    /// Gets or sets whether an empty variable-length field is accepted for compatibility with legacy encoders.
    /// Standards-conformance validation should leave this disabled.
    /// </summary>
    public bool AllowEmptyVariableLengthData { get; set; }

    /// <summary>
    /// Gets or sets whether redundant FNC1 separators after predefined-length fields are tolerated.
    /// Standards-conformance validation should leave this disabled.
    /// </summary>
    public bool AllowRedundantFnc1Separators { get; set; }

    internal Gs1ValidationOptions Clone() {
        return new Gs1ValidationOptions {
            AllowUnknownApplicationIdentifiers = AllowUnknownApplicationIdentifiers,
            ValidateAssociations = ValidateAssociations,
            ValidateSemanticRules = ValidateSemanticRules,
            ValidateCharacterSets = ValidateCharacterSets,
            AllowEmptyVariableLengthData = AllowEmptyVariableLengthData,
            AllowRedundantFnc1Separators = AllowRedundantFnc1Separators
        };
    }
}
