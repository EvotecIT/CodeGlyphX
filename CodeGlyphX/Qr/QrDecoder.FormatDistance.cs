using System;

namespace CodeGlyphX;

public static partial class QrDecoder {
    /// <summary>Returns the nearest valid BCH format-word distance across both observed copies.</summary>
    internal static int GetBestFormatDistance(int bitsA, int bitsB) {
        if ((uint)bitsA < FormatDistanceLookup.WordCount && (uint)bitsB < FormatDistanceLookup.WordCount) {
            var scores = FormatDistanceLookup.Scores;
            return Math.Min(scores[bitsA], scores[bitsB]);
        }

        // Preserve full integer distance for callers outside the 15-bit observation domain.
        var best = int.MaxValue;
        for (var i = 0; i < FormatPatterns.Length; i++) {
            var pattern = FormatPatterns[i];
            var distance = Math.Min(CountBits(bitsA ^ pattern), CountBits(bitsB ^ pattern));
            if (distance < best) best = distance;
        }
        return best;
    }

    // Initialize only when pixel refinement first requests a score. Building from the canonical
    // generator avoids depending on static-field initialization order across partial files.
    private static class FormatDistanceLookup {
        internal const uint WordCount = 1 << 15;
        internal static readonly byte[] Scores = BuildScores();

        private static byte[] BuildScores() {
            var patterns = BuildFormatPatterns();
            var scores = new byte[WordCount];
            for (var bits = 0; bits < scores.Length; bits++) {
                var best = 15;
                for (var i = 0; i < patterns.Length; i++) {
                    var distance = CountBits(bits ^ patterns[i]);
                    if (distance < best) best = distance;
                    if (best == 0) break;
                }
                scores[bits] = (byte)best;
            }
            return scores;
        }
    }
}
