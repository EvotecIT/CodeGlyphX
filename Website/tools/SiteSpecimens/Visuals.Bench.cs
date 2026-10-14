// Benchmark charts: the published summary (static/data/benchmark-summary.json) reduced to bar geometry for each
// platform and run mode, so the page draws the charts without script and they follow the site theme.
using System.Text.Json.Nodes;

internal static partial class SiteVisuals {
    static readonly string[] VendorOrder = { "CodeGlyphX", "ZXing.Net", "QRCoder", "Barcoder" };

    static object BuildBench(out double[] navSpeedups) {
        var path = Path.Combine(staticRoot, "data", "benchmark-summary.json");
        var summary = JsonNode.Parse(File.ReadAllText(path))!;
        var sets = new List<object>();
        navSpeedups = Array.Empty<double>();
        string? first = null;

        foreach (var os in new[] { "windows", "linux", "macos" }) {
            foreach (var mode in new[] { "full", "quick" }) {
                var entry = summary[os]?[mode] ?? summary[os]?[mode == "full" ? "Full" : "Quick"];
                var rows = entry?["summary"]?.AsArray();
                if (rows is null || rows.Count == 0) continue;
                var id = os + "-" + mode;
                first ??= id;

                var scenarios = new List<object>();
                var speedups = new List<(string Name, double Factor, string Runner)>();
                var wins = 0;
                foreach (var row in rows) {
                    var vendors = row!["vendors"]!.AsObject();
                    var means = VendorOrder.Where(v => vendors[v]?["meanNs"] is not null)
                        .Select(v => (Name: v, Ns: (double)vendors[v]!["meanNs"]!, Mean: (string?)vendors[v]!["mean"] ?? "", Alloc: (string?)vendors[v]!["allocated"] ?? ""))
                        .ToList();
                    if (!means.Any(m => m.Name == "CodeGlyphX")) continue;
                    var cgx = means.First(m => m.Name == "CodeGlyphX").Ns;
                    var max = means.Max(m => m.Ns);
                    var fastest = (string?)row["fastestVendor"] ?? "";
                    if (fastest == "CodeGlyphX") wins++;
                    var others = means.Where(m => m.Name != "CodeGlyphX").ToList();
                    var runner = others.OrderBy(m => m.Ns).FirstOrDefault();
                    if (runner.Name is not null) speedups.Add(((string?)row["scenario"] ?? "", runner.Ns / cgx, runner.Name));
                    scenarios.Add(new {
                        scenario = (string?)row["scenario"] ?? "",
                        benchmark = (string?)row["benchmark"] ?? "",
                        fastest,
                        bars = means.Select(m => new {
                            vendor = m.Name,
                            cgx = m.Name == "CodeGlyphX",
                            mean = m.Mean,
                            alloc = m.Alloc,
                            pct = Math.Round(Math.Max(0.6, 100 * m.Ns / max), 2),
                            factor = m.Name == "CodeGlyphX" ? "" : FormatFactor(m.Ns / cgx)
                        }).ToList()
                    });
                }

                // Headline: speed-up over the next fastest library, on a labelled log axis (1x to the next power of ten).
                var top = Math.Max(10, Math.Pow(10, Math.Ceiling(Math.Log10(Math.Max(1.0001, speedups.DefaultIfEmpty(("", 1, "")).Max(s => s.Factor))))));
                double Pos(double f) => Math.Round(100 * Math.Log10(Math.Max(1, f)) / Math.Log10(top), 2);
                var ticks = new List<object>();
                for (var t = 1.0; t <= top; t *= 10) ticks.Add(new { label = t.ToString("0", Inv) + "×", pos = Pos(t) });
                var headline = speedups.OrderByDescending(s => s.Factor).Select(s => new {
                    scenario = s.Name, runner = s.Runner, factor = FormatFactor(s.Factor), pos = Math.Max(0.8, Pos(s.Factor)), slower = s.Factor < 1
                }).ToList();

                if (id == "windows-full" || navSpeedups.Length == 0) navSpeedups = speedups.Select(s => s.Factor).ToArray();
                sets.Add(new {
                    id, os, mode,
                    generated = (string?)entry!["generatedUtc"] ?? "",
                    count = scenarios.Count,
                    wins,
                    median = FormatFactor(Median(speedups.Select(s => s.Factor))),
                    ticks, headline, scenarios
                });
            }
        }
        return new { initial = first, sets };
    }

    static string FormatFactor(double f) => f >= 10 ? f.ToString("0", Inv) + "×" : f.ToString("0.0", Inv) + "×";

    static double Median(IEnumerable<double> values) {
        var v = values.OrderBy(x => x).ToArray();
        if (v.Length == 0) return 1;
        return v.Length % 2 == 1 ? v[v.Length / 2] : (v[v.Length / 2 - 1] + v[v.Length / 2]) / 2;
    }
}
