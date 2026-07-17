using System;
using System.Collections.Generic;

namespace CodeGlyphX.Gs1Data;

/// <summary>Parses and validates bracketed GS1 AI syntax and raw GS1 element strings.</summary>
public static partial class Gs1Validator {
    /// <summary>Parses and validates a GS1 message while collecting all detectable issues.</summary>
    public static Gs1ValidationResult Validate(string input, Gs1ValidationOptions? options = null) {
        if (input is null) throw new ArgumentNullException(nameof(input));
        var effectiveOptions = options?.Clone() ?? new Gs1ValidationOptions();
        var parsed = new List<ParsedElement>();
        var issues = new List<Gs1ValidationIssue>();
        var unappliedRules = new HashSet<string>(StringComparer.Ordinal);

        if (input.Length == 0) {
            issues.Add(new Gs1ValidationIssue(
                Gs1ValidationIssueCode.MalformedInput,
                null,
                0,
                "GS1 input cannot be empty."));
        } else if (input[0] == '(') {
            ParseBracketed(input, effectiveOptions, parsed, issues);
        } else {
            ParseElementString(input, effectiveOptions, parsed, issues);
        }

        for (var i = 0; i < parsed.Count; i++) {
            ValidateData(parsed[i], effectiveOptions, issues, unappliedRules);
        }

        if (effectiveOptions.ValidateAssociations) {
            ValidateAssociations(parsed, issues);
        }

        var elements = new global::CodeGlyphX.Gs1Element[parsed.Count];
        for (var i = 0; i < parsed.Count; i++) elements[i] = parsed[i].Element;
        var unapplied = new string[unappliedRules.Count];
        unappliedRules.CopyTo(unapplied);
        Array.Sort(unapplied, StringComparer.Ordinal);
        return new Gs1ValidationResult(elements, issues.ToArray(), unapplied);
    }

    /// <summary>Attempts to parse and fully validate a GS1 message.</summary>
    public static bool TryValidate(string input, out Gs1ValidationResult result, Gs1ValidationOptions? options = null) {
        result = Validate(input, options);
        return result.IsValid;
    }

    /// <summary>Builds the raw barcode element string from a valid bracketed GS1 message.</summary>
    public static string ToElementString(string bracketedInput, Gs1ValidationOptions? options = null) {
        if (bracketedInput is null) throw new ArgumentNullException(nameof(bracketedInput));
        var effectiveOptions = options?.Clone() ?? new Gs1ValidationOptions();
        var result = Validate(bracketedInput, effectiveOptions);
        if (!result.IsValid) throw new FormatException(result.Issues[0].Message);
        return ToElementString(result.Elements);
    }

    /// <summary>Builds a raw barcode element string from explicit parsed elements.</summary>
    public static string ToElementString(IReadOnlyList<global::CodeGlyphX.Gs1Element> elements) {
        if (elements is null) throw new ArgumentNullException(nameof(elements));
        if (elements.Count == 0) throw new ArgumentException("At least one GS1 element is required.", nameof(elements));

        var builder = new System.Text.StringBuilder();
        for (var i = 0; i < elements.Count; i++) {
            var element = elements[i];
            var aiIssues = new List<Gs1ValidationIssue>();
            if (!ValidateAiToken(element.Ai, 0, aiIssues)) throw new FormatException(aiIssues[0].Message);
            builder.Append(element.Ai);
            builder.Append(element.Data ?? string.Empty);
            var variableLength = element.Definition?.RequiresFnc1Separator ?? element.IsVariableLength;
            if (variableLength && i < elements.Count - 1) builder.Append(global::CodeGlyphX.Gs1.GroupSeparator);
        }
        return builder.ToString();
    }

    private static void ParseBracketed(
        string input,
        Gs1ValidationOptions options,
        List<ParsedElement> parsed,
        List<Gs1ValidationIssue> issues) {
        var offset = 0;
        while (offset < input.Length) {
            if (input[offset] != '(') {
                issues.Add(new Gs1ValidationIssue(
                    Gs1ValidationIssueCode.MalformedInput,
                    null,
                    offset,
                    "Bracketed GS1 input must start every element with '(AI)'."));
                return;
            }

            var close = input.IndexOf(')', offset + 1);
            if (close < 0) {
                issues.Add(new Gs1ValidationIssue(
                    Gs1ValidationIssueCode.MalformedInput,
                    null,
                    offset,
                    "GS1 Application Identifier is missing a closing ')'."));
                return;
            }

            var ai = input.Substring(offset + 1, close - offset - 1);
            var aiIsValid = ValidateAiToken(ai, offset + 1, issues);
            var dataStart = close + 1;
            var next = input.IndexOf('(', dataStart);
            var dataEnd = next < 0 ? input.Length : next;
            var separatorCount = 0;

            while (dataEnd > dataStart && IsSeparator(input[dataEnd - 1])) {
                separatorCount++;
                dataEnd--;
            }

            var hadSeparator = separatorCount > 0;
            if (separatorCount > 1) {
                issues.Add(new Gs1ValidationIssue(
                    Gs1ValidationIssueCode.MalformedInput,
                    ai,
                    dataEnd + 1,
                    "Only one FNC1 separator may occur between GS1 elements."));
            }

            if (hadSeparator && next < 0) {
                ReportTrailingSeparator(ai, dataEnd, issues);
            }

            for (var i = dataStart; i < dataEnd; i++) {
                if (!IsSeparator(input[i])) continue;
                issues.Add(new Gs1ValidationIssue(
                    Gs1ValidationIssueCode.MalformedInput,
                    ai,
                    i,
                    "FNC1 separator must occur only between GS1 elements."));
            }

            var data = input.Substring(dataStart, dataEnd - dataStart);
            Gs1ApplicationIdentifier? definition = null;
            if (aiIsValid && !Gs1ApplicationIdentifierCatalog.TryGet(ai, out definition!)) {
                if (!options.AllowUnknownApplicationIdentifiers) {
                    issues.Add(new Gs1ValidationIssue(
                        Gs1ValidationIssueCode.UnknownApplicationIdentifier,
                        ai,
                        offset + 1,
                        $"Application Identifier '{ai}' is not assigned in GS1 dictionary release {Gs1ApplicationIdentifierCatalog.Release}."));
                }
            }

            if (definition is not null && hadSeparator && definition.HasPredefinedLength && !options.AllowRedundantFnc1Separators) {
                issues.Add(new Gs1ValidationIssue(
                    Gs1ValidationIssueCode.UnexpectedFnc1Separator,
                    ai,
                    dataEnd,
                    "A predefined-length GS1 element must not be followed by FNC1."));
            }

            parsed.Add(new ParsedElement(
                new global::CodeGlyphX.Gs1Element(ai, data, definition?.RequiresFnc1Separator ?? true, definition),
                definition,
                dataStart));
            offset = next < 0 ? input.Length : next;
        }
    }

    private static void ParseElementString(
        string input,
        Gs1ValidationOptions options,
        List<ParsedElement> parsed,
        List<Gs1ValidationIssue> issues) {
        if (input.IndexOf('|') >= 0) input = input.Replace('|', global::CodeGlyphX.Gs1.GroupSeparator);
        var offset = 0;
        while (offset < input.Length) {
            if (input[offset] == global::CodeGlyphX.Gs1.GroupSeparator) {
                issues.Add(new Gs1ValidationIssue(
                    Gs1ValidationIssueCode.MalformedInput,
                    null,
                    offset,
                    "GS1 element string contains an empty element."));
                offset++;
                continue;
            }

            if (!Gs1ApplicationIdentifierCatalog.TryMatch(input, offset, out var definition)) {
                issues.Add(new Gs1ValidationIssue(
                    Gs1ValidationIssueCode.UnknownApplicationIdentifier,
                    null,
                    offset,
                    $"No assigned GS1 Application Identifier starts at position {offset}."));
                return;
            }

            var dataStart = offset + definition.Ai.Length;
            string data;
            if (definition.HasPredefinedLength) {
                var available = input.Length - dataStart;
                var length = Math.Min(definition.MaximumDataLength, available);
                data = input.Substring(dataStart, length);
                offset = dataStart + length;
                if (offset < input.Length && input[offset] == global::CodeGlyphX.Gs1.GroupSeparator) {
                    if (!options.AllowRedundantFnc1Separators) {
                        issues.Add(new Gs1ValidationIssue(
                            Gs1ValidationIssueCode.UnexpectedFnc1Separator,
                            definition.Ai,
                            offset,
                            "A predefined-length GS1 element must not be followed by FNC1."));
                    }
                    offset++;
                    if (offset == input.Length) {
                        ReportTrailingSeparator(definition.Ai, offset - 1, issues);
                    }
                }
            } else {
                var separator = input.IndexOf(global::CodeGlyphX.Gs1.GroupSeparator, dataStart);
                var dataEnd = separator < 0 ? input.Length : separator;
                data = input.Substring(dataStart, dataEnd - dataStart);
                offset = separator < 0 ? input.Length : separator + 1;
                if (separator >= 0 && offset == input.Length) {
                    ReportTrailingSeparator(definition.Ai, separator, issues);
                }
            }

            parsed.Add(new ParsedElement(
                new global::CodeGlyphX.Gs1Element(definition.Ai, data, definition.RequiresFnc1Separator, definition),
                definition,
                dataStart));
        }
    }

    private static void ReportTrailingSeparator(
        string? ai,
        int position,
        List<Gs1ValidationIssue> issues) {
        issues.Add(new Gs1ValidationIssue(
            Gs1ValidationIssueCode.MalformedInput,
            ai,
            position,
            "FNC1 separator must be followed by another GS1 element."));
    }

    private static bool ValidateAiToken(string ai, int position, List<Gs1ValidationIssue> issues) {
        if (ai.Length < 2 || ai.Length > 4) {
            issues.Add(new Gs1ValidationIssue(
                Gs1ValidationIssueCode.MalformedInput,
                ai,
                position,
                "A GS1 Application Identifier must contain two through four digits."));
            return false;
        }
        for (var i = 0; i < ai.Length; i++) {
            if (ai[i] >= '0' && ai[i] <= '9') continue;
            issues.Add(new Gs1ValidationIssue(
                Gs1ValidationIssueCode.MalformedInput,
                ai,
                position + i,
                "A GS1 Application Identifier must contain only digits."));
            return false;
        }
        return true;
    }

    private static bool IsSeparator(char value) {
        return value == '|' || value == global::CodeGlyphX.Gs1.GroupSeparator;
    }

    private sealed class ParsedElement {
        internal global::CodeGlyphX.Gs1Element Element { get; }
        internal Gs1ApplicationIdentifier? Definition { get; }
        internal int DataPosition { get; }

        internal ParsedElement(
            global::CodeGlyphX.Gs1Element element,
            Gs1ApplicationIdentifier? definition,
            int dataPosition) {
            Element = element;
            Definition = definition;
            DataPosition = dataPosition;
        }
    }
}
