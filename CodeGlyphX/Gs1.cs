using System;
using System.Collections.Generic;
using System.Text;
using CodeGlyphX.Code128;

namespace CodeGlyphX;

/// <summary>
/// Helpers for building and encoding GS1 element strings (GS1-128).
/// </summary>
public static class Gs1 {
    /// <summary>
    /// ASCII Group Separator (FNC1) used between variable-length GS1 elements.
    /// </summary>
    public const char GroupSeparator = (char)29;

    /// <summary>
    /// Builds a GS1 element string from an AI string like <c>(01)09506000134352(10)ABC(17)240101</c>.
    /// Unknown AIs are treated as variable length; use <c>|</c> or <c>GroupSeparator</c> in the input to force a separator.
    /// </summary>
    public static string ElementString(string aiText) {
        if (aiText is null) throw new ArgumentNullException(nameof(aiText));
        if (aiText.Length == 0) throw new ArgumentException("GS1 AI text cannot be empty.", nameof(aiText));

        if (!aiText.Contains('(')) {
            return ReplaceSeparators(aiText);
        }

        var elements = ParseAiText(aiText);
        return ElementString(elements);
    }

    /// <summary>
    /// Builds a GS1 element string from explicit elements.
    /// </summary>
    public static string ElementString(params Gs1Element[] elements) {
        if (elements is null) throw new ArgumentNullException(nameof(elements));
        if (elements.Length == 0) throw new ArgumentException("At least one GS1 element is required.", nameof(elements));

        var sb = new StringBuilder();
        for (var i = 0; i < elements.Length; i++) {
            var element = elements[i];
            ValidateAi(element.Ai);
            sb.Append(element.Ai);
            sb.Append(element.Data ?? string.Empty);
            if (element.IsVariableLength && i < elements.Length - 1) sb.Append(GroupSeparator);
        }
        return sb.ToString();
    }

    /// <summary>
    /// Encodes a GS1-128 barcode from an AI string (parentheses format or raw element string).
    /// </summary>
    public static Barcode1D Encode128(string aiText) {
        var elementString = ElementString(aiText);
        return Code128Encoder.EncodeGs1(elementString);
    }

    /// <summary>
    /// Encodes a GS1-128 barcode from explicit elements.
    /// </summary>
    public static Barcode1D Encode128(params Gs1Element[] elements) {
        var elementString = ElementString(elements);
        return Code128Encoder.EncodeGs1(elementString);
    }

    private static string ReplaceSeparators(string input) {
        if (input.IndexOf('|') < 0) return input;
        return input.Replace('|', GroupSeparator);
    }

    private static Gs1Element[] ParseAiText(string aiText) {
        var list = new List<Gs1Element>(8);
        var i = 0;
        while (i < aiText.Length) {
            if (aiText[i] != '(') {
                throw new FormatException("GS1 AI text must use parentheses around each AI.");
            }

            var close = aiText.IndexOf(')', i + 1);
            if (close < 0) throw new FormatException("GS1 AI text is missing a closing ')'.");

            var ai = aiText.Substring(i + 1, close - i - 1).Trim();
            ValidateAi(ai);
            i = close + 1;

            if (i >= aiText.Length) {
                list.Add(Gs1Element.Fixed(ai, string.Empty));
                break;
            }

            var data = new StringBuilder();
            var forceSeparator = false;
            while (i < aiText.Length && aiText[i] != '(') {
                var ch = aiText[i];
                if (ch == '|' || ch == GroupSeparator) {
                    forceSeparator = true;
                    i++;
                    continue;
                }
                data.Append(ch);
                i++;
            }

            var dataText = data.ToString();
            var spec = GetAiSpec(ai, dataText.Length);
            if (spec.IsFixedLength) {
                if (dataText.Length != spec.Length) {
                    throw new FormatException($"AI ({ai}) expects {spec.Length} characters but got {dataText.Length}.");
                }
                list.Add(Gs1Element.Fixed(ai, dataText));
            } else {
                if (spec.MaxLength > 0 && dataText.Length > spec.MaxLength) {
                    throw new FormatException($"AI ({ai}) exceeds the maximum length of {spec.MaxLength} characters.");
                }
                list.Add(new Gs1Element(ai, dataText, isVariableLength: forceSeparator || spec.IsVariableLength));
            }
        }

        return list.ToArray();
    }

    private static void ValidateAi(string ai) {
        if (string.IsNullOrWhiteSpace(ai)) throw new FormatException("GS1 AI is missing.");
        for (var i = 0; i < ai.Length; i++) {
            if (ai[i] is < '0' or > '9') throw new FormatException($"GS1 AI '{ai}' must be numeric.");
        }
    }

    private static Gs1AiSpec GetAiSpec(string ai, int dataLength) {
        if (TryGetFixedLength(ai, out var fixedLength)) {
            return Gs1AiSpec.Fixed(fixedLength);
        }

        if (TryGetVariableLength(ai, out var maxLength)) {
            return Gs1AiSpec.Variable(maxLength);
        }

        // If unknown, assume variable-length to keep the fields separated.
        return Gs1AiSpec.Variable(0);
    }

    private static bool TryGetFixedLength(string ai, out int length) {
        length = 0;
        switch (ai) {
            case "00": length = 18; return true;
            case "01": length = 14; return true;
            case "02": length = 14; return true;
            case "11": length = 6; return true;
            case "12": length = 6; return true;
            case "13": length = 6; return true;
            case "15": length = 6; return true;
            case "16": length = 6; return true;
            case "17": length = 6; return true;
            case "20": length = 2; return true;
        }

        if (ai.Length == 4) {
            if (ai.StartsWith("31", StringComparison.Ordinal)) { length = 6; return true; }
            if (ai.StartsWith("32", StringComparison.Ordinal)) { length = 6; return true; }
            if (ai.StartsWith("33", StringComparison.Ordinal)) { length = 6; return true; }
            if (ai.StartsWith("34", StringComparison.Ordinal)) { length = 6; return true; }
            if (ai.StartsWith("35", StringComparison.Ordinal)) { length = 6; return true; }
            if (ai.StartsWith("36", StringComparison.Ordinal)) { length = 6; return true; }
        }

        return false;
    }

    private static bool TryGetVariableLength(string ai, out int maxLength) {
        maxLength = 0;
        switch (ai) {
            case "10": maxLength = 20; return true;
            case "21": maxLength = 20; return true;
            case "22": maxLength = 29; return true;
            case "240": maxLength = 30; return true;
            case "241": maxLength = 30; return true;
            case "242": maxLength = 6; return true;
            case "30": maxLength = 8; return true;
            case "37": maxLength = 8; return true;
        }

        if (ai.Length == 4) {
            if (ai.StartsWith("39", StringComparison.Ordinal)) { maxLength = 15; return true; }
        }

        return false;
    }

    private readonly struct Gs1AiSpec {
        public int Length { get; }
        public int MaxLength { get; }
        public bool IsFixedLength { get; }
        public bool IsVariableLength => !IsFixedLength;

        private Gs1AiSpec(int length, int maxLength, bool isFixed) {
            Length = length;
            MaxLength = maxLength;
            IsFixedLength = isFixed;
        }

        public static Gs1AiSpec Fixed(int length) => new(length, length, true);

        public static Gs1AiSpec Variable(int maxLength) => new(0, maxLength, false);
    }
}

/// <summary>
/// Represents a GS1 element (AI + data) with explicit length semantics.
/// </summary>
public readonly struct Gs1Element {
    /// <summary>
    /// Application Identifier.
    /// </summary>
    public string Ai { get; }

    /// <summary>
    /// Data associated with the AI.
    /// </summary>
    public string Data { get; }

    /// <summary>
    /// Indicates whether the element is variable length.
    /// </summary>
    public bool IsVariableLength { get; }

    internal Gs1Element(string ai, string data, bool isVariableLength) {
        Ai = ai ?? throw new ArgumentNullException(nameof(ai));
        Data = data ?? string.Empty;
        IsVariableLength = isVariableLength;
    }

    /// <summary>
    /// Creates a fixed-length GS1 element.
    /// </summary>
    public static Gs1Element Fixed(string ai, string data) => new(ai, data, isVariableLength: false);

    /// <summary>
    /// Creates a variable-length GS1 element.
    /// </summary>
    public static Gs1Element Variable(string ai, string data) => new(ai, data, isVariableLength: true);
}
