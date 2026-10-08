using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using CodeGlyphX.Rendering;
#if COMPARE_ZXING
using ZXing;
using ZXing.PDF417;
#endif
#if COMPARE_BARCODER
using Barcoder.Pdf417;
#endif

namespace CodeGlyphX.Benchmarks;

#if BENCH_QUICK
[SimpleJob(RuntimeMoniker.Net80, warmupCount: 1, iterationCount: 3, invocationCount: 1)]
#else
[SimpleJob(RuntimeMoniker.Net80)]
#endif
[MemoryDiagnoser]
[RankColumn]
public class Pdf417CompareBenchmarks
{
    private const string LongText = "Document ID: 98765 | Invoice: INV-2024-001234 | Amount: $1,234.56";
    private readonly MatrixOptions _options = new();
#if COMPARE_ZXING
    private int _widthPx;
    private int _heightPx;
    private BarcodeWriterGeneric _zxingWriter = null!;
#endif

#if COMPARE_BARCODER
    private ManagedBarcodeRenderer _barcoderRenderer = null!;
#endif

    [GlobalSetup]
    public void Setup()
    {
#if COMPARE_ZXING
        var modules = Pdf417Code.Encode(LongText).Modules;
        _widthPx = CompareBenchmarkHelpers.MatrixWidthPx(modules, _options);
        _heightPx = CompareBenchmarkHelpers.MatrixHeightPx(modules, _options);
        var zxingOptions = new PDF417EncodingOptions
        {
            Width = _widthPx,
            Height = _heightPx,
            Margin = _options.QuietZone
        };
        _zxingWriter = new BarcodeWriterGeneric
        {
            Format = BarcodeFormat.PDF_417,
            Options = zxingOptions
        };
#endif

#if COMPARE_BARCODER
        _barcoderRenderer = CompareBenchmarkHelpers.CreateBarcoderMatrixRenderer(_options);
#endif
    }

    [Benchmark(Baseline = true, Description = "CodeGlyphX PDF417 PNG")]
    public byte[] CodeGlyphX_Pdf417_Png()
    {
        return Pdf417Code.Render(LongText, OutputFormat.Png, renderOptions: _options).ToArray();
    }

#if COMPARE_ZXING
    [Benchmark(Description = "ZXing.Net PDF417 PNG")]
    public byte[] ZXing_Pdf417_Png()
    {
        return CompareBenchmarkHelpers.EncodeZxingPng(_zxingWriter, LongText);
    }
#endif

#if COMPARE_BARCODER
    // This upstream encoder's PDF417 output is not payload-qualified by either decoder.
    // Keep the adapter available for pixel-fidelity preflight, outside timed comparisons.
    public byte[] Barcoder_Pdf417_Png()
    {
        var barcode = Pdf417Encoder.Encode(LongText, securityLevel: 2);
        return _barcoderRenderer.Render(barcode);
    }
#endif
}
