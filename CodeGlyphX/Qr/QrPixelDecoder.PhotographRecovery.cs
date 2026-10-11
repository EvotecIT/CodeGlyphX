#if NET8_0_OR_GREATER
using System;
using System.Collections.Generic;

namespace CodeGlyphX.Qr;

internal static partial class QrPixelDecoder {
    // Try one high-quality finder triple before broad threshold/transform searches
    // consume a short allowance. Keep this extra preprocessing bounded to small
    // images whose primary attempt already found plausible finder geometry.
    private static bool TryDecodeNormalizedPhotograph(int scale, QrGrayImage image, QrProfileSettings settings,
        List<QrFinderPatternDetector.FinderPattern> candidates, QrPixelDecodeDiagnostics primary,
        DecodeBudget budget, Func<QrDecoded, bool>? accept, QrGrayImagePool? pool,
        out QrDecoded result, out QrPixelDecodeDiagnostics diagnostics) {
        result = null!;
        diagnostics = default;
        if (!settings.AllowNormalize || !settings.AllowAdaptiveThreshold ||
            Math.Max(image.Width, image.Height) > 900 || primary.CandidateCount is < 3 or > 12 ||
            primary.CandidateTriplesTried == 0 || budget.IsNearDeadline(150)) return false;

        var normalized = image.WithLocalNormalize(GetNormalizeWindow(image), pool);
        if (budget.IsExpired) return false;
        var hybrid = CreateHybridThresholdImage(normalized, settings.AggressiveSampling, pool);
        if (budget.IsExpired) return false;
        QrFinderPatternDetector.FindCandidates(hybrid, invert: false, candidates, aggressive: false,
            shouldStop: () => budget.IsExpired, rowStepOverride: 1, maxCandidates: 12,
            allowFullScan: true, requireDiagonalCheck: true);
        if (candidates.Count is < 3 or > 12 || budget.IsExpired) return false;
        candidates.Sort(static (a, b) => b.Count.CompareTo(a.Count));
        if (!TryOrderAsTlTrBl(candidates[0], candidates[1], candidates[2], 1.8, out var tl, out var tr, out var bl)) return false;
        return TrySampleAndDecode(scale, normalized.Threshold, hybrid, invert: false, tl, tr, bl,
            candidates.Count, 1, accept, settings.AggressiveSampling, settings.StylizedSampling, budget,
            out result, out diagnostics, allowPerspectiveRecovery: true);
    }

    private static QrGrayImage CreateHybridThresholdImage(QrGrayImage image, bool aggressive, QrGrayImagePool? pool) {
        var minDim = Math.Min(image.Width, image.Height);
        var blockSize = minDim >= 800 ? 12 : minDim >= 480 ? 10 : minDim >= 240 ? 8 : 6;
        return image.WithHybridThreshold(blockSize, aggressive ? 16 : 24, aggressive ? 3 : 5, pool);
    }
}
#endif
