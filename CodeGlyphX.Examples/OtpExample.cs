using System.IO;
using CodeGlyphX;

namespace CodeGlyphX.Examples;

internal static class OtpExample {
    public static void Run(string outputDir) {
        var totp = Otp.Totp("CodeGlyphX", "alice@example.com", "JBSWY3DPEHPK3PXP");
        var hotp = Otp.Hotp("CodeGlyphX", "alice@example.com", "JBSWY3DPEHPK3PXP", counter: 42);

        totp.Save(Path.Combine(outputDir, "otp-totp.png"));
        hotp.Save(Path.Combine(outputDir, "otp-hotp.png"));
    }
}
