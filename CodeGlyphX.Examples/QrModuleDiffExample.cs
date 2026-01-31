using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CodeGlyphX.Qr;

namespace CodeGlyphX.Examples;

internal static class QrModuleDiffExample {
    private static readonly Regex DimRegex = new(@"-dim(?<dim>\d+)-", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static void Run(string outputDir) {
        var payload = Environment.GetEnvironmentVariable("CODEGLYPHX_MODULE_DIFF_PAYLOAD") ?? "http://jess3.com";
        var root = Environment.GetEnvironmentVariable("CODEGLYPHX_MODULE_DIFF_DIR");
        if (string.IsNullOrWhiteSpace(root)) {
            root = FindLatestFailureDir(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "BenchmarkReports"));
        }

        if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root)) {
            Console.WriteLine("Module diff: no failure directory found.");
            return;
        }

        var moduleFiles = Directory.EnumerateFiles(root, "*.txt", SearchOption.AllDirectories)
            .Where(path => Path.GetFileName(path).StartsWith("qr-modules-", StringComparison.Ordinal))
            .Where(path => !path.EndsWith(".meta.txt", StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToList();

        if (moduleFiles.Count == 0) {
            Console.WriteLine($"Module diff: no module dumps under {root}.");
            return;
        }

        var lines = new List<string> {
            $"Root: {root}",
            $"Payload: {payload}",
            string.Empty
        };

        foreach (var file in moduleFiles) {
            if (!TryLoadMatrix(file, out var observed)) {
                lines.Add($"{Path.GetFileName(file)}: SKIP (parse failed)");
                continue;
            }

            var dim = observed.Width;
            var version = (dim - 17) / 4;
            var match = DimRegex.Match(file);
            if (match.Success && int.TryParse(match.Groups["dim"].Value, out var parsedDim)) {
                dim = parsedDim;
                version = (dim - 17) / 4;
            }

            if (version < 1 || version > 40) {
                lines.Add($"{Path.GetFileName(file)}: SKIP (bad version)");
                continue;
            }

            var results = new List<(QrErrorCorrectionLevel ecc, int mask, int mismatches, int dataMismatches, int dataTotal)>();
            var functionMask = BuildFunctionMask(version, observed.Width);
            foreach (QrErrorCorrectionLevel ecc in Enum.GetValues(typeof(QrErrorCorrectionLevel))) {
                for (var mask = 0; mask < 8; mask++) {
                    try {
                        var qr = QrCodeEncoder.EncodeText(payload, ecc, version, version, mask);
                        if (qr.Modules.Width != observed.Width) continue;
                        var mismatches = CountMismatches(qr.Modules, observed, null);
                        var dataMismatches = CountMismatches(qr.Modules, observed, functionMask);
                        var dataTotal = CountDataTotal(functionMask);
                        results.Add((ecc, mask, mismatches, dataMismatches, dataTotal));
                    } catch (ArgumentException) {
                        // Payload doesn't fit this version/ecc; skip.
                    }
                }
            }

            if (results.Count == 0) {
                lines.Add($"{Path.GetFileName(file)}: SKIP (no candidates)");
                continue;
            }

            var best = results.OrderBy(r => r.mismatches).Take(3).ToArray();
            var total = observed.Width * observed.Height;
            var summary = string.Join(", ", best.Select(r =>
                $"{r.ecc}/m{r.mask} mismatches={r.mismatches} ({r.mismatches * 100.0 / total:0.00}%), " +
                $"data={r.dataMismatches}/{r.dataTotal} ({(r.dataTotal == 0 ? 0 : r.dataMismatches * 100.0 / r.dataTotal):0.00}%)"));
            lines.Add($"{Path.GetFileName(file)}: dim={observed.Width} v{version} -> {summary}");
        }

        var outputPath = Path.Combine(outputDir, "qr-module-diff.txt");
        File.WriteAllLines(outputPath, lines);
        Console.WriteLine(string.Join(Environment.NewLine, lines));
    }

    private static string? FindLatestFailureDir(string reportsDir) {
        if (!Directory.Exists(reportsDir)) return null;

        var dirs = Directory.EnumerateDirectories(reportsDir, "qr-decode-failures-*", SearchOption.TopDirectoryOnly)
            .OrderByDescending(d => d, StringComparer.Ordinal)
            .ToList();
        return dirs.FirstOrDefault();
    }

    private static bool TryLoadMatrix(string path, out BitMatrix matrix) {
        matrix = null!;
        var rawLines = File.ReadAllLines(path);
        if (rawLines.Length == 0) return false;

        var width = rawLines.Max(l => l.Length);
        var padded = rawLines
            .Select(l => l.PadRight(width, ' '))
            .ToArray();

        var topBlank = IsBlankRow(padded[0]);
        var bottomBlank = IsBlankRow(padded[^1]);
        var leftBlank = IsBlankCol(padded, 0);
        var rightBlank = IsBlankCol(padded, width - 1);

        var yStart = topBlank ? 1 : 0;
        var yEnd = bottomBlank ? padded.Length - 2 : padded.Length - 1;
        var xStart = leftBlank ? 1 : 0;
        var xEnd = rightBlank ? width - 2 : width - 1;

        var h = yEnd - yStart + 1;
        var w = xEnd - xStart + 1;
        if (w <= 0 || h <= 0) return false;

        matrix = new BitMatrix(w, h);
        for (var y = 0; y < h; y++) {
            var line = padded[yStart + y];
            for (var x = 0; x < w; x++) {
                matrix[x, y] = line[xStart + x] == '#';
            }
        }

        return true;
    }

    private static bool IsBlankRow(string line) {
        for (var i = 0; i < line.Length; i++) {
            if (line[i] == '#') return false;
        }
        return true;
    }

    private static bool IsBlankCol(string[] lines, int col) {
        for (var i = 0; i < lines.Length; i++) {
            if (col < lines[i].Length && lines[i][col] == '#') return false;
        }
        return true;
    }

    private static int CountMismatches(BitMatrix expected, BitMatrix observed, BitMatrix? functionMask) {
        var mismatches = 0;
        var size = expected.Width;
        for (var y = 0; y < size; y++) {
            for (var x = 0; x < size; x++) {
                if (functionMask != null && functionMask[x, y]) continue;
                if (expected[x, y] != observed[x, y]) mismatches++;
            }
        }
        return mismatches;
    }

    private static int CountDataTotal(BitMatrix functionMask) {
        var total = 0;
        var size = functionMask.Width;
        for (var y = 0; y < size; y++) {
            for (var x = 0; x < size; x++) {
                if (!functionMask[x, y]) total++;
            }
        }
        return total;
    }

    private static BitMatrix BuildFunctionMask(int version, int size) {
        var isFunction = new BitMatrix(size, size);

        MarkFinder(0, 0, isFunction);
        MarkFinder(size - 7, 0, isFunction);
        MarkFinder(0, size - 7, isFunction);

        for (var i = 0; i < size; i++) {
            isFunction[6, i] = true;
            isFunction[i, 6] = true;
        }

        var align = GetAlignmentPatternPositions(version);
        for (var i = 0; i < align.Length; i++) {
            for (var j = 0; j < align.Length; j++) {
                if ((i == 0 && j == 0) || (i == 0 && j == align.Length - 1) || (i == align.Length - 1 && j == 0)) {
                    continue;
                }
                MarkAlignment(align[i], align[j], isFunction);
            }
        }

        isFunction[8, size - 8] = true;

        for (var i = 0; i <= 5; i++) isFunction[8, i] = true;
        isFunction[8, 7] = true;
        isFunction[8, 8] = true;
        isFunction[7, 8] = true;
        for (var i = 9; i < 15; i++) isFunction[14 - i, 8] = true;
        for (var i = 0; i < 8; i++) isFunction[size - 1 - i, 8] = true;
        for (var i = 8; i < 15; i++) isFunction[8, size - 15 + i] = true;

        if (version >= 7) {
            for (var i = 0; i < 18; i++) {
                var a = size - 11 + (i % 3);
                var b = i / 3;
                isFunction[a, b] = true;
                isFunction[b, a] = true;
            }
        }

        return isFunction;
    }

    private static int[] GetAlignmentPatternPositions(int version) {
        if (version == 1) return Array.Empty<int>();

        var numAlign = (version / 7) + 2;
        var size = (version * 4) + 17;
        var step = version == 32
            ? 26
            : ((version * 4 + numAlign * 2 + 1) / (2 * numAlign - 2)) * 2;

        var result = new int[numAlign];
        result[0] = 6;
        result[numAlign - 1] = size - 7;
        for (var i = 1; i < numAlign - 1; i++) {
            result[i] = result[numAlign - 1] - step * (numAlign - 1 - i);
        }
        return result;
    }

    private static void MarkFinder(int x, int y, BitMatrix isFunction) {
        for (var dy = -1; dy <= 7; dy++) {
            for (var dx = -1; dx <= 7; dx++) {
                var xx = x + dx;
                var yy = y + dy;
                if ((uint)xx >= (uint)isFunction.Width || (uint)yy >= (uint)isFunction.Height) continue;
                isFunction[xx, yy] = true;
            }
        }
    }

    private static void MarkAlignment(int x, int y, BitMatrix isFunction) {
        for (var dy = -2; dy <= 2; dy++) {
            for (var dx = -2; dx <= 2; dx++) {
                isFunction[x + dx, y + dy] = true;
            }
        }
    }
}
