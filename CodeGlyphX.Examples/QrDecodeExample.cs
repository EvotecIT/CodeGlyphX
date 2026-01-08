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
    }
}
