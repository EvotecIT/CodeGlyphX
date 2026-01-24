using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Benchmarks;

#if BENCH_QUICK
[SimpleJob(RuntimeMoniker.Net80, warmupCount: 1, iterationCount: 3, invocationCount: 1)]
#else
[SimpleJob(RuntimeMoniker.Net80)]
#endif
[MemoryDiagnoser]
[RankColumn]
public class QrCodeBenchmarks
{
    private const string ShortText = "Hello, World!";
    private const string MediumText = "https://github.com/EvotecIT/CodeGlyphX";
    private const string LongText = "CodeGlyphX is a blazing-fast, zero-dependency .NET library for encoding and decoding QR codes, Data Matrix, PDF417, Aztec, and all major 1D barcode formats.";
#if !BENCH_QUICK
    private static readonly QrEasyOptions LogoOptions = CreateLogoOptions();
    private static readonly QrEasyOptions FancyOptions = new() { Style = QrRenderStyle.Fancy };
#endif

    [Benchmark(Description = "QR PNG (short text)")]
    public byte[] QrPng_ShortText()
    {
        return QrEasy.RenderPng(ShortText);
    }

    [Benchmark(Description = "QR PNG (medium text)")]
    public byte[] QrPng_MediumText()
    {
        return QrEasy.RenderPng(MediumText);
    }

    [Benchmark(Description = "QR PNG (long text)")]
    public byte[] QrPng_LongText()
    {
        return QrEasy.RenderPng(LongText);
    }

    [Benchmark(Description = "QR SVG (medium text)")]
    public string QrSvg_MediumText()
    {
        return QrEasy.RenderSvg(MediumText);
    }

    [Benchmark(Description = "QR PNG High Error Correction")]
    public byte[] QrPng_HighEC()
    {
        return QrEasy.RenderPng(MediumText, new QrEasyOptions { ErrorCorrectionLevel = QrErrorCorrectionLevel.H });
    }

#if !BENCH_QUICK
    [Benchmark(Description = "QR PNG (medium text, logo)")]
    public byte[] QrPng_MediumText_Logo()
    {
        return QrEasy.RenderPng(MediumText, LogoOptions);
    }

    [Benchmark(Description = "QR PNG (medium text, fancy)")]
    public byte[] QrPng_MediumText_Fancy()
    {
        return QrEasy.RenderPng(MediumText, FancyOptions);
    }
#endif

    [Benchmark(Description = "QR HTML (medium text)")]
    public string QrHtml_MediumText()
    {
        return QrEasy.RenderHtml(MediumText);
    }

#if !BENCH_QUICK
    private static QrEasyOptions CreateLogoOptions()
    {
        var logo = LogoBuilder.CreateCirclePng(
            size: 96,
            color: new Rgba32(24, 24, 24, 255),
            accent: new Rgba32(240, 240, 240, 255),
            out _,
            out _);
        return QrPresets.Logo(logo);
    }
#endif
}
