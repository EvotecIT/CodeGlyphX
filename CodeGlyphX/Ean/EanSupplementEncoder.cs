using System;
using System.Collections.Generic;

namespace CodeGlyphX.Ean;

/// <summary>Encodes standalone EAN/UPC two- and five-digit supplements.</summary>
public static class EanSupplementEncoder {
    /// <summary>Encodes exactly two or five digits, retaining the supplement's format identity.</summary>
    public static Barcode1D Encode(string value) {
        if (value is null) throw new ArgumentNullException(nameof(value));
        return Encode(value, value.Length);
    }

    internal static Barcode1D Encode(string value, int digits) {
        if (value is null) throw new ArgumentNullException(nameof(value));
        if ((digits != 2 && digits != 5) || value.Length != digits)
            throw new ArgumentException("An EAN supplement requires exactly two or five digits.", nameof(value));
        var segments = new List<BarSegment>();
        EanAddOn.AppendAddOn(segments, value);
        // The shared attached-supplement encoder prefixes an inter-symbol space.
        segments.RemoveAt(0);
        return new Barcode1D(segments, digits == 2 ? SymbolFormat.Ean2 : SymbolFormat.Ean5);
    }
}
