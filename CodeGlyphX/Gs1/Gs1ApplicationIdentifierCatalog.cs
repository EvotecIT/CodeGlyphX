using System;
using System.Collections.Generic;

namespace CodeGlyphX.Gs1Data;

/// <summary>
/// Versioned catalog of every assigned GS1 Application Identifier in the bundled GS1 Syntax Dictionary release.
/// </summary>
public static partial class Gs1ApplicationIdentifierCatalog {
    private static readonly Gs1ApplicationIdentifier[] Definitions = CreateDefinitions();
    private static readonly IReadOnlyList<Gs1ApplicationIdentifier> ReadOnlyDefinitions = Array.AsReadOnly(Definitions);
    private static readonly Dictionary<string, Gs1ApplicationIdentifier> ByAi = CreateLookup();

    /// <summary>Gets all assigned identifiers, including expanded members of official AI ranges.</summary>
    public static IReadOnlyList<Gs1ApplicationIdentifier> All => ReadOnlyDefinitions;

    /// <summary>Looks up an assigned Application Identifier.</summary>
    /// <param name="ai">Two-, three-, or four-digit AI.</param>
    /// <param name="definition">Receives the official definition when assigned.</param>
    /// <returns><see langword="true"/> when the AI is assigned in this catalog release.</returns>
    public static bool TryGet(string ai, out Gs1ApplicationIdentifier definition) {
        if (ai is null) throw new ArgumentNullException(nameof(ai));
        return ByAi.TryGetValue(ai, out definition!);
    }

    /// <summary>Gets an assigned Application Identifier or throws when the AI is unknown.</summary>
    public static Gs1ApplicationIdentifier Get(string ai) {
        if (TryGet(ai, out var definition)) return definition;
        throw new KeyNotFoundException($"GS1 Application Identifier '{ai}' is not assigned in dictionary release {Release}.");
    }

    internal static bool TryMatch(string value, int offset, out Gs1ApplicationIdentifier definition) {
        var remaining = value.Length - offset;
        var maximum = Math.Min(4, remaining);
        for (var length = maximum; length >= 2; length--) {
            var candidate = value.Substring(offset, length);
            if (ByAi.TryGetValue(candidate, out definition!)) return true;
        }
        definition = null!;
        return false;
    }

    private static Dictionary<string, Gs1ApplicationIdentifier> CreateLookup() {
        var lookup = new Dictionary<string, Gs1ApplicationIdentifier>(Definitions.Length, StringComparer.Ordinal);
        for (var i = 0; i < Definitions.Length; i++) {
            lookup.Add(Definitions[i].Ai, Definitions[i]);
        }
        return lookup;
    }
}
