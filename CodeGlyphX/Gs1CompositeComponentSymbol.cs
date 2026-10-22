namespace CodeGlyphX;

/// <summary>A standalone GS1 Composite two-dimensional component, before linking to its linear carrier.</summary>
public sealed class Gs1CompositeComponentSymbol : MatrixSymbol {
    /// <summary>Gets the component's GS1 element string.</summary>
    public string Text { get; }
    /// <summary>Gets the resolved CC-A, CC-B or CC-C type.</summary>
    public Gs1CompositeComponent Component { get; }
    /// <summary>Gets the number of data columns.</summary>
    public int Columns { get; }
    internal Gs1CompositeComponentSymbol(BitMatrix modules, string text, Gs1CompositeComponent component, int columns)
        : base(SymbolFormat.Gs1Composite, modules) {
        Text = text; Component = component; Columns = columns;
    }
}
