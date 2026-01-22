using System.IO;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using CodeGlyphX.Rendering;
#if COMPARE_ZXING
using ZXing;
using ZXing.PDF417;
#endif
#if COMPARE_BARCODER
using Barcoder.Pdf417;
using Barcoder.Renderer.Image;
#endif

namespace CodeGlyphX.Benchmarks;

[SimpleJob(RuntimeMoniker.Net80)]
[MemoryDiagnoser]
[RankColumn]
public class Pdf417CompareBenchmarks
{
    private const string LongText = "Document ID: 98765 | Invoice: INV-2024-001234 | Amount: $1,234.56";
    private readonly MatrixOptions _options = new();
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
        var modules = Pdf417Code.Encode(LongText);
        _widthPx = CompareBenchmarkHelpers.MatrixWidthPx(modules, _options);
        _heightPx = CompareBenchmarkHelpers.MatrixHeightPx(modules, _options);

#if COMPARE_ZXING
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
        var barcoderOptions = CompareBenchmarkHelpers.CreateBarcoderMatrixOptions(_options);
        _barcoderRenderer = new ImageRenderer(barcoderOptions);
#endif
    }

    [Benchmark(Baseline = true, Description = "CodeGlyphX PDF417 PNG")]
    public byte[] CodeGlyphX_Pdf417_Png()
    {
        return Pdf417Code.Png(LongText, renderOptions: _options);
    }

#if COMPARE_ZXING
    [Benchmark(Description = "ZXing.Net PDF417 PNG")]
    public byte[] ZXing_Pdf417_Png()
    {
        using var image = _zxingWriter.WriteAsImageSharp<SixLabors.ImageSharp.PixelFormats.Rgba32>(LongText);
        return ImageSharpBenchmarkHelpers.ToPngBytes(image);
    }
#endif

#if COMPARE_BARCODER
    [Benchmark(Description = "Barcoder PDF417 PNG")]
    public byte[] Barcoder_Pdf417_Png()
    {
        var barcode = Pdf417Encoder.Encode(LongText, securityLevel: 2);
        using var stream = new MemoryStream();
        _barcoderRenderer.Render(barcode, stream);
        return stream.ToArray();
    }
#endif
}
