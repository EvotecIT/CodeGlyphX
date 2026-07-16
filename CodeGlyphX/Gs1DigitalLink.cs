using System;
using System.Collections.Generic;
using CodeGlyphX.Gs1Data;

namespace CodeGlyphX;

/// <summary>
/// Parses, validates, and builds uncompressed GS1 Digital Link URIs according to URI Syntax release 1.6.0.
/// </summary>
public static class Gs1DigitalLink {
    /// <summary>Gets the implemented GS1 Digital Link URI Syntax release.</summary>
    public const string SyntaxVersion = "1.6.0";

    /// <summary>Parses and validates an uncompressed GS1 Digital Link URI while collecting all detectable issues.</summary>
    public static Gs1DigitalLinkValidationResult Validate(string uri, Gs1ValidationOptions? options = null) {
        return Gs1DigitalLinkCodec.Validate(uri, options);
    }

    /// <summary>Parses a valid uncompressed GS1 Digital Link URI.</summary>
    /// <exception cref="FormatException">The URI or its GS1 data is invalid.</exception>
    public static Gs1DigitalLinkUri Parse(string uri, Gs1ValidationOptions? options = null) {
        var result = Validate(uri, options);
        if (result.IsValid) return result.Value!;

        if (result.Issues.Count > 0) throw new FormatException(result.Issues[0].Message);
        if (result.ElementValidation is not null && result.ElementValidation.Issues.Count > 0) {
            throw new FormatException(result.ElementValidation.Issues[0].Message);
        }
        throw new FormatException("The value is not a valid GS1 Digital Link URI.");
    }

    /// <summary>Attempts to parse and fully validate an uncompressed GS1 Digital Link URI.</summary>
    public static bool TryParse(string uri, out Gs1DigitalLinkUri result, Gs1ValidationOptions? options = null) {
        var validation = Validate(uri, options);
        result = validation.Value!;
        return validation.IsValid;
    }

    /// <summary>
    /// Builds and validates a GS1 Digital Link URI on the supplied HTTP or HTTPS URI stem.
    /// Key qualifiers are placed in standards-defined path order and remaining GS1 attributes are sorted lexically in the query.
    /// </summary>
    public static Gs1DigitalLinkUri Build(
        string uriStem,
        IReadOnlyList<Gs1Element> elements,
        string? primaryIdentifierAi = null,
        IReadOnlyDictionary<string, string>? extensionParameters = null,
        string? fragment = null,
        Gs1ValidationOptions? validationOptions = null) {
        return Gs1DigitalLinkCodec.Build(
            uriStem,
            elements,
            primaryIdentifierAi,
            extensionParameters,
            fragment,
            validationOptions);
    }

    /// <summary>Builds the canonical GS1 Digital Link URI on https://id.gs1.org.</summary>
    public static Gs1DigitalLinkUri BuildCanonical(
        IReadOnlyList<Gs1Element> elements,
        string? primaryIdentifierAi = null,
        Gs1ValidationOptions? validationOptions = null) {
        return Build(
            "https://id.gs1.org",
            elements,
            primaryIdentifierAi,
            extensionParameters: null,
            fragment: null,
            validationOptions);
    }

    /// <summary>Builds the canonical GS1 Digital Link URI on https://id.gs1.org.</summary>
    public static Gs1DigitalLinkUri BuildCanonical(params Gs1Element[] elements) {
        return BuildCanonical((IReadOnlyList<Gs1Element>)elements);
    }
}
