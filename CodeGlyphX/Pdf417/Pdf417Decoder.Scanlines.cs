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
    private static readonly int[][][] PixelCodewordRuns = BuildPixelCodewordRuns();
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
        if (TryReadPixelRows(darkPixels, width, height, cancellationToken, false, out decoded, out hasStartPattern)) return true;
        // Preserve the ordinary reader's decisions. A print-gain retry requires
        // repeated, consistent row indicators and shares the caller's deadline.
        if (!hasStartPattern || DecodeBudget.ShouldAbort(cancellationToken)) return false;
        return TryReadPixelRows(darkPixels, width, height, cancellationToken, true, out decoded, out _);
    }

    private static bool TryReadPixelRows(bool[] darkPixels, int width, int height, CancellationToken cancellationToken,
        bool correctPrintGain, out Pdf417Decoded decoded, out bool hasStartPattern) {
        decoded = null!;
        hasStartPattern = false;
        for (var axis = 0; axis < 2; axis++) {
            var across = axis == 0 ? width : height;
            var down = axis == 0 ? height : width;
            for (var reverse = 0; reverse < 2; reverse++) {
                for (var invert = 0; invert < 2; invert++) {
                    var slopes = new[] { 0.0, -0.05, 0.05, -0.1, 0.1, -0.15, 0.15, -0.2, 0.2, -0.25, 0.25, -0.3, 0.3 };
                    var orientationHasStart = false;
                    var headerRows = new Dictionary<int, PixelRowHints>();
                    var assemblies = new Dictionary<(int Columns, int Upper, int Lower, int Ec, bool Compact), List<PixelRowAssembly>>();
                    foreach (var slope in slopes) {
                        if (slope != 0 && !orientationHasStart) break;
                        var rowSets = new Dictionary<(int Columns, bool Compact, int StartBin), PixelRowSet>();
                        for (var y = 0; y < down; y++) {
                            if (DecodeBudget.ShouldAbort(cancellationToken)) return false;
                            var runs = new List<int>();
                            var first = false;
                            var color = false;
                            var length = 0;
                            for (var x = 0; x < across; x++) {
                                if ((x & 255) == 0 && DecodeBudget.ShouldAbort(cancellationToken)) return false;
                                var at = reverse == 0 ? x : across - x - 1;
                                var lineY = y + (int)Math.Round((at - across / 2.0) * slope);
                                var sx = axis == 0 ? at : lineY;
                                var sy = axis == 0 ? lineY : at;
                                var dark = ((uint)sx < (uint)width && (uint)sy < (uint)height &&
                                    darkPixels[sy * width + sx]) != (invert != 0);
                                if (x == 0) first = color = dark;
                                if (dark == color) length++;
                                else { runs.Add(length); length = 1; color = dark; }
                            }
                            runs.Add(length);
                            for (var start = first ? 0 : 1; start + 41 <= runs.Count; start += 2) {
                                if (!MatchPixelRuns(runs, start, new[] { 8, 1, 1, 1, 1, 1, 1, 3 }, 17)) continue;
                                var cluster = -1;
                                var left = 0;
                                for (var c = 0; c < 3; c++) {
                                    if (!ReadPixelCodeword(runs, start + 8, c, out left, approximate: false)) continue;
                                    cluster = c; break;
                                }
                                if (cluster < 0) {
                                    for (var c = 0; c < 3; c++) {
                                        if (!ReadPixelCodeword(runs, start + 8, c, out left, correctPrintGain: correctPrintGain)) continue;
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
                                if (hints.Observe(logicalRow, cluster, left)) hasStartPattern = orientationHasStart = true;
                                var words = new List<int>();
                                var at = start + 16;
                                for (; words.Count <= 31 && at + 9 <= runs.Count; at += 8) {
                                    if (DecodeBudget.ShouldAbort(cancellationToken)) return false;
                                    if (MatchPixelRuns(runs, at, new[] { 7, 1, 1, 3, 1, 1, 1, 2, 1 }, 18)) break;
                                    if (!ReadPixelCodeword(runs, at, cluster, out var word, correctPrintGain: correctPrintGain)) break;
                                    words.Add(word);
                                }
                                var regular = at + 9 <= runs.Count && MatchPixelRuns(runs, at, new[] { 7, 1, 1, 3, 1, 1, 1, 2, 1 }, 18);
                                var compact = !regular && at + 1 < runs.Count && runs[at + 1] > runs[at] * 2;
                                var indicatedColumns = cluster == 2 ? left % 30 + 1 : hints.Columns;
                                var clipped = !regular && !compact && at + 8 >= runs.Count && indicatedColumns > 0 &&
                                    indicatedColumns >= words.Count && indicatedColumns - words.Count <= 1;
                                if (clipped) compact = true;
                                if (!regular && !compact || words.Count < (compact ? 1 : 2) || words.Count > (compact ? 30 : 31)) continue;
                                var right = compact ? -1 : words[words.Count - 1];
                                var cols = clipped ? indicatedColumns : words.Count - (compact ? 0 : 1);
                                if (cluster == 2 && left % 30 != cols - 1) continue;
                                if (regular && (right / 30 != left / 30 || cluster == 0 && right % 30 != cols - 1)) continue;
                                var setKey = (cols, compact, headerBin);
                                if (!rowSets.TryGetValue(setKey, out var rowSet)) rowSets[setKey] = rowSet = new PixelRowSet(cols, compact);
                                var rowWords = new int[cols + (compact ? 1 : 2)];
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
                            if (!assembly.TryAssemble(out var rows) || rows.Count < 3 ||
                                (count - rows.Count) * columns * 2 > (1 << (ecLevel + 1))) continue;
                            var matrix = new BitMatrix(columns * 17 + (compactSymbol == true ? 35 : 69), count);
                            for (var row = 0; row < count; row++) {
                                if (!rows.TryGetValue(row, out var rowData)) {
                                    rowData = new int[columns + (compactSymbol == true ? 1 : 2)];
                                    var group = 30 * (row / 3);
                                    rowData[0] = group + (row % 3 == 0 ? rowUpper : row % 3 == 1 ? ecLevel * 3 + rowLower : columns - 1);
                                    if (compactSymbol != true) rowData[columns + 1] = group + (row % 3 == 0 ? columns - 1 : row % 3 == 1 ? rowUpper : ecLevel * 3 + rowLower);
                                }
                                WritePixelRowPattern(matrix, row, 0, StartPattern, 17);
                                for (var word = 0; word < rowData.Length; word++)
                                    WritePixelRowPattern(matrix, row, 17 + word * 17, Pdf417CodewordTable.Table[row % 3][rowData[word]], 17);
                                var stopWidth = compactSymbol == true ? 1 : 18;
                                WritePixelRowPattern(matrix, row, matrix.Width - stopWidth, compactSymbol == true ? 1 : StopPattern, stopWidth);
                            }
                            if (TryDecodeCore(matrix, cancellationToken, out var text, out var macro)) {
                                decoded = new Pdf417Decoded(text, macro);
                                return true;
                            }
                        }
                    }
                }
            }
        }
        return false;
    }

    private static bool ReadPixelCodeword(List<int> runs, int start, int cluster, out int word, bool approximate = true, bool correctPrintGain = false) {
        word = 0;
        if (start + 8 > runs.Count) return false;
        var total = 0;
        for (var i = 0; i < 8; i++) total += runs[start + i];
        var pitch = total / 17.0;
        var bits = 0;
        var modules = 0;
        for (var i = 0; i < 8; i++) {
            var count = (int)Math.Round(runs[start + i] / pitch);
            if (count < 1 || count > 6) { modules = -100; break; }
            modules += count;
            for (var j = 0; j < count; j++) bits = (bits << 1) | ((i & 1) == 0 ? 1 : 0);
        }
        if (modules == 17 && PatternToCodeword[cluster].TryGetValue(bits, out word)) return true;
        if (!approximate) return false;
        // Pixel rounding can move a run across its half-module boundary. Select
        // only a close legal character, then retain row metadata and RS validation.
        var bestError = 1.5;
        var found = false;
        for (var candidate = 0; candidate < PixelCodewordRuns[cluster].Length; candidate++) {
            // Blur and print gain expand every dark run while shrinking its
            // neighboring space. Fit that common bias without changing pitch.
            var bias = 0.0;
            if (correctPrintGain) for (var i = 0; i < 8; i++)
                bias += (runs[start + i] / pitch - PixelCodewordRuns[cluster][candidate][i]) * ((i & 1) == 0 ? 1 : -1);
            bias = Math.Max(-0.65, Math.Min(0.65, bias / 8));
            var error = 0.0;
            for (var i = 0; i < 8; i++) {
                var residual = runs[start + i] / pitch - PixelCodewordRuns[cluster][candidate][i] - bias * ((i & 1) == 0 ? 1 : -1);
                if (Math.Abs(residual) > 0.85) { error = bestError; break; }
                error += residual * residual;
            }
            if (error >= bestError) continue;
            word = candidate; bestError = error; found = true;
        }
        return found;
    }

    private static int[][][] BuildPixelCodewordRuns() {
        var result = new int[3][][];
        for (var cluster = 0; cluster < 3; cluster++) {
            result[cluster] = new int[929][];
            for (var word = 0; word < 929; word++) {
                var runs = new int[8];
                var index = 0;
                var previous = true;
                for (var bit = 16; bit >= 0; bit--) {
                    var dark = (Pdf417CodewordTable.Table[cluster][word] & (1 << bit)) != 0;
                    if (dark != previous) { index++; previous = dark; }
                    runs[index]++;
                }
                result[cluster][word] = runs;
            }
        }
        return result;
    }

    private static bool MatchPixelRuns(List<int> runs, int start, int[] pattern, int modules) {
        if (start + pattern.Length > runs.Count) return false;
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

    private static void WritePixelRowPattern(BitMatrix matrix, int row, int offset, int pattern, int width) {
        for (var bit = 0; bit < width; bit++) matrix[offset + bit, row] = (pattern & (1 << (width - bit - 1))) != 0;
    }
}
