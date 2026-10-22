using System;
using System.Collections.Generic;
using System.Threading;

namespace CodeGlyphX.Internal;

internal static partial class SpecialistImageDecoder {
    private static bool TryStacked(MaskView view, ImageRegion region, SymbolFormat format, CancellationToken token,
        out DetectedSymbol symbol, out int rotation, out bool mirrored, Gs1CompositeCarrier? compositeCarrier = null) {
        symbol = null!; rotation = 0; mirrored = false;
        if (region.Height < 2 || DecodeBudget.ShouldAbort(token)) return false;
        (DetectedSymbol Symbol, int Rotation, bool Mirrored)? partial = null;
        foreach (var width in StackedWidths(view, region, format, token)) {
            if (DecodeBudget.ShouldAbort(token)) break;
            var height = Math.Min(region.Height, Math.Min(4096, 1048576 / width));
            if (TrySample(view, region, width, height, token, out var raster)
                && TryStackedRaster(view, region, raster, format, token, out symbol, out rotation, out mirrored, compositeCarrier)) {
                if (symbol.Metadata is Gs1CompositeSymbolMetadata { IsStandalone: true }) partial ??= (symbol, rotation, mirrored);
                else return true;
            }
        }
        return UseCompositeGridFallback(partial, out symbol, out rotation, out mirrored);
    }

    private static bool TryStackedRaster(MaskView view, ImageRegion region, BitMatrix raster, SymbolFormat format,
        CancellationToken token, out DetectedSymbol symbol, out int rotation, out bool mirrored, Gs1CompositeCarrier? compositeCarrier) {
        symbol = null!; rotation = 0; mirrored = false;
        (DetectedSymbol Symbol, int Rotation, bool Mirrored)? partial = null;
        var width = raster.Width; var height = raster.Height;
        foreach (var stableOnly in new[] { false, true }) {
            // Mild projective resampling can move a few sampled edges by a pixel.
            // Keep the four-module retail separator changes distinct.
            var allowance = format == SymbolFormat.Gs1Composite ? Math.Min(3, Math.Max(1, width / 32)) : Math.Max(1, width / 100);
            var rows = ReadStackedRows(raster, region.Width / (double)width * height / region.Height, stableOnly, token, allowance);
            if (DecodeBudget.ShouldAbort(token)) return UseCompositeGridFallback(partial, out symbol, out rotation, out mirrored);
            if (stableOnly && format == SymbolFormat.Gs1Composite)
                MergeCompositeBarBands(raster, rows, region.Width / (double)width * height / region.Height, token);
            if (stableOnly && (format == SymbolFormat.Code16K || format == SymbolFormat.Code49)) {
                rows.RemoveAll(row => {
                    var dark = 0;
                    for (var x = 0; x < width; x++) if (raster[x, row.Center]) dark++;
                    return dark >= width * .9;
                });
            }
            if (rows.Count > 200 || rows.Count < 2) continue;
            var compact = new BitMatrix(width, rows.Count);
            for (var y = 0; y < rows.Count; y++) {
                for (var x = 0; x < width; x++) {
                    if ((x & 4095) == 0 && DecodeBudget.ShouldAbort(token)) return UseCompositeGridFallback(partial, out symbol, out rotation, out mirrored);
                    if (stableOnly && format == SymbolFormat.Gs1Composite) {
                        var row = rows[y];
                        var dark = 0;
                        for (var sourceY = row.Start; sourceY < row.End; sourceY++) {
                            if ((sourceY & 255) == 0 && DecodeBudget.ShouldAbort(token)) return UseCompositeGridFallback(partial, out symbol, out rotation, out mirrored);
                            if (raster[x, sourceY]) dark++;
                        }
                        compact[x, y] = dark * 2 > row.End - row.Start;
                    } else compact[x, y] = raster[x, rows[y].Center];
                }
            }
            if (!TryOrientations(compact, format, token, out symbol, out rotation, out mirrored, compositeCarrier)) continue;
            if (symbol.Metadata is Gs1CompositeSymbolMetadata metadata) {
                var turn = rotation;
                var mirror = mirrored;
                symbol = symbol.WithMetadata(metadata.ProjectModules(point => {
                    var unturned = UndoGridOrientation(point, width, rows.Count, turn, mirror);
                    var y = StackedY(unturned.Y, rows);
                    return view.Map(region.X + unturned.X * region.Width / width,
                        region.Y + y * region.Height / raster.Height);
                }));
            }
            if (symbol.Metadata is Gs1CompositeSymbolMetadata { IsStandalone: true }) partial ??= (symbol, rotation, mirrored);
            else return true;
        }
        return UseCompositeGridFallback(partial, out symbol, out rotation, out mirrored);
    }

    private static IEnumerable<int> StackedWidths(MaskView view, ImageRegion region, SymbolFormat format, CancellationToken token) {
        if (format == SymbolFormat.Code16K || format == SymbolFormat.Code49) { yield return 70; yield return 81; yield break; }
        if (format == SymbolFormat.MicroPdf417) {
            yield return 38; yield return 55; yield return 82; yield return 99; yield break;
        }
        var pitch = EstimatePitch(view, region, token, fractional: true);
        if (pitch == 0) yield break;
        if (format == SymbolFormat.CodablockF) {
            for (var width = 101; width <= 739; width += 11)
                if (Math.Abs(region.Width / (double)width / pitch - 1) < .22) yield return width;
        } else {
            // Linked and explicitly standalone Composite results retain different metadata.
            // Narrow bar pitch establishes their common horizontal lattice.
            var center = (int)Math.Round(region.Width / pitch);
            var minimum = format == SymbolFormat.PosiCode ? 40 : 50;
            for (var delta = 0; delta <= 3; delta++) {
                if (center + delta >= minimum) yield return center + delta;
                if (delta != 0 && center - delta >= minimum) yield return center - delta;
            }
        }
    }

    private readonly struct StackedRow {
        internal readonly int Start, End;
        internal int Center => (Start + End - 1) / 2;
        internal StackedRow(int start, int end) { Start = start; End = end; }
    }

    private static void MergeCompositeBarBands(BitMatrix raster, List<StackedRow> rows, double pitch, CancellationToken token) {
        // One long bar row can flicker into several bands after nearest-pixel projection.
        // Merge only long, adjoining observations of the same bars. Thin component and
        // separator rows keep their own addresses even when their patterns are similar.
        for (var i = 0; i + 1 < rows.Count;) {
            if (DecodeBudget.ShouldAbort(token)) return;
            var first = rows[i]; var second = rows[i + 1];
            if (first.End - first.Start < pitch * 3 || second.End - second.Start < pitch * 3
                || second.Start - first.End > pitch * .5) { i++; continue; }
            var errors = 0;
            for (var x = 0; x < raster.Width && errors <= 5; x++)
                if (raster[x, first.Center] != raster[x, second.Center]) errors++;
            if (errors > 5) { i++; continue; }
            rows[i] = new StackedRow(first.Start, second.End);
            rows.RemoveAt(i + 1);
        }
    }

    private static SymbolPoint UndoGridOrientation(SymbolPoint point, int width, int height, int turn, bool mirror) {
        var unturned = turn switch {
            1 => new SymbolPoint(point.Y, height - point.X),
            2 => new SymbolPoint(width - point.X, height - point.Y),
            3 => new SymbolPoint(width - point.Y, point.X),
            _ => point
        };
        return mirror ? new SymbolPoint(width - unturned.X, unturned.Y) : unturned;
    }

    private static double StackedY(double y, List<StackedRow> rows) {
        var index = Math.Max(0, Math.Min(rows.Count - 1, (int)Math.Floor(y)));
        var row = rows[index];
        return row.Start + (y - index) * (row.End - row.Start);
    }

    private static List<StackedRow> ReadStackedRows(BitMatrix raster, double pitch, bool stableOnly, CancellationToken token, int allowance) {
        var rows = new List<StackedRow>();
        var start = 0;
        for (var y = 1; y <= raster.Height; y++) {
            if (DecodeBudget.ShouldAbort(token)) return rows;
            var different = y == raster.Height;
            if (!different) {
                var errors = 0;
                for (var x = 0; x < raster.Width; x++) {
                    if ((x & 4095) == 0 && DecodeBudget.ShouldAbort(token)) return rows;
                    if (raster[x, y] != raster[x, start]) errors++;
                }
                different = errors > (stableOnly ? allowance : 0);
            }
            if (!different) continue;
            if (!stableOnly || y - start >= Math.Max(2, (int)(pitch * .6))) rows.Add(new StackedRow(start, y));
            start = y;
            if (rows.Count > 200) return rows;
        }
        return rows;
    }
}
