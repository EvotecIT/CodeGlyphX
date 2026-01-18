using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace CodeGlyphX.Benchmarks;

[SimpleJob(RuntimeMoniker.Net80)]
[MemoryDiagnoser]
[RankColumn]
public class BarcodeBenchmarks
{
    private const string Code128Text = "PRODUCT-12345-ABC";
    private const string EanText = "5901234123457";
    private const string Code39Text = "HELLO-123";

    [Benchmark(Description = "Code 128 PNG")]
    public byte[] Code128_Png()
    {
        return BarcodeEasy.RenderPng(BarcodeType.Code128, Code128Text);
    }

    [Benchmark(Description = "Code 128 SVG")]
    public string Code128_Svg()
    {
        return BarcodeEasy.RenderSvg(BarcodeType.Code128, Code128Text);
    }

    [Benchmark(Description = "EAN PNG")]
    public byte[] Ean_Png()
    {
        return BarcodeEasy.RenderPng(BarcodeType.EAN, EanText);
    }

    [Benchmark(Description = "Code 39 PNG")]
    public byte[] Code39_Png()
    {
        return BarcodeEasy.RenderPng(BarcodeType.Code39, Code39Text);
    }

    [Benchmark(Description = "Code 93 PNG")]
    public byte[] Code93_Png()
    {
        return BarcodeEasy.RenderPng(BarcodeType.Code93, Code39Text);
    }

    [Benchmark(Description = "UPC-A PNG")]
    public byte[] UpcA_Png()
    {
        return BarcodeEasy.RenderPng(BarcodeType.UPCA, "012345678905");
    }
}
