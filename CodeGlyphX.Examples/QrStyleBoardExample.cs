using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using CodeGlyphX;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Examples;

internal static class QrStyleBoardExample {
    private const int StyleBoardTargetSizePx = 384;
    private const string StyleDocsBase = "https://codeglyphx.com/docs/qr?style=";

    public static void Run(string outputDir) {
        var dir = Path.Combine(outputDir, "qr-style-board");
        Directory.CreateDirectory(dir);

        var presets = BuildPresets();
        var manifestEntries = new List<StyleBoardEntry>(presets.Count);

        for (var i = 0; i < presets.Count; i++) {
            var preset = presets[i];
            var fileName = $"{Slugify(preset.Name)}.png";
            var path = Path.Combine(dir, fileName);

            var options = preset.CreateOptions();
            if (preset.LogoPng is not null) {
                options.LogoPng = preset.LogoPng;
            }

            QR.Save(preset.Payload, path, options);

            manifestEntries.Add(new StyleBoardEntry(preset.Name, fileName, preset.Payload));
        }

        var manifestJson = JsonSerializer.Serialize(manifestEntries, new JsonSerializerOptions {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        manifestJson.WriteText(Path.Combine(dir, "style-board.json"));
        WriteHtmlIndex(dir, presets);
    }

    private static List<StylePreset> BuildPresets() {
        var logoWarm = LogoBuilder.CreateCirclePng(52, R(18, 25, 45), R(255, 190, 70), out _, out _);
        var logoCool = LogoBuilder.CreateCirclePng(52, R(12, 42, 96), R(90, 220, 255), out _, out _);

        return new List<StylePreset> {
            new("Neon Dot", StyleDocs("neon-dot"), () => BaseSticker(
                fg: R(0, 255, 213),
                palette: Palette(QrPngPaletteMode.Random, 14001, R(0, 255, 213), R(255, 59, 255), R(255, 214, 0)),
                shape: QrPngModuleShape.Dot,
                eyes: Eye(QrPngEyeFrameStyle.Target, R(0, 255, 213), R(255, 59, 255)),
                canvas: CanvasGradient(R(18, 18, 28), R(48, 23, 72)))),

            new("Neon Glow", StyleDocs("neon-glow"), () => BaseSticker(
                fg: R(0, 255, 240),
                palette: Palette(QrPngPaletteMode.Cycle, 0, R(0, 255, 240), R(0, 170, 255), R(255, 92, 255)),
                shape: QrPngModuleShape.Dot,
                eyes: EyeGlow(R(0, 255, 240), R(255, 92, 255), R(0, 200, 255, 200)),
                scaleMap: ScaleMap(QrPngModuleScaleMode.Radial, 0.82, 1.0),
                canvas: CanvasGradient(R(8, 10, 28), R(28, 18, 64)))),

            new("Liquid Glass", StyleDocs("liquid-glass"), () => BaseSticker(
                fg: R(120, 210, 255),
                palette: Palette(QrPngPaletteMode.Cycle, 0, R(120, 210, 255), R(160, 160, 255), R(210, 170, 255)),
                shape: QrPngModuleShape.ConnectedRounded,
                eyes: EyeGlow(R(120, 210, 255), R(210, 170, 255), R(120, 210, 255, 190)),
                scaleMap: ScaleMap(QrPngModuleScaleMode.Radial, 0.86, 1.0),
                canvas: CanvasGradient(R(14, 18, 46), R(32, 26, 82)))),

            new("Card Frame", StyleDocs("card-frame"), () => BaseSticker(
                fg: R(24, 48, 120),
                palette: Palette(QrPngPaletteMode.Cycle, 0, R(24, 48, 120), R(52, 102, 220), R(110, 180, 255)),
                shape: QrPngModuleShape.ConnectedRounded,
                eyes: Eye(QrPngEyeFrameStyle.Badge, R(24, 48, 120), R(110, 180, 255)),
                scaleMap: ScaleMap(QrPngModuleScaleMode.Radial, 0.9, 1.0, 26026),
                canvas: CanvasCardFrame(
                    start: R(245, 248, 255),
                    end: R(224, 235, 255),
                    frameColor: R(28, 64, 180, 220),
                    innerFrameColor: R(120, 190, 255, 210)))),

            new("Sticker Frame", StyleDocs("sticker-frame"), () => BaseSticker(
                fg: R(18, 36, 66),
                palette: Palette(QrPngPaletteMode.Cycle, 0, R(18, 36, 66), R(0, 150, 136), R(255, 111, 97)),
                shape: QrPngModuleShape.ConnectedSquircle,
                eyes: EyeGlow(R(18, 36, 66), R(0, 150, 136), R(255, 111, 97, 180)),
                scaleMap: ScaleMap(QrPngModuleScaleMode.Radial, 0.9, 1.0, 27027),
                canvas: CanvasStickerFrame(
                    background: R(250, 252, 255),
                    frameColor: R(18, 36, 66, 220),
                    accentColor: R(0, 150, 136, 220)))),

            new("Candy Checker", StyleDocs("candy-checker"), () => BaseSticker(
                fg: R(255, 107, 107),
                palette: Palette(QrPngPaletteMode.Checker, 0, R(255, 107, 107), R(255, 217, 61)),
                shape: QrPngModuleShape.Rounded,
                eyes: Eye(QrPngEyeFrameStyle.Badge, R(255, 107, 107), R(255, 217, 61)),
                canvas: CanvasPattern(R(255, 248, 240), Pattern(QrPngBackgroundPatternType.Dots, R(255, 107, 107, 28))))),

            new("Pastel Rings", StyleDocs("pastel-rings"), () => BaseSticker(
                fg: R(121, 134, 255),
                palette: Palette(QrPngPaletteMode.Rings, 0, R(121, 134, 255), R(255, 174, 206), R(144, 226, 196)),
                shape: QrPngModuleShape.Squircle,
                eyes: Eye(QrPngEyeFrameStyle.DoubleRing, R(121, 134, 255), R(255, 174, 206)),
                scaleMap: ScaleMap(QrPngModuleScaleMode.Radial, 0.65, 1.0),
                canvas: CanvasPattern(R(245, 248, 255), Pattern(QrPngBackgroundPatternType.Checker, R(121, 134, 255, 20))))),

            new("Inset Rings", StyleDocs("inset-rings"), () => BaseSticker(
                fg: R(96, 120, 255),
                palette: Palette(QrPngPaletteMode.Rings, 0, R(96, 120, 255), R(140, 110, 255), R(120, 210, 255)),
                shape: QrPngModuleShape.Squircle,
                eyes: EyeInsetRing(R(96, 120, 255), R(120, 210, 255)),
                scaleMap: ScaleMap(QrPngModuleScaleMode.Radial, 0.78, 1.0),
                canvas: CanvasGradient(R(18, 20, 60), R(36, 28, 96)))),

            new("Ocean Grid", StyleDocs("ocean-grid"), () => BaseSticker(
                fg: R(0, 133, 255),
                palette: Palette(QrPngPaletteMode.Cycle, 0, R(0, 133, 255), R(0, 201, 255), R(0, 255, 196)),
                shape: QrPngModuleShape.DotGrid,
                eyes: Eye(QrPngEyeFrameStyle.Single, R(0, 133, 255), R(0, 201, 255)),
                canvas: CanvasPattern(R(10, 24, 45), Pattern(QrPngBackgroundPatternType.Grid, R(0, 133, 255, 24))))),

            new("Stripe Frame", StyleDocs("stripe-frame"), () => BaseSticker(
                fg: R(24, 84, 220),
                palette: Palette(QrPngPaletteMode.Cycle, 0, R(24, 84, 220), R(40, 176, 255), R(120, 90, 255)),
                shape: QrPngModuleShape.ConnectedRounded,
                eyes: EyeGlow(R(24, 84, 220), R(120, 90, 255), R(40, 176, 255, 170)),
                scaleMap: ScaleMap(QrPngModuleScaleMode.Radial, 0.86, 1.0, 2222),
                pattern: Speckle(R(255, 255, 255, 58), seed: 2323, sizePx: 7, thicknessPx: 2, variation: 0.82, density: 0.92),
                canvas: CanvasPattern(
                    R(246, 248, 255),
                    Pattern(QrPngBackgroundPatternType.DiagonalStripes, R(24, 84, 220, 24), sizePx: 16, thicknessPx: 2, moduleStep: 3)))),

            new("Crosshatch Pop", StyleDocs("crosshatch-pop"), () => BaseSticker(
                fg: R(16, 22, 38),
                palette: Palette(QrPngPaletteMode.Cycle, 0, R(16, 22, 38), R(0, 173, 181), R(255, 93, 143)),
                shape: QrPngModuleShape.Rounded,
                eyes: Eye(QrPngEyeFrameStyle.DoubleRing, R(16, 22, 38), R(0, 173, 181)),
                scaleMap: ScaleMap(QrPngModuleScaleMode.Rings, 0.72, 1.0, 2424),
                canvas: CanvasPattern(
                    R(255, 255, 255),
                    Pattern(QrPngBackgroundPatternType.Crosshatch, R(16, 22, 38, 18), sizePx: 18, thicknessPx: 2, moduleStep: 3)))),

            new("Mono Badge", StyleDocs("mono-badge"), () => BaseSticker(
                fg: R(0, 0, 0),
                palette: null,
                shape: QrPngModuleShape.Square,
                eyes: Eye(QrPngEyeFrameStyle.Badge, R(0, 0, 0), R(0, 0, 0)),
                canvas: CanvasBorder(R(255, 255, 255), R(0, 0, 0)))),

            new("Bracket Tech", StyleDocs("bracket-tech"), () => BaseSticker(
                fg: R(24, 230, 145),
                palette: Palette(QrPngPaletteMode.Random, 9001, R(24, 230, 145), R(52, 147, 255), R(255, 255, 255)),
                shape: QrPngModuleShape.Diamond,
                eyes: Eye(QrPngEyeFrameStyle.Bracket, R(24, 230, 145), R(52, 147, 255)),
                scaleMap: ScaleMap(QrPngModuleScaleMode.Rings, 0.7, 1.0),
                canvas: CanvasGradient(R(7, 21, 28), R(9, 42, 54)))),

            new("Cut Corner Tech", StyleDocs("cut-corner-tech"), () => BaseSticker(
                fg: R(80, 220, 255),
                palette: Palette(QrPngPaletteMode.Random, 5120, R(80, 220, 255), R(100, 140, 255), R(220, 120, 255)),
                shape: QrPngModuleShape.Rounded,
                eyes: EyeCutCorner(R(80, 220, 255), R(220, 120, 255)),
                scaleMap: ScaleMap(QrPngModuleScaleMode.Radial, 0.78, 1.0),
                canvas: CanvasGradient(R(10, 18, 40), R(22, 38, 72)))),

            new("Sunset Sticker", StyleDocs("sunset-sticker"), () => BaseSticker(
                fg: R(255, 93, 93),
                palette: Palette(QrPngPaletteMode.Cycle, 0, R(255, 93, 93), R(255, 180, 60), R(255, 76, 193)),
                shape: QrPngModuleShape.Rounded,
                eyes: Eye(QrPngEyeFrameStyle.Target, R(255, 93, 93), R(255, 180, 60)),
                canvas: CanvasGradient(R(35, 9, 25), R(83, 22, 52)),
                logo: logoWarm), logoWarm),

            new("Aurora", StyleDocs("aurora"), () => BaseSticker(
                fg: R(110, 255, 200),
                palette: Palette(QrPngPaletteMode.Random, 4512, R(110, 255, 200), R(130, 195, 255), R(226, 170, 255)),
                shape: QrPngModuleShape.Circle,
                eyes: Eye(QrPngEyeFrameStyle.DoubleRing, R(110, 255, 200), R(130, 195, 255)),
                scaleMap: ScaleMap(QrPngModuleScaleMode.Random, 0.6, 1.0, 9876),
                canvas: CanvasPattern(R(15, 18, 34), Pattern(QrPngBackgroundPatternType.Dots, R(110, 255, 200, 22))))),

            new("Speckle Splash", StyleDocs("speckle-splash"), () => BaseSticker(
                fg: R(0, 160, 200),
                palette: Palette(QrPngPaletteMode.Cycle, 0, R(0, 160, 200), R(0, 210, 140), R(220, 120, 255)),
                shape: QrPngModuleShape.ConnectedRounded,
                eyes: new QrPngEyeOptions {
                    UseFrame = true,
                    FrameStyle = QrPngEyeFrameStyle.Target,
                    OuterShape = QrPngModuleShape.Rounded,
                    InnerShape = QrPngModuleShape.Circle,
                    OuterCornerRadiusPx = 7,
                    InnerCornerRadiusPx = 4,
                    OuterGradients = new[] {
                        new QrPngGradientOptions { Type = QrPngGradientType.Radial, StartColor = R(0, 220, 255), EndColor = R(0, 120, 200) },
                        new QrPngGradientOptions { Type = QrPngGradientType.Radial, StartColor = R(255, 120, 200), EndColor = R(210, 70, 160) },
                        new QrPngGradientOptions { Type = QrPngGradientType.Radial, StartColor = R(0, 230, 150), EndColor = R(0, 150, 120) },
                    },
                    InnerColors = new[] { R(255, 255, 255), R(255, 255, 255), R(255, 255, 255) },
                    SparkleCount = 18,
                    SparkleRadiusPx = 3,
                    SparkleSpreadPx = 26,
                    SparkleSeed = 1337,
                    SparkleColor = R(255, 255, 255, 180),
                },
                scaleMap: ScaleMap(QrPngModuleScaleMode.Radial, 0.8, 1.0, 31415),
                pattern: Speckle(R(0, 80, 120, 96), seed: 4242, sizePx: 7, thicknessPx: 2, variation: 0.84, density: 0.94),
                canvas: CanvasGradient(R(8, 20, 36), R(16, 44, 72)))),

            new("Edge Drips", StyleDocs("edge-drips"), () => BaseSticker(
                fg: R(22, 40, 92),
                palette: Palette(QrPngPaletteMode.Cycle, 0, R(22, 40, 92), R(24, 138, 216), R(0, 190, 150)),
                shape: QrPngModuleShape.ConnectedRounded,
                eyes: EyeGlow(R(22, 40, 92), R(255, 255, 255), R(40, 150, 230, 170)),
                scaleMap: ScaleMap(QrPngModuleScaleMode.Radial, 0.86, 1.0, 8088),
                pattern: Speckle(R(255, 255, 255, 54), seed: 8181, sizePx: 7, thicknessPx: 2, variation: 0.78, density: 0.88),
                canvas: CanvasEdgeSplash(
                    R(236, 244, 255),
                    R(214, 230, 255),
                    splashSeed: 9090,
                    edgeBandPx: 104,
                    splashColors: new[] {
                        R(30, 144, 255, 140),
                        R(0, 190, 150, 132),
                        R(255, 120, 210, 128),
                        R(255, 170, 60, 132),
                    }))),

            new("Halftone Bloom", StyleDocs("halftone-bloom"), () => BaseSticker(
                fg: R(92, 90, 255),
                palette: Palette(QrPngPaletteMode.Cycle, 0, R(92, 90, 255), R(190, 120, 255), R(120, 210, 255)),
                shape: QrPngModuleShape.ConnectedRounded,
                eyes: EyeGlow(R(92, 90, 255), R(190, 120, 255), R(120, 210, 255, 170)),
                scaleMap: ScaleMap(QrPngModuleScaleMode.Radial, 0.82, 1.0, 2468),
                pattern: Halftone(R(255, 255, 255, 92), seed: 8080, sizePx: 10, thicknessPx: 3, variation: 0.96, density: 0.97),
                canvas: CanvasGradient(R(18, 16, 60), R(46, 26, 98)))),

            new("Vignette Noir", StyleDocs("vignette-noir"), () => BaseSticker(
                fg: R(28, 34, 54),
                palette: Palette(QrPngPaletteMode.Cycle, 0, R(28, 34, 54), R(60, 74, 108), R(120, 150, 220)),
                shape: QrPngModuleShape.ConnectedRounded,
                eyes: EyeGlow(R(28, 34, 54), R(120, 150, 220), R(40, 70, 120, 160)),
                scaleMap: ScaleMap(QrPngModuleScaleMode.Radial, 0.84, 1.0, 7341),
                pattern: Speckle(R(255, 255, 255, 64), seed: 6006, sizePx: 7, thicknessPx: 2, variation: 0.8, density: 0.9),
                canvas: CanvasGradientVignette(R(242, 246, 255), R(210, 220, 244), R(8, 12, 26, 120), bandPx: 88, strength: 1.0))),

            new("Grain Paper", StyleDocs("grain-paper"), () => BaseSticker(
                fg: R(52, 58, 74),
                palette: Palette(QrPngPaletteMode.Cycle, 0, R(52, 58, 74), R(92, 100, 128), R(160, 170, 210)),
                shape: QrPngModuleShape.ConnectedRounded,
                eyes: EyeGlow(R(52, 58, 74), R(160, 170, 210), R(80, 90, 130, 150)),
                scaleMap: ScaleMap(QrPngModuleScaleMode.Radial, 0.86, 1.0, 9191),
                pattern: Speckle(R(255, 255, 255, 58), seed: 7272, sizePx: 7, thicknessPx: 2, variation: 0.78, density: 0.9),
                canvas: CanvasGradientTexture(
                    R(246, 244, 236),
                    R(230, 226, 214),
                    grainColor: R(60, 52, 44, 70),
                    grainDensity: 0.28,
                    grainPixelSize: 2,
                    grainSeed: 13371337,
                    vignetteColor: R(28, 24, 18, 90),
                    vignetteBandPx: 96,
                    vignetteStrength: 1.0))),

            new("Eye Accents", StyleDocs("eye-accents"), () => BaseSticker(
                fg: R(30, 190, 255),
                palette: Palette(QrPngPaletteMode.Cycle, 0, R(30, 190, 255), R(90, 240, 200), R(255, 130, 210)),
                shape: QrPngModuleShape.ConnectedRounded,
                eyes: new QrPngEyeOptions {
                    UseFrame = true,
                    FrameStyle = QrPngEyeFrameStyle.Target,
                    OuterShape = QrPngModuleShape.Rounded,
                    InnerShape = QrPngModuleShape.Circle,
                    OuterCornerRadiusPx = 7,
                    InnerCornerRadiusPx = 4,
                    OuterGradients = new[] {
                        new QrPngGradientOptions { Type = QrPngGradientType.Radial, StartColor = R(30, 220, 255), EndColor = R(0, 140, 210) },
                        new QrPngGradientOptions { Type = QrPngGradientType.Radial, StartColor = R(255, 150, 220), EndColor = R(210, 80, 160) },
                        new QrPngGradientOptions { Type = QrPngGradientType.Radial, StartColor = R(90, 255, 210), EndColor = R(20, 180, 140) },
                    },
                    InnerColors = new[] { R(255, 255, 255), R(255, 255, 255), R(255, 255, 255) },
                    AccentRingCount = 7,
                    AccentRingThicknessPx = 6,
                    AccentRingSpreadPx = 44,
                    AccentRingJitterPx = 6,
                    AccentRingSeed = 91025,
                    AccentRingColor = R(255, 255, 255, 140),
                    SparkleCount = 10,
                    SparkleRadiusPx = 3,
                    SparkleSpreadPx = 24,
                    SparkleSeed = 424242,
                    SparkleColor = R(255, 255, 255, 170),
                },
                scaleMap: ScaleMap(QrPngModuleScaleMode.Radial, 0.82, 1.0, 1122),
                pattern: Speckle(R(0, 80, 120, 90), seed: 5151, sizePx: 7, thicknessPx: 2, variation: 0.82, density: 0.93),
                canvas: CanvasGradient(R(10, 24, 44), R(22, 54, 86)))),

            new("Eye Rays", StyleDocs("eye-rays"), () => BaseSticker(
                fg: R(40, 200, 255),
                palette: Palette(QrPngPaletteMode.Cycle, 0, R(40, 200, 255), R(120, 255, 210), R(255, 150, 220)),
                shape: QrPngModuleShape.ConnectedRounded,
                eyes: new QrPngEyeOptions {
                    UseFrame = true,
                    FrameStyle = QrPngEyeFrameStyle.Target,
                    OuterShape = QrPngModuleShape.Rounded,
                    InnerShape = QrPngModuleShape.Circle,
                    OuterCornerRadiusPx = 7,
                    InnerCornerRadiusPx = 4,
                    OuterGradients = new[] {
                        new QrPngGradientOptions { Type = QrPngGradientType.Radial, StartColor = R(40, 230, 255), EndColor = R(0, 150, 210) },
                        new QrPngGradientOptions { Type = QrPngGradientType.Radial, StartColor = R(255, 170, 230), EndColor = R(210, 90, 170) },
                        new QrPngGradientOptions { Type = QrPngGradientType.Radial, StartColor = R(120, 255, 220), EndColor = R(30, 190, 150) },
                    },
                    InnerColors = new[] { R(255, 255, 255), R(255, 255, 255), R(255, 255, 255) },
                    AccentRayCount = 12,
                    AccentRayLengthPx = 58,
                    AccentRayThicknessPx = 6,
                    AccentRaySpreadPx = 48,
                    AccentRayJitterPx = 8,
                    AccentRayLengthJitterPx = 14,
                    AccentRaySeed = 8675309,
                    AccentRayColor = R(255, 255, 255, 145),
                    SparkleCount = 8,
                    SparkleRadiusPx = 3,
                    SparkleSpreadPx = 24,
                    SparkleSeed = 30303,
                    SparkleColor = R(255, 255, 255, 175),
                },
                scaleMap: ScaleMap(QrPngModuleScaleMode.Radial, 0.84, 1.0, 5533),
                pattern: Halftone(R(255, 255, 255, 86), seed: 1900, sizePx: 10, thicknessPx: 3, variation: 0.94, density: 0.96),
                canvas: CanvasGradient(R(8, 22, 40), R(20, 56, 88)))),

            new("Eye Stripes", StyleDocs("eye-stripes"), () => BaseSticker(
                fg: R(34, 96, 220),
                palette: Palette(QrPngPaletteMode.Cycle, 0, R(34, 96, 220), R(70, 182, 255), R(170, 120, 255)),
                shape: QrPngModuleShape.ConnectedRounded,
                eyes: EyeAccentStripes(R(34, 96, 220), R(255, 255, 255), R(255, 255, 255, 152), seed: 60606),
                scaleMap: ScaleMap(QrPngModuleScaleMode.Radial, 0.84, 1.0, 6060),
                pattern: Speckle(R(255, 255, 255, 58), seed: 6161, sizePx: 7, thicknessPx: 2, variation: 0.8, density: 0.9),
                canvas: CanvasGradientTexture(
                    R(238, 244, 255),
                    R(218, 228, 255),
                    grainColor: R(40, 60, 120, 66),
                    grainDensity: 0.26,
                    grainPixelSize: 2,
                    grainSeed: 606060,
                    vignetteColor: R(10, 20, 60, 80),
                    vignetteBandPx: 92,
                    vignetteStrength: 1.0))),

            new("Mint Board", StyleDocs("mint-board"), () => BaseSticker(
                fg: R(0, 156, 121),
                palette: Palette(QrPngPaletteMode.Checker, 0, R(0, 156, 121), R(99, 224, 181)),
                shape: QrPngModuleShape.Squircle,
                eyes: Eye(QrPngEyeFrameStyle.Single, R(0, 156, 121), R(99, 224, 181)),
                canvas: CanvasBorder(R(238, 255, 248), R(0, 156, 121)))),

            new("Deep Space", StyleDocs("deep-space"), () => BaseSticker(
                fg: R(165, 100, 255),
                palette: Palette(QrPngPaletteMode.Rings, 0, R(165, 100, 255), R(255, 119, 198), R(124, 255, 232)),
                shape: QrPngModuleShape.Dot,
                eyes: Eye(QrPngEyeFrameStyle.Target, R(165, 100, 255), R(255, 119, 198)),
                canvas: CanvasGradient(R(9, 9, 20), R(22, 12, 40)),
                logo: logoCool), logoCool),

            new("Leaf Bloom", StyleDocs("leaf-bloom"), () => BaseSticker(
                fg: R(64, 196, 155),
                palette: Palette(QrPngPaletteMode.Cycle, 0, R(64, 196, 155), R(140, 255, 214), R(255, 198, 125)),
                shape: QrPngModuleShape.Leaf,
                eyes: Eye(QrPngEyeFrameStyle.DoubleRing, R(64, 196, 155), R(255, 198, 125)),
                canvas: CanvasGradient(R(8, 26, 22), R(12, 52, 44)))),

            new("Wave Pulse", StyleDocs("wave-pulse"), () => BaseSticker(
                fg: R(88, 140, 255),
                palette: Palette(QrPngPaletteMode.Random, 7421, R(88, 140, 255), R(46, 244, 255), R(255, 255, 255)),
                shape: QrPngModuleShape.Wave,
                eyes: Eye(QrPngEyeFrameStyle.Target, R(88, 140, 255), R(46, 244, 255)),
                canvas: CanvasPattern(R(12, 16, 32), Pattern(QrPngBackgroundPatternType.Dots, R(88, 140, 255, 22))))),

            new("Ink Blob", StyleDocs("ink-blob"), () => BaseSticker(
                fg: R(30, 30, 30),
                palette: Palette(QrPngPaletteMode.Random, 2024, R(30, 30, 30), R(80, 80, 80), R(200, 200, 200)),
                shape: QrPngModuleShape.Blob,
                eyes: Eye(QrPngEyeFrameStyle.Badge, R(30, 30, 30), R(80, 80, 80)),
                canvas: CanvasBorder(R(248, 248, 248), R(30, 30, 30)))),

            new("Soft Diamond", StyleDocs("soft-diamond"), () => BaseSticker(
                fg: R(255, 128, 74),
                palette: Palette(QrPngPaletteMode.Rings, 0, R(255, 128, 74), R(255, 214, 122), R(255, 94, 128)),
                shape: QrPngModuleShape.SoftDiamond,
                eyes: Eye(QrPngEyeFrameStyle.DoubleRing, R(255, 128, 74), R(255, 94, 128)),
                canvas: CanvasGradient(R(40, 16, 10), R(72, 26, 14)))),

            new("Sticker Grid", StyleDocs("sticker-grid"), () => BaseSticker(
                fg: R(30, 30, 30),
                palette: Palette(QrPngPaletteMode.Cycle, 0, R(30, 30, 30), R(80, 80, 80)),
                shape: QrPngModuleShape.Square,
                eyes: Eye(QrPngEyeFrameStyle.Badge, R(30, 30, 30), R(80, 80, 80)),
                canvas: CanvasPattern(R(255, 255, 255), Pattern(QrPngBackgroundPatternType.Grid, R(30, 30, 30, 18))))),

            new("Connected Melt", StyleDocs("connected-melt"), () => BaseSticker(
                fg: R(88, 120, 255),
                palette: Palette(QrPngPaletteMode.Cycle, 0, R(88, 120, 255), R(120, 96, 255), R(88, 210, 255)),
                shape: QrPngModuleShape.ConnectedRounded,
                eyes: Eye(QrPngEyeFrameStyle.Target, R(88, 120, 255), R(88, 210, 255)),
                scaleMap: ScaleMap(QrPngModuleScaleMode.Radial, 0.86, 1.0),
                canvas: CanvasGradient(R(14, 18, 42), R(28, 20, 76)))),

            new("Minimal Mono", StyleDocs("minimal-mono"), () => BaseSticker(
                fg: R(18, 18, 18),
                palette: null,
                shape: QrPngModuleShape.ConnectedRounded,
                eyes: EyeGlow(R(18, 18, 18), R(18, 18, 18), R(120, 120, 120, 150)),
                scaleMap: ScaleMap(QrPngModuleScaleMode.Radial, 0.9, 1.0),
                canvas: CanvasBorder(R(255, 255, 255), R(18, 18, 18)))),

            new("Center Pop", StyleDocs("center-pop"), () => BaseSticker(
                fg: R(35, 54, 89),
                palette: Palette(QrPngPaletteMode.Cycle, 0, R(35, 54, 89), R(60, 90, 140)),
                zones: Zones(
                    Palette(QrPngPaletteMode.Random, 130, R(255, 107, 107), R(255, 217, 61), R(110, 255, 200)), 9,
                    Palette(QrPngPaletteMode.Checker, 0, R(35, 54, 89), R(255, 217, 61)), 5),
                shape: QrPngModuleShape.Rounded,
                eyes: Eye(QrPngEyeFrameStyle.Single, R(35, 54, 89), R(255, 217, 61)),
                canvas: CanvasBorder(R(250, 252, 255), R(35, 54, 89)), logo: logoWarm), logoWarm)
        };
    }

    private readonly record struct StyleBoardEntry(string Name, string File, string Payload);

    private static QrEasyOptions BaseSticker(
        Rgba32 fg,
        QrPngPaletteOptions? palette,
        QrPngModuleShape shape,
        QrPngEyeOptions eyes,
        QrPngCanvasOptions canvas,
        QrPngModuleScaleMapOptions? scaleMap = null,
        QrPngPaletteZoneOptions? zones = null,
        QrPngForegroundPatternOptions? pattern = null,
        byte[]? logo = null) {
        return new QrEasyOptions {
            ErrorCorrectionLevel = QrErrorCorrectionLevel.H,
            // Web-friendly size: keeps assets lightweight while remaining crisp in the grid.
            TargetSizePx = StyleBoardTargetSizePx,
            TargetSizeIncludesQuietZone = true,
            ModuleSize = 10,
            QuietZone = 4,
            Foreground = fg,
            Background = R(255, 255, 255),
            BackgroundSupersample = 2,
            ModuleShape = shape,
            ModuleScale = 0.9,
            ForegroundPalette = palette,
            ForegroundPaletteZones = zones,
            ModuleScaleMap = scaleMap,
            ForegroundPattern = pattern,
            Eyes = eyes,
            Canvas = canvas,
            LogoPng = logo,
            LogoScale = 0.2,
            LogoPaddingPx = 6,
        };
    }

    private static QrPngEyeOptions Eye(QrPngEyeFrameStyle style, Rgba32 outer, Rgba32 inner) {
        return new QrPngEyeOptions {
            UseFrame = true,
            FrameStyle = style,
            OuterShape = QrPngModuleShape.Rounded,
            InnerShape = QrPngModuleShape.Circle,
            OuterColor = outer,
            InnerColor = inner,
            OuterCornerRadiusPx = 6,
            InnerCornerRadiusPx = 4,
        };
    }

    private static QrPngEyeOptions EyeGlow(Rgba32 outer, Rgba32 inner, Rgba32? glow = null) {
        var eyes = Eye(QrPngEyeFrameStyle.Glow, outer, inner);
        eyes.GlowRadiusPx = 28;
        eyes.GlowAlpha = 125;
        eyes.GlowColor = glow ?? outer;
        return eyes;
    }

    private static QrPngEyeOptions EyeAccentStripes(Rgba32 outer, Rgba32 inner, Rgba32 stripeColor, int seed) {
        var eyes = Eye(QrPngEyeFrameStyle.Target, outer, inner);
        eyes.OuterCornerRadiusPx = 7;
        eyes.InnerCornerRadiusPx = 4;
        eyes.AccentStripeCount = 26;
        eyes.AccentStripeLengthPx = 28;
        eyes.AccentStripeThicknessPx = 4;
        eyes.AccentStripeSpreadPx = 36;
        eyes.AccentStripeJitterPx = 6;
        eyes.AccentStripeLengthJitterPx = 8;
        eyes.AccentStripeSeed = seed;
        eyes.AccentStripeColor = stripeColor;
        eyes.SparkleCount = 8;
        eyes.SparkleRadiusPx = 3;
        eyes.SparkleSpreadPx = 24;
        eyes.SparkleSeed = seed ^ 0x55aa55aa;
        eyes.SparkleColor = R(255, 255, 255, 176);
        return eyes;
    }

    private static QrPngEyeOptions EyeInsetRing(Rgba32 outer, Rgba32 inner) {
        var eyes = Eye(QrPngEyeFrameStyle.InsetRing, outer, inner);
        eyes.InnerScale = 0.92;
        return eyes;
    }

    private static QrPngEyeOptions EyeCutCorner(Rgba32 outer, Rgba32 inner) {
        var eyes = Eye(QrPngEyeFrameStyle.CutCorner, outer, inner);
        eyes.OuterShape = QrPngModuleShape.Square;
        eyes.InnerShape = QrPngModuleShape.Rounded;
        eyes.OuterCornerRadiusPx = 0;
        eyes.InnerCornerRadiusPx = 4;
        return eyes;
    }

    private static QrPngPaletteOptions Palette(QrPngPaletteMode mode, int seed, params Rgba32[] colors) {
        return new QrPngPaletteOptions {
            Mode = mode,
            Seed = seed,
            RingSize = 2,
            ApplyToEyes = false,
            Colors = colors,
        };
    }

    private static QrPngPaletteZoneOptions Zones(QrPngPaletteOptions? center, int centerSize, QrPngPaletteOptions? corners, int cornerSize) {
        return new QrPngPaletteZoneOptions {
            CenterPalette = center,
            CenterSize = centerSize,
            CornerPalette = corners,
            CornerSize = cornerSize,
        };
    }

    private static QrPngModuleScaleMapOptions ScaleMap(QrPngModuleScaleMode mode, double min, double max, int seed = 12345) {
        return new QrPngModuleScaleMapOptions {
            Mode = mode,
            MinScale = min,
            MaxScale = max,
            Seed = seed,
            RingSize = 2,
        };
    }

    private static QrPngForegroundPatternOptions Speckle(Rgba32 color, int seed, int sizePx = 6, int thicknessPx = 1, double variation = 0.78, double density = 0.92, bool applyToEyes = false) {
        return new QrPngForegroundPatternOptions {
            Type = QrPngForegroundPatternType.SpeckleDots,
            Color = color,
            Seed = seed,
            Variation = variation,
            Density = density,
            SizePx = sizePx,
            ThicknessPx = thicknessPx,
            ApplyToModules = true,
            ApplyToEyes = applyToEyes,
        };
    }

    private static QrPngForegroundPatternOptions Halftone(Rgba32 color, int seed, int sizePx = 8, int thicknessPx = 2, double variation = 0.9, double density = 0.95, bool applyToEyes = false) {
        return new QrPngForegroundPatternOptions {
            Type = QrPngForegroundPatternType.HalftoneDots,
            Color = color,
            Seed = seed,
            Variation = variation,
            Density = density,
            SizePx = sizePx,
            ThicknessPx = thicknessPx,
            ApplyToModules = true,
            ApplyToEyes = applyToEyes,
        };
    }

    private static QrPngBackgroundPatternOptions Pattern(
        QrPngBackgroundPatternType type,
        Rgba32 color,
        int sizePx = 14,
        int thicknessPx = 1,
        bool snapToModuleSize = true,
        int moduleStep = 2) {
        return new QrPngBackgroundPatternOptions {
            Type = type,
            Color = color,
            SizePx = sizePx,
            ThicknessPx = thicknessPx,
            SnapToModuleSize = snapToModuleSize,
            ModuleStep = moduleStep,
        };
    }

    private static QrPngCanvasOptions CanvasGradient(Rgba32 start, Rgba32 end) {
        return new QrPngCanvasOptions {
            PaddingPx = 24,
            CornerRadiusPx = 26,
            BackgroundGradient = new QrPngGradientOptions {
                Type = QrPngGradientType.DiagonalDown,
                StartColor = start,
                EndColor = end,
            },
            BorderPx = 2,
            BorderColor = R(255, 255, 255, 40),
            ShadowOffsetX = 6,
            ShadowOffsetY = 8,
            ShadowColor = R(0, 0, 0, 60),
        };
    }

    private static QrPngCanvasOptions CanvasGradientVignette(Rgba32 start, Rgba32 end, Rgba32 vignetteColor, int bandPx = 72, double strength = 1.0) {
        var canvas = CanvasGradient(start, end);
        canvas.Vignette = new QrPngCanvasVignetteOptions {
            Color = vignetteColor,
            BandPx = bandPx,
            Strength = strength,
            ProtectQrArea = true,
        };
        return canvas;
    }

    private static QrPngCanvasOptions CanvasGradientTexture(
        Rgba32 start,
        Rgba32 end,
        Rgba32 grainColor,
        double grainDensity,
        int grainPixelSize,
        int grainSeed,
        Rgba32? vignetteColor = null,
        int vignetteBandPx = 84,
        double vignetteStrength = 1.0) {
        var canvas = CanvasGradient(start, end);
        canvas.Grain = new QrPngCanvasGrainOptions {
            Color = grainColor,
            Density = grainDensity,
            PixelSizePx = grainPixelSize,
            AlphaJitter = 0.6,
            Seed = grainSeed,
            ProtectQrArea = true,
        };
        if (vignetteColor is not null) {
            canvas.Vignette = new QrPngCanvasVignetteOptions {
                Color = vignetteColor.Value,
                BandPx = vignetteBandPx,
                Strength = vignetteStrength,
                ProtectQrArea = true,
            };
        }
        return canvas;
    }

    private static QrPngCanvasOptions CanvasEdgeSplash(
        Rgba32 start,
        Rgba32 end,
        int splashSeed,
        int edgeBandPx,
        Rgba32[] splashColors) {
        var canvas = CanvasGradientTexture(
            start,
            end,
            grainColor: R(16, 24, 60, 66),
            grainDensity: 0.24,
            grainPixelSize: 2,
            grainSeed: splashSeed ^ 0x13579bdf,
            vignetteColor: R(6, 10, 26, 90),
            vignetteBandPx: 104,
            vignetteStrength: 1.0);

        canvas.Splash = new QrPngCanvasSplashOptions {
            Colors = splashColors,
            Count = 18,
            MinRadiusPx = 18,
            MaxRadiusPx = 56,
            SpreadPx = 28,
            Placement = QrPngCanvasSplashPlacement.CanvasEdges,
            EdgeBandPx = edgeBandPx,
            DripChance = 0.6,
            DripLengthPx = 48,
            DripWidthPx = 10,
            Seed = splashSeed,
            ProtectQrArea = true,
        };

        return canvas;
    }

    private static QrPngCanvasOptions CanvasPattern(Rgba32 background, QrPngBackgroundPatternOptions pattern) {
        return new QrPngCanvasOptions {
            PaddingPx = 22,
            CornerRadiusPx = 24,
            Background = background,
            Pattern = pattern,
            BorderPx = 2,
            BorderColor = R(0, 0, 0, 20),
            ShadowOffsetX = 5,
            ShadowOffsetY = 7,
            ShadowColor = R(0, 0, 0, 50),
        };
    }

    private static QrPngCanvasOptions CanvasBorder(Rgba32 background, Rgba32 border) {
        return new QrPngCanvasOptions {
            PaddingPx = 20,
            CornerRadiusPx = 22,
            Background = background,
            BorderPx = 3,
            BorderColor = border,
            ShadowOffsetX = 4,
            ShadowOffsetY = 6,
            ShadowColor = R(0, 0, 0, 45),
        };
    }

    private static QrPngCanvasOptions CanvasCardFrame(Rgba32 start, Rgba32 end, Rgba32 frameColor, Rgba32 innerFrameColor) {
        return new QrPngCanvasOptions {
            PaddingPx = 44,
            CornerRadiusPx = 32,
            BackgroundGradient = new QrPngGradientOptions {
                Type = QrPngGradientType.DiagonalDown,
                StartColor = start,
                EndColor = end,
            },
            BorderPx = 2,
            BorderColor = R(255, 255, 255, 36),
            Frame = new QrPngCanvasFrameOptions {
                ThicknessPx = 16,
                GapPx = 12,
                RadiusPx = 28,
                Color = frameColor,
                InnerThicknessPx = 4,
                InnerGapPx = 4,
                InnerColor = innerFrameColor,
            },
            ShadowOffsetX = 8,
            ShadowOffsetY = 12,
            ShadowColor = R(0, 0, 0, 70),
        };
    }

    private static QrPngCanvasOptions CanvasStickerFrame(Rgba32 background, Rgba32 frameColor, Rgba32 accentColor) {
        return new QrPngCanvasOptions {
            PaddingPx = 42,
            CornerRadiusPx = 36,
            Background = background,
            BorderPx = 6,
            BorderColor = R(255, 255, 255),
            Frame = new QrPngCanvasFrameOptions {
                ThicknessPx = 12,
                GapPx = 10,
                RadiusPx = 30,
                Color = frameColor,
                InnerThicknessPx = 3,
                InnerGapPx = 3,
                InnerColor = accentColor,
            },
            ShadowOffsetX = 10,
            ShadowOffsetY = 14,
            ShadowColor = R(0, 0, 0, 64),
        };
    }

    private static void WriteHtmlIndex(string dir, List<StylePreset> presets) {
        var sb = new StringBuilder();
        sb.AppendLine("<!doctype html>");
        sb.AppendLine("<html lang=\"en\"><head><meta charset=\"utf-8\"/>");
        sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"/>");
        sb.AppendLine("<title>QR Style Board</title>");
        sb.AppendLine("<style>");
        sb.AppendLine("body{font-family:Segoe UI,Arial,sans-serif;background:#0e0e14;color:#e6e6e6;margin:0;padding:24px}");
        sb.AppendLine(".grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(220px,1fr));gap:18px}");
        sb.AppendLine(".card{background:#141521;border-radius:16px;padding:14px;text-align:center;box-shadow:0 12px 28px rgba(0,0,0,.35)}");
        sb.AppendLine(".card img{width:100%;height:auto;border-radius:12px}");
        sb.AppendLine(".label{margin-top:10px;font-weight:600;font-size:14px;letter-spacing:.3px}");
        sb.AppendLine("</style></head><body>");
        sb.AppendLine("<h1>QR Style Board</h1>");
        sb.AppendLine("<div class=\"grid\">");
        for (var i = 0; i < presets.Count; i++) {
            var name = presets[i].Name;
            var file = $"{Slugify(name)}.png";
            sb.Append("<div class=\"card\"><img src=\"").Append(file).Append("\" alt=\"").Append(name).Append("\"/>")
                .Append("<div class=\"label\">").Append(name).Append("</div></div>");
        }
        sb.AppendLine("</div></body></html>");
        sb.ToString().WriteText(Path.Combine(dir, "style-board.html"));
    }

    private static Rgba32 R(byte r, byte g, byte b, byte a = 255) => new(r, g, b, a);

    private static string Slugify(string value) {
        var buffer = new char[value.Length];
        var length = 0;
        for (var i = 0; i < value.Length; i++) {
            var ch = value[i];
            if (ch >= 'A' && ch <= 'Z') {
                buffer[length++] = (char)(ch + 32);
            } else if ((ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9')) {
                buffer[length++] = ch;
            } else if (ch is ' ' or '-' or '_' or '.') {
                buffer[length++] = '-';
            }
        }
        return length == 0 ? "style" : new string(buffer, 0, length);
    }

    private static string StyleDocs(string slug) => $"{StyleDocsBase}{slug}#styling-options";

    private sealed record StylePreset(
        string Name,
        string Payload,
        Func<QrEasyOptions> CreateOptions,
        byte[]? LogoPng = null);
}
