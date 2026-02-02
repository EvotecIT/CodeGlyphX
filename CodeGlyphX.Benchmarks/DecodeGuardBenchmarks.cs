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
    private readonly ImageDecodeOptions _safe = ImageDecodeOptions.Safe();
    private readonly ImageDecodeOptions _ultraSafe = ImageDecodeOptions.UltraSafe();

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

    [Benchmark(Description = "ImageReader Decode (Safe)")]
    public bool DecodeSafe()
    {
        return ImageReader.TryDecodeRgba32(_png, _safe, out _, out _, out _);
    }

    [Benchmark(Description = "ImageReader Decode (UltraSafe)")]
    public bool DecodeUltraSafe()
    {
        return ImageReader.TryDecodeRgba32(_png, _ultraSafe, out _, out _, out _);
    }
}
