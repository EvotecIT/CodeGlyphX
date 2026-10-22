using System;
using CodeGlyphX.Gs1Composite;

namespace CodeGlyphX;

/// <summary>Encodes and decodes standalone CC-A, CC-B and CC-C components for a GS1 Composite workflow.</summary>
/// <remarks>A standalone component has no associated linear message. Use Gs1CompositeEncoder to create a linked label.</remarks>
public static class Gs1CompositeComponents {
    /// <summary>Encodes GS1 data into a component with the requested data-column count.</summary>
    /// <param name="text">Bracketed GS1 data or a raw element string.</param>
    /// <param name="component">CC-A, CC-B or CC-C.</param>
    /// <param name="columns">Two through four for CC-A/B; one through thirty for CC-C.</param>
    public static Gs1CompositeComponentSymbol Encode(string text, Gs1CompositeComponent component, int columns = 4) {
        if (component is < Gs1CompositeComponent.CcA or > Gs1CompositeComponent.CcC)
            throw new ArgumentOutOfRangeException(nameof(component));
        if (columns < (component == Gs1CompositeComponent.CcC ? 1 : 2) || columns > (component == Gs1CompositeComponent.CcC ? 30 : 4))
            throw new ArgumentOutOfRangeException(nameof(columns));
        var elementString = Gs1.ElementString(text);
        var bits = CompositeBitStreamCodec.Encode(elementString, component, columns,
            component == Gs1CompositeComponent.CcC ? columns * 17 + 52 : 0, out var resolvedColumns, out var level);
        if (resolvedColumns != columns) throw new ArgumentException("The payload exceeds the requested component column count.", nameof(text));
        var modules = CompositeComponentCodec.Encode(bits, component, resolvedColumns, level);
        return new Gs1CompositeComponentSymbol(modules, elementString, component, resolvedColumns);
    }

    /// <summary>Checks the component's framing and error correction, then recovers its GS1 element string and type.</summary>
    public static bool TryDecode(BitMatrix modules, out string text, out Gs1CompositeComponent component) {
        return TryDecodeCore(modules, out text, out component, tolerateSamplingErrors: false);
    }

    internal static bool TryDecodeImage(BitMatrix modules, out string text, out Gs1CompositeComponent component) =>
        TryDecodeCore(modules, out text, out component, tolerateSamplingErrors: true);

    private static bool TryDecodeCore(BitMatrix modules, out string text, out Gs1CompositeComponent component, bool tolerateSamplingErrors) {
        text = string.Empty;
        component = Gs1CompositeComponent.Auto;
        if (modules is null || modules.Width > 579 || modules.Height > 90) return false;
        if (!CompositeComponentCodec.TryDecode(modules, out var bits, out var detected, tolerateSamplingErrors)
            || !CompositeBitStreamCodec.TryDecode(bits, out text)) return false;
        component = detected;
        return true;
    }
}
