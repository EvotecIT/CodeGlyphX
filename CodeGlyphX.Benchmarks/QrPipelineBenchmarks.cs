using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using CodeGlyphX;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Benchmarks;

#if BENCH_QUICK
[SimpleJob(RuntimeMoniker.Net80, warmupCount: 1, iterationCount: 3, invocationCount: 1)]
#else
[SimpleJob(RuntimeMoniker.Net80)]
#endif
[MemoryDiagnoser]
[RankColumn]
public class QrPipelineBenchmarks
{
    private const string ShortText = "Hello, World!";
    private const string MediumText = "https://github.com/EvotecIT/CodeGlyphX";
    private const string LongText = "CodeGlyphX is a blazing-fast, zero-dependency .NET library for encoding and decoding QR codes, Data Matrix, PDF417, Aztec, and all major 1D barcode formats.";

    private QrCode _qrShort = null!;
    private QrCode _qrMedium = null!;
    private QrCode _qrLong = null!;
    private QrPngRenderOptions _renderShort = null!;
    private QrPngRenderOptions _renderMedium = null!;
    private QrPngRenderOptions _renderLong = null!;

    [GlobalSetup]
    public void Setup()
    {
        var opts = new QrEasyOptions();
        _qrShort = QrEasy.Encode(ShortText, opts);
        _qrMedium = QrEasy.Encode(MediumText, opts);
        _qrLong = QrEasy.Encode(LongText, opts);
        _renderShort = CreateDefaultRenderOptions(opts);
        _renderMedium = _renderShort;
        _renderLong = _renderShort;
    }

    [Benchmark(Description = "QR Encode (short text)")]
    public QrCode QrEncode_ShortText()
    {
        return QrEasy.Encode(ShortText);
    }

    [Benchmark(Description = "QR Encode (medium text)")]
    public QrCode QrEncode_MediumText()
    {
        return QrEasy.Encode(MediumText);
    }

    [Benchmark(Description = "QR Encode (long text)")]
    public QrCode QrEncode_LongText()
    {
        return QrEasy.Encode(LongText);
    }

    [Benchmark(Description = "QR Render PNG (short, pre-encoded)")]
    public byte[] QrRenderPng_ShortText()
    {
        return QrPngRenderer.Render(_qrShort.Modules, _renderShort);
    }

    [Benchmark(Description = "QR Render PNG (medium, pre-encoded)")]
    public byte[] QrRenderPng_MediumText()
    {
        return QrPngRenderer.Render(_qrMedium.Modules, _renderMedium);
    }

    [Benchmark(Description = "QR Render PNG (long, pre-encoded)")]
    public byte[] QrRenderPng_LongText()
    {
        return QrPngRenderer.Render(_qrLong.Modules, _renderLong);
    }

    [Benchmark(Description = "QR Render Pixels (short, pre-encoded)")]
    public byte[] QrRenderPixels_ShortText()
    {
        return QrPngRenderer.RenderPixels(_qrShort.Modules, _renderShort, out _, out _, out _);
    }

    [Benchmark(Description = "QR Render Pixels (medium, pre-encoded)")]
    public byte[] QrRenderPixels_MediumText()
    {
        return QrPngRenderer.RenderPixels(_qrMedium.Modules, _renderMedium, out _, out _, out _);
    }

    [Benchmark(Description = "QR Render Pixels (long, pre-encoded)")]
    public byte[] QrRenderPixels_LongText()
    {
        return QrPngRenderer.RenderPixels(_qrLong.Modules, _renderLong, out _, out _, out _);
    }

    private static QrPngRenderOptions CreateDefaultRenderOptions(QrEasyOptions opts)
    {
        return new QrPngRenderOptions
        {
            ModuleSize = opts.ModuleSize,
            QuietZone = opts.QuietZone,
            Foreground = opts.Foreground,
            Background = opts.Background
        };
    }
}
