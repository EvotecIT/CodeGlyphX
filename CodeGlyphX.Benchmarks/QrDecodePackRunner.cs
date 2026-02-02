using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime;
using System.Text;
using System.Text.Json;
using CodeGlyphX.Rendering;

namespace CodeGlyphX.Benchmarks;

[Flags]
internal enum QrReportFormats {
    Text = 1,
    Json = 2,
    Csv = 4
}

internal sealed class QrDecodePackRunnerOptions {
    public required QrPackMode Mode { get; init; }
    public required HashSet<string> Packs { get; init; }
    public required IReadOnlyList<IQrDecodeEngine> Engines { get; init; }
    public required IReadOnlyList<string> ScenarioFilters { get; init; }
    public required QrReportFormats ReportFormats { get; init; }
    public string? ReportsDirectory { get; init; }
    public required int Iterations { get; init; }
    public required int MinIterationMilliseconds { get; init; }
    public required int OpsCap { get; init; }
    public required int ExternalRunsCap { get; init; }
}

internal static class QrDecodePackRunner {
    private static readonly string[] QuickDefaultPacks = {
        QrDecodeScenarioPacks.Ideal,
        QrDecodeScenarioPacks.Stress,
        QrDecodeScenarioPacks.Screenshot,
        QrDecodeScenarioPacks.Multi,
        QrDecodeScenarioPacks.Art
    };

    public static bool TryParseArgs(string[] args, out QrDecodePackRunnerOptions options, out string[] remainingArgs) {
        options = null!;
        var remaining = new List<string>(args.Length);
        var packList = new List<string>(8);
        var engineList = new List<string>(4);
        var scenarioList = new List<string>(4);
        var runRequested = false;

        QrPackMode? mode = null;
        int? iterations = null;
        int? minIterMs = null;
        int? opsCap = null;
        QrReportFormats? formats = null;
        string? reportsDir = null;

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

            if ((string.Equals(arg, "--scenario", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(arg, "--scenarios", StringComparison.OrdinalIgnoreCase)) && i + 1 < args.Length) {
                runRequested = true;
                AddScenarios(scenarioList, args[++i]);
                continue;
            }

            if ((string.Equals(arg, "--engine", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(arg, "--engines", StringComparison.OrdinalIgnoreCase)) && i + 1 < args.Length) {
                runRequested = true;
                AddEngines(engineList, args[++i]);
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

            if ((string.Equals(arg, "--reports-dir", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(arg, "--reports-path", StringComparison.OrdinalIgnoreCase)) && i + 1 < args.Length) {
                runRequested = true;
                reportsDir = args[++i];
                continue;
            }

            if (string.Equals(arg, "--format", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length) {
                runRequested = true;
                formats = ParseFormats(args[++i], formats);
                continue;
            }

            if (string.Equals(arg, "--json", StringComparison.OrdinalIgnoreCase)) {
                runRequested = true;
                formats = (formats ?? QrReportFormats.Text) | QrReportFormats.Json;
                continue;
            }

            if (string.Equals(arg, "--csv", StringComparison.OrdinalIgnoreCase)) {
                runRequested = true;
                formats = (formats ?? QrReportFormats.Text) | QrReportFormats.Csv;
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

        var resolvedMode = mode ?? ResolveModeFromBenchQuickEnv() ?? QrPackMode.Quick;
        var resolvedIterations = iterations ?? (resolvedMode == QrPackMode.Quick ? 3 : 5);
        var resolvedMinIterMs = minIterMs ?? (resolvedMode == QrPackMode.Quick ? 400 : 800);
        var resolvedOpsCap = opsCap ?? 12;
        var externalRunsCap = resolvedMode == QrPackMode.Quick ? 2 : 3;
        var resolvedFormats = (formats ?? QrReportFormats.Text) | QrReportFormats.Text;
        reportsDir ??= Environment.GetEnvironmentVariable("CODEGLYPHX_PACK_REPORTS_DIR");

        var packs = ResolvePacks(packList, resolvedMode);
        var availableEngines = QrDecodeEngines.Create();
        var engines = ResolveEngines(availableEngines, engineList);
        var scenarioFilters = ResolveScenarioFilters(scenarioList);
        options = new QrDecodePackRunnerOptions {
            Mode = resolvedMode,
            Packs = packs,
            Engines = engines,
            ScenarioFilters = scenarioFilters,
            ReportFormats = resolvedFormats,
            ReportsDirectory = reportsDir,
            Iterations = resolvedIterations,
            MinIterationMilliseconds = resolvedMinIterMs,
            OpsCap = resolvedOpsCap,
            ExternalRunsCap = externalRunsCap
        };

        return true;
    }

    public static int Run(QrDecodePackRunnerOptions options) {
        var scenarios = QrDecodeScenarioPacks.GetScenarios(options.Mode)
            .Where(s => options.Packs.Contains(s.Pack))
            .Where(s => MatchesScenarioFilters(s.Name, options.ScenarioFilters))
            .ToArray();

        if (scenarios.Length == 0) {
            Console.Error.WriteLine("No scenarios matched the selected packs.");
            return 1;
        }

        var nowUtc = DateTime.UtcNow;
        var hasCustomReportsDir = !string.IsNullOrWhiteSpace(options.ReportsDirectory);
        var reportsDir = hasCustomReportsDir
            ? Path.GetFullPath(options.ReportsDirectory!)
            : RepoFiles.EnsureReportDirectory();
        Directory.CreateDirectory(reportsDir);
        var dumpFailures = ShouldDumpFailures();

        var results = new List<QrDecodeScenarioResult>(scenarios.Length * options.Engines.Count);
        foreach (var engine in options.Engines) {
            foreach (var scenario in scenarios) {
                var data = scenario.CreateData();
                results.Add(RunScenario(engine, scenario, data, options, reportsDir, nowUtc, dumpFailures));
            }
        }

        var report = BuildReport(options, results, nowUtc);
        Console.WriteLine(report);

        var modeName = options.Mode.ToString().ToLowerInvariant();
        var stamp = nowUtc.ToString("yyyyMMdd-HHmmss");
        var baseName = hasCustomReportsDir
            ? $"qr-decode-packs-{modeName}"
            : $"qr-decode-packs-{stamp}-{modeName}";
        var reportPath = Path.Combine(reportsDir, baseName + ".txt");
        File.WriteAllText(reportPath, report, Encoding.UTF8);

        var written = new List<string>(3) { reportPath };
        if (options.ReportFormats.HasFlag(QrReportFormats.Json)) {
            var jsonPath = Path.Combine(reportsDir, baseName + ".json");
            var json = BuildJsonReport(options, results, nowUtc);
            File.WriteAllText(jsonPath, json, Encoding.UTF8);
            written.Add(jsonPath);
        }
        if (options.ReportFormats.HasFlag(QrReportFormats.Csv)) {
            var csvPath = Path.Combine(reportsDir, baseName + ".csv");
            var csv = BuildCsvReport(options, results, nowUtc);
            File.WriteAllText(csvPath, csv, Encoding.UTF8);
            written.Add(csvPath);
        }

        Console.WriteLine();
        Console.WriteLine($"Reports written: {string.Join(", ", written)}");

        return 0;
    }

    private static QrDecodeScenarioResult RunScenario(IQrDecodeEngine engine, QrDecodeScenario scenario, QrDecodeScenarioData data, QrDecodePackRunnerOptions options, string reportsDir, DateTime nowUtc, bool dumpFailures) {
        var times = new List<double>(options.Iterations * 4);
        var infos = new List<QrPixelDecodeInfo>(options.Iterations * 4);
        var decodeSuccess = 0;
        var expectedMatch = 0;
        var decodedCountSum = 0;

        // Calibration run to determine ops-per-iteration.
        var calibration = DecodeOnce(engine, data, scenario.Options, scenario.ExpectedTexts);
        times.Add(calibration.ElapsedMilliseconds);
        if (calibration.Info is { } calibrationInfo) infos.Add(calibrationInfo);
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
        } else if (calibration.ElapsedMilliseconds >= (long)options.MinIterationMilliseconds * 4L) {
            targetRuns = Math.Min(targetRuns, options.Iterations);
        }
        if (options.Mode == QrPackMode.Full && string.Equals(scenario.Pack, QrDecodeScenarioPacks.Art, StringComparison.OrdinalIgnoreCase)) {
            targetRuns = Math.Min(targetRuns, 2);
        }
        if (engine.IsExternal) {
            targetRuns = Math.Min(targetRuns, options.ExternalRunsCap);
        }
        for (var run = 1; run < targetRuns; run++) {
            var res = DecodeOnce(engine, data, scenario.Options, scenario.ExpectedTexts);
            times.Add(res.ElapsedMilliseconds);
            if (res.Info is { } runInfo) infos.Add(runInfo);
            if (res.Decoded) decodeSuccess++;
            if (res.ExpectedMatched) expectedMatch++;
            decodedCountSum += res.DecodedCount;
        }

        if (dumpFailures &&
            (decodeSuccess == 0 ||
             (scenario.ExpectedTexts is { Length: > 0 } && expectedMatch == 0))) {
            DumpFailureDebug(reportsDir, nowUtc, engine, scenario, data);
        }

        return new QrDecodeScenarioResult(
            scenario,
            targetRuns,
            opsPerIteration,
            decodeSuccess,
            expectedMatch,
            decodedCountSum / (double)targetRuns,
            times,
            infos,
            engine.Name,
            engine.IsExternal,
            data.Width,
            data.Height);
    }

    private static DecodeRunResult DecodeOnce(IQrDecodeEngine engine, QrDecodeScenarioData data, QrPixelDecodeOptions options, string[]? expectedTexts) {
        var sw = Stopwatch.StartNew();
        var result = engine.Decode(data, options);
        sw.Stop();

        var decodedCount = result.Count;
        var decodedSuccess = result.Success;
        var expectedMatched = decodedSuccess && ExpectedMatched(result.Texts, expectedTexts);
        return new DecodeRunResult(decodedSuccess, expectedMatched, decodedCount, sw.Elapsed.TotalMilliseconds, result.Info);
    }

    private static bool ExpectedMatched(string[] decodedTexts, string[]? expectedTexts) {
        if (expectedTexts is null || expectedTexts.Length == 0) return decodedTexts.Length > 0;
        var texts = decodedTexts
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .ToHashSet(StringComparer.Ordinal);
        for (var i = 0; i < expectedTexts.Length; i++) {
            if (!texts.Contains(expectedTexts[i])) return false;
        }
        return true;
    }

    private static bool ShouldDumpFailures() {
        var value = Environment.GetEnvironmentVariable("CODEGLYPHX_BENCH_DUMP_FAILS");
        return string.Equals(value, "1", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
    }

    private static void DumpFailureDebug(string reportsDir, DateTime nowUtc, IQrDecodeEngine engine, QrDecodeScenario scenario, QrDecodeScenarioData data) {
        var root = Path.Combine(reportsDir, $"qr-decode-failures-{nowUtc:yyyyMMdd-HHmmss}");
        var packDir = Path.Combine(root, SanitizeFileName(scenario.Pack));
        var engineDir = Path.Combine(packDir, SanitizeFileName(engine.Name));
        Directory.CreateDirectory(engineDir);

        var baseName = SanitizeFileName(scenario.Name);
        var opts = new global::CodeGlyphX.QrPixelDebugOptions();
        global::CodeGlyphX.QrPixelDebug.RenderToFile(data.Rgba, data.Width, data.Height, data.Stride, PixelFormat.Rgba32, global::CodeGlyphX.QrPixelDebugMode.Grayscale, engineDir, $"{baseName}-gray.png", opts);
        global::CodeGlyphX.QrPixelDebug.RenderToFile(data.Rgba, data.Width, data.Height, data.Stride, PixelFormat.Rgba32, global::CodeGlyphX.QrPixelDebugMode.Threshold, engineDir, $"{baseName}-threshold.png", opts);
        global::CodeGlyphX.QrPixelDebug.RenderToFile(data.Rgba, data.Width, data.Height, data.Stride, PixelFormat.Rgba32, global::CodeGlyphX.QrPixelDebugMode.Binarized, engineDir, $"{baseName}-bin.png", opts);
        global::CodeGlyphX.QrPixelDebug.RenderToFile(data.Rgba, data.Width, data.Height, data.Stride, PixelFormat.Rgba32, global::CodeGlyphX.QrPixelDebugMode.Heatmap, engineDir, $"{baseName}-heatmap.png", opts);

        var adaptive = new global::CodeGlyphX.QrPixelDebugOptions {
            AdaptiveThreshold = true,
            AdaptiveWindowSize = 25,
            AdaptiveOffset = 4
        };
        global::CodeGlyphX.QrPixelDebug.RenderToFile(data.Rgba, data.Width, data.Height, data.Stride, PixelFormat.Rgba32, global::CodeGlyphX.QrPixelDebugMode.Binarized, engineDir, $"{baseName}-bin-adaptive.png", adaptive);

        var metaPath = Path.Combine(engineDir, $"{baseName}-meta.txt");
        var metaLines = new List<string> {
            $"Pack: {scenario.Pack}",
            $"Scenario: {scenario.Name}",
            $"Engine: {engine.Name}",
            $"Size: {data.Width}x{data.Height}",
            $"Options: {FormatOptions(scenario.Options)}",
            $"Expected: {(scenario.ExpectedTexts is null ? "any" : string.Join(" | ", scenario.ExpectedTexts))}"
        };

        var baseRatio = ComputeBinarizedBlackRatio(data, new global::CodeGlyphX.QrPixelDebugOptions());
        if (baseRatio >= 0) metaLines.Add($"BinarizedBlack%: {baseRatio:P1}");
        var adaptiveRatio = ComputeBinarizedBlackRatio(data, new global::CodeGlyphX.QrPixelDebugOptions {
            AdaptiveThreshold = true,
            AdaptiveWindowSize = 25,
            AdaptiveOffset = 4
        });
        if (adaptiveRatio >= 0) metaLines.Add($"BinarizedAdaptiveBlack%: {adaptiveRatio:P1}");

        try {
            var engineResult = engine.Decode(data, scenario.Options);
            metaLines.Add($"EngineDecodedCount: {engineResult.Count}");
            metaLines.Add($"EngineSuccess: {engineResult.Success}");
            if (engineResult.Texts.Length > 0) {
                for (var i = 0; i < engineResult.Texts.Length; i++) {
                    var text = engineResult.Texts[i] ?? string.Empty;
                    var textPreview = text.Length > 400 ? text[..400] + "..." : text;
                    metaLines.Add($"EngineText[{i}]: textLen={text.Length} text=\"{textPreview}\"");
                }
            }
        } catch (Exception ex) {
            metaLines.Add($"EngineDecodeError: {ex.GetType().Name} {ex.Message}");
        }

        var prevModuleDumpDir = Environment.GetEnvironmentVariable("CODEGLYPHX_QR_MODULE_DUMP_DIR");
        var prevModuleDumpLimit = Environment.GetEnvironmentVariable("CODEGLYPHX_QR_MODULE_DUMP_LIMIT");
        Environment.SetEnvironmentVariable("CODEGLYPHX_QR_MODULE_DUMP_DIR", Path.Combine(engineDir, "modules", baseName));
        if (string.IsNullOrWhiteSpace(prevModuleDumpLimit)) {
            Environment.SetEnvironmentVariable("CODEGLYPHX_QR_MODULE_DUMP_LIMIT", "3");
        }
        try {
            if (QrDecoder.TryDecodeAll(data.Rgba, data.Width, data.Height, data.Stride, PixelFormat.Rgba32, out var decoded, out var info, scenario.Options)) {
                metaLines.Add($"DecodedCount: {decoded.Length}");
                metaLines.Add($"InfoAll: {info}");
                for (var i = 0; i < decoded.Length; i++) {
                    var text = decoded[i].Text ?? string.Empty;
                    var textPreview = text.Length > 120 ? text[..120] + "..." : text;
                    metaLines.Add($"Result[{i}]: v{decoded[i].Version} {decoded[i].ErrorCorrectionLevel} m{decoded[i].Mask} textLen={text.Length} text=\"{textPreview}\"");
                }
            } else {
                metaLines.Add($"InfoAll: {info}");
            }

            if (QrDecoder.TryDecode(data.Rgba, data.Width, data.Height, data.Stride, PixelFormat.Rgba32, out var single, out var infoSingle, scenario.Options)) {
                var text = single.Text ?? string.Empty;
                var textPreview = text.Length > 120 ? text[..120] + "..." : text;
                metaLines.Add($"Single: v{single.Version} {single.ErrorCorrectionLevel} m{single.Mask} textLen={text.Length} text=\"{textPreview}\"");
                metaLines.Add($"InfoSingle: {infoSingle}");
            } else {
                metaLines.Add($"InfoSingle: {infoSingle}");
            }
        } finally {
            Environment.SetEnvironmentVariable("CODEGLYPHX_QR_MODULE_DUMP_DIR", prevModuleDumpDir);
            Environment.SetEnvironmentVariable("CODEGLYPHX_QR_MODULE_DUMP_LIMIT", prevModuleDumpLimit);
        }

        File.WriteAllLines(metaPath, metaLines);
    }

    private static double ComputeBinarizedBlackRatio(QrDecodeScenarioData data, global::CodeGlyphX.QrPixelDebugOptions options) {
        try {
            var pixels = global::CodeGlyphX.QrPixelDebug.RenderPixels(
                data.Rgba,
                data.Width,
                data.Height,
                data.Stride,
                PixelFormat.Rgba32,
                global::CodeGlyphX.QrPixelDebugMode.Binarized,
                out var widthPx,
                out var heightPx,
                out var stridePx,
                options);

            if (widthPx <= 0 || heightPx <= 0 || stridePx <= 0) return -1;
            var black = 0;
            var total = widthPx * heightPx;
            for (var y = 0; y < heightPx; y++) {
                var row = y * stridePx;
                var p = row;
                for (var x = 0; x < widthPx; x++, p += 4) {
                    if (pixels[p] == 0) black++;
                }
            }
            return total == 0 ? -1 : black / (double)total;
        } catch {
            return -1;
        }
    }

    private static string SanitizeFileName(string name) {
        if (string.IsNullOrWhiteSpace(name)) return "unnamed";
        var invalid = Path.GetInvalidFileNameChars();
        var buffer = name.ToCharArray();
        for (var i = 0; i < buffer.Length; i++) {
            if (invalid.Contains(buffer[i])) buffer[i] = '_';
        }
        return new string(buffer);
    }

    private static string BuildReport(QrDecodePackRunnerOptions options, List<QrDecodeScenarioResult> results, DateTime nowUtc) {
        var sb = new StringBuilder(4096);
        var packs = options.Packs.OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToArray();

        sb.AppendLine("QR Decode Scenario Packs");
        sb.AppendLine($"Date (UTC): {nowUtc:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"Mode: {options.Mode}");
        sb.AppendLine($"Packs: {string.Join(", ", packs)}");
        if (options.ScenarioFilters.Count > 0) {
            sb.AppendLine($"Scenarios: {string.Join(", ", options.ScenarioFilters)}");
        }
        sb.AppendLine($"Iterations: {options.Iterations} (min iteration ms: {options.MinIterationMilliseconds}, ops cap: {options.OpsCap})");
        sb.AppendLine($"Engines: {string.Join(", ", options.Engines.Select(e => e.Name))} (external runs cap: {options.ExternalRunsCap})");
        sb.AppendLine($"Runtime: {RuntimeInformation.FrameworkDescription} | OS: {RuntimeInformation.OSDescription} | Arch: {RuntimeInformation.ProcessArchitecture}");
        sb.AppendLine($"CPU: {Environment.ProcessorCount} logical cores | GC: {(GCSettings.IsServerGC ? "Server" : "Workstation")}");
        sb.AppendLine();
        sb.AppendLine("Interpretation:");
        sb.AppendLine("- decode% = any QR decoded");
        sb.AppendLine("- expected% = expected payload(s) decoded");
        sb.AppendLine("- ideal packs should be ~100%; stress/art packs track reliability progress");
        sb.AppendLine("- external engines run fewer reps to keep runs tractable");
        sb.AppendLine("- opt = decode option summary for the scenario");
        sb.AppendLine("- diag = median diagnostics (scale/threshold/invert/candidates/dimension) when available");
        sb.AppendLine();

        foreach (var group in results.GroupBy(r => r.Scenario.Pack).OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)) {
            var scenarioCount = group.Select(r => r.Scenario.Name).Distinct(StringComparer.Ordinal).Count();
            var info = QrDecodeScenarioPacks.GetPackInfo(group.Key);
            var categoryLabel = string.IsNullOrWhiteSpace(info.Category) ? string.Empty : $" ({info.Category})";
            sb.AppendLine($"Pack: {group.Key}{categoryLabel}  scenarios={scenarioCount}");
            if (!string.IsNullOrWhiteSpace(info.Description)) sb.AppendLine($"  Notes: {info.Description}");
            if (!string.IsNullOrWhiteSpace(info.Guidance)) sb.AppendLine($"  Guidance: {info.Guidance}");

            var engineSummaries = group
                .GroupBy(r => r.EngineName)
                .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
                .Select(g => (Name: g.Key, Summary: Summarize(g.ToList())))
                .ToArray();
            var baselineName = engineSummaries
                .Select(s => s.Name)
                .FirstOrDefault(n => string.Equals(n, "CodeGlyphX", StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(baselineName) && engineSummaries.Length > 1) {
                var baseline = engineSummaries.First(s => string.Equals(s.Name, baselineName, StringComparison.OrdinalIgnoreCase)).Summary;
                sb.AppendLine("  Delta vs CodeGlyphX (pack summary):");
                foreach (var summary in engineSummaries.Where(s => !string.Equals(s.Name, baselineName, StringComparison.OrdinalIgnoreCase))) {
                    var decodeDelta = FormatRateDelta(summary.Summary.DecodeRate, baseline.DecodeRate);
                    var expectedDelta = FormatRateDelta(summary.Summary.ExpectedRate, baseline.ExpectedRate);
                    var medianDelta = FormatMsDelta(summary.Summary.MedianMs, baseline.MedianMs);
                    var p95Delta = FormatMsDelta(summary.Summary.P95Ms, baseline.P95Ms);
                    sb.AppendLine($"    - {summary.Name,-14} decode {decodeDelta} expected {expectedDelta} median {medianDelta} p95 {p95Delta}");
                }
            }

            foreach (var engineGroup in group.GroupBy(r => r.EngineName).OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)) {
                var packSummary = Summarize(engineGroup.ToList());
                var externalTag = engineGroup.First().EngineIsExternal ? " (external)" : string.Empty;
                sb.AppendLine($"  Engine: {engineGroup.Key}{externalTag}  runs={packSummary.Runs}  decode%={packSummary.DecodeRate:P0}  expected%={packSummary.ExpectedRate:P0}  medianMs={packSummary.MedianMs:F1}  p95Ms={packSummary.P95Ms:F1}");
                var failureBreakdown = FormatFailureBreakdown(engineGroup);
                if (!string.IsNullOrWhiteSpace(failureBreakdown)) {
                    sb.AppendLine($"    Failures: {failureBreakdown}");
                }
                foreach (var result in engineGroup.OrderBy(r => r.Scenario.Name, StringComparer.OrdinalIgnoreCase)) {
                    var median = Percentile(result.Times, 0.50);
                    var p95 = Percentile(result.Times, 0.95);
                    var decodeRate = result.DecodeSuccess / (double)result.Runs;
                    var expectedRate = result.ExpectedMatch / (double)result.Runs;
                    var expectedLabel = result.Scenario.ExpectedTexts is null ? "any" : TruncateExpected(result.Scenario.ExpectedTexts);
                    var optionsLabel = FormatOptions(result.Scenario.Options);
                    var diagSummary = SummarizeDiagnostics(result.Infos);
                    var diagLabel = diagSummary is null ? "diag=n/a" : $"diag={FormatDiagnostics(diagSummary.Value)}";
                    sb.AppendLine(
                        $"    - {result.Scenario.Name,-26} size={result.Width}x{result.Height} ops={result.OpsPerIteration,2} decode%={decodeRate,6:P0} expected%={expectedRate,6:P0} medianMs={median,7:F1} p95Ms={p95,7:F1} decoded~={result.AvgDecodedCount,4:F1} expected={expectedLabel} opt={optionsLabel} {diagLabel}");
                }

                var slowest = engineGroup
                    .Select(r => new {
                        r.Scenario.Name,
                        Median = Percentile(r.Times, 0.50),
                        P95 = Percentile(r.Times, 0.95)
                    })
                    .OrderByDescending(r => r.Median)
                    .ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
                    .Take(3)
                    .ToArray();
                if (slowest.Length > 0) {
                    sb.AppendLine("    Slowest (median ms):");
                    for (var i = 0; i < slowest.Length; i++) {
                        sb.AppendLine($"      - {slowest[i].Name,-26} medianMs={slowest[i].Median,7:F1} p95Ms={slowest[i].P95,7:F1}");
                    }
                }
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

    private static string FormatOptions(QrPixelDecodeOptions options) {
        var tile = options.EnableTileScan ? options.TileGrid : 0;
        return $"p={options.Profile} md={options.MaxDimension} ms={options.MaxMilliseconds} bud={options.BudgetMilliseconds} sc={options.MaxScale} crop={(options.AutoCrop ? 1 : 0)} tile={tile} agg={(options.AggressiveSampling ? 1 : 0)} styl={(options.StylizedSampling ? 1 : 0)} xform={(options.DisableTransforms ? 1 : 0)}";
    }

    private static DiagnosticSummary? SummarizeDiagnostics(IReadOnlyList<QrPixelDecodeInfo> infos) {
        if (infos is null || infos.Count == 0) return null;
        var scales = infos.Select(i => i.Scale).ToArray();
        var thresholds = infos.Select(i => (int)i.Threshold).ToArray();
        var candidateCounts = infos.Select(i => i.CandidateCount).ToArray();
        var tripleCounts = infos.Select(i => i.CandidateTriplesTried).ToArray();
        var dimensions = infos.Select(i => i.Dimension).ToArray();
        var invertRate = infos.Count(i => i.Invert) / (double)infos.Count;
        var successRate = infos.Count(i => i.Module.IsSuccess) / (double)infos.Count;
        var topFailure = infos
            .GroupBy(i => i.Module.Failure)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key)
            .First().Key;

        return new DiagnosticSummary(
            MedianInt(scales),
            MedianInt(thresholds),
            invertRate,
            MedianInt(candidateCounts),
            MedianInt(tripleCounts),
            MedianInt(dimensions),
            successRate,
            topFailure);
    }

    private static int MedianInt(IReadOnlyList<int> values) {
        if (values.Count == 0) return 0;
        var ordered = values.OrderBy(v => v).ToArray();
        var idx = (int)Math.Ceiling(0.50 * (ordered.Length - 1));
        if (idx < 0) idx = 0;
        if (idx >= ordered.Length) idx = ordered.Length - 1;
        return ordered[idx];
    }

    private static string FormatDiagnostics(DiagnosticSummary summary) {
        var topFailure = FormatFailure(summary.TopFailure);
        return $"s{summary.ScaleMedian} t{summary.ThresholdMedian} inv{summary.InvertRate:P0} cand{summary.CandidateMedian} tri{summary.TriplesMedian} dim{summary.DimensionMedian} ok{summary.SuccessRate:P0} fail={topFailure}";
    }

    private static string FormatRateDelta(double value, double baseline) {
        return $"{(value - baseline) * 100:+0;-0;0}pt";
    }

    private static string FormatMsDelta(double value, double baseline) {
        if (baseline <= 0) return "n/a";
        return $"{(value - baseline) / baseline * 100:+0;-0;0}%";
    }

    private static string FormatFailure(QrDecodeFailureReason reason) {
        return reason == QrDecodeFailureReason.None ? "ok" : reason.ToString().ToLowerInvariant();
    }

    private static string? FormatFailureBreakdown(IEnumerable<QrDecodeScenarioResult> results) {
        var counts = new Dictionary<QrDecodeFailureReason, int>();
        var total = 0;
        foreach (var info in results.SelectMany(r => r.Infos)) {
            var failure = info.Module.Failure;
            if (failure == QrDecodeFailureReason.None) continue;
            if (!counts.TryGetValue(failure, out var count)) count = 0;
            counts[failure] = count + 1;
            total++;
        }

        if (total == 0) return null;

        var ordered = counts
            .OrderByDescending(kv => kv.Value)
            .ThenBy(kv => kv.Key)
            .ToArray();
        var take = Math.Min(3, ordered.Length);
        var parts = new List<string>(take + 1);
        var topSum = 0;
        for (var i = 0; i < take; i++) {
            var (failure, count) = ordered[i];
            topSum += count;
            parts.Add($"{FormatFailure(failure)}={count} ({count / (double)total:P0})");
        }
        if (ordered.Length > take) {
            var remaining = total - topSum;
            if (remaining > 0) {
                parts.Add($"+{remaining} other ({remaining / (double)total:P0})");
            }
        }

        return string.Join(", ", parts);
    }

    private static string BuildJsonReport(QrDecodePackRunnerOptions options, List<QrDecodeScenarioResult> results, DateTime nowUtc) {
        var model = BuildReportModel(options, results, nowUtc);
        var jsonOptions = new JsonSerializerOptions {
            WriteIndented = true
        };
        return JsonSerializer.Serialize(model, jsonOptions);
    }

    private static string BuildCsvReport(QrDecodePackRunnerOptions options, List<QrDecodeScenarioResult> results, DateTime nowUtc) {
        var model = BuildReportModel(options, results, nowUtc);
        var sb = new StringBuilder(8192);
        sb.AppendLine("dateUtc,mode,pack,packCategory,engine,isExternal,scenario,width,height,runs,opsPerIteration,decodeRate,expectedRate,medianMs,p95Ms,avgDecodedCount,expected,options,diagScaleMedian,diagThresholdMedian,diagInvertRate,diagCandidateMedian,diagTriplesMedian,diagDimensionMedian,diagSuccessRate,diagTopFailure");
        sb.AppendLine("dateUtc,mode,pack,packCategory,engine,isExternal,scenario,width,height,runs,opsPerIteration,decodeRate,expectedRate,medianMs,p95Ms,avgDecodedCount,expected,options,diagScaleMedian,diagThresholdMedian,diagInvertRate,diagCandidateMedian,diagTriplesMedian,diagDimensionMedian,diagSuccessRate,diagTopFailure");

        var date = nowUtc.ToString("O");
        foreach (var pack in model.Packs) {
            foreach (var engine in pack.Engines) {
                foreach (var scenario in engine.Scenarios) {
                    sb.Append(date).Append(',');
                    sb.Append(model.Mode).Append(',');
                    sb.Append(pack.Name).Append(',');
                    sb.Append(pack.Category).Append(',');
                    sb.Append(engine.Name).Append(',');
                    sb.Append(engine.IsExternal ? "true" : "false").Append(',');
                    sb.Append(scenario.Name).Append(',');
                    sb.Append(scenario.Width).Append(',');
                    sb.Append(scenario.Height).Append(',');
                    sb.Append(scenario.Runs).Append(',');
                    sb.Append(scenario.OpsPerIteration).Append(',');
                    sb.Append(scenario.DecodeRate.ToString("F4")).Append(',');
                    sb.Append(scenario.ExpectedRate.ToString("F4")).Append(',');
                    sb.Append(scenario.MedianMs.ToString("F2")).Append(',');
                    sb.Append(scenario.P95Ms.ToString("F2")).Append(',');
                    sb.Append(scenario.AvgDecodedCount.ToString("F2")).Append(',');
                    sb.Append(EscapeCsv(scenario.Expected));
                    sb.Append(',');
                    sb.Append(EscapeCsv(scenario.Options));
                    sb.Append(',');
                    sb.Append(FormatCsvInt(scenario.DiagScaleMedian)).Append(',');
                    sb.Append(FormatCsvInt(scenario.DiagThresholdMedian)).Append(',');
                    sb.Append(FormatCsvDouble(scenario.DiagInvertRate)).Append(',');
                    sb.Append(FormatCsvInt(scenario.DiagCandidateMedian)).Append(',');
                    sb.Append(FormatCsvInt(scenario.DiagTriplesMedian)).Append(',');
                    sb.Append(FormatCsvInt(scenario.DiagDimensionMedian)).Append(',');
                    sb.Append(FormatCsvDouble(scenario.DiagSuccessRate)).Append(',');
                    sb.Append(EscapeCsv(scenario.DiagTopFailure ?? string.Empty));
                    sb.AppendLine();
                }
            }
        }

        return sb.ToString().TrimEnd();
    }

    private static ReportModel BuildReportModel(QrDecodePackRunnerOptions options, List<QrDecodeScenarioResult> results, DateTime nowUtc) {
        var packs = results
            .GroupBy(r => r.Scenario.Pack)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .Select(packGroup => {
                var scenarioCount = packGroup.Select(r => r.Scenario.Name).Distinct(StringComparer.Ordinal).Count();
                var engines = packGroup
                    .GroupBy(r => r.EngineName)
                    .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
                    .Select(engineGroup => {
                        var packSummary = Summarize(engineGroup.ToList());
                        var scenarios = engineGroup
                            .OrderBy(r => r.Scenario.Name, StringComparer.OrdinalIgnoreCase)
                            .Select(result => {
                                var median = Percentile(result.Times, 0.50);
                                var p95 = Percentile(result.Times, 0.95);
                                var decodeRate = result.DecodeSuccess / (double)result.Runs;
                                var expectedRate = result.ExpectedMatch / (double)result.Runs;
                                var expectedLabel = result.Scenario.ExpectedTexts is null ? "any" : TruncateExpected(result.Scenario.ExpectedTexts);
                                var optionsLabel = FormatOptions(result.Scenario.Options);
                                var diagSummary = SummarizeDiagnostics(result.Infos);
                                return new ScenarioModel {
                                    Name = result.Scenario.Name,
                                    Width = result.Width,
                                    Height = result.Height,
                                    Runs = result.Runs,
                                    OpsPerIteration = result.OpsPerIteration,
                                    DecodeRate = decodeRate,
                                    ExpectedRate = expectedRate,
                                    MedianMs = median,
                                    P95Ms = p95,
                                    AvgDecodedCount = result.AvgDecodedCount,
                                    Expected = expectedLabel,
                                    Options = optionsLabel,
                                    DiagScaleMedian = diagSummary?.ScaleMedian,
                                    DiagThresholdMedian = diagSummary?.ThresholdMedian,
                                    DiagInvertRate = diagSummary?.InvertRate,
                                    DiagCandidateMedian = diagSummary?.CandidateMedian,
                                    DiagTriplesMedian = diagSummary?.TriplesMedian,
                                    DiagDimensionMedian = diagSummary?.DimensionMedian,
                                    DiagSuccessRate = diagSummary?.SuccessRate,
                                    DiagTopFailure = diagSummary is null ? null : FormatFailure(diagSummary.Value.TopFailure)
                                };
                            })
                            .ToArray();

                        return new EngineModel {
                            Name = engineGroup.Key,
                            IsExternal = engineGroup.First().EngineIsExternal,
                            Runs = packSummary.Runs,
                            DecodeRate = packSummary.DecodeRate,
                            ExpectedRate = packSummary.ExpectedRate,
                            MedianMs = packSummary.MedianMs,
                            P95Ms = packSummary.P95Ms,
                            Scenarios = scenarios
                        };
                    })
                    .ToArray();

                var info = QrDecodeScenarioPacks.GetPackInfo(packGroup.Key);
                return new PackModel {
                    Name = packGroup.Key,
                    Category = info.Category,
                    Description = info.Description,
                    Guidance = info.Guidance,
                    ScenarioCount = scenarioCount,
                    Engines = engines
                };
            })
            .ToArray();

        return new ReportModel {
            DateUtc = nowUtc,
            Mode = options.Mode.ToString(),
            PacksRequested = options.Packs.OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToArray(),
            Iterations = options.Iterations,
            MinIterationMilliseconds = options.MinIterationMilliseconds,
            OpsCap = options.OpsCap,
            ExternalRunsCap = options.ExternalRunsCap,
            Engines = options.Engines
                .Select(e => new EngineMeta { Name = e.Name, IsExternal = e.IsExternal })
                .ToArray(),
            Runtime = RuntimeInformation.FrameworkDescription,
            Os = RuntimeInformation.OSDescription,
            Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
            CpuLogicalCores = Environment.ProcessorCount,
            GcMode = GCSettings.IsServerGC ? "Server" : "Workstation",
            Packs = packs
        };
    }

    private static IReadOnlyList<IQrDecodeEngine> ResolveEngines(IReadOnlyList<IQrDecodeEngine> available, List<string> requested) {
        if (requested.Count == 0) return available;

        var requestedTokens = requested
            .Select(NormalizeToken)
            .Where(t => t.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (requestedTokens.Length == 0) return available;

        var unmatched = new HashSet<string>(requestedTokens, StringComparer.Ordinal);
        var matched = new List<IQrDecodeEngine>(available.Count);
        foreach (var engine in available) {
            var aliasSet = new HashSet<string>(
                engine.Aliases.Append(engine.Name).Select(NormalizeToken).Where(t => t.Length > 0),
                StringComparer.Ordinal);
            var isMatch = requestedTokens.Any(aliasSet.Contains);
            if (!isMatch) continue;
            matched.Add(engine);
            foreach (var token in requestedTokens) {
                if (aliasSet.Contains(token)) unmatched.Remove(token);
            }
        }

        if (matched.Count == 0) {
            Console.Error.WriteLine($"No engines matched '{string.Join(",", requested)}'; using all available engines.");
            return available;
        }

        if (unmatched.Count > 0) {
            Console.Error.WriteLine($"Some engines were not available: {string.Join(", ", unmatched)}");
        }

        return matched;
    }

    private static void AddEngines(List<string> engineList, string engineArg) {
        if (string.IsNullOrWhiteSpace(engineArg)) return;
        var parts = engineArg.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        for (var i = 0; i < parts.Length; i++) {
            var engine = parts[i].Trim();
            if (engine.Length > 0) engineList.Add(engine);
        }
    }

    private static string NormalizeToken(string value) {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var buffer = new char[value.Length];
        var count = 0;
        for (var i = 0; i < value.Length; i++) {
            var c = value[i];
            if (!char.IsLetterOrDigit(c)) continue;
            buffer[count++] = char.ToLowerInvariant(c);
        }
        return count == 0 ? string.Empty : new string(buffer, 0, count);
    }

    private static QrReportFormats ParseFormats(string formatArg, QrReportFormats? current) {
        var formats = current ?? QrReportFormats.Text;
        if (string.IsNullOrWhiteSpace(formatArg)) return formats;
        var parts = formatArg.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        for (var i = 0; i < parts.Length; i++) {
            var token = NormalizeToken(parts[i]);
            if (token is "all") {
                formats |= QrReportFormats.Json | QrReportFormats.Csv | QrReportFormats.Text;
                continue;
            }
            if (token is "json") {
                formats |= QrReportFormats.Json;
                continue;
            }
            if (token is "csv") {
                formats |= QrReportFormats.Csv;
                continue;
            }
            if (token is "text" or "txt") {
                formats |= QrReportFormats.Text;
            }
        }
        return formats;
    }

    private static string EscapeCsv(string? value) {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        var needsQuotes = value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0;
        if (!needsQuotes) return value;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

    private static string FormatCsvInt(int? value) => value?.ToString() ?? string.Empty;
    private static string FormatCsvDouble(double? value) => value?.ToString("F4") ?? string.Empty;

    private static HashSet<string> ResolvePacks(List<string> requestedPacks, QrPackMode mode) {
        var defaults = mode == QrPackMode.Quick ? QuickDefaultPacks : QrDecodeScenarioPacks.AllPacks;
        if (requestedPacks.Count == 0) {
            return new HashSet<string>(defaults, StringComparer.OrdinalIgnoreCase);
        }

        var valid = new HashSet<string>(QrDecodeScenarioPacks.AllPacks, StringComparer.OrdinalIgnoreCase);
        var packs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pack in requestedPacks.Where(valid.Contains)) {
            packs.Add(pack);
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

    private static void AddScenarios(List<string> scenarioList, string scenarioArg) {
        if (string.IsNullOrWhiteSpace(scenarioArg)) return;
        var parts = scenarioArg.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        for (var i = 0; i < parts.Length; i++) {
            var scenario = parts[i].Trim();
            if (scenario.Length > 0) scenarioList.Add(scenario);
        }
    }

    private static IReadOnlyList<string> ResolveScenarioFilters(List<string> scenarios) {
        if (scenarios.Count == 0) return Array.Empty<string>();
        return scenarios
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static bool MatchesScenarioFilters(string scenarioName, IReadOnlyList<string> filters) {
        if (filters.Count == 0) return true;
        for (var i = 0; i < filters.Count; i++) {
            var filter = filters[i];
            if (scenarioName.Contains(filter, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    private static QrPackMode ParseMode(string? value) {
        if (string.Equals(value, "full", StringComparison.OrdinalIgnoreCase)) return QrPackMode.Full;
        return QrPackMode.Quick;
    }

    private static QrPackMode? ResolveModeFromBenchQuickEnv() {
        var benchQuick = Environment.GetEnvironmentVariable("BENCH_QUICK");
        if (string.IsNullOrWhiteSpace(benchQuick)) return null;
        if (string.Equals(benchQuick, "true", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(benchQuick, "1", StringComparison.OrdinalIgnoreCase)) {
            return QrPackMode.Quick;
        }
        if (string.Equals(benchQuick, "false", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(benchQuick, "0", StringComparison.OrdinalIgnoreCase)) {
            return QrPackMode.Full;
        }
        return null;
    }

    private sealed class ReportModel {
        public required DateTime DateUtc { get; init; }
        public required string Mode { get; init; }
        public required string[] PacksRequested { get; init; }
        public required int Iterations { get; init; }
        public required int MinIterationMilliseconds { get; init; }
        public required int OpsCap { get; init; }
        public required int ExternalRunsCap { get; init; }
        public required EngineMeta[] Engines { get; init; }
        public required string Runtime { get; init; }
        public required string Os { get; init; }
        public required string Architecture { get; init; }
        public required int CpuLogicalCores { get; init; }
        public required string GcMode { get; init; }
        public required PackModel[] Packs { get; init; }
    }

    private sealed class EngineMeta {
        public required string Name { get; init; }
        public required bool IsExternal { get; init; }
    }

    private sealed class PackModel {
        public required string Name { get; init; }
        public required string Category { get; init; }
        public required string Description { get; init; }
        public required string Guidance { get; init; }
        public required int ScenarioCount { get; init; }
        public required EngineModel[] Engines { get; init; }
    }

    private sealed class EngineModel {
        public required string Name { get; init; }
        public required bool IsExternal { get; init; }
        public required int Runs { get; init; }
        public required double DecodeRate { get; init; }
        public required double ExpectedRate { get; init; }
        public required double MedianMs { get; init; }
        public required double P95Ms { get; init; }
        public required ScenarioModel[] Scenarios { get; init; }
    }

    private sealed class ScenarioModel {
        public required string Name { get; init; }
        public required int Width { get; init; }
        public required int Height { get; init; }
        public required int Runs { get; init; }
        public required int OpsPerIteration { get; init; }
        public required double DecodeRate { get; init; }
        public required double ExpectedRate { get; init; }
        public required double MedianMs { get; init; }
        public required double P95Ms { get; init; }
        public required double AvgDecodedCount { get; init; }
        public required string Expected { get; init; }
        public required string Options { get; init; }
        public required int? DiagScaleMedian { get; init; }
        public required int? DiagThresholdMedian { get; init; }
        public required double? DiagInvertRate { get; init; }
        public required int? DiagCandidateMedian { get; init; }
        public required int? DiagTriplesMedian { get; init; }
        public required int? DiagDimensionMedian { get; init; }
        public required double? DiagSuccessRate { get; init; }
        public required string? DiagTopFailure { get; init; }
    }

    private readonly record struct DecodeRunResult(
        bool Decoded,
        bool ExpectedMatched,
        int DecodedCount,
        double ElapsedMilliseconds,
        QrPixelDecodeInfo? Info);

    private sealed class QrDecodeScenarioResult {
        public QrDecodeScenarioResult(
            QrDecodeScenario scenario,
            int runs,
            int opsPerIteration,
            int decodeSuccess,
            int expectedMatch,
            double avgDecodedCount,
            List<double> times,
            List<QrPixelDecodeInfo> infos,
            string engineName,
            bool engineIsExternal,
            int width,
            int height) {
            Scenario = scenario;
            Runs = runs;
            OpsPerIteration = opsPerIteration;
            DecodeSuccess = decodeSuccess;
            ExpectedMatch = expectedMatch;
            AvgDecodedCount = avgDecodedCount;
            Times = times;
            Infos = infos;
            EngineName = engineName;
            EngineIsExternal = engineIsExternal;
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
        public List<QrPixelDecodeInfo> Infos { get; }
        public string EngineName { get; }
        public bool EngineIsExternal { get; }
        public int Width { get; }
        public int Height { get; }
    }

    private readonly record struct PackSummary(int Runs, double DecodeRate, double ExpectedRate, double MedianMs, double P95Ms);
    private readonly record struct DiagnosticSummary(
        int ScaleMedian,
        int ThresholdMedian,
        double InvertRate,
        int CandidateMedian,
        int TriplesMedian,
        int DimensionMedian,
        double SuccessRate,
        QrDecodeFailureReason TopFailure);
}
