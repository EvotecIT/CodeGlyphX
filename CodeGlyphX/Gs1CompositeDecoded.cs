using System;

namespace CodeGlyphX;

/// <summary>Contains both messages recovered from a GS1 Composite symbol.</summary>
public sealed class Gs1CompositeDecoded {
    /// <summary>Gets the primary text returned by generic decoding facades.</summary>
    public string Text => CompositeText;
    /// <summary>Gets the linear GS1 element string.</summary>
    public string LinearText { get; }
    /// <summary>Gets the two-dimensional GS1 element string.</summary>
    public string CompositeText { get; }
    /// <summary>Gets the detected composite component.</summary>
    public Gs1CompositeComponent Component { get; }

    internal Gs1CompositeDecoded(string linearText, string compositeText, Gs1CompositeComponent component) {
        LinearText = linearText ?? throw new ArgumentNullException(nameof(linearText));
        CompositeText = compositeText ?? throw new ArgumentNullException(nameof(compositeText));
        Component = component;
    }
}
