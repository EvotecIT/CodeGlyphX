using System;
using CodeGlyphX.Rendering;

namespace CodeGlyphX;

public static partial class QrArt {
    /// <summary>Creates an independent, editable hero appearance. Dark compositions retain
    /// a light QR panel and full quiet zone; Neon uses its dark composition for both choices.
    /// Brand provides the two-color appearance;
    /// pass it to RenderLogo to place and validate the actual logo without silently omitting it.</summary>
    public static QrRenderOptions Hero(QrHeroPreset preset, bool darkPanel = false) {
        if (preset is < QrHeroPreset.Ink or > QrHeroPreset.Berry) throw new ArgumentOutOfRangeException(nameof(preset));
        var options = new QrRenderOptions {
            HeroPreset = preset, TargetSizePx = 1024, ModuleSize = 16, QuietZone = 4, Foreground = new Rgba32(25, 25, 27),
            LogoCornerRadiusPx = 0,
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
                options.ModuleShape = QrModuleShape.ConnectedRounded;
                options.Background = new Rgba32(255, 250, 242);
                options.ForegroundGradient = Gradient(QrGradientType.DiagonalDown,
                    new Rgba32(220, 101, 0), new Rgba32(224, 36, 122));
                break;
            case QrHeroPreset.Ocean:
                options.ModuleShape = QrModuleShape.BarsHorizontal;
                options.Eyes.OuterShape = QrModuleShape.Squircle;
                options.Eyes.InnerShape = QrModuleShape.Squircle;
                options.ForegroundGradient = Gradient(QrGradientType.Radial,
                    new Rgba32(11, 46, 107), new Rgba32(19, 181, 177));
                options.ForegroundGradient.StartOffset = .4;
                break;
            case QrHeroPreset.MonoClassy:
                options.ModuleShape = QrModuleShape.ConnectedRounded;
                options.Foreground = new Rgba32(43, 43, 43);
                options.Background = new Rgba32(244, 239, 230);
                options.Eyes.OuterShape = QrModuleShape.Classy;
                options.Eyes.MirrorOutward = true;
                options.Eyes.InnerShape = QrModuleShape.Square;
                break;
            case QrHeroPreset.Brand:
                options.ModuleShape = QrModuleShape.Rounded;
                options.Foreground = new Rgba32(36, 38, 42);
                options.Eyes.OuterColor = options.Eyes.InnerColor = new Rgba32(80, 32, 42);
                options.Eyes.OuterShape = QrModuleShape.Rounded;
                options.Eyes.InnerShape = QrModuleShape.Circle;
                options.Eyes.OuterCornerRadiusPx = 16;
                break;
            case QrHeroPreset.Neon:
                options.ModuleShape = QrModuleShape.BarsVertical;
                options.Foreground = new Rgba32(48, 10, 71);
                options.Background = new Rgba32(251, 239, 255);
                options.Eyes.OuterColor = options.Eyes.InnerColor = new Rgba32(94, 13, 80);
                options.Canvas = Panel(new Rgba32(10, 6, 20), 48);
                options.Canvas.PanelGlowColor = new Rgba32(255, 43, 214);
                options.Canvas.SecondaryPanelGlowColor = new Rgba32(123, 43, 255);
                break;
            case QrHeroPreset.PaperCutout:
                options.ModuleShape = QrModuleShape.Liquid;
                options.Foreground = new Rgba32(69, 51, 38);
                options.Background = new Rgba32(255, 252, 245);
                options.Canvas = Panel(darkPanel ? new Rgba32(32, 27, 24) : new Rgba32(237, 225, 205), 52);
                options.Canvas.ShadowColor = new Rgba32(35, 23, 12, 46);
                options.Canvas.StackedPanelColor = darkPanel ? new Rgba32(187, 167, 139) : new Rgba32(225, 207, 176);
                break;
            case QrHeroPreset.RetroPixel:
                options.ModuleShape = QrModuleShape.Square;
                options.Eyes.OuterShape = options.Eyes.InnerShape = QrModuleShape.Square;
                options.Foreground = new Rgba32(30, 30, 36);
                options.Background = new Rgba32(242, 193, 78);
                options.Canvas = Panel(darkPanel ? new Rgba32(36, 29, 23) : new Rgba32(246, 213, 154), 32);
                options.Canvas.CornerRadiusPx = 0;
                options.LogoPlateShape = QrLogoPlateShape.Square;
                break;
            case QrHeroPreset.MinimalLine:
                options.ModuleShape = QrModuleShape.Dot;
                options.Foreground = new Rgba32(31, 31, 31);
                options.ModuleScale = .6875; // Dot applies 0.8: the final diameter is 55% of a module.
                options.Eyes.RingThicknessModules = .75;
                options.Eyes.OuterShape = options.Eyes.InnerShape = QrModuleShape.Circle;
                break;
            case QrHeroPreset.Botanical:
                options.ModuleShape = QrModuleShape.Leaf;
                options.Foreground = new Rgba32(19, 87, 50);
                options.Background = new Rgba32(248, 250, 238);
                options.Eyes.OuterShape = QrModuleShape.Leaf;
                options.Eyes.InnerShape = QrModuleShape.Leaf;
                options.Eyes.MirrorOutward = true;
                break;
            case QrHeroPreset.Berry:
                options.ModuleShape = QrModuleShape.Rounded;
                options.ForegroundGradient = Gradient(QrGradientType.DiagonalUp,
                    new Rgba32(103, 26, 116), new Rgba32(176, 28, 86));
                options.Background = new Rgba32(255, 246, 250);
                options.Eyes.OuterShape = QrModuleShape.Squircle;
                break;
        }
        if (darkPanel || preset == QrHeroPreset.Neon) {
            var ground = preset switch {
                QrHeroPreset.Ocean => new Rgba32(5, 20, 40),
                QrHeroPreset.Sunset => new Rgba32(42, 18, 6),
                QrHeroPreset.Botanical => new Rgba32(12, 30, 20),
                QrHeroPreset.Berry => new Rgba32(34, 12, 35),
                QrHeroPreset.DotMatrix => new Rgba32(10, 20, 39),
                QrHeroPreset.MonoClassy => new Rgba32(35, 31, 27),
                QrHeroPreset.Ink => new Rgba32(15, 17, 20),
                QrHeroPreset.MinimalLine => new Rgba32(24, 26, 25),
                QrHeroPreset.Brand => new Rgba32(49, 8, 21),
                QrHeroPreset.PaperCutout => new Rgba32(42, 31, 21),
                QrHeroPreset.RetroPixel => new Rgba32(37, 24, 32),
                _ => new Rgba32(20, 6, 44)
            };
            options.Canvas ??= Panel(ground, 40);
            options.Canvas.Background = ground;
            options.Canvas.BackgroundGradient = Gradient(QrGradientType.DiagonalDown, ground,
                new Rgba32((byte)(ground.R + 12), (byte)(ground.G + 10), (byte)(ground.B + 16)));
            options.Background = preset switch {
                QrHeroPreset.Ink => new Rgba32(233, 237, 244),
                QrHeroPreset.MinimalLine => new Rgba32(244, 244, 240),
                QrHeroPreset.DotMatrix => new Rgba32(231, 239, 252),
                QrHeroPreset.Sunset => new Rgba32(255, 233, 238),
                QrHeroPreset.Ocean => new Rgba32(223, 247, 246),
                QrHeroPreset.MonoClassy => new Rgba32(235, 231, 222),
                QrHeroPreset.Brand => new Rgba32(245, 231, 238),
                QrHeroPreset.Neon => new Rgba32(251, 239, 255),
                QrHeroPreset.PaperCutout => new Rgba32(237, 223, 198),
                QrHeroPreset.RetroPixel => new Rgba32(245, 210, 125),
                QrHeroPreset.Botanical => new Rgba32(228, 241, 222),
                _ => new Rgba32(252, 229, 241)
            };
            if (preset == QrHeroPreset.Berry) options.ForegroundGradient!.EndColor = new Rgba32(159, 24, 82);
            var accent = HeroAccent(preset);
            var wash = preset == QrHeroPreset.Neon ? new Rgba32(123, 43, 255) : accent;
            options.Canvas.BackgroundGradient!.EndColor = new Rgba32((byte)((ground.R * 3 + wash.R * 2) / 5),
                (byte)((ground.G * 3 + wash.G * 2) / 5), (byte)((ground.B * 3 + wash.B * 2) / 5));
            if (preset == QrHeroPreset.Sunset) {
                options.Canvas.BackgroundGradient.EndColor = new Rgba32(122, 42, 14);
                options.Canvas.PanelGlowColor = new Rgba32(255, 111, 24, 150);
                options.Canvas.SecondaryPanelGlowColor = new Rgba32(224, 36, 122, 120);
            }
            if (preset != QrHeroPreset.PaperCutout)
                options.Canvas.PanelGlowColor ??= new Rgba32(accent.R, accent.G, accent.B, 150);
        }
        if (preset is QrHeroPreset.Ink or QrHeroPreset.DotMatrix or QrHeroPreset.MinimalLine or QrHeroPreset.MonoClassy)
            options.LogoTint = options.Foreground;
        if (preset is QrHeroPreset.Sunset or QrHeroPreset.Ocean or QrHeroPreset.Neon or QrHeroPreset.Berry or QrHeroPreset.Brand) {
            options.LogoRingColor = HeroAccent(preset);
            options.LogoRingWidthPx = 2;
        }
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

    private static Rgba32 HeroAccent(QrHeroPreset preset) => preset switch {
        QrHeroPreset.DotMatrix => new Rgba32(44, 94, 180),
        QrHeroPreset.Ocean => new Rgba32(19, 181, 177),
        QrHeroPreset.Sunset => new Rgba32(224, 36, 122),
        QrHeroPreset.Brand => new Rgba32(150, 52, 80),
        QrHeroPreset.Neon => new Rgba32(255, 43, 214),
        QrHeroPreset.Botanical => new Rgba32(100, 174, 87),
        QrHeroPreset.Berry => new Rgba32(208, 59, 137),
        QrHeroPreset.RetroPixel => new Rgba32(180, 85, 111),
        QrHeroPreset.PaperCutout => new Rgba32(152, 112, 65),
        QrHeroPreset.MonoClassy => new Rgba32(130, 116, 96),
        QrHeroPreset.MinimalLine => new Rgba32(108, 122, 113),
        _ => new Rgba32(104, 116, 132)
    };

    private static QrCanvasOptions Panel(Rgba32 color, int padding) =>
        new() { PaddingPx = padding, Background = color, CornerRadiusPx = 20 };
}
