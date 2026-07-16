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
public class Gs1DigitalLinkBenchmarks {
    private static readonly string ElementString =
        "010952012345678810ABC1" + Gs1.GroupSeparator + "2112345" + Gs1.GroupSeparator + "17180426";
    private static readonly string DigitalLinkUri =
        "https://id.gs1.org/01/09520123456788/10/ABC1/21/12345?17=180426";

    [Benchmark(Baseline = true, Description = "Validate GS1 element string")]
    public Gs1ValidationResult ValidateElementString() => Gs1.Validate(ElementString);

    [Benchmark(Description = "Validate GS1 Digital Link URI")]
    public Gs1DigitalLinkValidationResult ValidateDigitalLinkUri() => Gs1DigitalLink.Validate(DigitalLinkUri);
}
