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

        var runHardArtDiagnostics = Environment.GetEnvironmentVariable("CODEGLYPHX_DECODE_HARD_ART");
        if (!string.IsNullOrEmpty(runHardArtDiagnostics) &&
            (string.Equals(runHardArtDiagnostics, "1", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(runHardArtDiagnostics, "true", StringComparison.OrdinalIgnoreCase))) {
            runner.Run("QR (hard art diagnostics)", QrHardArtDiagnosticsExample.Run);
            runner.PrintSummary();
            return;
        }

        var runDecodeSamples = Environment.GetEnvironmentVariable("CODEGLYPHX_DECODE_SAMPLES");
        if (!string.IsNullOrEmpty(runDecodeSamples) &&
            (string.Equals(runDecodeSamples, "1", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(runDecodeSamples, "true", StringComparison.OrdinalIgnoreCase))) {
            runner.Run("QR (decode samples)", QrDecodeSamplesExample.Run);
            runner.PrintSummary();
            return;
        }

        var runModuleDiff = Environment.GetEnvironmentVariable("CODEGLYPHX_MODULE_DIFF");
        if (!string.IsNullOrEmpty(runModuleDiff) &&
            (string.Equals(runModuleDiff, "1", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(runModuleDiff, "true", StringComparison.OrdinalIgnoreCase))) {
            runner.Run("QR (module diff)", QrModuleDiffExample.Run);
            runner.PrintSummary();
            return;
        }

        var runScreenshotWalkthrough = Environment.GetEnvironmentVariable("CODEGLYPHX_SCREENSHOT_WALKTHROUGH");
        if (!string.IsNullOrEmpty(runScreenshotWalkthrough) &&
            (string.Equals(runScreenshotWalkthrough, "1", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(runScreenshotWalkthrough, "true", StringComparison.OrdinalIgnoreCase))) {
            runner.Run("QR (screenshot walkthrough)", QrScreenshotWalkthroughExample.Run);
            runner.PrintSummary();
            return;
        }

        runner.Run("QR (basic)", QrGenerationExample.Run);
        runner.Run("QR (ascii console)", QrAsciiExample.Run);
        runner.Run("QR (payloads)", QrPayloadsExample.Run);
        runner.Run("QR (styling)", QrFancyExample.Run);
        runner.Run("QR (art presets)", QrArtPresetsExample.Run);
        runner.Run("QR (connected)", QrConnectedExample.Run);
        runner.Run("QR (glow eyes)", QrGlowExample.Run);
        runner.Run("QR (style board)", QrStyleBoardExample.Run);
        runner.Run("QR (print)", QrPrintExample.Run);
        runner.Run("QR (logo)", EvotecExamples.Run);
        runner.Run("QR (decode)", QrDecodeExample.Run);
        runner.Run("QR (screenshot walkthrough)", QrScreenshotWalkthroughExample.Run);
        runner.Run("Decode (auto)", CodeGlyphDecodeExample.Run);
        runner.Run("OTP", OtpExample.Run);
        runner.Run("Barcode", BarcodeExample.Run);
        runner.Run("Data Matrix", DataMatrixExample.Run);
        runner.Run("PDF417", Pdf417Example.Run);

        runner.PrintSummary();
    }
}
