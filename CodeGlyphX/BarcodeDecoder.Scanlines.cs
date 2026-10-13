using System.Threading;
using System.Collections.Generic;
using CodeGlyphX.Internal;

namespace CodeGlyphX;

public static partial class BarcodeDecoder {
    /// <summary>
    /// Selects one decoded symbol from the bounded threshold and pitch hypotheses
    /// for a physical scanline. Format priority remains owned by the core decoder;
    /// competing pitches for that format are ranked by their pixel-run residuals.
    /// </summary>
    private static bool TryDecodeScanline(
        BarcodeScanlineCandidate[] candidates,
        ref int index,
        BarcodeType? expectedType,
        BarcodeDecodeOptions? options,
        CancellationToken cancellationToken,
        BarcodeDecodeDiagnostics? diagnostics,
        out BarcodeDecoded decoded,
        out BarcodeScanlineCandidate selected,
        bool retailOnly = false,
        IReadOnlyList<BarcodeDecoded>? ordinaryHits = null,
        BarcodeDecoded? requiredDecoded = null) {
        var budget = DecodeBudget.Capture();
        BarcodeDecoded bestDecoded = null!;
        BarcodeScanlineCandidate bestScanline = default;
        var first = candidates[index];
        var bestPriority = int.MaxValue;
        var bestRank = int.MaxValue;
        var bestError = double.PositiveInfinity;
        List<BarcodeDecoded>? supplementsOnRow = null;
        // Collection keeps each physical row/column contiguous. Consume its
        // alternatives together so TryDecodeAll cannot admit them as extra symbols.
        var end = index + 1;
        while (end < candidates.Length && candidates[end].Position == first.Position && candidates[end].IsVertical == first.IsVertical) end++;
        for (var i = index; i < end; i++) {
            var candidate = candidates[i];
            if (budget.ShouldAbort(cancellationToken)) break;
            // Preserve an ordinary validated interpretation before considering
            // parity-constrained recovery. Within either group, local illumination
            // remains a fallback for the same physical row or column.
            var rank = (candidate.IsRetailParityFallback ? 2 : 0) + (candidate.IsLocalThreshold ? 1 : 0);
            if (rank > bestRank) continue;
            if (candidate.IsLocalThreshold && expectedType.HasValue &&
                expectedType != BarcodeType.EAN && expectedType != BarcodeType.UPCA && expectedType != BarcodeType.UPCE) continue;
            if (candidate.IsRetailParityFallback && expectedType.HasValue &&
                expectedType != BarcodeType.EAN && expectedType != BarcodeType.UPCA) continue;
            Consider(candidate.Modules, candidate.FitError, candidate);
            if (candidate.AlternativeModules.Length > 0 && !budget.ShouldAbort(cancellationToken)) {
                Consider(candidate.AlternativeModules, candidate.AlternativeFitError, candidate);
            }
        }
        index = end;
        decoded = bestDecoded!;
        selected = bestScanline;
        return decoded is not null;

        void Consider(bool[] modules, double error, BarcodeScanlineCandidate candidate) {
            if (!TryDecodeWithTransforms(modules, expectedType, options, cancellationToken, diagnostics, out var hit, out var priority,
                    backgroundIsDark: candidate.BackgroundIsDark)) return;
            if (retailOnly && hit.Type != BarcodeType.EAN && hit.Type != BarcodeType.UPCA && hit.Type != BarcodeType.UPCE) return;
            // Confirmation looks for this payload even when another symbol has
            // the preferred fit on the same neighboring physical row.
            if (requiredDecoded is not null && (hit.Type != requiredDecoded.Type || hit.Text != requiredDecoded.Text)) return;
            if (ordinaryHits is not null) {
                foreach (var ordinary in ordinaryHits) {
                    if (hit.Type != ordinary.Type) continue;
                    if (hit.Text != ordinary.Text) continue;
                    // Suppress a base-only interpretation only when this physical
                    // scanline also supplies the validated supplement. A different
                    // row can contain a separate symbol with the same base text.
                    if (hit.Text.IndexOf('+') >= 0) {
                        (supplementsOnRow ??= new List<BarcodeDecoded>()).Add(hit);
                        if (bestDecoded is not null && hit.Type == bestDecoded.Type &&
                            hit.Text.StartsWith(bestDecoded.Text + "+", System.StringComparison.Ordinal)) {
                            bestDecoded = null!;
                            bestPriority = bestRank = int.MaxValue;
                            bestError = double.PositiveInfinity;
                        }
                    }
                    return;
                }
            }
            if (supplementsOnRow is not null) {
                foreach (var supplement in supplementsOnRow)
                    if (hit.Type == supplement.Type && supplement.Text.StartsWith(hit.Text + "+", System.StringComparison.Ordinal)) return;
            }
            if ((candidate.IsLocalThreshold || candidate.IsRetailParityFallback) && !IsCompleteLocalRetailPayload(hit, modules.Length)) return;
            var rank = (candidate.IsRetailParityFallback ? 2 : 0) + (candidate.IsLocalThreshold ? 1 : 0);
            if (rank == bestRank && priority > bestPriority) return;
            var preserveSupplement = false;
            if (rank == bestRank && priority == bestPriority && bestDecoded is not null && hit.Type == bestDecoded.Type &&
                (hit.Type == BarcodeType.EAN || hit.Type == BarcodeType.UPCA || hit.Type == BarcodeType.UPCE)) {
                // A guarded local candidate can cover only the base symbol. A complete,
                // parity-validated supplement from the same scanline must not be discarded
                // merely because the base's local width fit has a smaller residual.
                if (bestDecoded.Text.StartsWith(hit.Text + "+", System.StringComparison.Ordinal)) return;
                preserveSupplement = hit.Text.StartsWith(bestDecoded.Text + "+", System.StringComparison.Ordinal);
            }
            if (rank == bestRank && priority == bestPriority && !preserveSupplement && error >= bestError) return;
            bestRank = rank;
            bestPriority = priority;
            bestError = error;
            bestDecoded = hit;
            // Located consumers need the modules that actually decoded, rather
            // than the other pitch's length when classifying the same scanline.
            bestScanline = new BarcodeScanlineCandidate(TrimModules(modules), candidate.Position, candidate.IsVertical,
                isLocalThreshold: candidate.IsLocalThreshold, isRetailParityFallback: candidate.IsRetailParityFallback,
                backgroundIsDark: candidate.BackgroundIsDark);
        }
    }

    private static bool IsRetailBarcodeType(BarcodeType? expectedType) =>
        expectedType == BarcodeType.EAN || expectedType == BarcodeType.UPCA || expectedType == BarcodeType.UPCE;

    private static bool ShouldTryRetailRecovery(BarcodeType? expectedType) =>
        !expectedType.HasValue || IsRetailBarcodeType(expectedType);

    private static bool ShouldTryDataBarRecovery(BarcodeType? expectedType) =>
        !expectedType.HasValue || expectedType == BarcodeType.GS1DataBarOmni || expectedType == BarcodeType.GS1DataBarTruncated;

    private static int PixelPassCount(BarcodeType? expectedType) =>
        1 + (ShouldTryRetailRecovery(expectedType) ? 1 : 0) + (ShouldTryDataBarRecovery(expectedType) ? 1 : 0);

    private static bool IsRetailRecoveryPass(BarcodeType? expectedType, int pass) =>
        pass == 1 && ShouldTryRetailRecovery(expectedType);

    private static bool IsDataBarRecoveryPass(BarcodeType? expectedType, int pass) =>
        pass > 0 && pass == PixelPassCount(expectedType) - 1 && ShouldTryDataBarRecovery(expectedType);

    private static bool IsCompleteLocalRetailPayload(BarcodeDecoded decoded, int moduleCount) {
        // A local run candidate already has the complete guards for one base
        // symbol. Do not reinterpret a prefix as a shorter format or supplement:
        // a checksum-valid UPC-E prefix can occur in unrelated striped textures.
        return decoded.Type switch {
            BarcodeType.EAN => (moduleCount == 67 && decoded.Text.Length == 8) || (moduleCount == 95 && decoded.Text.Length == 13),
            BarcodeType.UPCA => moduleCount == 95 && decoded.Text.Length == 12,
            BarcodeType.UPCE => moduleCount == 51 && decoded.Text.Length == 8,
            _ => false
        };
    }
}
