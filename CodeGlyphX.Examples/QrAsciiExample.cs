using System;
using CodeGlyphX;
using CodeGlyphX.Rendering.Ascii;

namespace CodeGlyphX.Examples;

internal static class QrAsciiExample {
    public static void Run(string outputDir) {
        var payload = "https://example.com/console";
        var ascii = QR.Ascii(payload, new MatrixAsciiRenderOptions {
            Dark = "##",
            Light = "  ",
            ModuleWidth = 1,
            ModuleHeight = 1
        });

        Console.WriteLine("ASCII QR preview:");
        Console.WriteLine(ascii);
    }
}

