using System.Text;

namespace CodeMatrix.Examples;

internal static class Program {
    private static void Main() {
        Console.OutputEncoding = Encoding.UTF8;
        var baseDir = AppContext.BaseDirectory;
        Directory.SetCurrentDirectory(baseDir);

        var outputDir = Path.Combine(baseDir, "Examples");
        Directory.CreateDirectory(outputDir);

        QrGenerationExample.Run(outputDir);
        QrPayloadsExample.Run(outputDir);
        QrFancyExample.Run(outputDir);
        EvotecExamples.Run(outputDir);
        QrDecodeExample.Run(outputDir);
        OtpExample.Run(outputDir);
        BarcodeExample.Run(outputDir);

        Console.WriteLine($"Examples written to: {outputDir}");
    }
}
