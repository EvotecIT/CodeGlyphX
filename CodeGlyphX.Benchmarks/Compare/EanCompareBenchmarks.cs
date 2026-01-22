using System.IO;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using CodeGlyphX.Rendering;
#if COMPARE_ZXING
using ZXing;
#endif
#if COMPARE_BARCODER
using Barcoder.Ean;
using Barcoder.Renderer.Image;
#endif

namespace CodeGlyphX.Benchmarks;

#if BENCH_QUICK
[SimpleJob(RuntimeMoniker.Net80, warmupCount: 1, iterationCount: 3, invocationCount: 1)]
#else
[SimpleJob(RuntimeMoniker.Net80)]
#endif
[MemoryDiagnoser]
[RankColumn]
public class EanCompareBenchmarks
{
    private const string EanText = "5901234123457";
    private readonly BarcodeOptions _options = new();
#if COMPARE_ZXING
    private int _widthPx;
    private int _heightPx;
    private BarcodeWriterGeneric _zxingWriter = null!;
#endif

#if COMPARE_BARCODER
    private ImageRenderer _barcoderRenderer = null!;
#endif

    [GlobalSetup]
    public void Setup()
    {
#if COMPARE_ZXING
        _widthPx = CompareBenchmarkHelpers.BarcodeWidthPx(BarcodeType.EAN, EanText, _options);
        _heightPx = CompareBenchmarkHelpers.BarcodeHeightPx(_options);
        _zxingWriter = new BarcodeWriterGeneric
        {
            Format = BarcodeFormat.EAN_13,
            Options = CompareBenchmarkHelpers.CreateZxingOptions(_widthPx, _heightPx, _options.QuietZone)
        };
#endif

#if COMPARE_BARCODER
        var barcoderOptions = CompareBenchmarkHelpers.CreateBarcoderBarcodeOptions(_options);
        _barcoderRenderer = new ImageRenderer(barcoderOptions);
#endif
    }

    [Benchmark(Baseline = true, Description = "CodeGlyphX EAN-13 PNG")]
    public byte[] CodeGlyphX_Ean_Png()
    {
        return BarcodeEasy.RenderPng(BarcodeType.EAN, EanText, _options);
    }

#if COMPARE_ZXING
    [Benchmark(Description = "ZXing.Net EAN-13 PNG")]
    public byte[] ZXing_Ean_Png()
    {
        using var image = _zxingWriter.WriteAsImageSharp<SixLabors.ImageSharp.PixelFormats.Rgba32>(EanText);
        return ImageSharpBenchmarkHelpers.ToPngBytes(image);
    }
#endif

#if COMPARE_BARCODER
    [Benchmark(Description = "Barcoder EAN-13 PNG")]
    public byte[] Barcoder_Ean_Png()
    {
        var barcode = EanEncoder.Encode(EanText);
        using var stream = new MemoryStream();
        _barcoderRenderer.Render(barcode, stream);
        return stream.ToArray();
    }
#endif
}
