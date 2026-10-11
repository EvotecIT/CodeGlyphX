using System;
using System.Globalization;

namespace CodeGlyphX.Benchmarks;

// The library exposes the secondary message and structured carrier fields separately. The upstream
// corpus and the other readers present both in one string. Compare the same information losslessly.
internal static class CorpusPresentation {
    internal static string Text(DetectedSymbol symbol) {
        if (symbol.Metadata is not MaxiCodeSymbolMetadata carrier || carrier.PostalCode is null ||
            !carrier.CountryCode.HasValue || !carrier.ServiceClass.HasValue) return symbol.Text;
        var primary = carrier.PostalCode + '\u001d' + carrier.CountryCode.Value.ToString("D3", CultureInfo.InvariantCulture) +
            '\u001d' + carrier.ServiceClass.Value.ToString("D3", CultureInfo.InvariantCulture) + '\u001d';
        return symbol.Text.StartsWith("[)>\u001e01\u001d", StringComparison.Ordinal) && symbol.Text.Length >= 9
            ? symbol.Text.Insert(9, primary) : primary + symbol.Text;
    }
}
