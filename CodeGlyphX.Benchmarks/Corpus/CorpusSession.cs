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
        var root = Path.GetFullPath(imageRoot) + Path.DirectorySeparatorChar;
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
    public string[][] Decode(string engine, bool robust = true) {
        var results = new string[Count][];
        for (var i = 0; i < Count; i++) {
            var sample = _samples[i];
            results[i] = CorpusReaders.Decode(_pixels[i], _widths[i], _heights[i], sample.Format, engine, robust, sample.Expected.Length != 1);
        }
        return results;
    }

    /// <summary>Retains every miss and unexpected payload instead of treating nonempty output as success.</summary>
    public CorpusOutcome[] Evaluate(string engine, string[][] results) {
        if (results.Length != Count) throw new InvalidDataException("Corpus result count differs from input count.");
        return _samples.Select((sample, i) => {
            var expected = sample.Expected.Select(text => CorpusScoring.Canonical(text, sample.Format)).ToHashSet(StringComparer.Ordinal);
            var actual = results[i].Select(text => CorpusScoring.Canonical(text, sample.Format)).ToHashSet(StringComparer.Ordinal);
            var missing = expected.Except(actual).ToArray();
            var unexpected = actual.Except(expected).ToArray();
            var unsupported = engine == "CodeGlyphX" && sample.Format != "All" &&
                !SymbolCapabilities.Get(Enum.Parse<SymbolFormat>(sample.Format == "Ean8" ? "Ean" : sample.Format)).CanScanImages;
            return new CorpusOutcome(sample.Id, sample.Format, engine, sample.Expected, results[i], missing, unexpected,
                unsupported ? "Unsupported" : missing.Length == 0 && unexpected.Length == 0 ? "Correct" : "Failed");
        }).ToArray();
    }

    public static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true, WriteIndented = true };
}

public sealed record CorpusManifest(string Revision, string Selection, CorpusSample[] Samples);
public sealed record CorpusSample(string Id, string File, string Sha256, string Format, string[] Expected);
public sealed record CorpusOutcome(string Id, string Format, string Engine, string[] Expected, string[] Actual, string[] Missing, string[] Unexpected, string Status);
