using System;
using System.Collections.Generic;

namespace CodeGlyphX.DataBar;

/// <summary>
/// Encodes GS1 DataBar Expanded and GS1 DataBar Expanded Stacked symbols.
/// </summary>
public static class DataBarExpandedEncoder {
    /// <summary>
    /// Encodes a GS1 element string as a linear GS1 DataBar Expanded symbol.
    /// </summary>
    /// <param name="value">A parenthesized GS1 AI string or raw GS1 element string.</param>
    /// <returns>The barcode's alternating light and dark module runs.</returns>
    public static Barcode1D EncodeExpanded(string value) {
        var symbol = DataBarExpandedSymbol.Create(value, requireEvenTotalCharacters: false);
        var segments = new List<BarSegment>(symbol.Elements.Length);
        var isBar = false;
        for (var i = 0; i < symbol.Elements.Length; i++) {
            segments.Add(new BarSegment(isBar, symbol.Elements[i]));
            isBar = !isBar;
        }
        return new Barcode1D(segments);
    }

    /// <summary>
    /// Encodes a GS1 element string as a multi-row GS1 DataBar Expanded Stacked symbol.
    /// </summary>
    /// <param name="value">A parenthesized GS1 AI string or raw GS1 element string.</param>
    /// <param name="columns">The number of finder/data-character blocks per row, from 1 through 10.</param>
    /// <returns>The stacked module matrix.</returns>
    public static BitMatrix EncodeExpandedStacked(string value, int columns = 2) {
        if (columns < 1 || columns > 10) throw new ArgumentOutOfRangeException(nameof(columns));
        var symbol = DataBarExpandedSymbol.Create(value, requireEvenTotalCharacters: false);
        if (symbol.TotalCharacters % (columns * 2) == 1) {
            symbol = DataBarExpandedSymbol.Create(value, requireEvenTotalCharacters: true);
        }
        return DataBarExpandedStacking.Build(symbol.Elements, symbol.TotalCharacters, columns);
    }
}
