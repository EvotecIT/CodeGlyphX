using System;
using System.Collections.Generic;
using System.Threading;
using CodeGlyphX.Internal;
#if NET8_0_OR_GREATER
using PixelSpan = System.ReadOnlySpan<byte>;
#else
using PixelSpan = byte[];
#endif

namespace CodeGlyphX.Pdf417;

public static partial class Pdf417Decoder {
    private static readonly int[] PixelStartRuns = { 8, 1, 1, 1, 1, 1, 1, 3 };
    private static readonly int[] PixelStopRuns = { 7, 1, 1, 3, 1, 1, 1, 2, 1 };
    private static readonly double[] PixelRowSlopes = { 0.0, -0.05, 0.05, -0.1, 0.1, -0.15, 0.15, -0.2, 0.2, -0.25, 0.25, -0.3, 0.3 };
    private static readonly double[] PixelShallowRowSlopes = { 0.0, -0.025, 0.025 };
    // A row's start/stop runs establish its local pitch. Row indicators identify
    // logical rows independently of physical row height, borders and image text.
    private static bool TryDecodePixelRows(PixelSpan pixels, int width, int height, int stride, PixelFormat format,
        int threshold, CancellationToken cancellationToken, out Pdf417Decoded decoded, out bool hasStartPattern) {
        decoded = null!;
        hasStartPattern = false;
        if (width <= 0 || height <= 0 || stride <= 0) return false;
        // Each orientation revisits the same pixels. Threshold once, including
        // alpha, so slope retries do not repeat RGBA conversion per sample.
        var darkPixels = new bool[checked(width * height)];
        for (var y = 0; y < height; y++) {
            if (DecodeBudget.ShouldAbort(cancellationToken)) return false;
            for (var x = 0; x < width; x++) {
                if ((x & 255) == 0 && DecodeBudget.ShouldAbort(cancellationToken)) return false;
                darkPixels[y * width + x] = IsDarkAt(pixels, y * stride, x, format, threshold);
            }
        }
        var runCache = new PixelRunCache(darkPixels, width, height);
        var framingCandidates = new List<PixelFramingCandidate>(16);
        if (TryReadPixelRows(width, height, runCache, cancellationToken, false, false, out decoded, out hasStartPattern, framingCandidates)) return true;
        // Preserve the ordinary reader's decisions. A print-gain retry requires
        // repeated, consistent row indicators and shares the caller's deadline.
        if (hasStartPattern) {
            if (DecodeBudget.ShouldAbort(cancellationToken)) return false;
            if (TryReadPixelRows(width, height, runCache, cancellationToken, true, false, out decoded, out _)) return true;
            if (DecodeBudget.ShouldAbort(cancellationToken)) return false;
            // New angles and payload-only rows must not replace an established
            // decode. Preserve both original passes before the shallow-skew retry,
            // sharing their thresholded pixels, cached runs and remaining budget.
            if (TryReadPixelRows(width, height, runCache, cancellationToken, false, true, out decoded, out _)) return true;
            if (DecodeBudget.ShouldAbort(cancellationToken)) return false;
            if (TryReadPixelRows(width, height, runCache, cancellationToken, true, true, out decoded, out _)) return true;
        }
        if (DecodeBudget.ShouldAbort(cancellationToken)) return false;
        if (TryReadDamagedFrameRows(width, height, runCache, framingCandidates, cancellationToken, out decoded)) return true;
        if (DecodeBudget.ShouldAbort(cancellationToken)) return false;
        // A crop can remove the left framing while leaving independently
        // validated right indicators. Preserve ordinary row-reader priority.
        if (TryReadRightFramedRows(width, height, runCache, cancellationToken, false, out decoded, out var hasRight)) return true;
        if (!hasRight || DecodeBudget.ShouldAbort(cancellationToken)) return false;
        return TryReadRightFramedRows(width, height, runCache, cancellationToken, true, out decoded, out _);
    }

    private static bool TryReadPixelRows(int width, int height, PixelRunCache runCache, CancellationToken cancellationToken,
        bool correctPrintGain, bool shallowSkew, out Pdf417Decoded decoded, out bool hasStartPattern,
        List<PixelFramingCandidate>? framingCandidates = null) {
        decoded = null!;
        hasStartPattern = false;
        for (var axis = 0; axis < 2; axis++) {
            var across = axis == 0 ? width : height;
            var down = axis == 0 ? height : width;
            for (var reverse = 0; reverse < 2; reverse++) {
                for (var invert = 0; invert < 2; invert++) {
                    var orientationHasStart = false;
                    var headerRows = new Dictionary<int, PixelRowHints>();
                    var assemblies = new Dictionary<(int Columns, int Upper, int Lower, int Ec, bool Compact), List<PixelRowAssembly>>();
                    var recoveries = new List<PixelRecoveryCandidate>();
                    foreach (var slope in shallowSkew ? PixelShallowRowSlopes : PixelRowSlopes) {
                        if (slope != 0 && !orientationHasStart) break;
                        var rowSets = new Dictionary<(int Columns, bool Compact, int StartBin), PixelRowSet>();
                        var partialSets = new Dictionary<(int Columns, int StartBin), PixelRowSet>();
                        var unframedSets = new Dictionary<(int Columns, int StartBin), PixelRowSet>();
                        var runs = new List<int>();
                        for (var y = 0; y < down; y++) {
                            if (DecodeBudget.ShouldAbort(cancellationToken)) return false;
                            if (!runCache.TryRead(axis, reverse != 0, invert != 0, y, slope,
                                runs, cancellationToken, out var first)) return false;
                            // Compact framing needs only start, left indicator,
                            // one data character and its final bar: 25 runs.
                            for (var start = first ? 0 : 1; start + 25 <= runs.Count; start += 2) {
                                if (!MatchPixelRuns(runs, start, PixelStartRuns, 17)) continue;
                                var cluster = -1;
                                var left = 0;
                                for (var c = 0; c < 3; c++) {
                                    if (!runCache.Codewords.Read(runs, start + 8, c, out left, approximate: false)) continue;
                                    cluster = c; break;
                                }
                                if (cluster < 0) {
                                    for (var c = 0; c < 3; c++) {
                                        if (!runCache.Codewords.Read(runs, start + 8, c, out left, correctPrintGain: correctPrintGain)) continue;
                                        cluster = c; break;
                                    }
                                }
                                if (cluster < 0) continue;
                                var logicalRow = left / 30 * 3 + cluster;
                                if (logicalRow >= 90) continue;
                                var leftPixel = 0;
                                for (var run = 0; run < start; run++) leftPixel += runs[run];
                                var headerBin = leftPixel / 32;
                                if (!headerRows.TryGetValue(headerBin, out var hints)) headerRows[headerBin] = hints = new PixelRowHints();
                                // A tilted symbol may cross row boundaries before reaching
                                // the right indicator. Repeated left indicators still justify
                                // the bounded slope search; a lone accidental pattern does not.
                                if (hints.Observe(logicalRow, cluster, left)) {
                                    hasStartPattern = orientationHasStart = true;
                                    if (framingCandidates is not null)
                                        RetainFramingCandidate(framingCandidates, hints, runs, start, leftPixel, axis, reverse != 0, invert != 0, slope);
                                }
                                var words = new List<int>();
                                var at = start + 16;
                                // A data character has eight runs. Its last space
                                // can reach a cropped image edge without a ninth run.
                                for (; words.Count <= 31 && at + 8 <= runs.Count; at += 8) {
                                    if (DecodeBudget.ShouldAbort(cancellationToken)) return false;
                                    if (MatchPixelRuns(runs, at, PixelStopRuns, 18)) break;
                                    if (!runCache.Codewords.Read(runs, at, cluster, out var word, correctPrintGain: correctPrintGain)) break;
                                    words.Add(word);
                                }
                                var regular = at + 9 <= runs.Count && MatchPixelRuns(runs, at, PixelStopRuns, 18);
                                var compact = !regular && at + 1 < runs.Count && runs[at + 1] > runs[at] * 2;
                                var indicatedColumns = cluster == 2 ? left % 30 + 1 : hints.Columns;
                                // A regular row can retain its right indicator but
                                // lose the stop. Keep the indicator in the ordinary
                                // metadata checks rather than counting it as payload.
                                if (!regular && !compact && at == runs.Count && indicatedColumns > 0 &&
                                    words.Count == indicatedColumns + 1) regular = true;
                                var clipped = !regular && !compact && at + 8 >= runs.Count && indicatedColumns > 0 &&
                                    indicatedColumns >= words.Count && indicatedColumns - words.Count <= 1;
                                if (clipped) compact = true;
                                if (!regular && indicatedColumns > 0 && indicatedColumns <= 30 &&
                                    words.Count > 0 && (words.Count < indicatedColumns || shallowSkew && words.Count == indicatedColumns)) {
                                    // Unknown suffix positions remain erasures. Complete
                                    // payloads with no right framing are kept separately:
                                    // they need repeated framing elsewhere on the symbol.
                                    var partialKey = (indicatedColumns, headerBin);
                                    var targetSets = words.Count == indicatedColumns ? unframedSets : partialSets;
                                    if (!targetSets.TryGetValue(partialKey, out var partial))
                                        targetSets[partialKey] = partial = new PixelRowSet(indicatedColumns, true);
                                    var prefix = new int[indicatedColumns + 1];
                                    for (var i = 1; i < prefix.Length; i++) prefix[i] = -1;
                                    prefix[0] = left;
                                    words.CopyTo(prefix, 1);
                                    var startWidth = 0;
                                    for (var i = 0; i < 8; i++) startWidth += runs[start + i];
                                    var estimatedWidth = startWidth / 17.0 * (indicatedColumns * 17 + 69);
                                    var center = leftPixel + estimatedWidth / 2;
                                    if (reverse != 0) center = across - 1 - center;
                                    partial.Observe(logicalRow, left, -1, prefix, center,
                                        y + (center - across / 2.0) * slope, slope, estimatedWidth);
                                }
                                if (!regular && !compact || words.Count < (compact ? 1 : 2) || words.Count > (compact ? 30 : 31)) continue;
                                var right = compact ? -1 : words[words.Count - 1];
                                var cols = clipped ? indicatedColumns : words.Count - (compact ? 0 : 1);
                                if (cluster == 2 && left % 30 != cols - 1) continue;
                                if (regular && (right / 30 != left / 30 || cluster == 0 && right % 30 != cols - 1)) continue;
                                var setKey = (cols, compact, headerBin);
                                if (!rowSets.TryGetValue(setKey, out var rowSet)) rowSets[setKey] = rowSet = new PixelRowSet(cols, compact);
                                var rowWords = new int[cols + (compact ? 1 : 2)];
                                if (clipped && words.Count < cols) rowWords[cols] = -1;
                                rowWords[0] = left;
                                words.CopyTo(rowWords, 1);
                                var rightPixel = leftPixel;
                                for (var run = start; run < Math.Min(runs.Count, at + (regular ? 9 : 1)); run++) rightPixel += runs[run];
                                var middle = (leftPixel + rightPixel) / 2.0;
                                if (reverse != 0) middle = across - 1 - middle;
                                var physicalY = y + (middle - across / 2.0) * slope;
                                rowSet.Observe(logicalRow, left, right, rowWords, middle, physicalY, slope, rightPixel - leftPixel);
                            }
                        }
                        foreach (var rowSet in rowSets.Values) {
                            var columns = rowSet.Columns;
                            var rowUpper = rowSet.Upper;
                            var rowLower = rowSet.Lower;
                            var ecLevel = rowSet.Ec;
                            var compactSymbol = rowSet.Compact;
                            var count = rowUpper * 3 + rowLower + 1;
                            if (!rowSet.Ordered || rowUpper < 0 || rowLower < 0 || ecLevel < 0 || ecLevel > 8 || count < 3 || count > 90 || count * columns > 928) continue;
                            var key = (columns, rowUpper, rowLower, ecLevel, compactSymbol == true);
                            if (!assemblies.TryGetValue(key, out var candidates)) assemblies[key] = candidates = new List<PixelRowAssembly>();
                            PixelRowAssembly? assembly = null;
                            foreach (var candidate in candidates) if (candidate.Accepts(rowSet)) { assembly = candidate; break; }
                            if (assembly is null) {
                                assembly = new PixelRowAssembly(columns, rowUpper, rowLower, ecLevel, compactSymbol, rowSet.MeanX, rowSet.MeanWidth);
                                candidates.Add(assembly);
                            }
                        }
                        // A small skew can put one row's start in an adjacent bin.
                        // Complete headers qualify assemblies; compatible partial
                        // bins may then contribute rows whose indicators validate.
                        foreach (var header in assemblies) foreach (var assembly in header.Value) {
                            var columns = header.Key.Columns;
                            var rowUpper = header.Key.Upper;
                            var rowLower = header.Key.Lower;
                            var ecLevel = header.Key.Ec;
                            var compactSymbol = header.Key.Compact;
                            var count = rowUpper * 3 + rowLower + 1;
                            foreach (var rowSet in rowSets.Values) {
                                if (rowSet.Ordered && rowSet.Columns == columns && rowSet.Compact == compactSymbol && assembly.Accepts(rowSet))
                                    assembly.Observe(rowSet);
                            }
                            foreach (var partial in partialSets.Values) {
                                if (partial.Ordered && partial.Columns == columns && assembly.Accepts(partial))
                                    assembly.Observe(partial, partial: true);
                            }
                            // A single plausible framed row can create a complete
                            // header. Require three distinct validated rows before
                            // admitting payload-only observations of regular rows.
                            if (!compactSymbol && assembly.FramedRowCount >= 3) foreach (var partial in unframedSets.Values) {
                                if (partial.Ordered && partial.Columns == columns && assembly.Accepts(partial))
                                    assembly.Observe(partial, partial: true);
                            }
                            if (!assembly.TryAssemble(out var rows) || rows.Count < 3) continue;
                            if (TryDecodePixelAssembly(rows, columns, count, ecLevel, false, cancellationToken, out decoded)) return true;
                            RetainPixelRecovery(recoveries, rows, columns, count, ecLevel);
                        }
                    }
                    foreach (var recovery in recoveries) {
                        if (DecodeBudget.ShouldAbort(cancellationToken)) return false;
                        if (TryDecodePixelAssembly(recovery.Rows, recovery.Columns, recovery.Count, recovery.EcLevel,
                            true, cancellationToken, out decoded)) return true;
                    }
                }
            }
        }
        return false;
    }

    private static bool TryReadPixelRuns(bool[] pixels, int width, int height, int axis, bool reverse,
        bool invert, int y, double slope, List<int> runs, CancellationToken cancellationToken, out bool first) {
        runs.Clear();
        first = false;
        var color = false;
        var length = 0;
        var across = axis == 0 ? width : height;
        // Horizontal and vertical scanlines are entirely in bounds. Index them
        // directly instead of rounding and checking a transformed pixel each time.
        if (slope == 0) {
            var step = axis == 0 ? 1 : width;
            var index = axis == 0 ? y * width : y;
            if (reverse) { index += (across - 1) * step; step = -step; }
            for (var x = 0; x < across; x++, index += step) {
                if ((x & 255) == 0 && DecodeBudget.ShouldAbort(cancellationToken)) return false;
                var dark = pixels[index] != invert;
                if (x == 0) first = color = dark;
                if (dark == color) length++;
                else { runs.Add(length); length = 1; color = dark; }
            }
        } else {
            for (var x = 0; x < across; x++) {
                if ((x & 255) == 0 && DecodeBudget.ShouldAbort(cancellationToken)) return false;
                var at = reverse ? across - x - 1 : x;
                var lineY = y + (int)Math.Round((at - across / 2.0) * slope);
                var sx = axis == 0 ? at : lineY;
                var sy = axis == 0 ? lineY : at;
                var dark = ((uint)sx < (uint)width && (uint)sy < (uint)height && pixels[sy * width + sx]) != invert;
                if (x == 0) first = color = dark;
                if (dark == color) length++;
                else { runs.Add(length); length = 1; color = dark; }
            }
        }
        runs.Add(length);
        return true;
    }

    private static bool MatchPixelRuns(List<int> runs, int start, int[] pattern, int modules) {
        if (start + pattern.Length > runs.Count) return false;
        // Both guards start with a bar of at least seven modules, then two
        // one-module runs. The existing residual limit permits at least six
        // modules for the first bar and at most two for either following run.
        // Reject only ratios strictly below 3:1; the full fit and its inclusive
        // residual boundary still decide every surviving guard.
        if (runs[start] < 3L * runs[start + 1] || runs[start] < 3L * runs[start + 2]) return false;
        var total = 0;
        for (var i = 0; i < pattern.Length; i++) total += runs[start + i];
        var pitch = total / (double)modules;
        var error = 0.0;
        for (var i = 0; i < pattern.Length; i++) {
            var residual = Math.Abs(runs[start + i] / pitch - pattern[i]);
            if (residual > 1.0) return false;
            error += residual * residual;
        }
        return error < pattern.Length * 0.35;
    }

}
