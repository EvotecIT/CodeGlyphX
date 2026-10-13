using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace CodeGlyphX.Benchmarks;

#if BENCH_QUICK
[SimpleJob(RuntimeMoniker.Net80, warmupCount: 1, iterationCount: 3, invocationCount: 1)]
#else
[SimpleJob(RuntimeMoniker.Net80)]
#endif
[MemoryDiagnoser]
[RankColumn]
public class QrCompareBenchmarks {
    private const string Content = "https://github.com/EvotecIT/CodeGlyphX";
    private readonly FairQrGeneration _generator = new(Content, 4);

    [GlobalSetup]
    public void Setup() {
        foreach (var engine in Engines()) _generator.Validate(_generator.Generate(engine, "Png"), "Png");
    }
    private static IEnumerable<string> Engines() {
        yield return "CodeGlyphX";
#if COMPARE_ZXING
        yield return "ZXing.Net";
#endif
#if COMPARE_QRCODER
        yield return "QRCoder";
#endif
#if COMPARE_ZXINGCPP
        yield return "ZXing-C++";
#endif
    }
    [Benchmark(Baseline = true, Description = "CodeGlyphX QR PNG (fixed V4/M)")]
    public byte[] CodeGlyphX_QrPng() => _generator.GeneratePng("CodeGlyphX");
#if COMPARE_ZXING
    [Benchmark(Description = "ZXing.Net QR PNG (fixed V4/M)")]
    public byte[] ZXing_QrPng() => _generator.GeneratePng("ZXing.Net");
#endif
#if COMPARE_QRCODER
    [Benchmark(Description = "QRCoder QR PNG (fixed V4/M)")]
    public byte[] QrCoder_QrPng() => _generator.GeneratePng("QRCoder");
#endif
#if COMPARE_ZXINGCPP
    [Benchmark(Description = "ZXing-C++ QR PNG (fixed V4/M)")]
    public byte[] ZXingCpp_QrPng() => _generator.GeneratePng("ZXing-C++");
#endif
}
