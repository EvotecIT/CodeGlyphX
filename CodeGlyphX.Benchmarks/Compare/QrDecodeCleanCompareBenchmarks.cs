using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using CodeGlyphX.Rendering;
#if COMPARE_ZXING
using ZXing;
using ZXing.Common;
#endif

namespace CodeGlyphX.Benchmarks;

[SimpleJob(RuntimeMoniker.Net80, warmupCount: 1, iterationCount: 3, invocationCount: 1)]
[MemoryDiagnoser]
[RankColumn]
public class QrDecodeCleanCompareBenchmarks
{
    private byte[] _cleanRgba = Array.Empty<byte>();
    private int _cleanWidth;
    private int _cleanHeight;
    private readonly QrPixelDecodeOptions _balanced = new() { Profile = QrDecodeProfile.Balanced };

#if COMPARE_ZXING
    private readonly BarcodeReaderGeneric _zxingReader = new()
    {
        Options = new DecodingOptions { PossibleFormats = new[] { BarcodeFormat.QR_CODE } }
    };
#endif

    [GlobalSetup]
    public void Setup()
    {
        DecodeSampleHelper.LoadRgba("Assets/DecodingSamples/qr-clean-small.png", out _cleanRgba, out _cleanWidth, out _cleanHeight);
    }

    [Benchmark(Baseline = true, Description = "CodeGlyphX QR Decode (clean, balanced)")]
    public bool CodeGlyphX_DecodeCleanBalanced()
    {
        return QrDecoder.TryDecode(_cleanRgba, _cleanWidth, _cleanHeight, _cleanWidth * 4, PixelFormat.Rgba32, out _, _balanced);
    }

#if COMPARE_ZXING
    [Benchmark(Description = "ZXing.Net QR Decode (clean)")]
    public bool ZXing_DecodeClean()
    {
        return _zxingReader.Decode(_cleanRgba, _cleanWidth, _cleanHeight, RGBLuminanceSource.BitmapFormat.RGBA32) is not null;
    }
#endif
}
