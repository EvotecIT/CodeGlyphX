using System.IO;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using CodeGlyphX.Rendering;
#if COMPARE_ZXING
using ZXing;
using ZXing.QrCode;
#endif
#if COMPARE_QRCODER
using QRCoder;
#endif
#if COMPARE_BARCODER
using Barcoder.Qr;
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
public class QrCompareBenchmarks
{
    private const string MediumText = "https://github.com/EvotecIT/CodeGlyphX";
    private readonly QrEasyOptions _options = new();
    private int _targetSizePx;

#if COMPARE_ZXING
    private BarcodeWriterGeneric _zxingWriter = null!;
#endif

#if COMPARE_BARCODER
    private ImageRenderer _barcoderRenderer = null!;
#endif

    [GlobalSetup]
    public void Setup()
    {
        var qr = QrEasy.Encode(MediumText, _options);
        _targetSizePx = (qr.Size + _options.QuietZone * 2) * _options.ModuleSize;

#if COMPARE_ZXING
        var zxingOptions = new QrCodeEncodingOptions
        {
            Width = _targetSizePx,
            Height = _targetSizePx,
            Margin = _options.QuietZone,
            ErrorCorrection = ZXing.QrCode.Internal.ErrorCorrectionLevel.M,
            CharacterSet = "UTF-8"
        };
        _zxingWriter = new BarcodeWriterGeneric
        {
            Format = BarcodeFormat.QR_CODE,
            Options = zxingOptions
        };
#endif

#if COMPARE_BARCODER
        _targetSizePx = (qr.Size + _options.QuietZone * 2) * _options.ModuleSize;
        var barcoderOptions = CompareBenchmarkHelpers.CreateBarcoderMatrixOptions(new MatrixOptions
        {
            ModuleSize = _options.ModuleSize,
            QuietZone = _options.QuietZone
        });
        _barcoderRenderer = new ImageRenderer(barcoderOptions);
#endif
    }

    [Benchmark(Baseline = true, Description = "CodeGlyphX QR PNG (medium)")]
    public byte[] CodeGlyphX_QrPng()
    {
        return QrCode.Render(MediumText, OutputFormat.Png, _options).Data;
    }

#if COMPARE_ZXING
    [Benchmark(Description = "ZXing.Net QR PNG (medium)")]
    public byte[] ZXing_QrPng()
    {
        using var image = _zxingWriter.WriteAsImageSharp<SixLabors.ImageSharp.PixelFormats.Rgba32>(MediumText);
        return ImageSharpBenchmarkHelpers.ToPngBytes(image);
    }
#endif

#if COMPARE_QRCODER
    [Benchmark(Description = "QRCoder QR PNG (medium)")]
    public byte[] QrCoder_QrPng()
    {
        return PngByteQRCodeHelper.GetQRCode(MediumText, QRCodeGenerator.ECCLevel.M, _options.ModuleSize, true);
    }
#endif

#if COMPARE_BARCODER
    [Benchmark(Description = "Barcoder QR PNG (medium)")]
    public byte[] Barcoder_QrPng()
    {
        var barcode = QrEncoder.Encode(MediumText, Barcoder.Qr.ErrorCorrectionLevel.M, Barcoder.Qr.Encoding.Auto);
        using var stream = new MemoryStream();
        _barcoderRenderer.Render(barcode, stream);
        return stream.ToArray();
    }
#endif
}
