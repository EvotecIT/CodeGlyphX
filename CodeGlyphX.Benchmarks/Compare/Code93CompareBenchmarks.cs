using System.IO;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using CodeGlyphX.Rendering;
#if COMPARE_ZXING
using ZXing;
#endif
#if COMPARE_BARCODER
using Barcoder.Code93;
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
public class Code93CompareBenchmarks
{
    private const string Code93Text = "HELLO-123";
    private readonly BarcodeOptions _options = new();
    private int _widthPx;
    private int _heightPx;

#if COMPARE_ZXING
    private BarcodeWriterGeneric _zxingWriter = null!;
#endif

#if COMPARE_BARCODER
    private ImageRenderer _barcoderRenderer = null!;
#endif

    [GlobalSetup]
    public void Setup()
    {
        _widthPx = CompareBenchmarkHelpers.BarcodeWidthPx(BarcodeType.Code93, Code93Text, _options);
        _heightPx = CompareBenchmarkHelpers.BarcodeHeightPx(_options);

#if COMPARE_ZXING
        _zxingWriter = new BarcodeWriterGeneric
        {
            Format = BarcodeFormat.CODE_93,
            Options = CompareBenchmarkHelpers.CreateZxingOptions(_widthPx, _heightPx, _options.QuietZone)
        };
#endif

#if COMPARE_BARCODER
        var barcoderOptions = CompareBenchmarkHelpers.CreateBarcoderBarcodeOptions(_options);
        _barcoderRenderer = new ImageRenderer(barcoderOptions);
#endif
    }

    [Benchmark(Baseline = true, Description = "CodeGlyphX Code93 PNG")]
    public byte[] CodeGlyphX_Code93_Png()
    {
        return BarcodeEasy.RenderPng(BarcodeType.Code93, Code93Text, _options);
    }

#if COMPARE_ZXING
    [Benchmark(Description = "ZXing.Net Code93 PNG")]
    public byte[] ZXing_Code93_Png()
    {
        using var image = _zxingWriter.WriteAsImageSharp<SixLabors.ImageSharp.PixelFormats.Rgba32>(Code93Text);
        return ImageSharpBenchmarkHelpers.ToPngBytes(image);
    }
#endif

#if COMPARE_BARCODER
    [Benchmark(Description = "Barcoder Code93 PNG")]
    public byte[] Barcoder_Code93_Png()
    {
        var barcode = Code93Encoder.Encode(Code93Text, includeChecksum: true, fullAsciiMode: false);
        using var stream = new MemoryStream();
        _barcoderRenderer.Render(barcode, stream);
        return stream.ToArray();
    }
#endif
}
