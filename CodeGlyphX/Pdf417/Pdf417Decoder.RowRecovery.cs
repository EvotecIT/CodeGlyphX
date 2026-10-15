using System;
using System.Collections.Generic;
using System.Threading;
using CodeGlyphX.Internal;
using CodeGlyphX.Pdf417.Ec;

namespace CodeGlyphX.Pdf417;

public static partial class Pdf417Decoder {
    private sealed class PixelRecoveryCandidate {
        internal readonly Dictionary<int, int[]> Rows;
        internal readonly int Columns, Count, EcLevel, Erasures;
        internal PixelRecoveryCandidate(Dictionary<int, int[]> rows, int columns, int count, int ecLevel, int erasures) {
            Rows = rows; Columns = columns; Count = count; EcLevel = ecLevel; Erasures = erasures;
        }
    }

    // Keep immutable row snapshots: later slopes can move a physical row or
    // vote for a different sequence. Ordinary recognition gets every slope
    // before any erasure candidate is allowed to complete a hypothesis.
    private static void RetainPixelRecovery(List<PixelRecoveryCandidate> candidates, Dictionary<int, int[]> rows,
        int columns, int count, int ecLevel) {
        var missing = (count - rows.Count) * columns;
        foreach (var words in rows.Values)
            for (var column = 1; column <= columns; column++) if (words[column] < 0) missing++;
        if (missing == 0 || missing > (1 << (ecLevel + 1))) return;
        foreach (var candidate in candidates) {
            if (candidate.Columns != columns || candidate.Count != count || candidate.EcLevel != ecLevel ||
                candidate.Rows.Count != rows.Count) continue;
            var same = true;
            foreach (var row in rows) {
                if (!candidate.Rows.TryGetValue(row.Key, out var prior) || !ReferenceEquals(prior, row.Value)) { same = false; break; }
            }
            if (same) return;
        }
        var at = 0;
        while (at < candidates.Count && candidates[at].Erasures <= missing) at++;
        const int maximumCandidates = 128;
        if (at >= maximumCandidates) return;
        candidates.Insert(at, new PixelRecoveryCandidate(rows, columns, count, ecLevel, missing));
        if (candidates.Count > maximumCandidates) candidates.RemoveAt(maximumCandidates);
    }

    // Row indicators have already established dimensions and parity level.
    // Preserve missing data positions as erasures instead of inventing valid
    // zero characters and spending twice the parity capacity on unknown errors.
    private static bool TryDecodePixelAssembly(Dictionary<int, int[]> rows, int columns, int count, int ecLevel, bool allowErasures,
        CancellationToken cancellationToken, out Pdf417Decoded decoded) {
        decoded = null!;
        var parity = 1 << (ecLevel + 1);
        var capacity = count * columns;
        if (capacity <= parity || (count - rows.Count) * columns > parity) return false;
        var received = new int[capacity];
        var erasures = new List<int>();
        for (var row = 0; row < count; row++) {
            if (DecodeBudget.ShouldAbort(cancellationToken)) return false;
            rows.TryGetValue(row, out var words);
            for (var column = 0; column < columns; column++) {
                var position = row * columns + column;
                if (words is null || words[column + 1] < 0) erasures.Add(position);
                else received[position] = words[column + 1];
            }
        }
        if ((allowErasures ? erasures.Count : erasures.Count * 2) > parity || !new ErrorCorrection().Decode(received, parity,
            allowErasures && erasures.Count > 0 ? erasures.ToArray() : null, cancellationToken)) return false;
        var length = capacity - parity;
        if (received[0] != length || length < 1 || DecodeBudget.ShouldAbort(cancellationToken)) return false;
        var data = new int[length - 1];
        Array.Copy(received, 1, data, 0, data.Length);
        var text = Pdf417DecodedBitStreamParser.Decode(data, out var macro);
        if (text is null || DecodeBudget.ShouldAbort(cancellationToken)) return false;
        decoded = new Pdf417Decoded(text, macro);
        return true;
    }
}
