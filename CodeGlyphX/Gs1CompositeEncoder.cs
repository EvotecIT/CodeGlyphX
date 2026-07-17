using System;
using CodeGlyphX.Code128;
using CodeGlyphX.Gs1Composite;

namespace CodeGlyphX;

/// <summary>Encodes standards-linked GS1-128 Composite symbols.</summary>
public static class Gs1CompositeEncoder {
    /// <summary>Encodes a GS1-128 linear message and its associated two-dimensional message.</summary>
    /// <param name="linearText">The GS1 data carried by the GS1-128 component.</param>
    /// <param name="compositeText">The GS1 data carried by CC-A, CC-B, or CC-C.</param>
    /// <param name="options">Optional component selection.</param>
    public static Gs1CompositeSymbol Encode(string linearText, string compositeText,
        Gs1CompositeEncodingOptions? options = null) {
        options ??= new Gs1CompositeEncodingOptions();
        var linear = Gs1.ElementString(linearText);
        var composite = Gs1.ElementString(compositeText);
        if (linear.Length == 0) throw new ArgumentException("The linear component cannot be empty.", nameof(linearText));
        if (composite.Length == 0) throw new ArgumentException("The two-dimensional component cannot be empty.", nameof(compositeText));

        var sizingLinear = Code128Encoder.EncodeGs1Composite(linear, ccC: false);
        var requested = options.Component;
        if (requested == Gs1CompositeComponent.Auto) {
            if (TryEncode(linear, composite, sizingLinear.TotalModules, Gs1CompositeComponent.CcA, out var smallest)) return smallest;
            if (TryEncode(linear, composite, sizingLinear.TotalModules, Gs1CompositeComponent.CcB, out smallest)) return smallest;
            return EncodeSelected(linear, composite, sizingLinear.TotalModules, Gs1CompositeComponent.CcC);
        }
        return EncodeSelected(linear, composite, sizingLinear.TotalModules, requested);
    }

    private static bool TryEncode(string linear, string composite, int linearWidth,
        Gs1CompositeComponent component, out Gs1CompositeSymbol symbol) {
        try {
            symbol = EncodeSelected(linear, composite, linearWidth, component);
            return true;
        } catch (ArgumentException ex) when (ex.ParamName == "elementString") {
            symbol = null!;
            return false;
        }
    }

    private static Gs1CompositeSymbol EncodeSelected(string linear, string composite, int linearWidth,
        Gs1CompositeComponent component) {
        var initialColumns = component == Gs1CompositeComponent.CcC ? 1 : 4;
        var bits = CompositeBitStreamCodec.Encode(composite, component, initialColumns, linearWidth,
            out var columns, out var errorCorrectionLevel);
        var twoDimensional = CompositeComponentCodec.Encode(bits, component, columns, errorCorrectionLevel);
        var linearBarcode = Code128Encoder.EncodeGs1Composite(linear, component == Gs1CompositeComponent.CcC);
        var modules = Merge(twoDimensional, linearBarcode, component);
        return new Gs1CompositeSymbol(modules, linear, composite, component, twoDimensional.Height);
    }

    private static BitMatrix Merge(BitMatrix component, Barcode1D linear, Gs1CompositeComponent type) {
        var linearModules = ToModules(linear);
        var topShift = 0;
        var bottomShift = 0;
        if (type == Gs1CompositeComponent.CcC) {
            bottomShift = 7;
        } else {
            var numberOfSymbols = (linearModules.Length - 2) / 11;
            var position = (numberOfSymbols - 9) / 2;
            var shift = linearModules.Length - position * 11 - 1 - component.Width;
            if (position != 0) shift -= 2;
            if (shift > 0) topShift = shift;
            else bottomShift = -shift;
        }

        var width = Math.Max(component.Width + topShift, linearModules.Length + bottomShift);
        var matrix = new BitMatrix(width, component.Height + 2);
        for (var y = 0; y < component.Height; y++) {
            for (var x = 0; x < component.Width; x++) if (component[x, y]) matrix[x + topShift, y] = true;
        }
        var separatorRow = component.Height;
        var linearRow = separatorRow + 1;
        for (var x = 0; x < linearModules.Length; x++) {
            matrix[x + bottomShift, separatorRow] = !linearModules[x];
            matrix[x + bottomShift, linearRow] = linearModules[x];
        }
        return matrix;
    }

    private static bool[] ToModules(Barcode1D barcode) {
        var modules = new bool[barcode.TotalModules];
        var position = 0;
        for (var i = 0; i < barcode.Segments.Count; i++) {
            var segment = barcode.Segments[i];
            for (var j = 0; j < segment.Modules; j++) modules[position++] = segment.IsBar;
        }
        return modules;
    }
}
