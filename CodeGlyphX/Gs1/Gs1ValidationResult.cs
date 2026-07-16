using System;
using System.Collections.Generic;

namespace CodeGlyphX.Gs1Data;

/// <summary>Structured result of parsing and validating a GS1 AI message.</summary>
public sealed class Gs1ValidationResult {
    private readonly IReadOnlyList<global::CodeGlyphX.Gs1Element> _elements;
    private readonly IReadOnlyList<Gs1ValidationIssue> _issues;
    private readonly IReadOnlyList<string> _unappliedSemanticRules;

    /// <summary>Gets whether no validation issues were found.</summary>
    public bool IsValid => _issues.Count == 0;

    /// <summary>Gets the parsed elements in message order.</summary>
    public IReadOnlyList<global::CodeGlyphX.Gs1Element> Elements => _elements;

    /// <summary>Gets all validation issues found in one pass.</summary>
    public IReadOnlyList<Gs1ValidationIssue> Issues => _issues;

    /// <summary>Gets semantic rule names present in the dictionary but not evaluated by this library version.</summary>
    public IReadOnlyList<string> UnappliedSemanticRules => _unappliedSemanticRules;

    /// <summary>Gets whether every semantic rule referenced by the parsed AIs was evaluated.</summary>
    public bool HasCompleteSemanticValidation => _unappliedSemanticRules.Count == 0;

    internal Gs1ValidationResult(
        global::CodeGlyphX.Gs1Element[] elements,
        Gs1ValidationIssue[] issues,
        string[] unappliedSemanticRules) {
        _elements = Array.AsReadOnly(elements ?? throw new ArgumentNullException(nameof(elements)));
        _issues = Array.AsReadOnly(issues ?? throw new ArgumentNullException(nameof(issues)));
        _unappliedSemanticRules = Array.AsReadOnly(unappliedSemanticRules ?? throw new ArgumentNullException(nameof(unappliedSemanticRules)));
    }
}
