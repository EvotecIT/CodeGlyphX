using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using CodeGlyphX.Rendering;
#if COMPARE_ZXING
using ZXing;
using ZXing.Common;
#endif

namespace CodeGlyphX.Benchmarks;

#if BENCH_QUICK
[SimpleJob(RuntimeMoniker.Net80, warmupCount: 1, iterationCount: 3, invocationCount: 1)]
#else
[SimpleJob(RuntimeMoniker.Net80)]
#endif
[MemoryDiagnoser]
[RankColumn]
public class QrDecodeNoisyCompareBenchmarks
{
    private byte[] _noisyRgba = Array.Empty<byte>();
    private int _noisyWidth;
    private int _noisyHeight;
    private readonly QrPixelDecodeOptions _robust = new()
    {
        Profile = QrDecodeProfile.Robust,
        MaxMilliseconds = 800,
        MaxDimension = 1600,
        AggressiveSampling = true
    };

#if COMPARE_ZXING
    private readonly BarcodeReaderGeneric _zxingReader = new()
    {
        Options = new DecodingOptions
        {
            PossibleFormats = new[] { BarcodeFormat.QR_CODE },
            TryHarder = true
        }
    };
#endif

    [GlobalSetup]
    public void Setup()
    {
        DecodeSampleHelper.LoadRgba("Assets/DecodingSamples/qr-noisy-ui.png", out _noisyRgba, out _noisyWidth, out _noisyHeight);
    }

    [Benchmark(Baseline = true, Description = "CodeGlyphX QR Decode (noisy, robust)")]
    public bool CodeGlyphX_DecodeNoisyRobust()
    {
        return QrDecoder.TryDecode(_noisyRgba, _noisyWidth, _noisyHeight, _noisyWidth * 4, PixelFormat.Rgba32, out _, _robust);
    }

#if COMPARE_ZXING
    [Benchmark(Description = "ZXing.Net QR Decode (noisy, try harder)")]
    public bool ZXing_DecodeNoisyTryHarder()
    {
        return _zxingReader.Decode(_noisyRgba, _noisyWidth, _noisyHeight, RGBLuminanceSource.BitmapFormat.RGBA32) is not null;
    }
#endif
}
