using BenchmarkDotNet.Running;

namespace CodeGlyphX.Benchmarks;

public class Program
{
    public static void Main(string[] args)
    {
        // Run all benchmarks
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);

        // Or run specific benchmark:
        // BenchmarkRunner.Run<QrCodeBenchmarks>();
        // BenchmarkRunner.Run<BarcodeBenchmarks>();
        // BenchmarkRunner.Run<DecodingBenchmarks>();
    }
}
