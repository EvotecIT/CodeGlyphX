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
public class QrSegmentPlanningBenchmarks {
    private const string MixedText = "ORDER-2026-00000123456789012345/customer/ref-42/LOT-ABC123456789";
    private readonly string _longByteText = new('a', 2000);
    private readonly QrEncodingOptions _optimized = new() { OptimizeSegments = true };
    private readonly QrEncodingOptions _forcedByte = new() { OptimizeSegments = false };

    [Benchmark(Baseline = true, Description = "QR mixed text (optimized segments)")]
    public QrCode MixedOptimized() => QrCodeEncoder.EncodeText(MixedText, _optimized);

    [Benchmark(Description = "QR mixed text (forced byte)")]
    public QrCode MixedForcedByte() => QrCodeEncoder.EncodeText(MixedText, _forcedByte);

    [Benchmark(Description = "QR 2000-byte text (optimized segments)")]
    public QrCode LongByteOptimized() => QrCodeEncoder.EncodeText(_longByteText, _optimized);

    [Benchmark(Description = "QR 2000-byte text (forced byte)")]
    public QrCode LongByteForced() => QrCodeEncoder.EncodeText(_longByteText, _forcedByte);
}
