using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using CodeGlyphX;
using CodeGlyphX.Rendering;

namespace CodeGlyphX.Benchmarks;

#if BENCH_QUICK
[SimpleJob(RuntimeMoniker.Net80, warmupCount: 1, iterationCount: 3, invocationCount: 1)]
#else
[SimpleJob(RuntimeMoniker.Net80)]
#endif
[MemoryDiagnoser]
[RankColumn]
public class DecodeGuardBenchmarks
{
    private byte[] _png = Array.Empty<byte>();
    private readonly ImageDecodeOptions _guarded = ImageDecodeOptions.Guarded();
    private readonly ImageDecodeOptions _strict = ImageDecodeOptions.Strict();

    [GlobalSetup]
    public void Setup()
    {
        _png = QrCode.Render("GUARD", OutputFormat.Png, new QrEasyOptions { ModuleSize = 8, QuietZone = 2 }).Data;
    }

    [Benchmark(Description = "ImageReader Decode (default limits)")]
    public bool DecodeDefault()
    {
        return ImageReader.TryDecodeRgba32(_png, out _, out _, out _);
    }

    [Benchmark(Description = "ImageReader Decode (Guarded)")]
    public bool DecodeGuarded()
    {
        return ImageReader.TryDecodeRgba32(_png, _guarded, out _, out _, out _);
    }

    [Benchmark(Description = "ImageReader Decode (Strict)")]
    public bool DecodeStrict()
    {
        return ImageReader.TryDecodeRgba32(_png, _strict, out _, out _, out _);
    }
}
