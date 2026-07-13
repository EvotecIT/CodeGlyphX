using System.Text;

namespace CodeGlyphX.Examples;

internal static class Program {
    private static void Main() {
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;

        var outputDir = ExampleRunner.PrepareOutputDirectory();
        var runner = new ExampleRunner(outputDir);

        if (!TryRunRequestedExample(runner)) {
            RunDefaultExamples(runner);
        }

        Environment.ExitCode = runner.PrintSummary() == 0 ? 0 : 1;
    }

    private static bool TryRunRequestedExample(ExampleRunner runner) {
        if (RunWhenEnabled(runner, "CODEGLYPHX_CI_SMOKE", "Public API smoke", FlagshipApiExample.Run)) return true;
        if (RunWhenEnabled(runner, "CODEGLYPHX_DIAG_QR", "QR (diagnostics)", QrDiagnosticsExample.Run)) return true;
        if (RunWhenEnabled(runner, "CODEGLYPHX_DECODE_HARD_ART", "QR (hard art diagnostics)", QrHardArtDiagnosticsExample.Run)) return true;
        if (RunWhenEnabled(runner, "CODEGLYPHX_DECODE_SAMPLES", "QR (decode samples)", QrDecodeSamplesExample.Run)) return true;
        if (RunWhenEnabled(runner, "CODEGLYPHX_MODULE_DIFF", "QR (module diff)", QrModuleDiffExample.Run)) return true;
        return RunWhenEnabled(runner, "CODEGLYPHX_SCREENSHOT_WALKTHROUGH", "QR (screenshot walkthrough)", QrScreenshotWalkthroughExample.Run);
    }

    private static bool RunWhenEnabled(ExampleRunner runner, string variable, string name, Action<string> run) {
        if (!IsEnabled(variable)) return false;
        runner.Run(name, run);
        return true;
    }

    private static void RunDefaultExamples(ExampleRunner runner) {
        runner.Run("QR (basic)", QrGenerationExample.Run);
        runner.Run("QR (ascii console)", QrAsciiExample.Run);
        runner.Run("QR (payloads)", QrPayloadsExample.Run);
        runner.Run("QR (styling)", QrFancyExample.Run);
        runner.Run("QR (art themes)", QrArtThemesExample.Run);
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
    }

    private static bool IsEnabled(string name) {
        var value = Environment.GetEnvironmentVariable(name);
        return string.Equals(value, "1", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
    }
}