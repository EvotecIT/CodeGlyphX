using System;
using System.IO;
using CodeGlyphX;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Ascii;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Examples;

internal static class QrAsciiExample {
    public static void Run(string outputDir) {
        const int CompactTargetWidth = 28;
        const int CompactTargetHeight = 14;
        const int SquareTargetWidth = 48;
        const int SquareTargetHeight = 24;

        var payload = "https://example.com/console";
        var payloadCompact = "https://exm.pl";
        var payloadLarge = "https://example.com/console?ref=codematrix";
        var qr = QrCodeEncoder.EncodeText(payload, QrErrorCorrectionLevel.M);
        var qrCompact = QrCodeEncoder.EncodeText(payloadCompact, QrErrorCorrectionLevel.M);
        var qrLarge = QrCodeEncoder.EncodeText(payloadLarge, QrErrorCorrectionLevel.M);

        var darkCompactOptions = AsciiConsolePresets.CompactDarkForeground();
        darkCompactOptions.DarkColor = new Rgba32(0, 220, 255);
        darkCompactOptions.DarkGradient = new AsciiGradientOptions {
            Type = AsciiGradientType.Diagonal,
            StartColor = new Rgba32(0, 220, 255),
            EndColor = new Rgba32(255, 96, 200)
        };
        darkCompactOptions.QuietZone = 0;
        darkCompactOptions.MinScale = 1;
        darkCompactOptions.MaxScale = 1;
        darkCompactOptions.PaddingColumns = 1;
        darkCompactOptions.PaddingRows = 1;
        darkCompactOptions.TargetWidth = CompactTargetWidth;
        darkCompactOptions.TargetHeight = CompactTargetHeight;
        darkCompactOptions.CellAspectRatio = 0.45;
        var darkCompactAsciiOptions = AsciiConsole.Fit(qrCompact.Modules, darkCompactOptions);
        var darkCompactAscii = MatrixAsciiRenderer.Render(qrCompact.Modules, darkCompactAsciiOptions);

        var lightCompactOptions = AsciiConsolePresets.Compact();
        lightCompactOptions.DarkColor = new Rgba32(0, 0, 0);
        lightCompactOptions.LightColor = new Rgba32(255, 255, 255);
        lightCompactOptions.QuietZone = 0;
        lightCompactOptions.MinScale = 1;
        lightCompactOptions.MaxScale = 1;
        lightCompactOptions.PaddingColumns = 1;
        lightCompactOptions.PaddingRows = 1;
        lightCompactOptions.TargetWidth = CompactTargetWidth;
        lightCompactOptions.TargetHeight = CompactTargetHeight;
        lightCompactOptions.CellAspectRatio = 0.45;
        var lightCompactAsciiOptions = AsciiConsole.Fit(qrCompact.Modules, lightCompactOptions);
        var lightCompactAscii = MatrixAsciiRenderer.Render(qrCompact.Modules, lightCompactAsciiOptions);

        var lightSquareOptions = AsciiConsolePresets.Square();
        lightSquareOptions.DarkColor = new Rgba32(0, 0, 0);
        lightSquareOptions.LightColor = new Rgba32(255, 255, 255);
        lightSquareOptions.QuietZone = 1;
        lightSquareOptions.MinScale = 1;
        lightSquareOptions.MaxScale = 1;
        lightSquareOptions.PaddingColumns = 1;
        lightSquareOptions.PaddingRows = 1;
        lightSquareOptions.TargetWidth = SquareTargetWidth;
        lightSquareOptions.TargetHeight = SquareTargetHeight;
        lightSquareOptions.CellAspectRatio = 0.5;
        lightSquareOptions.AllowModuleWidthShrink = false;
        lightSquareOptions.ModuleWidth = 2;
        lightSquareOptions.ModuleHeight = 1;
        var lightSquareAsciiOptions = AsciiConsole.Fit(qrCompact.Modules, lightSquareOptions);
        var lightSquareAscii = MatrixAsciiRenderer.Render(qrCompact.Modules, lightSquareAsciiOptions);

        var paletteCompactOptions = AsciiConsolePresets.Compact();
        paletteCompactOptions.DarkPalette = new AsciiPaletteOptions {
            Mode = AsciiPaletteMode.CycleDiagonal,
            Colors = new[] {
                new Rgba32(0, 190, 230),
                new Rgba32(120, 210, 255),
                new Rgba32(255, 140, 210)
            }
        };
        paletteCompactOptions.QuietZone = 0;
        paletteCompactOptions.MinScale = 1;
        paletteCompactOptions.MaxScale = 1;
        paletteCompactOptions.PaddingColumns = 1;
        paletteCompactOptions.PaddingRows = 1;
        paletteCompactOptions.TargetWidth = CompactTargetWidth;
        paletteCompactOptions.TargetHeight = CompactTargetHeight;
        paletteCompactOptions.CellAspectRatio = 0.45;
        var paletteCompactAsciiOptions = AsciiConsole.Fit(qrCompact.Modules, paletteCompactOptions);
        var paletteCompactAscii = MatrixAsciiRenderer.Render(qrCompact.Modules, paletteCompactAsciiOptions);

        var radialCompactOptions = AsciiConsolePresets.Compact();
        radialCompactOptions.DarkGradient = new AsciiGradientOptions {
            Type = AsciiGradientType.Radial,
            StartColor = new Rgba32(64, 220, 255),
            EndColor = new Rgba32(255, 110, 180),
            CenterX = 0.5,
            CenterY = 0.5
        };
        radialCompactOptions.QuietZone = 0;
        radialCompactOptions.MinScale = 1;
        radialCompactOptions.MaxScale = 1;
        radialCompactOptions.PaddingColumns = 1;
        radialCompactOptions.PaddingRows = 1;
        radialCompactOptions.TargetWidth = CompactTargetWidth;
        radialCompactOptions.TargetHeight = CompactTargetHeight;
        radialCompactOptions.CellAspectRatio = 0.45;
        var radialCompactAsciiOptions = AsciiConsole.Fit(qrCompact.Modules, radialCompactOptions);
        var radialCompactAscii = MatrixAsciiRenderer.Render(qrCompact.Modules, radialCompactAsciiOptions);

        var scanSafeOptions = AsciiConsolePresets.ScanSafe();
        scanSafeOptions.DarkColor = new Rgba32(10, 10, 10);
        scanSafeOptions.LightColor = new Rgba32(255, 255, 255);
        scanSafeOptions.TargetWidth = CompactTargetWidth;
        scanSafeOptions.TargetHeight = CompactTargetHeight;
        scanSafeOptions.CellAspectRatio = 0.45;
        var scanSafeAsciiOptions = AsciiConsole.Fit(qrCompact.Modules, scanSafeOptions);
        var scanSafeAscii = MatrixAsciiRenderer.Render(qrCompact.Modules, scanSafeAsciiOptions);

        var tinyOptions = AsciiConsolePresets.Tiny();
        tinyOptions.DarkColor = new Rgba32(0, 0, 0);
        tinyOptions.LightColor = new Rgba32(255, 255, 255);
        tinyOptions.TargetWidth = 24;
        tinyOptions.TargetHeight = 12;
        var tinyAsciiOptions = AsciiConsole.Fit(qrCompact.Modules, tinyOptions);
        var tinyAscii = MatrixAsciiRenderer.Render(qrCompact.Modules, tinyAsciiOptions);

        var monoAnsiOptions = new AsciiConsoleOptions {
            UseHalfBlocks = false,
            UseUnicodeBlocks = true,
            UseAnsiColors = true,
            UseTrueColor = true,
            ColorizeLight = true,
            QuietZone = 2,
            MinScale = 1,
            MaxScale = 1,
            ModuleWidth = 2,
            ModuleHeight = 1,
            AllowModuleWidthShrink = false,
            DarkColor = new Rgba32(0, 0, 0),
            LightColor = new Rgba32(255, 255, 255),
            PaddingColumns = 1,
            PaddingRows = 1,
            TargetWidth = SquareTargetWidth,
            TargetHeight = SquareTargetHeight
        };
        var monoAnsiAsciiOptions = AsciiConsole.Fit(qrCompact.Modules, monoAnsiOptions);
        var monoAnsiAscii = MatrixAsciiRenderer.Render(qrCompact.Modules, monoAnsiAsciiOptions);

        var pngPath = Path.Combine(outputDir, "qr-ascii-console.png");
        var png = QrPngRenderer.Render(qr.Modules, new QrPngRenderOptions {
            ModuleSize = 12,
            QuietZone = 6,
            Foreground = new Rgba32(12, 12, 12),
            Background = Rgba32.White,
        });
        File.WriteAllBytes(pngPath, png);

        Console.WriteLine($"ANSI ASCII QR preview (dark compact, {qrCompact.Size}x{qrCompact.Size} modules, scale {darkCompactAsciiOptions.Scale}, half-blocks {darkCompactAsciiOptions.UseHalfBlocks}, module {darkCompactAsciiOptions.ModuleWidth}x{darkCompactAsciiOptions.ModuleHeight}):");
        Console.WriteLine(darkCompactAscii);
        Console.WriteLine();
        Console.WriteLine($"ANSI ASCII QR preview (light compact, {qrCompact.Size}x{qrCompact.Size} modules, scale {lightCompactAsciiOptions.Scale}, half-blocks {lightCompactAsciiOptions.UseHalfBlocks}, module {lightCompactAsciiOptions.ModuleWidth}x{lightCompactAsciiOptions.ModuleHeight}):");
        Console.WriteLine(lightCompactAscii);
        Console.WriteLine();
        Console.WriteLine($"ANSI ASCII QR preview (light square, {qrCompact.Size}x{qrCompact.Size} modules, scale {lightSquareAsciiOptions.Scale}, half-blocks {lightSquareAsciiOptions.UseHalfBlocks}, module {lightSquareAsciiOptions.ModuleWidth}x{lightSquareAsciiOptions.ModuleHeight}):");
        Console.WriteLine(lightSquareAscii);
        Console.WriteLine();
        Console.WriteLine($"ANSI ASCII QR preview (palette compact, {qrCompact.Size}x{qrCompact.Size} modules, scale {paletteCompactAsciiOptions.Scale}, half-blocks {paletteCompactAsciiOptions.UseHalfBlocks}, module {paletteCompactAsciiOptions.ModuleWidth}x{paletteCompactAsciiOptions.ModuleHeight}):");
        Console.WriteLine(paletteCompactAscii);
        Console.WriteLine();
        Console.WriteLine($"ANSI ASCII QR preview (radial compact, {qrCompact.Size}x{qrCompact.Size} modules, scale {radialCompactAsciiOptions.Scale}, half-blocks {radialCompactAsciiOptions.UseHalfBlocks}, module {radialCompactAsciiOptions.ModuleWidth}x{radialCompactAsciiOptions.ModuleHeight}):");
        Console.WriteLine(radialCompactAscii);
        Console.WriteLine();
        Console.WriteLine($"ANSI ASCII QR preview (scan safe compact, {qrCompact.Size}x{qrCompact.Size} modules, scale {scanSafeAsciiOptions.Scale}, half-blocks {scanSafeAsciiOptions.UseHalfBlocks}, module {scanSafeAsciiOptions.ModuleWidth}x{scanSafeAsciiOptions.ModuleHeight}):");
        Console.WriteLine(scanSafeAscii);
        Console.WriteLine();
        Console.WriteLine($"ANSI ASCII QR preview (tiny target, {qrCompact.Size}x{qrCompact.Size} modules, scale {tinyAsciiOptions.Scale}, half-blocks {tinyAsciiOptions.UseHalfBlocks}, module {tinyAsciiOptions.ModuleWidth}x{tinyAsciiOptions.ModuleHeight}):");
        Console.WriteLine(tinyAscii);
        Console.WriteLine();
        Console.WriteLine($"ANSI ASCII QR preview (mono square, {qrCompact.Size}x{qrCompact.Size} modules, scale {monoAnsiAsciiOptions.Scale}, half-blocks {monoAnsiAsciiOptions.UseHalfBlocks}, module {monoAnsiAsciiOptions.ModuleWidth}x{monoAnsiAsciiOptions.ModuleHeight}):");
        Console.WriteLine(monoAnsiAscii);
        Console.WriteLine();
        Console.WriteLine("Tip: The QR size is driven by payload length (module count). Shorter payloads = smaller QR.");
        Console.WriteLine("Tip: If the QR looks too tall or wide, tweak AsciiConsoleOptions.CellAspectRatio (try 0.40-0.60).");
        Console.WriteLine("Tip: To shrink output, lower TargetWidth/TargetHeight (or MaxWindowWidth/MaxWindowHeight).");
        Console.WriteLine();
        Console.WriteLine($"Saved a phone-friendly PNG to: {pngPath}");
    }
}
