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

    /// <summary>Gets the associated carrier, or null for a standalone two-dimensional component.</summary>
    public Gs1CompositeCarrier? Carrier { get; }

    /// <summary>Gets whether this result contains only a two-dimensional component.</summary>
    public bool IsStandalone => !Carrier.HasValue;

    internal ImageRegion? ComponentRegion { get; }
    internal ImageRegion? LinearRegion { get; }

    internal Gs1CompositeDecoded(string linearText, string compositeText, Gs1CompositeComponent component,
        Gs1CompositeCarrier? carrier = Gs1CompositeCarrier.Gs1_128,
        ImageRegion? componentRegion = null, ImageRegion? linearRegion = null) {
        LinearText = linearText ?? throw new ArgumentNullException(nameof(linearText));
        CompositeText = compositeText ?? throw new ArgumentNullException(nameof(compositeText));
        Component = component;
        Carrier = carrier;
        ComponentRegion = componentRegion;
        LinearRegion = linearRegion;
    }
}
