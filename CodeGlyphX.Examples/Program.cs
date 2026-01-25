using System.Text;

namespace CodeGlyphX.Examples;

internal static class Program {
    private static void Main() {
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;

        var outputDir = ExampleRunner.PrepareOutputDirectory();
        var runner = new ExampleRunner(outputDir);

        var runDiagnostics = Environment.GetEnvironmentVariable("CODEGLYPHX_DIAG_QR");
        if (!string.IsNullOrEmpty(runDiagnostics) &&
            (string.Equals(runDiagnostics, "1", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(runDiagnostics, "true", StringComparison.OrdinalIgnoreCase))) {
            runner.Run("QR (diagnostics)", QrDiagnosticsExample.Run);
            runner.PrintSummary();
            return;
        }

        runner.Run("QR (basic)", QrGenerationExample.Run);
        runner.Run("QR (payloads)", QrPayloadsExample.Run);
        runner.Run("QR (styling)", QrFancyExample.Run);
        runner.Run("QR (style board)", QrStyleBoardExample.Run);
        runner.Run("QR (logo)", EvotecExamples.Run);
        runner.Run("QR (decode)", QrDecodeExample.Run);
        runner.Run("Decode (auto)", CodeGlyphDecodeExample.Run);
        runner.Run("OTP", OtpExample.Run);
        runner.Run("Barcode", BarcodeExample.Run);
        runner.Run("Data Matrix", DataMatrixExample.Run);
        runner.Run("PDF417", Pdf417Example.Run);

        runner.PrintSummary();
    }
}
