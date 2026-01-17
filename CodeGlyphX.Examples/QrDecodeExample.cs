using System.IO;
using System.Text;
using CodeGlyphX;
using CodeGlyphX.Rendering;

namespace CodeGlyphX.Examples;

internal static class QrDecodeExample {
    public static void Run(string outputDir) {
        var payload = "Decode me with CodeGlyphX 1234";
        var png = QR.Png(payload);
        if (QR.TryDecodePng(png, out var decoded)) {
            decoded.Text.WriteText(outputDir, "qr-decode.txt");
        }

        var screenPath = Path.Combine(outputDir, "qr-screen.png");
        png.WriteBinary(screenPath);
        if (QrImageDecoder.TryDecodeImage(File.ReadAllBytes(screenPath), out var screenDecoded, out var screenInfo, new QrPixelDecodeOptions { Profile = QrDecodeProfile.Robust })) {
            screenDecoded.Text.WriteText(outputDir, "qr-decode-screen.txt");
        } else {
            QrDiagnosticsDump.WriteText(outputDir, "qr-decode-screen.txt", screenInfo, label: "Decode failed", source: screenPath);
        }

        var fast = new QrPixelDecodeOptions { Profile = QrDecodeProfile.Fast };
        if (QrImageDecoder.TryDecodeImage(png, fast, out var decodedFast)) {
            decodedFast.Text.WriteText(outputDir, "qr-decode-fast.txt");
        }
    }
}
