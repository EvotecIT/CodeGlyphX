using System;

namespace CodeGlyphX;

/// <summary>Classifies a structural or URI-level GS1 Digital Link validation problem.</summary>
public enum Gs1DigitalLinkIssueCode {
    /// <summary>The input is not a well-formed absolute URI.</summary>
    MalformedUri,
    /// <summary>The URI does not use the HTTP or HTTPS scheme.</summary>
    UnsupportedScheme,
    /// <summary>No GS1 Digital Link primary identifier was found in the URI path.</summary>
    MissingPrimaryIdentifier,
    /// <summary>The GS1 path does not follow the primary-key and key-qualifier grammar.</summary>
    InvalidPath,
    /// <summary>The URI query string does not follow GS1 Digital Link rules.</summary>
    InvalidQuery,
    /// <summary>A percent-encoded URI component is malformed or is not valid UTF-8.</summary>
    InvalidPercentEncoding,
    /// <summary>An Application Identifier occurs more than once.</summary>
    DuplicateApplicationIdentifier,
    /// <summary>An Application Identifier is not assigned in the bundled GS1 dictionary release.</summary>
    UnknownApplicationIdentifier,
    /// <summary>An Application Identifier value failed GS1 syntax or semantic validation.</summary>
    InvalidApplicationIdentifier,
    /// <summary>A non-GS1 extension query parameter is malformed or duplicated.</summary>
    InvalidExtensionParameter
}

/// <summary>Describes one actionable GS1 Digital Link URI validation problem.</summary>
public sealed class Gs1DigitalLinkIssue {
    /// <summary>Gets the issue classification.</summary>
    public Gs1DigitalLinkIssueCode Code { get; }

    /// <summary>Gets the affected Application Identifier or URI component, when known.</summary>
    public string? Component { get; }

    /// <summary>Gets the human-readable diagnostic.</summary>
    public string Message { get; }

    internal Gs1DigitalLinkIssue(Gs1DigitalLinkIssueCode code, string? component, string message) {
        Code = code;
        Component = component;
        Message = message ?? throw new ArgumentNullException(nameof(message));
    }

    /// <inheritdoc />
    public override string ToString() => Component is null ? Message : $"{Component}: {Message}";
}
