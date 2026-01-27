using System;
using CodeGlyphX;
using CodeGlyphX.Rendering.Ascii;

namespace CodeGlyphX.Examples;

internal static class QrAsciiExample {
    public static void Run(string outputDir) {
        var payload = "https://example.com/console";
        var ascii = QR.Ascii(payload, new MatrixAsciiRenderOptions {
            QuietZone = 4,
            UseUnicodeBlocks = true,
            Scale = 2,
            ModuleWidth = 2,
            ModuleHeight = 1
        });

        Console.WriteLine("ASCII QR preview (increase Scale for phone scanning):");
        Console.WriteLine(ascii);
    }
}
