using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX;

/// <summary>
/// Expressive QR art presets with scan-friendly defaults.
/// </summary>
public static class QrArtPresets {
    /// <summary>
    /// High-contrast neon glow on a dark canvas.
    /// </summary>
    public static QrEasyOptions NeonGlow() {
        var opts = BaseArt();
        opts.Foreground = new Rgba32(0, 255, 240);
        opts.Background = new Rgba32(252, 254, 255);
        opts.ModuleShape = QrPngModuleShape.Dot;
        opts.ModuleScale = 0.92;
        opts.ModuleScaleMap = new QrPngModuleScaleMapOptions {
            Mode = QrPngModuleScaleMode.Radial,
            MinScale = 0.86,
            MaxScale = 1.0,
            RingSize = 2,
        };
        opts.ForegroundPalette = new QrPngPaletteOptions {
            Mode = QrPngPaletteMode.Cycle,
            RingSize = 2,
            ApplyToEyes = false,
            Colors = new[] {
                new Rgba32(0, 255, 240),
                new Rgba32(0, 168, 255),
                new Rgba32(255, 92, 255),
            },
        };
        opts.Eyes = new QrPngEyeOptions {
            UseFrame = true,
            FrameStyle = QrPngEyeFrameStyle.Glow,
            OuterShape = QrPngModuleShape.Rounded,
            InnerShape = QrPngModuleShape.Circle,
            OuterColor = new Rgba32(0, 255, 240),
            InnerColor = new Rgba32(255, 92, 255),
            OuterCornerRadiusPx = 6,
            InnerCornerRadiusPx = 4,
            GlowRadiusPx = 34,
            GlowAlpha = 140,
            GlowColor = new Rgba32(0, 210, 255, 220),
        };
        opts.Canvas = new QrPngCanvasOptions {
            PaddingPx = 28,
            CornerRadiusPx = 30,
            BackgroundGradient = new QrPngGradientOptions {
                Type = QrPngGradientType.DiagonalDown,
                StartColor = new Rgba32(8, 10, 28),
                EndColor = new Rgba32(30, 20, 76),
            },
            BorderPx = 2,
            BorderColor = new Rgba32(255, 255, 255, 54),
            ShadowOffsetX = 6,
            ShadowOffsetY = 8,
            ShadowColor = new Rgba32(0, 0, 0, 70),
        };
        return opts;
    }

    /// <summary>
    /// Soft, glassy gradients with smooth squircles.
    /// </summary>
    public static QrEasyOptions LiquidGlass() {
        var opts = BaseArt();
        opts.Foreground = new Rgba32(32, 98, 255);
        opts.Background = new Rgba32(248, 251, 255);
        opts.ModuleShape = QrPngModuleShape.Squircle;
        opts.ModuleScale = 0.94;
        opts.ModuleScaleMap = new QrPngModuleScaleMapOptions {
            Mode = QrPngModuleScaleMode.Rings,
            MinScale = 0.9,
            MaxScale = 1.0,
            RingSize = 3,
        };
        opts.ForegroundGradient = new QrPngGradientOptions {
            Type = QrPngGradientType.DiagonalDown,
            StartColor = new Rgba32(38, 190, 255),
            EndColor = new Rgba32(66, 84, 255),
        };
        opts.Eyes = new QrPngEyeOptions {
            UseFrame = true,
            FrameStyle = QrPngEyeFrameStyle.InsetRing,
            OuterShape = QrPngModuleShape.Squircle,
            InnerShape = QrPngModuleShape.Circle,
            OuterCornerRadiusPx = 7,
            InnerCornerRadiusPx = 4,
            OuterGradient = new QrPngGradientOptions {
                Type = QrPngGradientType.Radial,
                StartColor = new Rgba32(102, 224, 255),
                EndColor = new Rgba32(48, 96, 255),
            },
            InnerColor = new Rgba32(255, 255, 255),
        };
        opts.Canvas = new QrPngCanvasOptions {
            PaddingPx = 26,
            CornerRadiusPx = 28,
            BackgroundGradient = new QrPngGradientOptions {
                Type = QrPngGradientType.DiagonalDown,
                StartColor = new Rgba32(242, 248, 255),
                EndColor = new Rgba32(222, 236, 255),
            },
            Pattern = new QrPngBackgroundPatternOptions {
                Type = QrPngBackgroundPatternType.Dots,
                Color = new Rgba32(80, 120, 255, 24),
                SizePx = 12,
                ThicknessPx = 2,
            },
            BorderPx = 1,
            BorderColor = new Rgba32(255, 255, 255, 190),
            ShadowOffsetX = 4,
            ShadowOffsetY = 6,
            ShadowColor = new Rgba32(60, 90, 160, 54),
        };
        return opts;
    }

    /// <summary>
    /// Connected squircles with a bold, modern glow.
    /// </summary>
    public static QrEasyOptions ConnectedSquircleGlow() {
        var opts = BaseArt();
        opts.Foreground = new Rgba32(110, 120, 255);
        opts.Background = new Rgba32(250, 252, 255);
        opts.ModuleShape = QrPngModuleShape.ConnectedSquircle;
        opts.ModuleScale = 0.94;
        opts.ModuleScaleMap = new QrPngModuleScaleMapOptions {
            Mode = QrPngModuleScaleMode.Radial,
            MinScale = 0.9,
            MaxScale = 1.0,
            RingSize = 2,
        };
        opts.ForegroundPalette = new QrPngPaletteOptions {
            Mode = QrPngPaletteMode.Cycle,
            RingSize = 2,
            ApplyToEyes = false,
            Colors = new[] {
                new Rgba32(110, 120, 255),
                new Rgba32(140, 96, 255),
                new Rgba32(90, 210, 255),
            },
        };
        opts.Eyes = new QrPngEyeOptions {
            UseFrame = true,
            FrameStyle = QrPngEyeFrameStyle.Target,
            OuterShape = QrPngModuleShape.Rounded,
            InnerShape = QrPngModuleShape.Circle,
            OuterCornerRadiusPx = 6,
            InnerCornerRadiusPx = 4,
            OuterColor = new Rgba32(120, 128, 255),
            InnerColor = new Rgba32(90, 210, 255),
        };
        opts.Canvas = new QrPngCanvasOptions {
            PaddingPx = 28,
            CornerRadiusPx = 30,
            BackgroundGradient = new QrPngGradientOptions {
                Type = QrPngGradientType.DiagonalDown,
                StartColor = new Rgba32(14, 18, 42),
                EndColor = new Rgba32(34, 22, 88),
            },
            BorderPx = 2,
            BorderColor = new Rgba32(255, 255, 255, 52),
            ShadowOffsetX = 6,
            ShadowOffsetY = 8,
            ShadowColor = new Rgba32(0, 0, 0, 72),
        };
        return opts;
    }

    /// <summary>
    /// Techy cut-corner eyes with structured palettes.
    /// </summary>
    public static QrEasyOptions CutCornerTech() {
        var opts = BaseArt();
        opts.Foreground = new Rgba32(20, 32, 62);
        opts.Background = new Rgba32(244, 248, 255);
        opts.ModuleShape = QrPngModuleShape.Rounded;
        opts.ModuleScale = 0.9;
        opts.ModuleCornerRadiusPx = 4;
        opts.ForegroundPalette = new QrPngPaletteOptions {
            Mode = QrPngPaletteMode.Rings,
            RingSize = 2,
            ApplyToEyes = false,
            Colors = new[] {
                new Rgba32(34, 54, 110),
                new Rgba32(72, 104, 214),
                new Rgba32(26, 170, 220),
            },
        };
        opts.ForegroundPaletteZones = new QrPngPaletteZoneOptions {
            CornerSize = 9,
            CornerPalette = new QrPngPaletteOptions {
                Mode = QrPngPaletteMode.Checker,
                ApplyToEyes = true,
                Colors = new[] {
                    new Rgba32(22, 36, 84),
                    new Rgba32(80, 120, 255),
                },
            },
            CenterSize = 11,
            CenterPalette = new QrPngPaletteOptions {
                Mode = QrPngPaletteMode.Cycle,
                RingSize = 1,
                ApplyToEyes = false,
                Colors = new[] {
                    new Rgba32(44, 210, 255),
                    new Rgba32(110, 120, 255),
                },
            },
        };
        opts.Eyes = new QrPngEyeOptions {
            UseFrame = true,
            FrameStyle = QrPngEyeFrameStyle.CutCorner,
            OuterShape = QrPngModuleShape.Square,
            InnerShape = QrPngModuleShape.Rounded,
            OuterColor = new Rgba32(18, 28, 70),
            InnerColor = new Rgba32(98, 128, 255),
            OuterCornerRadiusPx = 0,
            InnerCornerRadiusPx = 4,
        };
        opts.Canvas = new QrPngCanvasOptions {
            PaddingPx = 26,
            CornerRadiusPx = 28,
            BackgroundGradient = new QrPngGradientOptions {
                Type = QrPngGradientType.Horizontal,
                StartColor = new Rgba32(232, 240, 255),
                EndColor = new Rgba32(210, 226, 255),
            },
            Pattern = new QrPngBackgroundPatternOptions {
                Type = QrPngBackgroundPatternType.Grid,
                Color = new Rgba32(60, 90, 180, 26),
                SizePx = 16,
                ThicknessPx = 1,
            },
            BorderPx = 1,
            BorderColor = new Rgba32(255, 255, 255, 210),
            ShadowOffsetX = 4,
            ShadowOffsetY = 6,
            ShadowColor = new Rgba32(30, 50, 110, 54),
        };
        return opts;
    }

    /// <summary>
    /// Inset-ring eyes with orderly ring palettes.
    /// </summary>
    public static QrEasyOptions InsetRings() {
        var opts = BaseArt();
        opts.Foreground = new Rgba32(44, 80, 180);
        opts.Background = new Rgba32(248, 250, 255);
        opts.ModuleShape = QrPngModuleShape.Squircle;
        opts.ModuleScale = 0.93;
        opts.ModuleScaleMap = new QrPngModuleScaleMapOptions {
            Mode = QrPngModuleScaleMode.Rings,
            MinScale = 0.9,
            MaxScale = 1.0,
            RingSize = 1,
        };
        opts.ForegroundPalette = new QrPngPaletteOptions {
            Mode = QrPngPaletteMode.Rings,
            RingSize = 1,
            ApplyToEyes = false,
            Colors = new[] {
                new Rgba32(44, 80, 180),
                new Rgba32(82, 112, 214),
                new Rgba32(28, 170, 220),
            },
        };
        opts.Eyes = new QrPngEyeOptions {
            UseFrame = true,
            FrameStyle = QrPngEyeFrameStyle.InsetRing,
            OuterShape = QrPngModuleShape.Rounded,
            InnerShape = QrPngModuleShape.Circle,
            OuterCornerRadiusPx = 7,
            InnerCornerRadiusPx = 4,
            OuterColor = new Rgba32(36, 64, 150),
            InnerColor = new Rgba32(255, 255, 255),
        };
        opts.Canvas = new QrPngCanvasOptions {
            PaddingPx = 24,
            CornerRadiusPx = 26,
            BackgroundGradient = new QrPngGradientOptions {
                Type = QrPngGradientType.Vertical,
                StartColor = new Rgba32(238, 244, 255),
                EndColor = new Rgba32(220, 232, 255),
            },
            BorderPx = 1,
            BorderColor = new Rgba32(255, 255, 255, 200),
            ShadowOffsetX = 4,
            ShadowOffsetY = 6,
            ShadowColor = new Rgba32(40, 70, 150, 48),
        };
        return opts;
    }

    private static QrEasyOptions BaseArt() {
        return new QrEasyOptions {
            ErrorCorrectionLevel = QrErrorCorrectionLevel.H,
            ModuleSize = 10,
            QuietZone = 4,
            BackgroundSupersample = 2,
            ProtectFunctionalPatterns = true,
            ProtectQuietZone = true,
        };
    }
}
