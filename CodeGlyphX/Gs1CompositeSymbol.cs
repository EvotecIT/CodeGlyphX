using System;

namespace CodeGlyphX;

/// <summary>Represents a standards-linked GS1-128 Composite symbol.</summary>
public sealed class Gs1CompositeSymbol {
    /// <summary>Gets the complete composite module matrix.</summary>
    public BitMatrix Modules { get; }
    /// <summary>Gets the linear GS1 element string.</summary>
    public string LinearText { get; }
    /// <summary>Gets the two-dimensional GS1 element string.</summary>
    public string CompositeText { get; }
    /// <summary>Gets the selected composite component.</summary>
    public Gs1CompositeComponent Component { get; }
    /// <summary>Gets the number of rows in the two-dimensional component.</summary>
    public int ComponentRows { get; }

    internal Gs1CompositeSymbol(BitMatrix modules, string linearText, string compositeText,
        Gs1CompositeComponent component, int componentRows) {
        Modules = modules ?? throw new ArgumentNullException(nameof(modules));
        LinearText = linearText ?? throw new ArgumentNullException(nameof(linearText));
        CompositeText = compositeText ?? throw new ArgumentNullException(nameof(compositeText));
        Component = component;
        ComponentRows = componentRows;
    }
}
