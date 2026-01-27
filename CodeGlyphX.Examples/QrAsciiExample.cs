using System;
using CodeGlyphX;
using CodeGlyphX.Rendering.Ascii;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Examples;

internal static class QrAsciiExample {
    public static void Run(string outputDir) {
        var payload = "https://example.com/console";
        var ascii = QR.Ascii(payload, new MatrixAsciiRenderOptions {
            QuietZone = 4,
            UseUnicodeBlocks = true,
            UseAnsiColors = true,
            UseAnsiTrueColor = true,
            AnsiDarkColor = new Rgba32(16, 16, 16),
            Scale = 3,
            ModuleWidth = 2,
            ModuleHeight = 1
        });

        Console.WriteLine("ANSI ASCII QR preview (increase Scale for phone scanning):");
        Console.WriteLine(ascii);
    }
}
