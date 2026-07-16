using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using CodeGlyphX.Gs1Data;

namespace CodeGlyphX.Benchmarks;

#if BENCH_QUICK
[InProcess]
[WarmupCount(1)]
[IterationCount(3)]
[InvocationCount(1)]
#else
[SimpleJob(RuntimeMoniker.Net80)]
#endif
[MemoryDiagnoser]
[RankColumn]
public class Gs1ValidationBenchmarks {
    private const string Bracketed = "(01)09506000134352(10)ABC123(17)240101";
    private static readonly string Raw = "010950600013435210ABC123" + Gs1.GroupSeparator + "17240101";

    [Benchmark(Baseline = true, Description = "GS1 catalog lookup")]
    public Gs1ApplicationIdentifier CatalogLookup() => Gs1ApplicationIdentifierCatalog.Get("3103");

    [Benchmark(Description = "GS1 validate bracketed message")]
    public Gs1ValidationResult ValidateBracketed() => Gs1.Validate(Bracketed);

    [Benchmark(Description = "GS1 validate raw element string")]
    public Gs1ValidationResult ValidateRaw() => Gs1.Validate(Raw);
}
