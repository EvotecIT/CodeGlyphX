using System;
using System.Collections.Generic;

namespace CodeGlyphX.Gs1Data;

internal static class Gs1SyntaxRules {
    internal static Gs1DataFormatComponent[] ParseFormat(string format) {
        var tokens = format.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        var components = new Gs1DataFormatComponent[tokens.Length];
        for (var i = 0; i < tokens.Length; i++) {
            components[i] = ParseComponent(tokens[i]);
        }
        return components;
    }

    internal static void ParseAttributes(
        string attributes,
        out string[] required,
        out string[] excluded,
        out bool isDigitalLinkPrimaryKey,
        out string? digitalLinkQualifiers) {
        var requiredItems = new List<string>();
        var excludedItems = new List<string>();
        isDigitalLinkPrimaryKey = false;
        digitalLinkQualifiers = null;

        var tokens = attributes.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < tokens.Length; i++) {
            var token = tokens[i];
            if (token.StartsWith("req=", StringComparison.Ordinal)) {
                requiredItems.Add(token.Substring(4));
            } else if (token.StartsWith("ex=", StringComparison.Ordinal)) {
                var patterns = token.Substring(3).Split(',');
                for (var p = 0; p < patterns.Length; p++) excludedItems.Add(patterns[p]);
            } else if (token == "dlpkey") {
                isDigitalLinkPrimaryKey = true;
            } else if (token.StartsWith("dlpkey=", StringComparison.Ordinal)) {
                isDigitalLinkPrimaryKey = true;
                digitalLinkQualifiers = token.Substring(7);
            }
        }

        required = requiredItems.ToArray();
        excluded = excludedItems.ToArray();
    }

    internal static bool MatchesAiPattern(string ai, string pattern) {
        if (ai.Length != pattern.Length) return false;
        for (var i = 0; i < ai.Length; i++) {
            var expected = pattern[i];
            if (expected != 'n' && expected != ai[i]) return false;
        }
        return true;
    }

    private static Gs1DataFormatComponent ParseComponent(string token) {
        var optional = token[0] == '[';
        if (optional) token = token.Substring(1);

        var parts = token.Split(',');
        var type = parts[0];
        if (type.EndsWith("]", StringComparison.Ordinal)) type = type.Substring(0, type.Length - 1);

        var characterSet = type[0] switch {
            'N' => Gs1DataCharacterSet.Numeric,
            'X' => Gs1DataCharacterSet.CharacterSet82,
            'Y' => Gs1DataCharacterSet.CharacterSet39,
            'Z' => Gs1DataCharacterSet.Base64Url,
            _ => throw new FormatException($"Unsupported GS1 data format '{type}'.")
        };

        int minimumLength;
        int maximumLength;
        var range = type.IndexOf("..", StringComparison.Ordinal);
        if (range >= 0) {
            minimumLength = 1;
            maximumLength = ParsePositiveLength(type.Substring(range + 2), type);
        } else {
            minimumLength = ParsePositiveLength(type.Substring(1), type);
            maximumLength = minimumLength;
        }

        var linters = new string[parts.Length - 1];
        for (var i = 1; i < parts.Length; i++) {
            var linter = parts[i];
            if (linter.EndsWith("]", StringComparison.Ordinal)) linter = linter.Substring(0, linter.Length - 1);
            linters[i - 1] = linter;
        }

        return new Gs1DataFormatComponent(characterSet, minimumLength, maximumLength, optional, linters);
    }

    private static int ParsePositiveLength(string value, string format) {
        if (!int.TryParse(value, out var length) || length <= 0) {
            throw new FormatException($"Unsupported GS1 data length in '{format}'.");
        }
        return length;
    }
}
