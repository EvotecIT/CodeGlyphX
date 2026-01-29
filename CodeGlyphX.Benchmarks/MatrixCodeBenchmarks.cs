using System.Text;
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
public class MatrixCodeBenchmarks
{
    private const string MediumText = "Serial: ABC123-XYZ789";
    private const string LongText = "Document ID: 98765 | Invoice: INV-2024-001234 | Amount: $1,234.56";
    private byte[] _mediumBytes = null!;
    private byte[] _longBytes = null!;

    [GlobalSetup]
    public void Setup()
    {
        _mediumBytes = Encoding.UTF8.GetBytes(MediumText);
        _longBytes = Encoding.UTF8.GetBytes(LongText);
    }

    [Benchmark(Description = "Data Matrix PNG (medium)")]
    public byte[] DataMatrix_Png_Medium()
    {
        return DataMatrixCode.Render(_mediumBytes, OutputFormat.Png).Data;
    }

    [Benchmark(Description = "Data Matrix PNG (long)")]
    public byte[] DataMatrix_Png_Long()
    {
        return DataMatrixCode.Render(_longBytes, OutputFormat.Png).Data;
    }

    [Benchmark(Description = "Data Matrix SVG")]
    public string DataMatrix_Svg()
    {
        return DataMatrixCode.Render(_mediumBytes, OutputFormat.Svg).GetText();
    }

    [Benchmark(Description = "PDF417 PNG")]
    public byte[] Pdf417_Png()
    {
        return Pdf417Code.Create(LongText).Png();
    }

    [Benchmark(Description = "PDF417 SVG")]
    public string Pdf417_Svg()
    {
        return Pdf417Code.Create(LongText).Svg();
    }

    [Benchmark(Description = "Aztec PNG")]
    public byte[] Aztec_Png()
    {
        return AztecCode.Render(MediumText, OutputFormat.Png).Data;
    }

    [Benchmark(Description = "Aztec SVG")]
    public string Aztec_Svg()
    {
        return AztecCode.Render(MediumText, OutputFormat.Svg).GetText();
    }
}
