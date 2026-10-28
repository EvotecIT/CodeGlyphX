using System;
using System.Collections.Generic;
using CodeGlyphX;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Art;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Examples;

/// <summary>
/// The looks on the website style board. Each one is built from public render options only
/// (hero presets, art themes, module shapes, eyes, palettes, canvas templates and logos), so the
/// board shows what a caller actually gets. The scan tests render every look through
/// <see cref="RenderPng"/> and decode the exact payload with two decoders.
/// </summary>
internal static class QrStyleBoardLooks {
    internal const string Payload = "https://codeglyphx.com";

    /// <summary>Code size including the quiet zone; canvases and templates add their own margin.</summary>
    internal const int TargetSizePx = 480;

    internal sealed record Look(string Name, string Summary, Func<QrRenderOptions> CreateOptions, Func<byte[]>? CreateLogo = null) {
        internal string Slug => Slugify(Name);
    }

    internal static IReadOnlyList<Look> All { get; } = new List<Look> {
        new("Sunset", "Connected body with an orange-to-pink gradient on cream.",
            () => Hero(QrHeroPreset.Sunset)),

        new("Neon", "Vertical bars and magenta eyes on a glowing panel at night.",
            () => Hero(QrHeroPreset.Neon, darkPanel: true)),

        new("Ink", "Liquid black body with round eyes on white.",
            () => Hero(QrHeroPreset.Ink)),

        new("Retro Pixel", "Square pixels and square eyes on mustard.",
            () => Hero(QrHeroPreset.RetroPixel)),

        new("Gold Sparkle", "Connected bronze body and sparkle eyes on a black-and-gold canvas.",
            () => {
                var options = Base(QrModuleShape.ConnectedRounded, R(92, 60, 4), R(255, 250, 232));
                options.Eyes = QrEyePresets.NeonSparkle();
                options.Eyes.OuterColor = R(150, 98, 0);
                options.Eyes.InnerColor = R(60, 38, 0);
                options.Eyes.SparkleColor = new Rgba32(255, 222, 140, 210);
                options.Canvas = GradientCanvas(R(14, 12, 8), R(70, 50, 12), paddingPx: 44);
                return options;
            }),

        new("Ticket", "Connected navy body inside a coral ticket stub with a caption.",
            () => {
                var options = Base(QrModuleShape.ConnectedRounded, R(24, 28, 52), Rgba32.White);
                options.Canvas = new QrCanvasOptions {
                    Template = QrCanvasTemplate.TicketStub,
                    Background = R(255, 92, 72),
                    Caption = new QrCanvasCaptionOptions { Text = "ADMIT ONE", Weight = QrCanvasCaptionWeight.Bold },
                };
                return options;
            }),

        new("Botanical", "Leaf modules and mirrored leaf eyes in forest green.",
            () => Hero(QrHeroPreset.Botanical)),

        new("Confetti", "Rounded modules in a random three-colour palette on cream.",
            () => Base(QrModuleShape.Rounded, R(204, 34, 84), R(255, 246, 236),
                palette: Palette(QrPaletteMode.Random, R(204, 34, 84), R(112, 38, 196), R(0, 112, 160)))),

        new("Mono Classy", "Square body and classy eyes on a charcoal panel.",
            () => Hero(QrHeroPreset.MonoClassy, darkPanel: true)),

        new("Ocean", "Horizontal bars with a navy-to-teal gradient on pale aqua.",
            () => {
                var options = Hero(QrHeroPreset.Ocean);
                options.Background = R(226, 247, 245);
                return options;
            }),

        new("Scan Me", "Rounded navy body with a SCAN ME pill on yellow.",
            () => {
                var options = Base(QrModuleShape.Rounded, R(30, 34, 96), Rgba32.White);
                options.Canvas = new QrCanvasOptions {
                    Template = QrCanvasTemplate.ScanMePill,
                    Background = R(255, 210, 80),
                    Caption = new QrCanvasCaptionOptions { Text = "SCAN ME", Weight = QrCanvasCaptionWeight.Bold },
                };
                return options;
            }),

        new("Berry", "Rounded body with a plum-to-raspberry gradient on blush.",
            () => Hero(QrHeroPreset.Berry)),

        new("Glow Frame", "The ConnectedSquircleGlow art theme in its pastel variant.",
            () => Theme(QrArtTheme.ConnectedSquircleGlow, QrArtVariant.Pastel)),

        new("Paper Cutout", "Liquid cocoa body on a cream card stacked over kraft paper.",
            () => Hero(QrHeroPreset.PaperCutout)),

        new("Brand Logo", "Rounded body, maroon eyes and a centred logo on white.",
            () => Hero(QrHeroPreset.Brand),
            () => SampleMark(160, R(80, 32, 42), R(255, 248, 246))),

        new("Soft Diamond", "Soft diamond modules in warm rings on a cream panel and an apricot canvas.",
            () => {
                var options = Base(QrModuleShape.SoftDiamond, R(150, 40, 20), R(255, 246, 234),
                    palette: Palette(QrPaletteMode.Rings, R(150, 40, 20), R(124, 56, 6), R(138, 22, 74)));
                options.ModuleScale = 1;
                options.Eyes = new QrEyeOptions { OuterShape = QrModuleShape.Rounded, InnerShape = QrModuleShape.Circle,
                    OuterColor = R(120, 30, 16), InnerColor = R(150, 40, 20) };
                options.Canvas = GradientCanvas(R(255, 214, 170), R(246, 150, 112), paddingPx: 44);
                return options;
            }),

        new("Wi-Fi Card", "Connected squircle body in a blue card with a caption band.",
            () => {
                var options = Base(QrModuleShape.ConnectedSquircle, R(18, 52, 120), Rgba32.White);
                options.Canvas = new QrCanvasOptions {
                    Template = QrCanvasTemplate.RoundedCardHeader,
                    Background = R(36, 99, 235),
                    Caption = new QrCanvasCaptionOptions { Text = "Join the Wi-Fi", Weight = QrCanvasCaptionWeight.Bold },
                };
                return options;
            }),

        new("Viewfinder", "Extra-rounded crimson body framed by viewfinder corners and a caption.",
            () => {
                var options = Base(QrModuleShape.ExtraRounded, R(168, 18, 52), Rgba32.White);
                options.Canvas = new QrCanvasOptions {
                    Template = QrCanvasTemplate.CornerViewfinder,
                    StrokeColor = R(168, 18, 52),
                    Caption = new QrCanvasCaptionOptions { Text = "Point your camera", Weight = QrCanvasCaptionWeight.Bold },
                };
                return options;
            }),

        new("Dot Matrix", "Navy dots with rounded eyes on pale blue.",
            () => {
                var options = Hero(QrHeroPreset.DotMatrix);
                options.Background = R(232, 240, 253);
                return options;
            }),

        new("Mint Logo", "Squircle modules in two greens with a logo, on mint with a green border.",
            () => {
                var options = Base(QrModuleShape.Squircle, R(0, 112, 86), R(234, 251, 243),
                    palette: Palette(QrPaletteMode.Checker, R(0, 112, 86), R(0, 84, 104)));
                options.Eyes = new QrEyeOptions { OuterShape = QrModuleShape.Squircle, InnerShape = QrModuleShape.Squircle,
                    OuterColor = R(0, 112, 86), InnerColor = R(0, 84, 104) };
                options.Canvas = new QrCanvasOptions { PaddingPx = 28, CornerRadiusPx = 28, Background = R(234, 251, 243),
                    BorderPx = 6, BorderColor = R(0, 112, 86) };
                return options;
            },
            () => SampleMark(160, R(0, 112, 86), R(234, 251, 243))),
    };

    internal static QrEncodingOptions Encoding() => new() { ErrorCorrectionLevel = QrErrorCorrectionLevel.H };

    /// <summary>Renders a look exactly as the website ships it.</summary>
    internal static byte[] RenderPng(Look look) {
        var options = look.CreateOptions();
        if (look.CreateLogo is null) return QR.Render(Payload, OutputFormat.Png, options, Encoding()).Data.ToArray();
        // RenderLogo places and validates the logo, and throws instead of silently dropping it.
        return QrArt.RenderLogo(Payload, look.CreateLogo(), new QrLogoRenderOptions { Appearance = options }, Encoding()).GetPng();
    }

    private static QrRenderOptions Hero(QrHeroPreset preset, bool darkPanel = false) {
        var options = QrArt.Hero(preset, darkPanel);
        options.TargetSizePx = TargetSizePx;
        return options;
    }

    private static QrRenderOptions Theme(QrArtTheme theme, QrArtVariant variant) => new() {
        TargetSizePx = TargetSizePx,
        Art = QrArt.Theme(theme, variant),
    };

    private static QrRenderOptions Base(QrModuleShape shape, Rgba32 foreground, Rgba32 background, QrPaletteOptions? palette = null) => new() {
        TargetSizePx = TargetSizePx,
        QuietZone = 4,
        ModuleShape = shape,
        Foreground = foreground,
        Background = background,
        ForegroundPalette = palette,
        Eyes = new QrEyeOptions { OuterShape = QrModuleShape.Rounded, InnerShape = QrModuleShape.Circle },
    };

    private static QrPaletteOptions Palette(QrPaletteMode mode, params Rgba32[] colors) => new() {
        Mode = mode, Seed = 7, RingSize = 2, ApplyToEyes = false, Colors = colors,
    };

    private static QrCanvasOptions GradientCanvas(Rgba32 start, Rgba32 end, int paddingPx) => new() {
        PaddingPx = paddingPx,
        CornerRadiusPx = 28,
        QrCornerRadiusPx = 16,
        BackgroundGradient = new QrGradientOptions { Type = QrGradientType.DiagonalDown, StartColor = start, EndColor = end },
    };

    // A neutral sample mark: a rounded tile carrying an open "C". Each pixel averages 4x4 samples.
    private static byte[] SampleMark(int size, Rgba32 tile, Rgba32 mark) {
        var rgba = new byte[size * size * 4];
        for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++) {
                int tileHits = 0, markHits = 0;
                for (var sy = 0; sy < 4; sy++)
                    for (var sx = 0; sx < 4; sx++) {
                        var u = (x + (sx + .5) / 4) / size - .5;
                        var v = (y + (sy + .5) / 4) / size - .5;
                        if (!InRoundedSquare(u, v, .5, .23)) continue;
                        tileHits++;
                        var r = Math.Sqrt(u * u + v * v);
                        var opening = u > 0 && Math.Abs(v) < u * .9;
                        var arc = r <= .29 && r >= .165 && !opening;
                        var terminal = InRoundedSquare(u - .2275, v, .0625, .018);
                        if (arc || terminal) markHits++;
                    }
                if (tileHits == 0) continue;
                var p = (y * size + x) * 4;
                var t = markHits / (double)tileHits;
                rgba[p] = (byte)Math.Round(tile.R + (mark.R - tile.R) * t);
                rgba[p + 1] = (byte)Math.Round(tile.G + (mark.G - tile.G) * t);
                rgba[p + 2] = (byte)Math.Round(tile.B + (mark.B - tile.B) * t);
                rgba[p + 3] = (byte)((tileHits * 255 + 8) / 16);
            }
        return PngImageEncoder.EncodeRgba32(rgba, size, size);

        static bool InRoundedSquare(double u, double v, double half, double radius) {
            var dx = Math.Abs(u) - (half - radius);
            var dy = Math.Abs(v) - (half - radius);
            if (dx <= 0 || dy <= 0) return Math.Abs(u) <= half && Math.Abs(v) <= half;
            return dx * dx + dy * dy <= radius * radius;
        }
    }

    private static Rgba32 R(byte r, byte g, byte b) => new(r, g, b);

    private static string Slugify(string value) {
        var buffer = new char[value.Length];
        var length = 0;
        foreach (var ch in value) {
            if (ch >= 'A' && ch <= 'Z') buffer[length++] = (char)(ch + 32);
            else if ((ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9')) buffer[length++] = ch;
            else if (ch is ' ' or '-' or '_' or '.' && length > 0 && buffer[length - 1] != '-') buffer[length++] = '-';
        }
        return length == 0 ? "style" : new string(buffer, 0, length).TrimEnd('-');
    }
}
