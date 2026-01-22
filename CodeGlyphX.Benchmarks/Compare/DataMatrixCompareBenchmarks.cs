using System.IO;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using CodeGlyphX.Rendering;
#if COMPARE_ZXING
using ZXing;
using ZXing.Datamatrix;
#endif
#if COMPARE_BARCODER
using Barcoder.DataMatrix;
using Barcoder.Renderer.Image;
#endif

namespace CodeGlyphX.Benchmarks;

[SimpleJob(RuntimeMoniker.Net80)]
[MemoryDiagnoser]
[RankColumn]
public class DataMatrixCompareBenchmarks
{
    private const string MediumText = "Serial: ABC123-XYZ789";
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
        var modules = DataMatrixCode.Encode(MediumText);
        _widthPx = CompareBenchmarkHelpers.MatrixWidthPx(modules, _options);
        _heightPx = CompareBenchmarkHelpers.MatrixHeightPx(modules, _options);

#if COMPARE_ZXING
        var zxingOptions = new DatamatrixEncodingOptions
        {
            Width = _widthPx,
            Height = _heightPx,
            Margin = _options.QuietZone
        };
        _zxingWriter = new BarcodeWriterGeneric
        {
            Format = BarcodeFormat.DATA_MATRIX,
            Options = zxingOptions
        };
#endif

#if COMPARE_BARCODER
        var barcoderOptions = CompareBenchmarkHelpers.CreateBarcoderMatrixOptions(_options);
        _barcoderRenderer = new ImageRenderer(barcoderOptions);
#endif
    }

    [Benchmark(Baseline = true, Description = "CodeGlyphX Data Matrix PNG (medium)")]
    public byte[] CodeGlyphX_DataMatrix_Png()
    {
        return DataMatrixCode.Png(MediumText, options: _options);
    }

#if COMPARE_ZXING
    [Benchmark(Description = "ZXing.Net Data Matrix PNG (medium)")]
    public byte[] ZXing_DataMatrix_Png()
    {
        using var image = _zxingWriter.WriteAsImageSharp<SixLabors.ImageSharp.PixelFormats.Rgba32>(MediumText);
        return ImageSharpBenchmarkHelpers.ToPngBytes(image);
    }
#endif

#if COMPARE_BARCODER
    [Benchmark(Description = "Barcoder Data Matrix PNG (medium)")]
    public byte[] Barcoder_DataMatrix_Png()
    {
        var barcode = DataMatrixEncoder.Encode(MediumText, fixedNumberOfRows: null, fixedNumberOfColumns: null, gs1ModeEnabled: false);
        using var stream = new MemoryStream();
        _barcoderRenderer.Render(barcode, stream);
        return stream.ToArray();
    }
#endif
}
