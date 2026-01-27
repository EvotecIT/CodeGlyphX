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
        opts.Foreground = new Rgba32(0, 96, 92);
        opts.Background = new Rgba32(250, 252, 255);
        opts.ModuleShape = QrPngModuleShape.Dot;
        opts.ModuleScale = 0.96;
        opts.ModuleScaleMap = new QrPngModuleScaleMapOptions {
            Mode = QrPngModuleScaleMode.Radial,
            MinScale = 0.92,
            MaxScale = 1.0,
            RingSize = 2,
        };
        opts.ForegroundPalette = new QrPngPaletteOptions {
            Mode = QrPngPaletteMode.Cycle,
            RingSize = 2,
            ApplyToEyes = false,
            Colors = new[] {
                new Rgba32(0, 96, 92),
                new Rgba32(0, 72, 156),
                new Rgba32(120, 40, 160),
            },
        };
        opts.Eyes = new QrPngEyeOptions {
            UseFrame = true,
            FrameStyle = QrPngEyeFrameStyle.Glow,
            OuterShape = QrPngModuleShape.Rounded,
            InnerShape = QrPngModuleShape.Circle,
            OuterColor = new Rgba32(0, 90, 140),
            InnerColor = new Rgba32(120, 40, 160),
            OuterCornerRadiusPx = 6,
            InnerCornerRadiusPx = 4,
            GlowRadiusPx = 22,
            GlowAlpha = 96,
            GlowColor = new Rgba32(0, 170, 220, 160),
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
        opts.Foreground = new Rgba32(24, 68, 160);
        opts.Background = new Rgba32(246, 250, 255);
        opts.ModuleShape = QrPngModuleShape.Squircle;
        opts.ModuleScale = 0.96;
        opts.ModuleScaleMap = new QrPngModuleScaleMapOptions {
            Mode = QrPngModuleScaleMode.Rings,
            MinScale = 0.94,
            MaxScale = 1.0,
            RingSize = 3,
        };
        opts.ForegroundGradient = new QrPngGradientOptions {
            Type = QrPngGradientType.DiagonalDown,
            StartColor = new Rgba32(24, 120, 180),
            EndColor = new Rgba32(28, 52, 150),
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
                StartColor = new Rgba32(56, 150, 200),
                EndColor = new Rgba32(28, 72, 176),
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
        opts.Foreground = new Rgba32(60, 72, 180);
        opts.Background = new Rgba32(250, 252, 255);
        opts.ModuleShape = QrPngModuleShape.ConnectedSquircle;
        opts.ModuleScale = 0.96;
        opts.ModuleScaleMap = new QrPngModuleScaleMapOptions {
            Mode = QrPngModuleScaleMode.Radial,
            MinScale = 0.94,
            MaxScale = 1.0,
            RingSize = 2,
        };
        opts.ForegroundPalette = new QrPngPaletteOptions {
            Mode = QrPngPaletteMode.Cycle,
            RingSize = 2,
            ApplyToEyes = false,
            Colors = new[] {
                new Rgba32(60, 72, 180),
                new Rgba32(80, 56, 180),
                new Rgba32(36, 120, 164),
            },
        };
        opts.Eyes = new QrPngEyeOptions {
            UseFrame = true,
            FrameStyle = QrPngEyeFrameStyle.Target,
            OuterShape = QrPngModuleShape.Rounded,
            InnerShape = QrPngModuleShape.Circle,
            OuterCornerRadiusPx = 6,
            InnerCornerRadiusPx = 4,
            OuterColor = new Rgba32(72, 82, 190),
            InnerColor = new Rgba32(40, 130, 170),
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
                new Rgba32(24, 38, 86),
                new Rgba32(40, 60, 140),
                new Rgba32(18, 98, 140),
            },
        };
        opts.ForegroundPaletteZones = new QrPngPaletteZoneOptions {
            CornerSize = 9,
            CornerPalette = new QrPngPaletteOptions {
                Mode = QrPngPaletteMode.Checker,
                ApplyToEyes = true,
                Colors = new[] {
                    new Rgba32(22, 36, 84),
                    new Rgba32(40, 60, 140),
                },
            },
            CenterSize = 11,
            CenterPalette = new QrPngPaletteOptions {
                Mode = QrPngPaletteMode.Cycle,
                RingSize = 1,
                ApplyToEyes = false,
                Colors = new[] {
                    new Rgba32(20, 92, 140),
                    new Rgba32(44, 60, 140),
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
        opts.Foreground = new Rgba32(28, 60, 150);
        opts.Background = new Rgba32(248, 250, 255);
        opts.ModuleShape = QrPngModuleShape.Squircle;
        opts.ModuleScale = 0.95;
        opts.ModuleScaleMap = new QrPngModuleScaleMapOptions {
            Mode = QrPngModuleScaleMode.Rings,
            MinScale = 0.94,
            MaxScale = 1.0,
            RingSize = 1,
        };
        opts.ForegroundPalette = new QrPngPaletteOptions {
            Mode = QrPngPaletteMode.Rings,
            RingSize = 1,
            ApplyToEyes = false,
            Colors = new[] {
                new Rgba32(28, 60, 150),
                new Rgba32(48, 80, 170),
                new Rgba32(16, 98, 140),
            },
        };
        opts.Eyes = new QrPngEyeOptions {
            UseFrame = true,
            FrameStyle = QrPngEyeFrameStyle.InsetRing,
            OuterShape = QrPngModuleShape.Rounded,
            InnerShape = QrPngModuleShape.Circle,
            OuterCornerRadiusPx = 7,
            InnerCornerRadiusPx = 4,
            OuterColor = new Rgba32(24, 52, 140),
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

    /// <summary>
    /// Explicit scan-safe variant of <see cref="NeonGlow"/>.
    /// </summary>
    public static QrEasyOptions NeonGlowSafe() => NeonGlow();

    /// <summary>
    /// Bolder variant of <see cref="NeonGlow"/> with stronger effects.
    /// </summary>
    public static QrEasyOptions NeonGlowBold() {
        var opts = NeonGlowSafe();
        opts.ModuleScale = 0.93;
        if (opts.ModuleScaleMap is not null) {
            opts.ModuleScaleMap.MinScale = 0.90;
            opts.ModuleScaleMap.RingSize = 1;
        }
        if (opts.ForegroundPalette is not null) {
            opts.ForegroundPalette.RingSize = 1;
            opts.ForegroundPalette.Colors = new[] {
                new Rgba32(0, 92, 88),
                new Rgba32(0, 78, 170),
                new Rgba32(136, 46, 188),
            };
        }
        if (opts.Eyes is not null) {
            opts.Eyes.GlowRadiusPx = 28;
            opts.Eyes.GlowAlpha = 120;
        }
        return opts;
    }

    /// <summary>
    /// Explicit scan-safe variant of <see cref="LiquidGlass"/>.
    /// </summary>
    public static QrEasyOptions LiquidGlassSafe() => LiquidGlass();

    /// <summary>
    /// Bolder variant of <see cref="LiquidGlass"/> with punchier gradients.
    /// </summary>
    public static QrEasyOptions LiquidGlassBold() {
        var opts = LiquidGlassSafe();
        opts.ModuleScale = 0.94;
        if (opts.ModuleScaleMap is not null) {
            opts.ModuleScaleMap.MinScale = 0.90;
            opts.ModuleScaleMap.RingSize = 2;
        }
        opts.ForegroundGradient = new QrPngGradientOptions {
            Type = QrPngGradientType.DiagonalDown,
            StartColor = new Rgba32(28, 132, 196),
            EndColor = new Rgba32(34, 62, 168),
        };
        if (opts.Eyes is not null) {
            opts.Eyes.OuterGradient = new QrPngGradientOptions {
                Type = QrPngGradientType.Radial,
                StartColor = new Rgba32(64, 166, 214),
                EndColor = new Rgba32(34, 82, 188),
            };
        }
        return opts;
    }

    /// <summary>
    /// Explicit scan-safe variant of <see cref="ConnectedSquircleGlow"/>.
    /// </summary>
    public static QrEasyOptions ConnectedSquircleGlowSafe() => ConnectedSquircleGlow();

    /// <summary>
    /// Bolder variant of <see cref="ConnectedSquircleGlow"/> with denser rings.
    /// </summary>
    public static QrEasyOptions ConnectedSquircleGlowBold() {
        var opts = ConnectedSquircleGlowSafe();
        opts.ModuleScale = 0.94;
        if (opts.ModuleScaleMap is not null) {
            opts.ModuleScaleMap.MinScale = 0.90;
            opts.ModuleScaleMap.RingSize = 1;
        }
        if (opts.ForegroundPalette is not null) {
            opts.ForegroundPalette.RingSize = 1;
            opts.ForegroundPalette.Colors = new[] {
                new Rgba32(58, 70, 184),
                new Rgba32(90, 60, 184),
                new Rgba32(40, 128, 176),
            };
        }
        return opts;
    }

    /// <summary>
    /// Explicit scan-safe variant of <see cref="CutCornerTech"/>.
    /// </summary>
    public static QrEasyOptions CutCornerTechSafe() => CutCornerTech();

    /// <summary>
    /// Bolder variant of <see cref="CutCornerTech"/> with tighter modules.
    /// </summary>
    public static QrEasyOptions CutCornerTechBold() {
        var opts = CutCornerTechSafe();
        opts.ModuleScale = 0.88;
        opts.ModuleCornerRadiusPx = 5;
        if (opts.ForegroundPalette is not null) {
            opts.ForegroundPalette.RingSize = 1;
        }
        return opts;
    }

    /// <summary>
    /// Explicit scan-safe variant of <see cref="InsetRings"/>.
    /// </summary>
    public static QrEasyOptions InsetRingsSafe() => InsetRings();

    /// <summary>
    /// Bolder variant of <see cref="InsetRings"/> with stronger ring contrast.
    /// </summary>
    public static QrEasyOptions InsetRingsBold() {
        var opts = InsetRingsSafe();
        opts.ModuleScale = 0.93;
        if (opts.ModuleScaleMap is not null) {
            opts.ModuleScaleMap.MinScale = 0.90;
        }
        if (opts.ForegroundPalette is not null) {
            opts.ForegroundPalette.Colors = new[] {
                new Rgba32(26, 58, 156),
                new Rgba32(54, 86, 184),
                new Rgba32(18, 108, 152),
            };
        }
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
