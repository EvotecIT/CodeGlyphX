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
        QrDecodeScenarioPacks.Multi
    };

    public static bool TryParseArgs(string[] args, out QrDecodePackRunnerOptions options, out string[] remainingArgs) {
        options = null!;
        var remaining = new List<string>(args.Length);
        var packList = new List<string>(8);
        var engineList = new List<string>(4);
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
        options = new QrDecodePackRunnerOptions {
            Mode = resolvedMode,
            Packs = packs,
            Engines = engines,
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
            .ToArray();

        if (scenarios.Length == 0) {
            Console.Error.WriteLine("No scenarios matched the selected packs.");
            return 1;
        }

        var nowUtc = DateTime.UtcNow;
        var results = new List<QrDecodeScenarioResult>(scenarios.Length * options.Engines.Count);
        foreach (var engine in options.Engines) {
            foreach (var scenario in scenarios) {
                var data = scenario.CreateData();
                results.Add(RunScenario(engine, scenario, data, options));
            }
        }

        var report = BuildReport(options, results, nowUtc);
        Console.WriteLine(report);

        var hasCustomReportsDir = !string.IsNullOrWhiteSpace(options.ReportsDirectory);
        var reportsDir = hasCustomReportsDir
            ? Path.GetFullPath(options.ReportsDirectory!)
            : RepoFiles.EnsureReportDirectory();
        Directory.CreateDirectory(reportsDir);

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

    private static QrDecodeScenarioResult RunScenario(IQrDecodeEngine engine, QrDecodeScenario scenario, QrDecodeScenarioData data, QrDecodePackRunnerOptions options) {
        var times = new List<double>(options.Iterations * 4);
        var decodeSuccess = 0;
        var expectedMatch = 0;
        var decodedCountSum = 0;

        // Calibration run to determine ops-per-iteration.
        var calibration = DecodeOnce(engine, data, scenario.Options, scenario.ExpectedTexts);
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
        return new DecodeRunResult(decodedSuccess, expectedMatched, decodedCount, sw.Elapsed.TotalMilliseconds);
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

    private static string BuildReport(QrDecodePackRunnerOptions options, List<QrDecodeScenarioResult> results, DateTime nowUtc) {
        var sb = new StringBuilder(4096);
        var packs = options.Packs.OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToArray();

        sb.AppendLine("QR Decode Scenario Packs");
        sb.AppendLine($"Date (UTC): {nowUtc:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"Mode: {options.Mode}");
        sb.AppendLine($"Packs: {string.Join(", ", packs)}");
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
        sb.AppendLine();

        foreach (var group in results.GroupBy(r => r.Scenario.Pack).OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)) {
            var scenarioCount = group.Select(r => r.Scenario.Name).Distinct(StringComparer.Ordinal).Count();
            sb.AppendLine($"Pack: {group.Key}  scenarios={scenarioCount}");

            foreach (var engineGroup in group.GroupBy(r => r.EngineName).OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)) {
                var packSummary = Summarize(engineGroup.ToList());
                var externalTag = engineGroup.First().EngineIsExternal ? " (external)" : string.Empty;
                sb.AppendLine($"  Engine: {engineGroup.Key}{externalTag}  runs={packSummary.Runs}  decode%={packSummary.DecodeRate:P0}  expected%={packSummary.ExpectedRate:P0}  medianMs={packSummary.MedianMs:F1}  p95Ms={packSummary.P95Ms:F1}");
                foreach (var result in engineGroup.OrderBy(r => r.Scenario.Name, StringComparer.OrdinalIgnoreCase)) {
                    var median = Percentile(result.Times, 0.50);
                    var p95 = Percentile(result.Times, 0.95);
                    var decodeRate = result.DecodeSuccess / (double)result.Runs;
                    var expectedRate = result.ExpectedMatch / (double)result.Runs;
                    var expectedLabel = result.Scenario.ExpectedTexts is null ? "any" : TruncateExpected(result.Scenario.ExpectedTexts);
                    sb.AppendLine(
                        $"    - {result.Scenario.Name,-26} size={result.Width}x{result.Height} ops={result.OpsPerIteration,2} decode%={decodeRate,6:P0} expected%={expectedRate,6:P0} medianMs={median,7:F1} p95Ms={p95,7:F1} decoded~={result.AvgDecodedCount,4:F1} expected={expectedLabel}");
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
        sb.AppendLine("dateUtc,mode,pack,engine,isExternal,scenario,width,height,runs,opsPerIteration,decodeRate,expectedRate,medianMs,p95Ms,avgDecodedCount,expected");

        var date = nowUtc.ToString("O");
        foreach (var pack in model.Packs) {
            foreach (var engine in pack.Engines) {
                foreach (var scenario in engine.Scenarios) {
                    sb.Append(date).Append(',');
                    sb.Append(model.Mode).Append(',');
                    sb.Append(pack.Name).Append(',');
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
                                    Expected = expectedLabel
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

                return new PackModel {
                    Name = packGroup.Key,
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
        public string EngineName { get; }
        public bool EngineIsExternal { get; }
        public int Width { get; }
        public int Height { get; }
    }

    private readonly record struct PackSummary(int Runs, double DecodeRate, double ExpectedRate, double MedianMs, double P95Ms);
}
