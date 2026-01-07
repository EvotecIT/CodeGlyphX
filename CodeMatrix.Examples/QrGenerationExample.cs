using CodeMatrix;

namespace CodeMatrix.Examples;

internal static class QrGenerationExample {
    public static void Run(string outputDir) {
        var payload = "https://example.com/codematrix?from=examples";
        var png = QrEasy.RenderPng(payload);
        ExampleHelpers.WriteBinary(outputDir, "qr-basic.png", png);

        var svg = QrEasy.RenderSvg(payload);
        ExampleHelpers.WriteText(outputDir, "qr-basic.svg", svg);

        var html = QrEasy.RenderHtml(payload);
        ExampleHelpers.WriteText(outputDir, "qr-basic.html", ExampleHelpers.WrapHtml("CodeMatrix QR", html));

        var jpeg = QrEasy.RenderJpeg(payload);
        ExampleHelpers.WriteBinary(outputDir, "qr-basic.jpg", jpeg);
    }
}
