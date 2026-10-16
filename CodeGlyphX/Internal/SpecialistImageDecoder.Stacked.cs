using System;
using System.Collections.Generic;
using System.Threading;

namespace CodeGlyphX.Internal;

internal static partial class SpecialistImageDecoder {
    private static bool TryStacked(MaskView view, ImageRegion region, SymbolFormat format, CancellationToken token,
        out DetectedSymbol symbol, out int rotation, out bool mirrored) {
        symbol = null!; rotation = 0; mirrored = false;
        foreach (var width in StackedWidths(view, region, format)) {
            if (DecodeBudget.ShouldAbort(token)) return false;
            var height = Math.Min(region.Height, Math.Min(4096, 1048576 / width));
            if (!TrySample(view, region, width, height, token, out var raster)) continue;
            foreach (var stableOnly in new[] { false, true }) {
                var rows = ReadStackedRows(raster, region.Width / (double)width * height / region.Height, stableOnly);
                if (stableOnly && (format == SymbolFormat.Code16K || format == SymbolFormat.Code49)) {
                    rows.RemoveAll(y => {
                        var dark = 0;
                        for (var x = 0; x < width; x++) if (raster[x, y]) dark++;
                        return dark >= width * .9;
                    });
                }
                if (rows.Count > 200 || rows.Count < 2) continue;
                var compact = new BitMatrix(width, rows.Count);
                for (var y = 0; y < rows.Count; y++) for (var x = 0; x < width; x++) compact[x, y] = raster[x, rows[y]];
                if (TryOrientations(compact, format, token, out symbol, out rotation, out mirrored)) return true;
            }
        }
        return false;
    }

    private static IEnumerable<int> StackedWidths(MaskView view, ImageRegion region, SymbolFormat format) {
        if (format == SymbolFormat.Code16K || format == SymbolFormat.Code49) { yield return 70; yield return 81; yield break; }
        if (format == SymbolFormat.MicroPdf417) {
            yield return 38; yield return 55; yield return 82; yield return 99; yield break;
        }
        var pitch = EstimatePitch(view, region, fractional: true);
        if (pitch == 0) yield break;
        if (format == SymbolFormat.CodablockF) {
            for (var width = 101; width <= 739; width += 11)
                if (Math.Abs(region.Width / (double)width / pitch - 1) < .22) yield return width;
        } else {
            // A Composite has independently sized linked components. Narrow bar pitch establishes
            // the common horizontal lattice; neither component may be decoded in isolation as Composite.
            var center = (int)Math.Round(region.Width / pitch);
            var minimum = format == SymbolFormat.PosiCode ? 40 : 50;
            for (var delta = 0; delta <= 3; delta++) {
                if (center + delta >= minimum) yield return center + delta;
                if (delta != 0 && center - delta >= minimum) yield return center - delta;
            }
        }
    }

    private static List<int> ReadStackedRows(BitMatrix raster, double pitch, bool stableOnly) {
        var rows = new List<int>();
        var start = 0;
        for (var y = 1; y <= raster.Height; y++) {
            var different = y == raster.Height;
            if (!different) {
                var errors = 0;
                for (var x = 0; x < raster.Width; x++) if (raster[x, y] != raster[x, start]) errors++;
                different = errors > (stableOnly ? Math.Max(1, raster.Width / 100) : 0);
            }
            if (!different) continue;
            if (!stableOnly || y - start >= Math.Max(2, (int)(pitch * .6))) rows.Add((start + y - 1) / 2);
            start = y;
            if (rows.Count > 200) return rows;
        }
        return rows;
    }
}
