using System.Text;
using CodeMatrix;
using CodeMatrix.Rendering;

namespace CodeMatrix.Examples;

internal static class QrDecodeExample {
    public static void Run(string outputDir) {
        var payload = "Decode me with CodeMatrix 1234";
        var sb = new StringBuilder();
        var png = QR.Png(payload);
        var decodedPng = QR.DecodePng(png);
        sb.AppendLine("PNG: ok");
        sb.AppendLine(decodedPng.Text);

        sb.ToString().WriteText(outputDir, "qr-decode.txt");
    }
}
