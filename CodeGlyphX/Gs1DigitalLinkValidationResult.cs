using System;
using System.Collections.Generic;
using CodeGlyphX.Gs1Data;

namespace CodeGlyphX;

/// <summary>Structured result of parsing and validating an uncompressed GS1 Digital Link URI.</summary>
public sealed class Gs1DigitalLinkValidationResult {
    private readonly IReadOnlyList<Gs1DigitalLinkIssue> _issues;

    /// <summary>Gets whether URI structure and every GS1 element passed validation.</summary>
    public bool IsValid => Value is not null && _issues.Count == 0 && ElementValidation?.IsValid == true;

    /// <summary>Gets the parsed URI when validation succeeded.</summary>
    public Gs1DigitalLinkUri? Value { get; }

    /// <summary>Gets GS1 element-level validation details when URI elements could be extracted.</summary>
    public Gs1ValidationResult? ElementValidation { get; }

    /// <summary>Gets all detected URI structure, Digital Link placement, and GS1 semantic problems.</summary>
    public IReadOnlyList<Gs1DigitalLinkIssue> Issues => _issues;

    internal Gs1DigitalLinkValidationResult(
        Gs1DigitalLinkUri? value,
        Gs1ValidationResult? elementValidation,
        Gs1DigitalLinkIssue[] issues) {
        Value = value;
        ElementValidation = elementValidation;
        _issues = Array.AsReadOnly(issues ?? throw new ArgumentNullException(nameof(issues)));
    }
}
