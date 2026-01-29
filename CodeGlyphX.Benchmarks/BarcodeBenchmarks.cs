using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using CodeGlyphX.Rendering;

namespace CodeGlyphX.Benchmarks;

#if BENCH_QUICK
[SimpleJob(RuntimeMoniker.Net80, warmupCount: 1, iterationCount: 3, invocationCount: 1)]
#else
[SimpleJob(RuntimeMoniker.Net80)]
#endif
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
        return Barcode.Render(BarcodeType.Code128, Code128Text, OutputFormat.Png).Data;
    }

    [Benchmark(Description = "Code 128 SVG")]
    public string Code128_Svg()
    {
        return Barcode.Render(BarcodeType.Code128, Code128Text, OutputFormat.Svg).GetText();
    }

    [Benchmark(Description = "EAN PNG")]
    public byte[] Ean_Png()
    {
        return Barcode.Render(BarcodeType.EAN, EanText, OutputFormat.Png).Data;
    }

    [Benchmark(Description = "Code 39 PNG")]
    public byte[] Code39_Png()
    {
        return Barcode.Render(BarcodeType.Code39, Code39Text, OutputFormat.Png).Data;
    }

    [Benchmark(Description = "Code 93 PNG")]
    public byte[] Code93_Png()
    {
        return Barcode.Render(BarcodeType.Code93, Code39Text, OutputFormat.Png).Data;
    }

    [Benchmark(Description = "UPC-A PNG")]
    public byte[] UpcA_Png()
    {
        return Barcode.Render(BarcodeType.UPCA, "012345678905", OutputFormat.Png).Data;
    }
}
