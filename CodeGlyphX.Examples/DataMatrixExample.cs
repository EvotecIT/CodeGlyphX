using System.IO;
using CodeGlyphX;

namespace CodeGlyphX.Examples;

internal static class DataMatrixExample {
    public static void Run(string outputDir) {
        var pngPath = Path.Combine(outputDir, "datamatrix.png");
        var svgPath = Path.Combine(outputDir, "datamatrix.svg");

        DataMatrixCode.Save("DataMatrix-HELLO", pngPath);
        DataMatrixCode.Save("DataMatrix-HELLO", svgPath);
    }
}
