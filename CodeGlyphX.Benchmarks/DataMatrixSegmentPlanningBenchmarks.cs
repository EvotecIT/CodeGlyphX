using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using CodeGlyphX.DataMatrix;

namespace CodeGlyphX.Benchmarks;

#if BENCH_QUICK
[SimpleJob(RuntimeMoniker.Net80, warmupCount: 1, iterationCount: 3, invocationCount: 1)]
#else
[SimpleJob(RuntimeMoniker.Net80)]
#endif
[MemoryDiagnoser]
[RankColumn]
public class DataMatrixSegmentPlanningBenchmarks {
    private const string MixedText = "HELLO-THIS-IS-UPPERCASE-lowercase-lowercase-1234567890";
    private const string UnicodeIsland = "AAAAAAAAAAAAAAAA😀BBBBBBBBBBBBBBBB12345678901234567890";
    private readonly string _longUppercase = new('A', 1500);
    private readonly DataMatrixEncodingOptions _forcedAscii = new() { Mode = DataMatrixEncodingMode.Ascii };
    private readonly DataMatrixEncodingOptions _utf8 = new() { EciAssignmentNumber = 26 };

    [Benchmark(Baseline = true, Description = "Data Matrix mixed text (optimized segments)")]
    public BitMatrix MixedOptimized() => DataMatrixEncoder.Encode(MixedText);

    [Benchmark(Description = "Data Matrix mixed text (forced ASCII)")]
    public BitMatrix MixedForcedAscii() => DataMatrixEncoder.Encode(MixedText, _forcedAscii);

    [Benchmark(Description = "Data Matrix Unicode island (optimized segments)")]
    public BitMatrix UnicodeIslandOptimized() => DataMatrixEncoder.Encode(UnicodeIsland, _utf8);

    [Benchmark(Description = "Data Matrix 1500 uppercase chars (optimized segments)")]
    public BitMatrix LongUppercaseOptimized() => DataMatrixEncoder.Encode(_longUppercase);

    [Benchmark(Description = "Data Matrix 1500 uppercase chars (forced ASCII)")]
    public BitMatrix LongUppercaseForcedAscii() => DataMatrixEncoder.Encode(_longUppercase, _forcedAscii);
}
