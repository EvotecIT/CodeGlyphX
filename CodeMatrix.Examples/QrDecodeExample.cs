using System.Text;
using CodeMatrix;
using CodeMatrix.Rendering.Png;

namespace CodeMatrix.Examples;

internal static class QrDecodeExample {
    public static void Run(string outputDir) {
        var payload = "Decode me with CodeMatrix 1234";
        var qr = QrCodeEncoder.EncodeText(payload, QrErrorCorrectionLevel.M, 1, 10, null);

        var sb = new StringBuilder();
        if (QrDecoder.TryDecode(qr.Modules, out var decodedModules)) {
            sb.AppendLine("Modules: ok");
            sb.AppendLine(decodedModules.Text);
        } else {
            sb.AppendLine("Modules: failed");
        }
        sb.AppendLine();

        var opts = new QrPngRenderOptions { ModuleSize = 6, QuietZone = 4 };
        var pixels = ExampleHelpers.RenderQrPixels(qr.Modules, opts, out var width, out var height, out var stride);
        if (QrDecoder.TryDecode(pixels, width, height, stride, PixelFormat.Rgba32, out var decodedPixels)) {
            sb.AppendLine("Pixels: ok");
            sb.AppendLine(decodedPixels.Text);
        } else {
            sb.AppendLine("Pixels: failed");
        }

        ExampleHelpers.WriteText(outputDir, "qr-decode.txt", sb.ToString());
    }
}
