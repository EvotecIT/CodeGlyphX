using System.IO;
using CodeGlyphX;

namespace CodeGlyphX.Examples;

internal static class DataMatrixExample {
    public static void Run(string outputDir) {
        var pngPath = Path.Combine(outputDir, "datamatrix.png");
        var svgPath = Path.Combine(outputDir, "datamatrix.svg");
        var pdfPath = Path.Combine(outputDir, "datamatrix.pdf");
        var epsPath = Path.Combine(outputDir, "datamatrix.eps");

        DataMatrixCode.Save("DataMatrix-HELLO", pngPath);
        DataMatrixCode.Save("DataMatrix-HELLO", svgPath);
        DataMatrixCode.Save("DataMatrix-HELLO", pdfPath);
        DataMatrixCode.Save("DataMatrix-HELLO", epsPath);
    }
}
