using System;
using System.Collections.Generic;

namespace CodeGlyphX.Gs1Data;

public static partial class Gs1Validator {
    private static void ValidateAssociations(List<ParsedElement> parsed, List<Gs1ValidationIssue> issues) {
        var reportedExclusions = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < parsed.Count; i++) {
            var definition = parsed[i].Definition;
            if (definition is null) continue;

            for (var r = 0; r < definition.RequiredAssociations.Count; r++) {
                var expression = definition.RequiredAssociations[r];
                if (IsRequiredAssociationSatisfied(expression, parsed)) continue;
                issues.Add(new Gs1ValidationIssue(
                    Gs1ValidationIssueCode.MissingRequiredApplicationIdentifier,
                    definition.Ai,
                    parsed[i].DataPosition,
                    $"AI ({definition.Ai}) requires one of these AI associations: {expression}."));
            }

            for (var e = 0; e < definition.ExcludedAssociations.Count; e++) {
                var pattern = definition.ExcludedAssociations[e];
                for (var j = 0; j < parsed.Count; j++) {
                    var other = parsed[j].Definition;
                    if (other is null || other.Ai == definition.Ai || !Gs1SyntaxRules.MatchesAiPattern(other.Ai, pattern)) continue;
                    var first = string.CompareOrdinal(definition.Ai, other.Ai) <= 0 ? definition.Ai : other.Ai;
                    var second = first == definition.Ai ? other.Ai : definition.Ai;
                    if (!reportedExclusions.Add(first + ":" + second)) continue;
                    issues.Add(new Gs1ValidationIssue(
                        Gs1ValidationIssueCode.MutuallyExclusiveApplicationIdentifiers,
                        definition.Ai,
                        parsed[i].DataPosition,
                        $"AI ({definition.Ai}) cannot be combined with AI ({other.Ai})."));
                }
            }
        }
    }

    private static bool IsRequiredAssociationSatisfied(string expression, List<ParsedElement> parsed) {
        var alternatives = expression.Split(',');
        for (var i = 0; i < alternatives.Length; i++) {
            var members = alternatives[i].Split('+');
            var complete = true;
            for (var m = 0; m < members.Length; m++) {
                if (ContainsAiPattern(parsed, members[m])) continue;
                complete = false;
                break;
            }
            if (complete) return true;
        }
        return false;
    }

    private static bool ContainsAiPattern(List<ParsedElement> parsed, string pattern) {
        for (var i = 0; i < parsed.Count; i++) {
            var ai = parsed[i].Definition?.Ai;
            if (ai is not null && Gs1SyntaxRules.MatchesAiPattern(ai, pattern)) return true;
        }
        return false;
    }
}
