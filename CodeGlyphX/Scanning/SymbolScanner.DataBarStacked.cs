using System.Collections.Generic;
using CodeGlyphX.DataBar;
using CodeGlyphX.Internal;

namespace CodeGlyphX;

public static partial class SymbolScanner {
    private static void ScanDataBarStacked(byte[] rgba, int width, int height, ImageRegion region,
        ScanOptions options, ScanDeadline deadline, ISet<SymbolFormat> requested,
        List<DetectedSymbol> results, HashSet<string>? seen, ref int remainingAttempts) {
        var stacked = requested.Contains(SymbolFormat.Gs1DataBarStacked);
        var omni = requested.Contains(SymbolFormat.Gs1DataBarStackedOmnidirectional);
        if (!stacked && !omni) return;
        using var attempt = deadline.CreateAttempt(remainingAttempts--, options.Image?.RecognitionBudgetMilliseconds ?? 0);
        if (DataBar14ImageDecoder.TryDecode(rgba, width, height, stacked, omni, attempt.Token, out var content, out var isOmni))
            Add(results, seen, SymbolResultFactory.From(new BarcodeDecoded(isOmni ? BarcodeType.GS1DataBarStackedOmni : BarcodeType.GS1DataBarStacked, content), region));
    }
}
