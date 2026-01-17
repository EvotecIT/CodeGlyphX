using System.IO;
using CodeGlyphX;

namespace CodeGlyphX.Examples;

internal static class Pdf417Example {
    public static void Run(string outputDir) {
        var pngPath = Path.Combine(outputDir, "pdf417.png");
        var svgPath = Path.Combine(outputDir, "pdf417.svg");
        var pdfPath = Path.Combine(outputDir, "pdf417.pdf");
        var epsPath = Path.Combine(outputDir, "pdf417.eps");

        Pdf417Code.Save("PDF417-HELLO", pngPath);
        Pdf417Code.Save("PDF417-HELLO", svgPath);
        Pdf417Code.Save("PDF417-HELLO", pdfPath);
        Pdf417Code.Save("PDF417-HELLO", epsPath);
    }
}
