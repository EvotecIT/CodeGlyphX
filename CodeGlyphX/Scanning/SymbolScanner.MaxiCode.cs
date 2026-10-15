using System.Collections.Generic;
using CodeGlyphX.MaxiCode;

namespace CodeGlyphX;

public static partial class SymbolScanner {
    private static void ScanMaxiCode(byte[] rgba, int width, int height, ImageRegion searchRegion,
        ScanOptions options, ScanDeadline deadline, ISet<SymbolFormat> requested,
        List<DetectedSymbol> results, HashSet<string>? seen, ref int remainingAttempts) {
        if (!requested.Contains(SymbolFormat.MaxiCode)) return;
        using var attempt = deadline.CreateAttempt(remainingAttempts--, options.Image?.RecognitionBudgetMilliseconds ?? 0);
        if (MaxiCodeImageDecoder.TryDecode(rgba, width, height, attempt.Token, out var decoded, out var inverted))
            Add(results, seen, SymbolResultFactory.From(decoded, searchRegion, inverted));
    }
}
