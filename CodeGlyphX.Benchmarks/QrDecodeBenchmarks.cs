using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Benchmarks;

#if BENCH_QUICK
[SimpleJob(RuntimeMoniker.Net80, warmupCount: 1, iterationCount: 3, invocationCount: 1)]
#else
[SimpleJob(RuntimeMoniker.Net80)]
#endif
[MemoryDiagnoser]
[RankColumn]
public class QrDecodeBenchmarks
{
    private const string SampleText = "https://github.com/EvotecIT/CodeGlyphX";
    private byte[] _cleanRgba = Array.Empty<byte>();
    private byte[] _noisyRgba = Array.Empty<byte>();
    private byte[] _screenshotRgba = Array.Empty<byte>();
    private byte[] _antialiasRgba = Array.Empty<byte>();
    private int _cleanWidth;
    private int _cleanHeight;
    private int _noisyWidth;
    private int _noisyHeight;
    private int _screenshotWidth;
    private int _screenshotHeight;
    private int _antialiasWidth;
    private int _antialiasHeight;
#if !BENCH_QUICK
    private byte[] _logoRgba = Array.Empty<byte>();
#endif
    private byte[] _fancyRgba = Array.Empty<byte>();
    private byte[] _resampledRgba = Array.Empty<byte>();
    private byte[] _noQuietRgba = Array.Empty<byte>();
#if !BENCH_QUICK
    private int _logoWidth;
    private int _logoHeight;
#endif
    private int _fancyWidth;
    private int _fancyHeight;
    private int _resampledWidth;
    private int _resampledHeight;
    private int _noQuietWidth;
    private int _noQuietHeight;
    private readonly QrPixelDecodeOptions _fast = new() { Profile = QrDecodeProfile.Fast };
    private readonly QrPixelDecodeOptions _balanced = new() { Profile = QrDecodeProfile.Balanced };
    private readonly QrPixelDecodeOptions _robust = new() {
        Profile = QrDecodeProfile.Robust,
        AggressiveSampling = true
    };
    private readonly QrPixelDecodeOptions _robustNoisy = new() {
        Profile = QrDecodeProfile.Robust,
        MaxMilliseconds = 800,
        MaxDimension = 1600,
        AggressiveSampling = true
    };

    [GlobalSetup]
    public void Setup()
    {
        LoadSample("Assets/DecodingSamples/qr-clean-small.png", out _cleanRgba, out _cleanWidth, out _cleanHeight);
        LoadSample("Assets/DecodingSamples/qr-noisy-ui.png", out _noisyRgba, out _noisyWidth, out _noisyHeight);
        LoadSample("Assets/DecodingSamples/qr-screenshot-1.png", out _screenshotRgba, out _screenshotWidth, out _screenshotHeight);
        LoadSample("Assets/DecodingSamples/qr-dot-aa.png", out _antialiasRgba, out _antialiasWidth, out _antialiasHeight);
#if !BENCH_QUICK
        BuildLogoSample(out _logoRgba, out _logoWidth, out _logoHeight);
#endif
        BuildFancySample(out _fancyRgba, out _fancyWidth, out _fancyHeight);
        BuildResampledSample(out _resampledRgba, out _resampledWidth, out _resampledHeight);
        BuildNoQuietSample(out _noQuietRgba, out _noQuietWidth, out _noQuietHeight);
    }

    [Benchmark(Description = "QR Decode (clean, fast)")]
    public bool DecodeCleanFast()
    {
        return QrDecoder.TryDecode(_cleanRgba, _cleanWidth, _cleanHeight, _cleanWidth * 4, PixelFormat.Rgba32, out _, _fast);
    }

    [Benchmark(Description = "QR Decode (clean, balanced)")]
    public bool DecodeCleanBalanced()
    {
        return QrDecoder.TryDecode(_cleanRgba, _cleanWidth, _cleanHeight, _cleanWidth * 4, PixelFormat.Rgba32, out _, _balanced);
    }

    [Benchmark(Description = "QR Decode (clean, robust)")]
    public bool DecodeCleanRobust()
    {
        return QrDecoder.TryDecode(_cleanRgba, _cleanWidth, _cleanHeight, _cleanWidth * 4, PixelFormat.Rgba32, out _, _robust);
    }

    [Benchmark(Description = "QR Decode (noisy, robust)")]
    public bool DecodeNoisyRobust()
    {
        return QrDecoder.TryDecode(_noisyRgba, _noisyWidth, _noisyHeight, _noisyWidth * 4, PixelFormat.Rgba32, out _, _robustNoisy);
    }

    [Benchmark(Description = "QR Decode (screenshot, balanced)")]
    public bool DecodeScreenshotBalanced()
    {
        return QrDecoder.TryDecode(_screenshotRgba, _screenshotWidth, _screenshotHeight, _screenshotWidth * 4, PixelFormat.Rgba32, out _, _balanced);
    }

    [Benchmark(Description = "QR Decode (antialias, robust)")]
    public bool DecodeAntialiasRobust()
    {
        return QrDecoder.TryDecode(_antialiasRgba, _antialiasWidth, _antialiasHeight, _antialiasWidth * 4, PixelFormat.Rgba32, out _, _robust);
    }

#if !BENCH_QUICK
    [Benchmark(Description = "QR Decode (logo, robust)")]
    public bool DecodeLogoRobust()
    {
        return QrDecoder.TryDecode(_logoRgba, _logoWidth, _logoHeight, _logoWidth * 4, PixelFormat.Rgba32, out _, _robust);
    }
#endif

    [Benchmark(Description = "QR Decode (fancy, robust)")]
    public bool DecodeFancyRobust()
    {
        return QrDecoder.TryDecode(_fancyRgba, _fancyWidth, _fancyHeight, _fancyWidth * 4, PixelFormat.Rgba32, out _, _robust);
    }

    [Benchmark(Description = "QR Decode (resampled, balanced)")]
    public bool DecodeResampledBalanced()
    {
        return QrDecoder.TryDecode(_resampledRgba, _resampledWidth, _resampledHeight, _resampledWidth * 4, PixelFormat.Rgba32, out _, _balanced);
    }

    [Benchmark(Description = "QR Decode (no quiet zone, robust)")]
    public bool DecodeNoQuietRobust()
    {
        return QrDecoder.TryDecode(_noQuietRgba, _noQuietWidth, _noQuietHeight, _noQuietWidth * 4, PixelFormat.Rgba32, out _, _robust);
    }

    private static void LoadSample(string relativePath, out byte[] rgba, out int width, out int height)
    {
        var data = QrDecodeSampleFactory.LoadSample(relativePath);
        rgba = data.Rgba;
        width = data.Width;
        height = data.Height;
    }

#if !BENCH_QUICK
    private static void BuildLogoSample(out byte[] rgba, out int width, out int height)
    {
        var logo = LogoBuilder.CreateCirclePng(
            size: 96,
            color: new Rgba32(24, 24, 24, 255),
            accent: new Rgba32(240, 240, 240, 255),
            out _,
            out _);
        var options = QrPresets.Logo(logo);
        var png = QrCode.Render(SampleText, OutputFormat.Png, options).Data;
        if (!ImageReader.TryDecodeRgba32(png, out rgba, out width, out height)) {
            throw new InvalidOperationException("Failed to decode logo QR sample.");
        }
    }
#endif

    private static void BuildFancySample(out byte[] rgba, out int width, out int height)
    {
        var data = QrDecodeSampleFactory.BuildFancyGenerated(SampleText);
        rgba = data.Rgba;
        width = data.Width;
        height = data.Height;
    }

    private static void BuildResampledSample(out byte[] rgba, out int width, out int height)
    {
        var data = QrDecodeSampleFactory.BuildResampledGenerated(SampleText);
        rgba = data.Rgba;
        width = data.Width;
        height = data.Height;
    }

    private static void BuildNoQuietSample(out byte[] rgba, out int width, out int height)
    {
        var data = QrDecodeSampleFactory.BuildNoQuietGenerated(SampleText);
        rgba = data.Rgba;
        width = data.Width;
        height = data.Height;
    }

}
