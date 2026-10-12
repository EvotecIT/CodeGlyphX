using System.Collections.Generic;

namespace CodeGlyphX.Pdf417;

public static partial class Pdf417Decoder {
    // Repeated rows and recovery passes revisit the same eight run widths.
    // Cache exact decisions, including failed fits, within one thresholded image.
    // Wider runs use the ordinary fitter; the compact key never truncates them.
    private sealed class PixelCodewordCache {
        private const int MaximumEntries = 16384;
        private readonly Dictionary<(ulong Widths, int Mode), (bool Found, int Word)> _words = new();

        internal bool Read(List<int> runs, int start, int cluster, out int word,
            bool approximate = true, bool correctPrintGain = false) {
            if (start + 8 > runs.Count)
                return ReadPixelCodeword(runs, start, cluster, out word, approximate, correctPrintGain);
            ulong widths = 0;
            for (var i = 0; i < 8; i++) {
                var width = runs[start + i];
                if ((uint)width > byte.MaxValue)
                    return ReadPixelCodeword(runs, start, cluster, out word, approximate, correctPrintGain);
                widths |= (ulong)width << (i * 8);
            }
            var key = (widths, cluster | (approximate ? 4 : 0) | (correctPrintGain ? 8 : 0));
            if (_words.TryGetValue(key, out var cached)) { word = cached.Word; return cached.Found; }
            var found = ReadPixelCodeword(runs, start, cluster, out word, approximate, correctPrintGain);
            if (_words.Count < MaximumEntries) _words.Add(key, (found, word));
            return found;
        }
    }
}
