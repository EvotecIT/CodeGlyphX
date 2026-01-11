using System.IO;
using CodeGlyphX;

namespace CodeGlyphX.Examples;

internal static class Pdf417Example {
    public static void Run(string outputDir) {
        var pngPath = Path.Combine(outputDir, "pdf417.png");
        var svgPath = Path.Combine(outputDir, "pdf417.svg");

        Pdf417Code.Save("PDF417-HELLO", pngPath);
        Pdf417Code.Save("PDF417-HELLO", svgPath);
    }
}
