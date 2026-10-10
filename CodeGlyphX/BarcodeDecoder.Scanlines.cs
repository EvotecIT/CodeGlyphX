using System.Threading;
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
        out BarcodeScanlineCandidate selected) {
        BarcodeDecoded bestDecoded = null!;
        BarcodeScanlineCandidate bestScanline = default;
        var first = candidates[index];
        var bestPriority = int.MaxValue;
        var bestError = double.PositiveInfinity;
        // Collection keeps each physical row/column contiguous. Consume its
        // alternatives together so TryDecodeAll cannot admit them as extra symbols.
        var end = index + 1;
        while (end < candidates.Length && candidates[end].Position == first.Position && candidates[end].IsVertical == first.IsVertical) end++;
        for (var i = index; i < end; i++) {
            var candidate = candidates[i];
            if (DecodeBudget.ShouldAbort(cancellationToken)) break;
            Consider(candidate.Modules, candidate.FitError, candidate);
            if (candidate.AlternativeModules.Length > 0 && !DecodeBudget.ShouldAbort(cancellationToken)) {
                Consider(candidate.AlternativeModules, candidate.AlternativeFitError, candidate);
            }
        }
        index = end;
        decoded = bestDecoded;
        selected = bestScanline;
        return decoded is not null;

        void Consider(bool[] modules, double error, BarcodeScanlineCandidate candidate) {
            if (!TryDecodeWithTransforms(modules, expectedType, options, cancellationToken, diagnostics, out var hit, out var priority)) return;
            if (priority > bestPriority) return;
            var preserveSupplement = false;
            if (priority == bestPriority && bestDecoded is not null && hit.Type == bestDecoded.Type &&
                (hit.Type == BarcodeType.EAN || hit.Type == BarcodeType.UPCA || hit.Type == BarcodeType.UPCE)) {
                // A guarded local candidate can cover only the base symbol. A complete,
                // parity-validated supplement from the same scanline must not be discarded
                // merely because the base's local width fit has a smaller residual.
                if (bestDecoded.Text.StartsWith(hit.Text + "+", System.StringComparison.Ordinal)) return;
                preserveSupplement = hit.Text.StartsWith(bestDecoded.Text + "+", System.StringComparison.Ordinal);
            }
            if (priority == bestPriority && !preserveSupplement && error >= bestError) return;
            bestPriority = priority;
            bestError = error;
            bestDecoded = hit;
            // Located consumers need the modules that actually decoded, rather
            // than the other pitch's length when classifying the same scanline.
            bestScanline = new BarcodeScanlineCandidate(modules, candidate.Position, candidate.IsVertical);
        }
    }
}
