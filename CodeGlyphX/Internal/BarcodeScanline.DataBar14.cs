using System;
using System.Collections.Generic;
using System.Threading;
using CodeGlyphX.DataBar;

namespace CodeGlyphX.Internal;

internal static partial class BarcodeScanline {
    /// <summary>Locates checksum-valid complete DataBar-14 rows without assuming one image-wide module pitch.</summary>
    private static void TryCollectDataBar14RunCandidates(List<int> runs, bool firstIsBar, CancellationToken token,
        List<BarcodeScanlineCandidate> candidates, int position, bool isVertical) {
        for (var direction = 0; direction < 2; direction++) {
            var source = direction == 0 ? runs : new List<int>(runs);
            var first = firstIsBar;
            if (direction != 0) { source.Reverse(); first = firstIsBar == ((runs.Count & 1) != 0); }
            for (var start = 0; start + 46 <= source.Count; start++) {
                if (DecodeBudget.ShouldAbort(token)) return;
                if (!DataBar14ImageDecoder.TryNormalizeUnstackedRow(source, start, out var widths, out var fit)) continue;
                // Validate the complete canonical symbol before exposing a
                // normalization hypothesis to other format decoders.
                var modules = new bool[96]; var at = 0; var dark = false;
                foreach (var width in widths) {
                    for (var i = 0; i < width; i++) modules[at++] = dark;
                    dark = !dark;
                }
                if (!DataBar14Decoder.TryDecodeOmnidirectional(modules, out _)) continue;
                if (first == ((start & 1) == 0))
                    for (var i = 0; i < modules.Length; i++) modules[i] = !modules[i];
                AddUniqueLocatedCandidate(candidates, new BarcodeScanlineCandidate(modules, position, isVertical, fitError: fit));
            }
        }
    }
}
