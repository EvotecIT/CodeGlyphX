using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CodeGlyphX.Gs1Data;

namespace CodeGlyphX;

/// <summary>Represents one validated, uncompressed GS1 Digital Link URI.</summary>
public sealed class Gs1DigitalLinkUri {
    private readonly IReadOnlyList<Gs1Element> _keyQualifiers;
    private readonly IReadOnlyList<Gs1Element> _dataAttributes;
    private readonly IReadOnlyList<Gs1Element> _elements;
    private readonly IReadOnlyDictionary<string, string> _extensionParameters;

    /// <summary>Gets the parsed Web URI.</summary>
    public Uri Uri { get; }

    /// <summary>Gets the original validated URI spelling before <see cref="System.Uri"/> normalization.</summary>
    public string OriginalUri { get; }

    /// <summary>Gets the scheme, authority, and optional custom path preceding the GS1 path.</summary>
    public string UriStem { get; }

    /// <summary>Gets the GS1 primary identifier carried in the URI path.</summary>
    public Gs1Element PrimaryIdentifier { get; }

    /// <summary>Gets ordered key qualifiers carried in the URI path.</summary>
    public IReadOnlyList<Gs1Element> KeyQualifiers => _keyQualifiers;

    /// <summary>Gets GS1 data attributes carried in the query string.</summary>
    public IReadOnlyList<Gs1Element> DataAttributes => _dataAttributes;

    /// <summary>Gets all GS1 elements in path order followed by query order.</summary>
    public IReadOnlyList<Gs1Element> Elements => _elements;

    /// <summary>Gets non-GS1 query parameters, such as resolver link controls.</summary>
    public IReadOnlyDictionary<string, string> ExtensionParameters => _extensionParameters;

    /// <summary>Gets the decoded fragment identifier, or <see langword="null"/> when absent.</summary>
    public string? Fragment { get; }

    /// <summary>Gets the canonical URI using HTTPS, id.gs1.org, and lexically ordered GS1 query attributes.</summary>
    public string CanonicalUri { get; }

    /// <summary>Gets whether the input URI already has canonical GS1 Digital Link form.</summary>
    public bool IsCanonical { get; }

    internal Gs1DigitalLinkUri(
        Uri uri,
        string originalUri,
        string uriStem,
        Gs1Element primaryIdentifier,
        Gs1Element[] keyQualifiers,
        Gs1Element[] dataAttributes,
        Gs1Element[] elements,
        IDictionary<string, string> extensionParameters,
        string? fragment,
        string canonicalUri) {
        Uri = uri ?? throw new ArgumentNullException(nameof(uri));
        OriginalUri = originalUri ?? throw new ArgumentNullException(nameof(originalUri));
        UriStem = uriStem ?? throw new ArgumentNullException(nameof(uriStem));
        PrimaryIdentifier = primaryIdentifier;
        _keyQualifiers = Array.AsReadOnly(keyQualifiers ?? throw new ArgumentNullException(nameof(keyQualifiers)));
        _dataAttributes = Array.AsReadOnly(dataAttributes ?? throw new ArgumentNullException(nameof(dataAttributes)));
        _elements = Array.AsReadOnly(elements ?? throw new ArgumentNullException(nameof(elements)));
        _extensionParameters = new ReadOnlyDictionary<string, string>(
            new Dictionary<string, string>(extensionParameters ?? throw new ArgumentNullException(nameof(extensionParameters)), StringComparer.Ordinal));
        Fragment = fragment;
        CanonicalUri = canonicalUri ?? throw new ArgumentNullException(nameof(canonicalUri));
        IsCanonical = string.Equals(OriginalUri, CanonicalUri, StringComparison.Ordinal);
    }

    /// <summary>Builds the equivalent GS1 element string.</summary>
    public string ToElementString() => Gs1Validator.ToElementString(_elements);

    /// <inheritdoc />
    public override string ToString() => OriginalUri;
}
