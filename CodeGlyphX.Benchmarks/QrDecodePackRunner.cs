using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime;
using System.Text;
using CodeGlyphX.Rendering;

namespace CodeGlyphX.Benchmarks;

internal sealed class QrDecodePackRunnerOptions {
    public required QrPackMode Mode { get; init; }
    public required HashSet<string> Packs { get; init; }
    public required int Iterations { get; init; }
    public required int MinIterationMilliseconds { get; init; }
    public required int OpsCap { get; init; }
}

internal static class QrDecodePackRunner {
    private static readonly string[] QuickDefaultPacks = {
        QrDecodeScenarioPacks.Ideal,
        QrDecodeScenarioPacks.Stress,
        QrDecodeScenarioPacks.Screenshot,
        QrDecodeScenarioPacks.Multi
    };

    public static bool TryParseArgs(string[] args, out QrDecodePackRunnerOptions options, out string[] remainingArgs) {
        options = null!;
        var remaining = new List<string>(args.Length);
        var packList = new List<string>(8);
        var runRequested = false;

        QrPackMode? mode = null;
        int? iterations = null;
        int? minIterMs = null;
        int? opsCap = null;

        for (var i = 0; i < args.Length; i++) {
            var arg = args[i];
            if (string.Equals(arg, "--pack-runner", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(arg, "--packs", StringComparison.OrdinalIgnoreCase)) {
                runRequested = true;
                if (string.Equals(arg, "--packs", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length) {
                    AddPacks(packList, args[++i]);
                }
                continue;
            }

            if (string.Equals(arg, "--pack", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length) {
                runRequested = true;
                AddPacks(packList, args[++i]);
                continue;
            }

            if (string.Equals(arg, "--mode", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length) {
                runRequested = true;
                mode = ParseMode(args[++i]);
                continue;
            }

            if (string.Equals(arg, "--iterations", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length) {
                runRequested = true;
                if (int.TryParse(args[++i], out var parsed) && parsed > 0) iterations = parsed;
                continue;
            }

            if (string.Equals(arg, "--min-iter-ms", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length) {
                runRequested = true;
                if (int.TryParse(args[++i], out var parsed) && parsed > 0) minIterMs = parsed;
                continue;
            }

            if (string.Equals(arg, "--ops-cap", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length) {
                runRequested = true;
                if (int.TryParse(args[++i], out var parsed) && parsed > 0) opsCap = parsed;
                continue;
            }

            remaining.Add(arg);
        }

        // Env fallbacks also trigger the runner.
        if (!runRequested) {
            var envMode = Environment.GetEnvironmentVariable("CODEGLYPHX_BENCH_MODE");
            var envPacks = Environment.GetEnvironmentVariable("CODEGLYPHX_BENCH_PACKS");
            if (!string.IsNullOrWhiteSpace(envMode) || !string.IsNullOrWhiteSpace(envPacks)) {
                runRequested = true;
                mode ??= ParseMode(envMode);
                if (!string.IsNullOrWhiteSpace(envPacks)) AddPacks(packList, envPacks);
            }
        }

        remainingArgs = remaining.ToArray();
        if (!runRequested) return false;

        var resolvedMode = mode ?? QrPackMode.Quick;
        var resolvedIterations = iterations ?? (resolvedMode == QrPackMode.Quick ? 3 : 5);
        var resolvedMinIterMs = minIterMs ?? (resolvedMode == QrPackMode.Quick ? 400 : 800);
        var resolvedOpsCap = opsCap ?? 12;

        var packs = ResolvePacks(packList, resolvedMode);
        options = new QrDecodePackRunnerOptions {
            Mode = resolvedMode,
            Packs = packs,
            Iterations = resolvedIterations,
            MinIterationMilliseconds = resolvedMinIterMs,
            OpsCap = resolvedOpsCap
        };

        return true;
    }

    public static int Run(QrDecodePackRunnerOptions options) {
        var scenarios = QrDecodeScenarioPacks.GetScenarios(options.Mode)
            .Where(s => options.Packs.Contains(s.Pack))
            .ToArray();

        if (scenarios.Length == 0) {
            Console.Error.WriteLine("No scenarios matched the selected packs.");
            return 1;
        }

        var results = new List<QrDecodeScenarioResult>(scenarios.Length);
        foreach (var scenario in scenarios) {
            results.Add(RunScenario(scenario, options));
        }

        var report = BuildReport(options, results);
        Console.WriteLine(report);

        var reportsDir = RepoFiles.EnsureReportDirectory();
        var stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        var reportPath = Path.Combine(reportsDir, $"qr-decode-packs-{stamp}-{options.Mode.ToString().ToLowerInvariant()}.txt");
        File.WriteAllText(reportPath, report, Encoding.UTF8);
        Console.WriteLine();
        Console.WriteLine($"Report written: {reportPath}");

        return 0;
    }

    private static QrDecodeScenarioResult RunScenario(QrDecodeScenario scenario, QrDecodePackRunnerOptions options) {
        var data = scenario.CreateData();
        var times = new List<double>(options.Iterations * 4);
        var decodeSuccess = 0;
        var expectedMatch = 0;
        var decodedCountSum = 0;

        // Calibration run to determine ops-per-iteration.
        var calibration = DecodeOnce(data, scenario.Options, scenario.ExpectedTexts);
        times.Add(calibration.ElapsedMilliseconds);
        if (calibration.Decoded) decodeSuccess++;
        if (calibration.ExpectedMatched) expectedMatch++;
        decodedCountSum += calibration.DecodedCount;

        var opsPerIteration = calibration.ElapsedMilliseconds <= 0
            ? 1
            : (int)Math.Ceiling(options.MinIterationMilliseconds / calibration.ElapsedMilliseconds);
        if (opsPerIteration < 1) opsPerIteration = 1;
        if (opsPerIteration > options.OpsCap) opsPerIteration = options.OpsCap;

        var targetRuns = Math.Max(1, options.Iterations * opsPerIteration);
        if (calibration.ElapsedMilliseconds >= 7000) {
            targetRuns = 1;
        } else if (calibration.ElapsedMilliseconds >= options.MinIterationMilliseconds * 4) {
            targetRuns = Math.Min(targetRuns, options.Iterations);
        }
        if (options.Mode == QrPackMode.Full && string.Equals(scenario.Pack, QrDecodeScenarioPacks.Art, StringComparison.OrdinalIgnoreCase)) {
            targetRuns = Math.Min(targetRuns, 2);
        }
        for (var run = 1; run < targetRuns; run++) {
            var res = DecodeOnce(data, scenario.Options, scenario.ExpectedTexts);
            times.Add(res.ElapsedMilliseconds);
            if (res.Decoded) decodeSuccess++;
            if (res.ExpectedMatched) expectedMatch++;
            decodedCountSum += res.DecodedCount;
        }

        return new QrDecodeScenarioResult(
            scenario,
            targetRuns,
            opsPerIteration,
            decodeSuccess,
            expectedMatch,
            decodedCountSum / (double)targetRuns,
            times,
            data.Width,
            data.Height);
    }

    private static DecodeRunResult DecodeOnce(QrDecodeScenarioData data, QrPixelDecodeOptions options, string[]? expectedTexts) {
        var sw = Stopwatch.StartNew();
        QrDecoded[] decoded;
        var okAll = QrDecoder.TryDecodeAll(
            data.Rgba,
            data.Width,
            data.Height,
            data.Stride,
            PixelFormat.Rgba32,
            out decoded,
            out _,
            options);
        if (!okAll || decoded.Length == 0) {
            if (QrDecoder.TryDecode(data.Rgba, data.Width, data.Height, data.Stride, PixelFormat.Rgba32, out var single, out _, options)) {
                decoded = new[] { single };
            } else {
                decoded = Array.Empty<QrDecoded>();
            }
        }
        sw.Stop();

        var decodedCount = decoded.Length;
        var decodedSuccess = decodedCount > 0;
        var expectedMatched = decodedSuccess && ExpectedMatched(decoded, expectedTexts);
        return new DecodeRunResult(decodedSuccess, expectedMatched, decodedCount, sw.Elapsed.TotalMilliseconds);
    }

    private static bool ExpectedMatched(QrDecoded[] decoded, string[]? expectedTexts) {
        if (expectedTexts is null || expectedTexts.Length == 0) return decoded.Length > 0;
        var texts = decoded
            .Select(d => d.Text)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .ToHashSet(StringComparer.Ordinal);
        for (var i = 0; i < expectedTexts.Length; i++) {
            if (!texts.Contains(expectedTexts[i])) return false;
        }
        return true;
    }

    private static string BuildReport(QrDecodePackRunnerOptions options, List<QrDecodeScenarioResult> results) {
        var sb = new StringBuilder(4096);
        var packs = options.Packs.OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToArray();

        sb.AppendLine("QR Decode Scenario Packs");
        sb.AppendLine($"Date (UTC): {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"Mode: {options.Mode}");
        sb.AppendLine($"Packs: {string.Join(", ", packs)}");
        sb.AppendLine($"Iterations: {options.Iterations} (min iteration ms: {options.MinIterationMilliseconds}, ops cap: {options.OpsCap})");
        sb.AppendLine($"Runtime: {RuntimeInformation.FrameworkDescription} | OS: {RuntimeInformation.OSDescription} | Arch: {RuntimeInformation.ProcessArchitecture}");
        sb.AppendLine($"CPU: {Environment.ProcessorCount} logical cores | GC: {(GCSettings.IsServerGC ? "Server" : "Workstation")}");
        sb.AppendLine();
        sb.AppendLine("Interpretation:");
        sb.AppendLine("- decode% = any QR decoded");
        sb.AppendLine("- expected% = expected payload(s) decoded");
        sb.AppendLine("- ideal packs should be ~100%; stress/art packs track reliability progress");
        sb.AppendLine();

        foreach (var group in results.GroupBy(r => r.Scenario.Pack).OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)) {
            var packSummary = Summarize(group.ToList());
            sb.AppendLine($"Pack: {group.Key}  scenarios={group.Count()}  runs={packSummary.Runs}  decode%={packSummary.DecodeRate:P0}  expected%={packSummary.ExpectedRate:P0}  medianMs={packSummary.MedianMs:F1}  p95Ms={packSummary.P95Ms:F1}");
            foreach (var result in group.OrderBy(r => r.Scenario.Name, StringComparer.OrdinalIgnoreCase)) {
                var median = Percentile(result.Times, 0.50);
                var p95 = Percentile(result.Times, 0.95);
                var decodeRate = result.DecodeSuccess / (double)result.Runs;
                var expectedRate = result.ExpectedMatch / (double)result.Runs;
                var expectedLabel = result.Scenario.ExpectedTexts is null ? "any" : TruncateExpected(result.Scenario.ExpectedTexts);
                sb.AppendLine(
                    $"  - {result.Scenario.Name,-28} size={result.Width}x{result.Height} ops={result.OpsPerIteration,2} decode%={decodeRate,6:P0} expected%={expectedRate,6:P0} medianMs={median,7:F1} p95Ms={p95,7:F1} decoded~={result.AvgDecodedCount,4:F1} expected={expectedLabel}");
            }
            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }

    private static PackSummary Summarize(List<QrDecodeScenarioResult> results) {
        var runs = results.Sum(r => r.Runs);
        var decode = results.Sum(r => r.DecodeSuccess);
        var expected = results.Sum(r => r.ExpectedMatch);
        var allTimes = results.SelectMany(r => r.Times).ToArray();
        return new PackSummary(
            runs,
            decode / (double)Math.Max(1, runs),
            expected / (double)Math.Max(1, runs),
            Percentile(allTimes, 0.50),
            Percentile(allTimes, 0.95));
    }

    private static double Percentile(IReadOnlyList<double> values, double percentile) {
        if (values.Count == 0) return 0;
        var ordered = values.OrderBy(v => v).ToArray();
        var idx = (int)Math.Ceiling(percentile * (ordered.Length - 1));
        if (idx < 0) idx = 0;
        if (idx >= ordered.Length) idx = ordered.Length - 1;
        return ordered[idx];
    }

    private static string TruncateExpected(string[] expectedTexts) {
        var joined = string.Join("|", expectedTexts);
        return joined.Length <= 80 ? joined : joined[..77] + "...";
    }

    private static HashSet<string> ResolvePacks(List<string> requestedPacks, QrPackMode mode) {
        var defaults = mode == QrPackMode.Quick ? QuickDefaultPacks : QrDecodeScenarioPacks.AllPacks;
        if (requestedPacks.Count == 0) {
            return new HashSet<string>(defaults, StringComparer.OrdinalIgnoreCase);
        }

        var valid = new HashSet<string>(QrDecodeScenarioPacks.AllPacks, StringComparer.OrdinalIgnoreCase);
        var packs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pack in requestedPacks) {
            if (valid.Contains(pack)) packs.Add(pack);
        }

        if (packs.Count == 0) {
            return new HashSet<string>(defaults, StringComparer.OrdinalIgnoreCase);
        }

        return packs;
    }

    private static void AddPacks(List<string> packList, string packArg) {
        if (string.IsNullOrWhiteSpace(packArg)) return;
        var parts = packArg.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        for (var i = 0; i < parts.Length; i++) {
            var pack = parts[i].Trim();
            if (pack.Length > 0) packList.Add(pack);
        }
    }

    private static QrPackMode ParseMode(string? value) {
        if (string.Equals(value, "full", StringComparison.OrdinalIgnoreCase)) return QrPackMode.Full;
        return QrPackMode.Quick;
    }

    private readonly record struct DecodeRunResult(bool Decoded, bool ExpectedMatched, int DecodedCount, double ElapsedMilliseconds);

    private sealed class QrDecodeScenarioResult {
        public QrDecodeScenarioResult(
            QrDecodeScenario scenario,
            int runs,
            int opsPerIteration,
            int decodeSuccess,
            int expectedMatch,
            double avgDecodedCount,
            List<double> times,
            int width,
            int height) {
            Scenario = scenario;
            Runs = runs;
            OpsPerIteration = opsPerIteration;
            DecodeSuccess = decodeSuccess;
            ExpectedMatch = expectedMatch;
            AvgDecodedCount = avgDecodedCount;
            Times = times;
            Width = width;
            Height = height;
        }

        public QrDecodeScenario Scenario { get; }
        public int Runs { get; }
        public int OpsPerIteration { get; }
        public int DecodeSuccess { get; }
        public int ExpectedMatch { get; }
        public double AvgDecodedCount { get; }
        public List<double> Times { get; }
        public int Width { get; }
        public int Height { get; }
    }

    private readonly record struct PackSummary(int Runs, double DecodeRate, double ExpectedRate, double MedianMs, double P95Ms);
}
