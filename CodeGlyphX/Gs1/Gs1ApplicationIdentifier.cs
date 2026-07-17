using System;
using System.Collections.Generic;

namespace CodeGlyphX.Gs1Data;

/// <summary>
/// Official metadata and syntax rules for one assigned GS1 Application Identifier.
/// </summary>
public sealed class Gs1ApplicationIdentifier {
    private readonly IReadOnlyList<Gs1DataFormatComponent> _components;
    private readonly IReadOnlyList<string> _requiredAssociations;
    private readonly IReadOnlyList<string> _excludedAssociations;

    /// <summary>Gets the two-, three-, or four-digit Application Identifier.</summary>
    public string Ai { get; }

    /// <summary>Gets the short title assigned by GS1.</summary>
    public string Title { get; }

    /// <summary>Gets the official compact data-format expression.</summary>
    public string Format { get; }

    /// <summary>Gets whether the AI and data field have predefined total length and do not need a trailing FNC1 separator.</summary>
    public bool HasPredefinedLength { get; }

    /// <summary>Gets whether FNC1 is required when another element follows this field.</summary>
    public bool RequiresFnc1Separator => !HasPredefinedLength;

    /// <summary>Gets whether the AI is permitted as a GS1 Digital Link data attribute.</summary>
    public bool IsDigitalLinkDataAttribute { get; }

    /// <summary>Gets the minimum permitted data length.</summary>
    public int MinimumDataLength { get; }

    /// <summary>Gets the maximum permitted data length.</summary>
    public int MaximumDataLength { get; }

    /// <summary>Gets the data-field components in payload order.</summary>
    public IReadOnlyList<Gs1DataFormatComponent> Components => _components;

    /// <summary>
    /// Gets mandatory association expressions. Each expression contains comma-separated alternatives;
    /// a plus sign joins AIs that must occur together.
    /// </summary>
    public IReadOnlyList<string> RequiredAssociations => _requiredAssociations;

    /// <summary>Gets AI patterns that cannot occur in the same message.</summary>
    public IReadOnlyList<string> ExcludedAssociations => _excludedAssociations;

    /// <summary>Gets the Digital Link primary-key qualifier expression, or <see langword="null"/> when this AI is not a primary key.</summary>
    public string? DigitalLinkPrimaryKeyQualifiers { get; }

    /// <summary>Gets whether the AI is a GS1 Digital Link primary key.</summary>
    public bool IsDigitalLinkPrimaryKey { get; }

    internal Gs1ApplicationIdentifier(
        string ai,
        string title,
        string format,
        bool hasPredefinedLength,
        bool isDigitalLinkDataAttribute,
        string attributes) {
        Ai = ai ?? throw new ArgumentNullException(nameof(ai));
        Title = title ?? throw new ArgumentNullException(nameof(title));
        Format = format ?? throw new ArgumentNullException(nameof(format));
        HasPredefinedLength = hasPredefinedLength;
        IsDigitalLinkDataAttribute = isDigitalLinkDataAttribute;

        var components = Gs1SyntaxRules.ParseFormat(format);
        _components = Array.AsReadOnly(components);
        var minimumLength = 0;
        var maximumLength = 0;
        for (var i = 0; i < components.Length; i++) {
            if (!components[i].IsOptional) minimumLength += components[i].MinimumLength;
            maximumLength += components[i].MaximumLength;
        }
        MinimumDataLength = minimumLength;
        MaximumDataLength = maximumLength;

        Gs1SyntaxRules.ParseAttributes(
            attributes,
            out var required,
            out var excluded,
            out var isDigitalLinkPrimaryKey,
            out var digitalLinkQualifiers);
        _requiredAssociations = Array.AsReadOnly(required);
        _excludedAssociations = Array.AsReadOnly(excluded);
        IsDigitalLinkPrimaryKey = isDigitalLinkPrimaryKey;
        DigitalLinkPrimaryKeyQualifiers = digitalLinkQualifiers;
    }

    /// <inheritdoc />
    public override string ToString() => $"({Ai}) {Title}";
}
