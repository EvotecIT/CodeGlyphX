using System.Text.Json;

namespace CodeGlyphX.Benchmarks;

internal static class CorpusRunner {
    internal static int Run(string[] args) {
        if (args.Length != 4) throw new ArgumentException("Usage: --corpus <manifest.json> <image-root> <results.json>");
        var session = new CorpusSession(args[1], args[2]);
        var outcomes = new List<CorpusOutcome>();
        foreach (var engine in new[] { "CodeGlyphX", "ZXing.Net", "ZXing-C++" }) {
            var result = session.Evaluate(engine, session.Decode(engine));
            outcomes.AddRange(result);
            Console.WriteLine($"{engine}: {result.Count(row => row.Status == "Correct")}/{session.Count} correct after documented representation rules; " +
                $"{result.Count(row => row.Status == "Unsupported")} unsupported; {result.Count(row => row.Unexpected.Length > 0)} unexpected payloads.");
        }
        File.WriteAllText(args[3], JsonSerializer.Serialize(outcomes, CorpusSession.JsonOptions));
        // Competitive misses are results to investigate. Infrastructure failures above are fatal.
        return 0;
    }
}
