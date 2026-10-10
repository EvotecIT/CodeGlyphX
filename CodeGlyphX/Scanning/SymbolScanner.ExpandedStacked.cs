using System.Collections.Generic;
using CodeGlyphX.Internal;

namespace CodeGlyphX;

public static partial class SymbolScanner {
    private static void ScanExpandedStacked(byte[] rgba, int width, int height, ImageRegion region,
        ScanOptions options, ScanDeadline deadline, ISet<SymbolFormat> requested,
        List<DetectedSymbol> results, HashSet<string>? seen, ref int remainingAttempts) {
        if (!requested.Contains(SymbolFormat.Gs1DataBarExpandedStacked)) return;
        using var attempt = deadline.CreateAttempt(remainingAttempts--, options.Image?.RecognitionBudgetMilliseconds ?? 0);
        if (BarcodeScanline.TryDecodeExpandedStacked(rgba, width, height, attempt.Token, out var content))
            Add(results, seen, SymbolResultFactory.From(new BarcodeDecoded(BarcodeType.GS1DataBarExpandedStacked, content), region));
    }
}
