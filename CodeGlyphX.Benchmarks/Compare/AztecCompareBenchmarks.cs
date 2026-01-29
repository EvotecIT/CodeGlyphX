using System.IO;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using CodeGlyphX.Rendering;
#if COMPARE_ZXING
using ZXing;
using ZXing.Aztec;
#endif
#if COMPARE_BARCODER
using Barcoder.Aztec;
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
public class AztecCompareBenchmarks
{
    private const string MediumText = "Serial: ABC123-XYZ789";
    private readonly MatrixOptions _options = new();
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
        var modules = AztecCode.Encode(MediumText);
        _widthPx = CompareBenchmarkHelpers.MatrixWidthPx(modules, _options);
        _heightPx = CompareBenchmarkHelpers.MatrixHeightPx(modules, _options);
        var zxingOptions = new AztecEncodingOptions
        {
            Width = _widthPx,
            Height = _heightPx,
            Margin = _options.QuietZone,
            ErrorCorrection = 33
        };
        _zxingWriter = new BarcodeWriterGeneric
        {
            Format = BarcodeFormat.AZTEC,
            Options = zxingOptions
        };
#endif

#if COMPARE_BARCODER
        var barcoderOptions = CompareBenchmarkHelpers.CreateBarcoderMatrixOptions(_options);
        _barcoderRenderer = new ImageRenderer(barcoderOptions);
#endif
    }

    [Benchmark(Baseline = true, Description = "CodeGlyphX Aztec PNG")]
    public byte[] CodeGlyphX_Aztec_Png()
    {
        return AztecCode.Render(MediumText, OutputFormat.Png, renderOptions: _options).Data;
    }

#if COMPARE_ZXING
    [Benchmark(Description = "ZXing.Net Aztec PNG")]
    public byte[] ZXing_Aztec_Png()
    {
        using var image = _zxingWriter.WriteAsImageSharp<SixLabors.ImageSharp.PixelFormats.Rgba32>(MediumText);
        return ImageSharpBenchmarkHelpers.ToPngBytes(image);
    }
#endif

#if COMPARE_BARCODER
    [Benchmark(Description = "Barcoder Aztec PNG")]
    public byte[] Barcoder_Aztec_Png()
    {
        var barcode = AztecEncoder.Encode(MediumText, minimumEccPercentage: 33, userSpecifiedLayers: 0);
        using var stream = new MemoryStream();
        _barcoderRenderer.Render(barcode, stream);
        return stream.ToArray();
    }
#endif
}
