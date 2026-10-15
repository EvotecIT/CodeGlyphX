// Benchmark charts: the qualified comparison (data/comparisons.json) reduced to bar geometry, so the page draws the
// charts without script and they follow the site theme. Values are copied as published; only bar lengths are computed.
using System.Text.Json.Nodes;

internal static partial class SiteVisuals {
    sealed record ReaderScore(string Engine, int Before, int Correct, int Positives);

    static readonly Dictionary<string, string> PayloadNames = new(StringComparer.OrdinalIgnoreCase) {
        ["Url"] = "URL",
        ["Numeric"] = "32 digits",
        ["Unicode"] = "Multilingual text"
    };

    static object BuildBench(out ReaderScore[] recognition) {
        var path = Path.Combine(root, "data", "comparisons.json");
        var c = JsonNode.Parse(File.ReadAllText(path))!;

        // Image recognition: exact payload sets on the frozen positive images, original source and current.
        recognition = c["recognition"]!.AsArray().Select(r => new ReaderScore(
            (string)r!["engine"]!, (int)r["before"]!, (int)r["correct"]!, (int)r["positives"]!)).ToArray();
        var positives = recognition.Max(r => r.Positives);
        var recognitionRows = c["recognition"]!.AsArray().Select(r => new {
            engine = (string)r!["engine"]!,
            cgx = (string)r["engine"]! == "CodeGlyphX",
            before = (int)r["before"]!,
            correct = (int)r["correct"]!,
            positives = (int)r["positives"]!,
            percent = (string?)r["percent"] ?? "",
            before_pct = Pct((int)r["before"]!, positives),
            pct = Pct((int)r["correct"]!, positives)
        }).ToList();

        // Generation and decoding per CPU cache domain: medians on a linear scale per scenario (slowest = full width).
        var domains = c["domains"]!.AsArray().Select(d => new {
            id = (string)d!["id"]!,
            label = (string)d["label"]!,
            affinity = (string?)d["affinity"] ?? "",
            generation = d["generation"]!.AsArray().Select(g => {
                var cells = g!["cells"]!.AsArray();
                var max = cells.Max(x => Num(x!["median"]));
                var payload = (string)g["payload"]!;
                return new {
                    operation = (string)g["operation"]! == "Png" ? "PNG" : (string)g["operation"]!,
                    payload = PayloadNames.TryGetValue(payload, out var name) ? name : payload,
                    bars = cells.Select(x => new {
                        engine = (string)x!["engine"]!,
                        cgx = (string)x["engine"]! == "CodeGlyphX",
                        median = (string)x["median"]!,
                        p95 = (string)x["p95"]!,
                        bytes = (string)x["bytes"]!,
                        pct = Pct(Num(x["median"]), max)
                    }).ToList()
                };
            }).ToList(),
            decoding = Decoding(d["decoding"]!.AsArray())
        }).ToList();

        // PNG output: managed KiB per 100 outputs for each public path, previous and current binary.
        var png = c["png_output"]?["buffer_rows"]?.AsArray();
        var pngMax = png is null || png.Count == 0 ? 1 : png.Max(r => Num(r!["before_kib"]));
        var pngRows = png?.Select(r => new {
            path = (string)r!["case"]!,
            before = (string)r["before_kib"]!,
            current = (string)r["current_kib"]!,
            reduction = (string)r["reduction"]!,
            before_pct = Pct(Num(r["before_kib"]), pngMax),
            pct = Pct(Num(r["current_kib"]), pngMax)
        }).ToList();

        return new {
            date = (string?)c["date"] ?? "",
            positives,
            recognition = recognitionRows,
            domains,
            png = pngRows
        };
    }

    static object Decoding(JsonArray rows) {
        var max = rows.Max(r => Num(r!["median"]));
        return rows.Select(r => new {
            engine = (string)r!["engine"]!,
            cgx = (string)r["engine"]! == "CodeGlyphX",
            median = (string)r["median"]!,
            p95 = (string)r["p95"]!,
            managed_mib = (string)r["managed_mib"]!,
            correct = (string)r["correct"]!,
            pct = Pct(Num(r["median"]), max)
        }).ToList();
    }

    static double Num(JsonNode? value) => double.Parse(((string?)value ?? "0").Replace(",", ""), Inv);

    // Bar length as a percentage of the scale; never zero so every measured value stays visible.
    static double Pct(double value, double max) => Math.Round(Math.Max(0.6, 100 * value / Math.Max(1e-9, max)), 2);
}
