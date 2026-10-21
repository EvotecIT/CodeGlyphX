using System;

namespace CodeGlyphX;

/// <summary>Both component messages and available component geometry recovered from a GS1 Composite symbol.</summary>
public sealed class Gs1CompositeSymbolMetadata : SymbolMetadata {
    private readonly Gs1CompositeDecoded _value;
    private readonly int _leadingModules;

    /// <summary>Gets the linear component's GS1 element string.</summary>
    public string LinearText => _value.LinearText;
    /// <summary>Gets the two-dimensional component's GS1 element string.</summary>
    public string CompositeText => _value.CompositeText;
    /// <summary>Gets the composite component type.</summary>
    public Gs1CompositeComponent Component => _value.Component;
    /// <summary>Gets the linked carrier, or null for a standalone component.</summary>
    public Gs1CompositeCarrier? Carrier => _value.Carrier;
    /// <summary>Gets whether only a two-dimensional component was recovered.</summary>
    public bool IsStandalone => _value.IsStandalone;
    /// <summary>Gets the two-dimensional component quadrilateral in source-image pixels, or null for sampled-module decoding.</summary>
    public SymbolGeometry? ComponentGeometry { get; }
    /// <summary>Gets the linear carrier quadrilateral in source-image pixels, or null for a standalone component or sampled-module decoding.</summary>
    /// <remarks>The extent includes the carrier's data rows and internal separators, excluding the separator above the carrier.</remarks>
    public SymbolGeometry? LinearGeometry { get; }
    internal ImageRegion? ComponentModuleRegion => _value.ComponentRegion;

    internal Gs1CompositeSymbolMetadata(Gs1CompositeDecoded value, int leadingModules = 0,
        SymbolGeometry? componentGeometry = null, SymbolGeometry? linearGeometry = null) {
        _value = value;
        _leadingModules = leadingModules;
        ComponentGeometry = componentGeometry;
        LinearGeometry = linearGeometry;
    }

    internal Gs1CompositeSymbolMetadata WithLeadingModules(int count) => new(_value, count);

    internal Gs1CompositeSymbolMetadata ProjectModules(Func<SymbolPoint, SymbolPoint> map) => new(_value, _leadingModules,
        ProjectRegion(_value.ComponentRegion, point => map(new SymbolPoint(point.X - _leadingModules, point.Y))),
        ProjectRegion(_value.LinearRegion, point => map(new SymbolPoint(point.X - _leadingModules, point.Y))));

    internal Gs1CompositeSymbolMetadata MapImageGeometry(Func<SymbolPoint, SymbolPoint> map) => new(_value, _leadingModules,
        Map(ComponentGeometry, map), Map(LinearGeometry, map));

    private static SymbolGeometry? ProjectRegion(ImageRegion? region, Func<SymbolPoint, SymbolPoint> map) =>
        region.HasValue ? Map(SymbolGeometry.FromBounds(new SymbolBounds(region.Value.X, region.Value.Y,
            region.Value.Width, region.Value.Height)), map) : null;

    private static SymbolGeometry? Map(SymbolGeometry? geometry, Func<SymbolPoint, SymbolPoint> map) => geometry is null ? null
        : new SymbolGeometry(map(geometry.TopLeft), map(geometry.TopRight), map(geometry.BottomRight), map(geometry.BottomLeft));
}
