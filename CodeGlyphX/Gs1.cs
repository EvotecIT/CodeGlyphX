using System;
using System.Collections.Generic;
using CodeGlyphX.Code128;
using CodeGlyphX.Gs1Data;

namespace CodeGlyphX;

/// <summary>
/// High-level helpers for parsing, validating, building, and encoding GS1 Application Identifier messages.
/// </summary>
public static class Gs1 {
    /// <summary>ASCII Group Separator (FNC1) used between variable-length GS1 elements.</summary>
    public const char GroupSeparator = (char)29;

    /// <summary>
    /// Parses and validates bracketed GS1 syntax or a raw element string against the bundled official catalog.
    /// </summary>
    public static Gs1ValidationResult Validate(string input, Gs1ValidationOptions? options = null) {
        return Gs1Validator.Validate(input, options);
    }

    /// <summary>Attempts to parse and fully validate a GS1 message.</summary>
    public static bool TryValidate(string input, out Gs1ValidationResult result, Gs1ValidationOptions? options = null) {
        return Gs1Validator.TryValidate(input, out result, options);
    }

    /// <summary>Parses a valid GS1 message into its assigned Application Identifier elements.</summary>
    public static IReadOnlyList<Gs1Element> Parse(string input, Gs1ValidationOptions? options = null) {
        var result = Validate(input, options);
        if (!result.IsValid) throw new FormatException(result.Issues[0].Message);
        return result.Elements;
    }

    /// <summary>
    /// Builds a machine-readable element string from bracketed syntax such as
    /// <c>(01)09506000134352(10)ABC(17)240101</c>. FNC1 separators are inserted from catalog metadata.
    /// </summary>
    public static string ElementString(string aiText) {
        if (aiText is null) throw new ArgumentNullException(nameof(aiText));
        if (aiText.IndexOf('(') < 0) return aiText.Replace('|', GroupSeparator);
        return Gs1Validator.ToElementString(aiText, new Gs1ValidationOptions {
            AllowUnknownApplicationIdentifiers = true,
            ValidateAssociations = false,
            ValidateSemanticRules = false,
            ValidateCharacterSets = false,
            AllowEmptyVariableLengthData = true,
            AllowRedundantFnc1Separators = true
        });
    }

    /// <summary>Builds a GS1 element string from explicit elements.</summary>
    public static string ElementString(params Gs1Element[] elements) {
        return Gs1Validator.ToElementString(elements);
    }

    /// <summary>Encodes a GS1-128 barcode from bracketed syntax or a raw element string.</summary>
    public static Barcode1D Encode128(string aiText) {
        return Code128Encoder.EncodeGs1(ElementString(aiText));
    }

    /// <summary>Encodes a GS1-128 barcode from explicit elements.</summary>
    public static Barcode1D Encode128(params Gs1Element[] elements) {
        return Code128Encoder.EncodeGs1(ElementString(elements));
    }
}

/// <summary>Represents one GS1 element (AI and data) with explicit length semantics.</summary>
public readonly struct Gs1Element {
    /// <summary>Gets the Application Identifier.</summary>
    public string Ai { get; }

    /// <summary>Gets the AI data field.</summary>
    public string Data { get; }

    /// <summary>Gets whether FNC1 is required when another element follows.</summary>
    public bool IsVariableLength { get; }

    /// <summary>Gets the official definition when the AI is assigned in the bundled catalog.</summary>
    public Gs1ApplicationIdentifier? Definition { get; }

    internal Gs1Element(string ai, string data, bool isVariableLength) {
        Ai = ai ?? throw new ArgumentNullException(nameof(ai));
        Data = data ?? string.Empty;
        IsVariableLength = isVariableLength;
        Definition = null;
    }

    internal Gs1Element(string ai, string data, bool isVariableLength, Gs1ApplicationIdentifier? definition) {
        Ai = ai ?? throw new ArgumentNullException(nameof(ai));
        Data = data ?? string.Empty;
        IsVariableLength = isVariableLength;
        Definition = definition;
    }

    /// <summary>Creates an element using official catalog length and separator semantics.</summary>
    public static Gs1Element Create(string ai, string data) {
        var definition = Gs1ApplicationIdentifierCatalog.Get(ai);
        return new Gs1Element(ai, data, definition.RequiresFnc1Separator, definition);
    }

    /// <summary>Creates an expert-specified fixed-length element without catalog lookup.</summary>
    public static Gs1Element Fixed(string ai, string data) => new(ai, data, isVariableLength: false);

    /// <summary>Creates an expert-specified variable-length element without catalog lookup.</summary>
    public static Gs1Element Variable(string ai, string data) => new(ai, data, isVariableLength: true);

    /// <inheritdoc />
    public override string ToString() => $"({Ai}){Data}";
}
