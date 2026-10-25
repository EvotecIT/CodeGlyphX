using System;
using CodeGlyphX.Rendering;

namespace CodeGlyphX;

public static partial class QrArt {
    /// <summary>Creates an independent, editable hero appearance. Dark-panel variants retain
    /// a light QR background and full quiet zone. Brand provides the two-color appearance;
    /// pass it to RenderLogo to place and validate the actual logo without silently omitting it.</summary>
    public static QrRenderOptions Hero(QrHeroPreset preset, bool darkPanel = false) {
        if (preset is < QrHeroPreset.Ink or > QrHeroPreset.Berry) throw new ArgumentOutOfRangeException(nameof(preset));
        var options = new QrRenderOptions {
            ModuleSize = 16, QuietZone = 4, Foreground = new Rgba32(25, 25, 27),
            Background = Rgba32.White, ModuleShape = QrModuleShape.Rounded,
            Eyes = new QrEyeOptions { OuterShape = QrModuleShape.Rounded, InnerShape = QrModuleShape.Circle }
        };
        switch (preset) {
            case QrHeroPreset.Ink:
                options.ModuleShape = QrModuleShape.Liquid;
                options.Foreground = Rgba32.Black;
                options.Eyes.OuterShape = QrModuleShape.Circle;
                break;
            case QrHeroPreset.DotMatrix:
                options.ModuleShape = QrModuleShape.Dot;
                options.Foreground = new Rgba32(24, 61, 128);
                break;
            case QrHeroPreset.Sunset:
                options.ModuleShape = QrModuleShape.ExtraRounded;
                options.Background = new Rgba32(255, 250, 242);
                options.ForegroundGradient = Gradient(QrGradientType.DiagonalDown,
                    new Rgba32(170, 73, 0), new Rgba32(145, 18, 94));
                break;
            case QrHeroPreset.Ocean:
                options.ModuleShape = QrModuleShape.ConnectedSquircle;
                options.Eyes.OuterShape = QrModuleShape.Squircle;
                options.Eyes.InnerShape = QrModuleShape.Squircle;
                options.ForegroundGradient = Gradient(QrGradientType.Radial,
                    new Rgba32(12, 45, 105), new Rgba32(0, 104, 111));
                break;
            case QrHeroPreset.MonoClassy:
                options.ModuleShape = QrModuleShape.Classy;
                options.Foreground = new Rgba32(48, 44, 40);
                options.Background = new Rgba32(248, 244, 236);
                options.Eyes.OuterShape = QrModuleShape.CutCorner;
                options.Eyes.InnerShape = QrModuleShape.Square;
                break;
            case QrHeroPreset.Brand:
                options.ModuleShape = QrModuleShape.ExtraRounded;
                options.ForegroundGradient = Gradient(QrGradientType.DiagonalDown,
                    new Rgba32(36, 38, 42), new Rgba32(174, 49, 33));
                break;
            case QrHeroPreset.Neon:
                options.ModuleShape = QrModuleShape.Liquid;
                options.Foreground = new Rgba32(103, 18, 112);
                options.Canvas = Panel(darkPanel ? new Rgba32(22, 10, 24) : new Rgba32(255, 240, 249), 48);
                options.Canvas.Halo = new QrCanvasHaloOptions { Color = new Rgba32(245, 26, 123, 96), RadiusPx = 32, ProtectQrArea = true };
                break;
            case QrHeroPreset.PaperCutout:
                options.ModuleShape = QrModuleShape.ExtraRounded;
                options.Foreground = new Rgba32(69, 51, 38);
                options.Background = new Rgba32(255, 252, 245);
                options.Canvas = Panel(darkPanel ? new Rgba32(32, 27, 24) : new Rgba32(237, 225, 205), 36);
                options.Canvas.ShadowOffsetX = 10;
                options.Canvas.ShadowOffsetY = 12;
                options.Canvas.ShadowColor = new Rgba32(35, 23, 12, 52);
                options.Canvas.Halo = new QrCanvasHaloOptions { Color = new Rgba32(35, 23, 12, 20), RadiusPx = 16, ProtectQrArea = true };
                break;
            case QrHeroPreset.RetroPixel:
                options.ModuleShape = QrModuleShape.Square;
                options.Eyes.OuterShape = options.Eyes.InnerShape = QrModuleShape.Square;
                options.Foreground = new Rgba32(40, 39, 30);
                options.Background = new Rgba32(255, 242, 204);
                options.Canvas = Panel(darkPanel ? new Rgba32(36, 29, 23) : new Rgba32(246, 213, 154), 32);
                options.Canvas.CornerRadiusPx = 0;
                options.Canvas.ShadowOffsetX = options.Canvas.ShadowOffsetY = 10;
                options.Canvas.ShadowColor = new Rgba32(186, 50, 45, 100);
                break;
            case QrHeroPreset.MinimalLine:
                options.ModuleShape = QrModuleShape.Dot;
                options.Foreground = new Rgba32(54, 54, 54);
                options.Eyes.OuterShape = options.Eyes.InnerShape = QrModuleShape.Circle;
                break;
            case QrHeroPreset.Botanical:
                options.ModuleShape = QrModuleShape.Leaf;
                options.Foreground = new Rgba32(19, 87, 50);
                options.Background = new Rgba32(248, 250, 238);
                options.Eyes.OuterShape = options.Eyes.InnerShape = QrModuleShape.Leaf;
                break;
            case QrHeroPreset.Berry:
                options.ModuleShape = QrModuleShape.ExtraRounded;
                options.ForegroundGradient = Gradient(QrGradientType.DiagonalUp,
                    new Rgba32(103, 26, 116), new Rgba32(176, 28, 86));
                options.Background = new Rgba32(255, 246, 250);
                options.Eyes.OuterShape = QrModuleShape.Squircle;
                break;
        }
        if (darkPanel && options.Canvas is null) options.Canvas = Panel(new Rgba32(23, 22, 24), 40);
        return options;
    }

    /// <summary>A short suggested use for the chosen hero appearance.</summary>
    public static string DescribeHero(QrHeroPreset preset) => preset switch {
        QrHeroPreset.Ink => "Expressive monochrome marks and print.",
        QrHeroPreset.DotMatrix => "Technology, inventory and compact labels.",
        QrHeroPreset.Sunset => "Events, menus and hospitality.",
        QrHeroPreset.Ocean => "Calm digital products and packaging.",
        QrHeroPreset.MonoClassy => "Editorial layouts and premium print.",
        QrHeroPreset.Brand => "A host's two colors and a budgeted logo.",
        QrHeroPreset.Neon => "Evening events and screen displays.",
        QrHeroPreset.PaperCutout => "Cards, invitations and stationery.",
        QrHeroPreset.RetroPixel => "Playful packaging and retro graphics.",
        QrHeroPreset.MinimalLine => "Quiet labels and minimal layouts.",
        QrHeroPreset.Botanical => "Natural products and outdoor experiences.",
        QrHeroPreset.Berry => "Invitations and personal products.",
        _ => throw new ArgumentOutOfRangeException(nameof(preset))
    };

    private static QrGradientOptions Gradient(QrGradientType type, Rgba32 start, Rgba32 end) =>
        new() { Type = type, StartColor = start, EndColor = end };

    private static QrCanvasOptions Panel(Rgba32 color, int padding) =>
        new() { PaddingPx = padding, Background = color, CornerRadiusPx = 20 };
}
