using System.Collections.Generic;
using CodeGlyphX.Ultracode;

namespace CodeGlyphX;

public static partial class SymbolScanner {
    // Colour localization has its own component pass and perspective sampling. Reserve
    // several shares, as for other matrix recognizers, rather than a linear-format share.
    private const int UltracodeAttemptWeight = 4;
    private static void ScanUltracode(byte[] rgba, int width, int height, ImageRegion searchRegion,
        ScanOptions options, ScanDeadline deadline, ISet<SymbolFormat> requested,
        List<DetectedSymbol> results, HashSet<string>? seen, ref int remainingAttempts) {
        if (!requested.Contains(SymbolFormat.Ultracode)) return;
        using var attempt = deadline.CreateAttempt(remainingAttempts, options.Image?.RecognitionBudgetMilliseconds ?? 0, UltracodeAttemptWeight);
        remainingAttempts -= UltracodeAttemptWeight;
        ScanUltracodeCore(rgba, width, height, searchRegion, options, attempt, results, seen);
    }

    private static void ScanUltracodeCore(byte[] rgba, int width, int height, ImageRegion searchRegion,
        ScanOptions options, ScanDeadline deadline, List<DetectedSymbol> results, HashSet<string>? seen) {
        var completed = UltracodeImageDecoder.Visit(rgba, width, height, options.SamplingGrid, deadline.Token, (decoded, geometry) => {
            Add(results, seen, WithImageEvidence(SymbolResultFactory.From(decoded), searchRegion, geometry, width, height, false, false));
            return ShouldStop(options, deadline, results);
        });
        if (!completed) deadline.MarkResourceLimitReached();
    }
}
