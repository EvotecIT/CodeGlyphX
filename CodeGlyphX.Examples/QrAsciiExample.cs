using System;
using CodeGlyphX;
using CodeGlyphX.Rendering.Ascii;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Examples;

internal static class QrAsciiExample {
    public static void Run(string outputDir) {
        var payload = "https://example.com/console";
        var ascii = QR.Ascii(payload, AsciiPresets.Console(scale: 4, darkColor: new Rgba32(12, 12, 12)));

        Console.WriteLine("ANSI ASCII QR preview (increase Scale for phone scanning):");
        Console.WriteLine(ascii);
    }
}
