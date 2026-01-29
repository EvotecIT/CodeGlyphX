using System;
using System.IO;
using CodeGlyphX;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Ascii;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Examples;

internal static class QrAsciiExample {
    public static void Run(string outputDir) {
        var payload = "https://example.com/console";
        var qr = QrCodeEncoder.EncodeText(payload, QrErrorCorrectionLevel.M);

        var windowWidth = 120;
        try {
            windowWidth = Console.WindowWidth;
        } catch {
            // Some hosts (CI, redirected output) do not expose window size.
        }

        var baseOptions = AsciiPresets.Console(scale: 1, darkColor: new Rgba32(12, 12, 12));
        var widthModules = qr.Size + baseOptions.QuietZone * 2;
        var charsPerModule = Math.Max(1, baseOptions.ModuleWidth);
        var usableWidth = Math.Max(40, windowWidth - 4);
        var scaleFit = usableWidth / Math.Max(1, widthModules * charsPerModule);
        if (scaleFit < 1) scaleFit = 1;
        if (scaleFit > 3) scaleFit = 3;

        var ascii = QrCode.Render(
            payload,
            OutputFormat.Ascii,
            extras: new RenderExtras { MatrixAscii = AsciiPresets.Console(scale: scaleFit, darkColor: new Rgba32(12, 12, 12)) }
        ).GetText();

        var pngPath = Path.Combine(outputDir, "qr-ascii-console.png");
        var png = QrPngRenderer.Render(qr.Modules, new QrPngRenderOptions {
            ModuleSize = 12,
            QuietZone = 6,
            Foreground = new Rgba32(12, 12, 12),
            Background = Rgba32.White,
        });
        File.WriteAllBytes(pngPath, png);

        Console.WriteLine($"ANSI ASCII QR preview (scale {scaleFit}, window width {windowWidth}):");
        Console.WriteLine(ascii);
        Console.WriteLine();
        Console.WriteLine($"Saved a phone-friendly PNG to: {pngPath}");
    }
}
