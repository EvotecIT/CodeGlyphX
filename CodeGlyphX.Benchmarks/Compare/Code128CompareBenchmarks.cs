using System.IO;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using CodeGlyphX.Rendering;
#if COMPARE_ZXING
using ZXing;
#endif
#if COMPARE_BARCODER
using Barcoder.Code128;
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
public class Code128CompareBenchmarks
{
    private const string Code128Text = "PRODUCT-12345-ABC";
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
        _widthPx = CompareBenchmarkHelpers.BarcodeWidthPx(BarcodeType.Code128, Code128Text, _options);
        _heightPx = CompareBenchmarkHelpers.BarcodeHeightPx(_options);
        _zxingWriter = new BarcodeWriterGeneric
        {
            Format = BarcodeFormat.CODE_128,
            Options = CompareBenchmarkHelpers.CreateZxingOptions(_widthPx, _heightPx, _options.QuietZone)
        };
#endif

#if COMPARE_BARCODER
        var barcoderOptions = CompareBenchmarkHelpers.CreateBarcoderBarcodeOptions(_options);
        _barcoderRenderer = new ImageRenderer(barcoderOptions);
#endif
    }

    [Benchmark(Baseline = true, Description = "CodeGlyphX Code128 PNG")]
    public byte[] CodeGlyphX_Code128_Png()
    {
        return Barcode.Render(BarcodeType.Code128, Code128Text, OutputFormat.Png, _options).Data;
    }

#if COMPARE_ZXING
    [Benchmark(Description = "ZXing.Net Code128 PNG")]
    public byte[] ZXing_Code128_Png()
    {
        using var image = _zxingWriter.WriteAsImageSharp<SixLabors.ImageSharp.PixelFormats.Rgba32>(Code128Text);
        return ImageSharpBenchmarkHelpers.ToPngBytes(image);
    }
#endif

#if COMPARE_BARCODER
    [Benchmark(Description = "Barcoder Code128 PNG")]
    public byte[] Barcoder_Code128_Png()
    {
        var barcode = Code128Encoder.Encode(Code128Text, includeChecksum: true, gs1ModeEnabled: false);
        using var stream = new MemoryStream();
        _barcoderRenderer.Render(barcode, stream);
        return stream.ToArray();
    }
#endif
}
