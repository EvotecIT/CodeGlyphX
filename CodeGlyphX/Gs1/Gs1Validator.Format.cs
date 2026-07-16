using System;
using System.Collections.Generic;

namespace CodeGlyphX.Gs1Data;

public static partial class Gs1Validator {
    private static void ValidateData(
        ParsedElement parsed,
        Gs1ValidationOptions options,
        List<Gs1ValidationIssue> issues,
        HashSet<string> unappliedRules) {
        var definition = parsed.Definition;
        if (definition is null) return;
        var data = parsed.Element.Data;

        var allowLegacyEmpty = options.AllowEmptyVariableLengthData && data.Length == 0 && !definition.HasPredefinedLength;
        if (data.Length < definition.MinimumDataLength && !allowLegacyEmpty) {
            issues.Add(new Gs1ValidationIssue(
                Gs1ValidationIssueCode.DataTooShort,
                definition.Ai,
                parsed.DataPosition,
                $"Data length {data.Length} is shorter than the minimum {definition.MinimumDataLength} for format {definition.Format}."));
        } else if (data.Length > definition.MaximumDataLength) {
            issues.Add(new Gs1ValidationIssue(
                Gs1ValidationIssueCode.DataTooLong,
                definition.Ai,
                parsed.DataPosition + definition.MaximumDataLength,
                $"Data length {data.Length} exceeds the maximum {definition.MaximumDataLength} for format {definition.Format}. A missing FNC1 separator is a common cause."));
        }

        var componentOffset = 0;
        for (var i = 0; i < definition.Components.Count; i++) {
            var component = definition.Components[i];
            var remaining = data.Length - componentOffset;
            if (remaining <= 0) {
                if (component.IsOptional) continue;
                break;
            }

            var requiredAfter = MinimumRequiredLengthAfter(definition.Components, i + 1);
            var available = Math.Max(0, remaining - requiredAfter);
            var length = Math.Min(component.MaximumLength, available);
            if (!component.IsOptional && length < component.MinimumLength) {
                length = Math.Min(remaining, component.MinimumLength);
            } else if (component.IsOptional && length < component.MinimumLength) {
                continue;
            }

            var value = data.Substring(componentOffset, length);
            if (options.ValidateCharacterSets) {
                ValidateCharacters(definition, component, value, parsed.DataPosition + componentOffset, issues);
            }
            if (options.ValidateSemanticRules) {
                for (var l = 0; l < component.Linters.Count; l++) {
                    if (!TryApplySemanticRule(component.Linters[l], value, definition, parsed.DataPosition + componentOffset, issues)) {
                        unappliedRules.Add(component.Linters[l]);
                    }
                }
            }
            componentOffset += length;
        }
    }

    private static int MinimumRequiredLengthAfter(IReadOnlyList<Gs1DataFormatComponent> components, int start) {
        var length = 0;
        for (var i = start; i < components.Count; i++) {
            if (!components[i].IsOptional) length += components[i].MinimumLength;
        }
        return length;
    }

    private static void ValidateCharacters(
        Gs1ApplicationIdentifier definition,
        Gs1DataFormatComponent component,
        string value,
        int position,
        List<Gs1ValidationIssue> issues) {
        for (var i = 0; i < value.Length; i++) {
            if (IsValidCharacter(value[i], component.CharacterSet)) continue;
            issues.Add(new Gs1ValidationIssue(
                Gs1ValidationIssueCode.InvalidCharacter,
                definition.Ai,
                position + i,
                $"Character U+{(int)value[i]:X4} is not permitted by GS1 {component.CharacterSet}."));
            return;
        }

        if (component.CharacterSet == Gs1DataCharacterSet.Base64Url) {
            ValidateBase64UrlPadding(definition, value, position, issues);
        }
    }

    private static bool IsValidCharacter(char value, Gs1DataCharacterSet characterSet) {
        return characterSet switch {
            Gs1DataCharacterSet.Numeric => value >= '0' && value <= '9',
            Gs1DataCharacterSet.CharacterSet82 =>
                value == '!' || value == '"' || value == '%' || value == '&' || value == '\'' ||
                value == '(' || value == ')' || value == '*' || value == '+' || value == ',' ||
                value == '-' || value == '.' || value == '/' ||
                (value >= '0' && value <= '9') ||
                (value >= ':' && value <= '?') ||
                (value >= 'A' && value <= 'Z') || value == '_' ||
                (value >= 'a' && value <= 'z'),
            Gs1DataCharacterSet.CharacterSet39 =>
                value == '#' || value == '-' || value == '/' ||
                (value >= '0' && value <= '9') ||
                (value >= 'A' && value <= 'Z'),
            Gs1DataCharacterSet.Base64Url =>
                (value >= 'A' && value <= 'Z') ||
                (value >= 'a' && value <= 'z') ||
                (value >= '0' && value <= '9') || value == '-' || value == '_' || value == '=',
            _ => false
        };
    }

    private static void ValidateBase64UrlPadding(
        Gs1ApplicationIdentifier definition,
        string value,
        int position,
        List<Gs1ValidationIssue> issues) {
        var firstPadding = value.IndexOf('=');
        if (firstPadding < 0) return;
        for (var i = firstPadding; i < value.Length; i++) {
            if (value[i] == '=') continue;
            issues.Add(new Gs1ValidationIssue(
                Gs1ValidationIssueCode.InvalidData,
                definition.Ai,
                position + i,
                "GS1 base64url padding may occur only at the end of the value."));
            return;
        }
        var padding = value.Length - firstPadding;
        if (padding > 2 || value.Length % 3 != 0) {
            issues.Add(new Gs1ValidationIssue(
                Gs1ValidationIssueCode.InvalidData,
                definition.Ai,
                position + firstPadding,
                "GS1 base64url padding is invalid."));
        }
    }
}
