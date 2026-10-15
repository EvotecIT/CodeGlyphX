using System.Security.Cryptography;
using System.Text.Json;
using CodeGlyphX.Rendering;

namespace CodeGlyphX.Benchmarks;

/// <summary>Loads identical verified pixels for comparison-only engines; contains no measurement policy.</summary>
public sealed class CorpusSession {
    private readonly CorpusSample[] _samples;
    private readonly byte[][] _pixels;
    private readonly int[] _widths;
    private readonly int[] _heights;
    public int Count => _samples.Length;

    public CorpusSession(string manifestPath, string imageRoot) {
        var manifest = JsonSerializer.Deserialize<CorpusManifest>(File.ReadAllText(manifestPath), JsonOptions)
            ?? throw new InvalidDataException("Empty corpus manifest.");
        _samples = manifest.Samples;
        if (_samples is null || _samples.Length == 0) throw new InvalidDataException("Corpus has no samples.");
        _pixels = new byte[Count][]; _widths = new int[Count]; _heights = new int[Count];
        var root = Path.GetFullPath(imageRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        for (var i = 0; i < Count; i++) {
            var path = Path.GetFullPath(Path.Combine(root, _samples[i].File));
            var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            if (!path.StartsWith(root, comparison)) throw new InvalidDataException("Corpus path escapes its root.");
            var encoded = File.ReadAllBytes(path);
            if (!Convert.ToHexString(SHA256.HashData(encoded)).Equals(_samples[i].Sha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"Corpus hash mismatch: {_samples[i].Id}.");
            if (!ImageReader.TryDecodeRgba32(encoded, out _pixels[i], out _widths[i], out _heights[i]))
                throw new InvalidDataException($"Cannot load corpus image: {_samples[i].Id}.");
        }
    }

    /// <summary>Reads each preloaded frame once. Expected-payload validation takes place outside this operation.</summary>
    public CorpusRead[] Decode(string engine, bool robust = true) {
        var results = new CorpusRead[Count];
        for (var i = 0; i < Count; i++) {
            var sample = _samples[i];
            var texts = CorpusReaders.Decode(_pixels[i], _widths[i], _heights[i], sample.Format, engine, robust, sample.Expected.Length != 1, out var completion);
            results[i] = new CorpusRead(texts, completion);
        }
        return results;
    }

    /// <summary>Retains every miss and unexpected payload instead of treating nonempty output as success.</summary>
    public CorpusOutcome[] Evaluate(string engine, CorpusRead[] results) {
        if (results.Length != Count) throw new InvalidDataException("Corpus result count differs from input count.");
        return _samples.Select((sample, i) => {
            var expected = sample.Expected.Select(text => CorpusScoring.Canonical(text, sample.Format)).ToHashSet(StringComparer.Ordinal);
            var actual = results[i].Texts.Select(text => CorpusScoring.Canonical(text, sample.Format)).ToHashSet(StringComparer.Ordinal);
            var missing = expected.Except(actual).ToArray();
            var unexpected = actual.Except(expected).ToArray();
            var unsupported = engine == "CodeGlyphX" && sample.Format != "All" &&
                !SymbolCapabilities.Get(Enum.Parse<SymbolFormat>(sample.Format == "Ean8" ? "Ean" : sample.Format)).CanScanImages;
            var inconclusive = sample.Expected.Length == 0 && actual.Count == 0 && results[i].Completion != "Completed";
            return new CorpusOutcome(sample.Id, sample.Format, engine, sample.Expected, results[i].Texts, missing, unexpected,
                unsupported ? "Unsupported" : inconclusive ? "Inconclusive" : missing.Length == 0 && unexpected.Length == 0 ? "Correct" : "Failed", results[i].Completion);
        }).ToArray();
    }

    public static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true, WriteIndented = true };
}

public sealed record CorpusManifest(string Revision, string Selection, CorpusSample[] Samples);
public sealed record CorpusSample(string Id, string File, string Sha256, string Format, string[] Expected);
public sealed record CorpusRead(string[] Texts, string Completion);
public sealed record CorpusOutcome(string Id, string Format, string Engine, string[] Expected, string[] Actual, string[] Missing, string[] Unexpected, string Status, string Completion);
