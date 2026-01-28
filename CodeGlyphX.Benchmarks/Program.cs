using BenchmarkDotNet.Running;
using System;
using System.Linq;

namespace CodeGlyphX.Benchmarks;

public class Program
{
    public static void Main(string[] args)
    {
        var preflight = args.Any(arg => string.Equals(arg, "--preflight", StringComparison.OrdinalIgnoreCase));
        var filteredArgs = args.Where(arg => !string.Equals(arg, "--preflight", StringComparison.OrdinalIgnoreCase)).ToArray();

        if (preflight)
        {
            var exitCode = PreflightChecks.Run();
            Environment.Exit(exitCode);
        }

        if (QrDecodePackRunner.TryParseArgs(filteredArgs, out var packOptions, out var remainingArgs))
        {
            var exitCode = QrDecodePackRunner.Run(packOptions);
            Environment.Exit(exitCode);
        }

        // Run all benchmarks
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(remainingArgs);

        // Or run specific benchmark:
        // BenchmarkRunner.Run<QrCodeBenchmarks>();
        // BenchmarkRunner.Run<QrPipelineBenchmarks>();
        // BenchmarkRunner.Run<BarcodeBenchmarks>();
        // BenchmarkRunner.Run<QrDecodeBenchmarks>();
        // BenchmarkRunner.Run<DecodingBenchmarks>();
    }
}
