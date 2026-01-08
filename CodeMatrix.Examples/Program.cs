using System.Text;

namespace CodeGlyphX.Examples;

internal static class Program {
    private static void Main() {
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;

        var outputDir = ExampleRunner.PrepareOutputDirectory();
        var runner = new ExampleRunner(outputDir);

        runner.Run("QR generation (PNG/SVG/HTML/JPEG)", QrGenerationExample.Run);
        runner.Run("QR payloads (URL, contact, wifi, etc.)", QrPayloadsExample.Run);
        runner.Run("QR styling (gradients/logo)", QrFancyExample.Run);
        runner.Run("QR logo (Evotec)", EvotecExamples.Run);
        runner.Run("QR decoding (modules/pixels)", QrDecodeExample.Run);
        runner.Run("OTP QR (TOTP/HOTP + safety)", OtpExample.Run);
        runner.Run("Barcode (Code 128)", BarcodeExample.Run);

        runner.PrintSummary();
    }
}
