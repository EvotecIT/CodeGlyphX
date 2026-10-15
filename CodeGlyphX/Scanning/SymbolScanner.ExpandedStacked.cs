using System.Collections.Generic;
using CodeGlyphX.Internal;

namespace CodeGlyphX;

public static partial class SymbolScanner {
    private static void ScanExpandedStacked(byte[] rgba, int width, int height, ImageRegion region,
        ScanOptions options, ScanDeadline deadline, ISet<SymbolFormat> requested,
        List<DetectedSymbol> results, HashSet<string>? seen, ref int remainingAttempts) {
        if (!requested.Contains(SymbolFormat.Gs1DataBarExpandedStacked)) return;
        using var attempt = deadline.CreateAttempt(remainingAttempts--, options.Image?.RecognitionBudgetMilliseconds ?? 0);
        // A one-row Expanded symbol has the same physical encoding in both APIs.
        // Mixed/default recognition assigns it to the linear format once.
        var allowSingleRow = !requested.Contains(SymbolFormat.Gs1DataBarExpanded);
        if (BarcodeScanline.TryDecodeExpandedStacked(rgba, width, height, allowSingleRow, attempt.Token, out var content))
            Add(results, seen, SymbolResultFactory.From(new BarcodeDecoded(BarcodeType.GS1DataBarExpandedStacked, content), region));
    }
}
