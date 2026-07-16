using System;
using System.Collections.Generic;

namespace CodeGlyphX.Gs1Data;

/// <summary>
/// Describes one component of a GS1 Application Identifier data field.
/// </summary>
public sealed class Gs1DataFormatComponent {
    private readonly IReadOnlyList<string> _linters;

    /// <summary>Gets the component character repertoire.</summary>
    public Gs1DataCharacterSet CharacterSet { get; }

    /// <summary>Gets the minimum component length.</summary>
    public int MinimumLength { get; }

    /// <summary>Gets the maximum component length.</summary>
    public int MaximumLength { get; }

    /// <summary>Gets whether the complete component may be omitted.</summary>
    public bool IsOptional { get; }

    /// <summary>Gets semantic linter names assigned by the GS1 Syntax Dictionary.</summary>
    public IReadOnlyList<string> Linters => _linters;

    internal Gs1DataFormatComponent(
        Gs1DataCharacterSet characterSet,
        int minimumLength,
        int maximumLength,
        bool isOptional,
        string[] linters) {
        if (minimumLength < 0) throw new ArgumentOutOfRangeException(nameof(minimumLength));
        if (maximumLength < minimumLength) throw new ArgumentOutOfRangeException(nameof(maximumLength));
        CharacterSet = characterSet;
        MinimumLength = minimumLength;
        MaximumLength = maximumLength;
        IsOptional = isOptional;
        _linters = Array.AsReadOnly(linters ?? throw new ArgumentNullException(nameof(linters)));
    }
}
