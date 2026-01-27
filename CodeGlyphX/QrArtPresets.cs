using System;
using System.ComponentModel;
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
    [Obsolete("Use QrArt.Theme(...) + QrEasyOptions.Art instead.", false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
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
        opts.ForegroundPattern = new QrPngForegroundPatternOptions {
            Type = QrPngForegroundPatternType.StippleDots,
            Color = new Rgba32(0, 48, 46, 120),
            SizePx = 5,
            ThicknessPx = 1,
            ApplyToModules = true,
            ApplyToEyes = false,
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
    [Obsolete("Use QrArt.Theme(...) + QrEasyOptions.Art instead.", false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
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
        opts.ForegroundPattern = new QrPngForegroundPatternOptions {
            Type = QrPngForegroundPatternType.StippleDots,
            Color = new Rgba32(18, 50, 120, 96),
            SizePx = 6,
            ThicknessPx = 1,
            ApplyToModules = true,
            ApplyToEyes = false,
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
    [Obsolete("Use QrArt.Theme(...) + QrEasyOptions.Art instead.", false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
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
        opts.ForegroundPattern = new QrPngForegroundPatternOptions {
            Type = QrPngForegroundPatternType.StippleDots,
            Color = new Rgba32(44, 54, 150, 96),
            SizePx = 6,
            ThicknessPx = 1,
            ApplyToModules = true,
            ApplyToEyes = false,
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
    [Obsolete("Use QrArt.Theme(...) + QrEasyOptions.Art instead.", false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
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
        opts.ForegroundPattern = new QrPngForegroundPatternOptions {
            Type = QrPngForegroundPatternType.Crosshatch,
            Color = new Rgba32(12, 24, 64, 92),
            SizePx = 6,
            ThicknessPx = 1,
            ApplyToModules = true,
            ApplyToEyes = false,
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
    [Obsolete("Use QrArt.Theme(...) + QrEasyOptions.Art instead.", false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
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
        opts.ForegroundPattern = new QrPngForegroundPatternOptions {
            Type = QrPngForegroundPatternType.StippleDots,
            Color = new Rgba32(18, 42, 120, 92),
            SizePx = 6,
            ThicknessPx = 1,
            ApplyToModules = true,
            ApplyToEyes = false,
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
    /// Stripe-textured eyes with calm module styling.
    /// </summary>
    [Obsolete("Use QrArt.Theme(...) + QrEasyOptions.Art instead.", false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static QrEasyOptions StripeEyes() {
        var opts = BaseArt();
        opts.Foreground = new Rgba32(26, 46, 128);
        opts.Background = new Rgba32(248, 250, 255);
        opts.ModuleShape = QrPngModuleShape.Squircle;
        opts.ModuleScale = 0.96;
        opts.ModuleScaleMap = new QrPngModuleScaleMapOptions {
            Mode = QrPngModuleScaleMode.Rings,
            MinScale = 0.94,
            MaxScale = 1.0,
            RingSize = 2,
        };
        opts.ForegroundPalette = new QrPngPaletteOptions {
            Mode = QrPngPaletteMode.Cycle,
            RingSize = 2,
            ApplyToEyes = false,
            Colors = new[] {
                new Rgba32(26, 46, 128),
                new Rgba32(40, 70, 150),
                new Rgba32(20, 110, 156),
            },
        };
        opts.ForegroundPattern = new QrPngForegroundPatternOptions {
            Type = QrPngForegroundPatternType.DiagonalStripes,
            Color = new Rgba32(18, 34, 108, 148),
            SizePx = 6,
            ThicknessPx = 2,
            SnapToModuleSize = true,
            ModuleStep = 1,
            ApplyToModules = false,
            ApplyToEyes = true,
        };
        opts.Eyes = new QrPngEyeOptions {
            UseFrame = false,
            OuterShape = QrPngModuleShape.Rounded,
            InnerShape = QrPngModuleShape.Squircle,
            OuterCornerRadiusPx = 6,
            InnerCornerRadiusPx = 4,
            OuterColor = new Rgba32(24, 44, 132),
            InnerColor = new Rgba32(46, 170, 188),
            SparkleCount = 22,
            SparkleRadiusPx = 3,
            SparkleSpreadPx = 24,
            SparkleSeed = 90210,
            SparkleColor = new Rgba32(70, 190, 210, 140),
        };
        opts.Canvas = new QrPngCanvasOptions {
            PaddingPx = 26,
            CornerRadiusPx = 28,
            BackgroundGradient = new QrPngGradientOptions {
                Type = QrPngGradientType.Vertical,
                StartColor = new Rgba32(236, 244, 255),
                EndColor = new Rgba32(220, 232, 255),
            },
            BorderPx = 1,
            BorderColor = new Rgba32(255, 255, 255, 200),
            ShadowOffsetX = 4,
            ShadowOffsetY = 6,
            ShadowColor = new Rgba32(32, 60, 130, 48),
        };
        return opts;
    }

    /// <summary>
    /// Paint-splash canvas with crisp, scan-friendly modules.
    /// </summary>
    [Obsolete("Use QrArt.Theme(...) + QrEasyOptions.Art instead.", false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static QrEasyOptions PaintSplash() {
        var opts = BaseArt();
        opts.Foreground = new Rgba32(18, 36, 92);
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
                new Rgba32(18, 36, 92),
                new Rgba32(26, 68, 148),
                new Rgba32(14, 120, 150),
            },
        };
        opts.Eyes = new QrPngEyeOptions {
            UseFrame = true,
            FrameStyle = QrPngEyeFrameStyle.Target,
            OuterShape = QrPngModuleShape.Rounded,
            InnerShape = QrPngModuleShape.Circle,
            OuterCornerRadiusPx = 6,
            InnerCornerRadiusPx = 4,
            OuterColor = new Rgba32(20, 44, 120),
            InnerColor = new Rgba32(26, 164, 182),
        };
        opts.Canvas = new QrPngCanvasOptions {
            PaddingPx = 30,
            CornerRadiusPx = 30,
            BackgroundGradient = new QrPngGradientOptions {
                Type = QrPngGradientType.DiagonalDown,
                StartColor = new Rgba32(238, 246, 255),
                EndColor = new Rgba32(218, 232, 255),
            },
            Splash = new QrPngCanvasSplashOptions {
                Color = new Rgba32(40, 150, 210, 96),
                Colors = new[] {
                    new Rgba32(40, 150, 210, 92),
                    new Rgba32(214, 72, 150, 92),
                    new Rgba32(110, 190, 70, 92),
                    new Rgba32(230, 96, 54, 92),
                },
                Count = 11,
                MinRadiusPx = 16,
                MaxRadiusPx = 46,
                SpreadPx = 28,
                Seed = 4242,
                DripChance = 0.42,
                DripLengthPx = 28,
                DripWidthPx = 9,
                ProtectQrArea = true,
            },
            BorderPx = 1,
            BorderColor = new Rgba32(255, 255, 255, 210),
            ShadowOffsetX = 6,
            ShadowOffsetY = 8,
            ShadowColor = new Rgba32(22, 50, 120, 56),
        };
        return opts;
    }

    /// <summary>
    /// Pastel paint-splash canvas with a softer palette.
    /// </summary>
    [Obsolete("Use QrArt.Theme(...) + QrEasyOptions.Art instead.", false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static QrEasyOptions PaintSplashPastel() {
        var opts = BaseArt();
        opts.Foreground = new Rgba32(20, 40, 98);
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
                new Rgba32(20, 40, 98),
                new Rgba32(32, 72, 144),
                new Rgba32(24, 108, 140),
            },
        };
        opts.Eyes = new QrPngEyeOptions {
            UseFrame = true,
            FrameStyle = QrPngEyeFrameStyle.Target,
            OuterShape = QrPngModuleShape.Rounded,
            InnerShape = QrPngModuleShape.Circle,
            OuterCornerRadiusPx = 6,
            InnerCornerRadiusPx = 4,
            OuterColor = new Rgba32(28, 60, 136),
            InnerColor = new Rgba32(60, 174, 186),
        };
        opts.Canvas = new QrPngCanvasOptions {
            PaddingPx = 30,
            CornerRadiusPx = 30,
            BackgroundGradient = new QrPngGradientOptions {
                Type = QrPngGradientType.DiagonalDown,
                StartColor = new Rgba32(244, 248, 255),
                EndColor = new Rgba32(226, 236, 255),
            },
            Splash = new QrPngCanvasSplashOptions {
                Color = new Rgba32(96, 132, 210, 92),
                Colors = new[] {
                    new Rgba32(96, 132, 210, 92),
                    new Rgba32(190, 110, 200, 92),
                    new Rgba32(110, 186, 140, 92),
                    new Rgba32(214, 152, 104, 92),
                },
                Count = 12,
                MinRadiusPx = 16,
                MaxRadiusPx = 48,
                SpreadPx = 30,
                Seed = 7331,
                DripChance = 0.45,
                DripLengthPx = 30,
                DripWidthPx = 9,
                ProtectQrArea = true,
            },
            BorderPx = 1,
            BorderColor = new Rgba32(255, 255, 255, 214),
            ShadowOffsetX = 6,
            ShadowOffsetY = 8,
            ShadowColor = new Rgba32(36, 66, 140, 52),
        };
        return opts;
    }

    /// <summary>
    /// Explicit scan-safe variant of <see cref="NeonGlow"/>.
    /// </summary>
    [Obsolete("Use QrArt.Theme(...) + QrEasyOptions.Art instead.", false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static QrEasyOptions NeonGlowSafe() => NeonGlow();

    /// <summary>
    /// Bolder variant of <see cref="NeonGlow"/> with stronger effects.
    /// </summary>
    [Obsolete("Use QrArt.Theme(...) + QrEasyOptions.Art instead.", false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
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
        if (opts.ForegroundPattern is not null) {
            opts.ForegroundPattern.Type = QrPngForegroundPatternType.Starburst;
            opts.ForegroundPattern.ApplyToEyes = true;
            opts.ForegroundPattern.SnapToModuleSize = true;
            opts.ForegroundPattern.ModuleStep = 2;
            opts.ForegroundPattern.SizePx = 8;
            opts.ForegroundPattern.ThicknessPx = 1;
            opts.ForegroundPattern.Color = new Rgba32(0, 140, 220, 92);
        }
        if (opts.Canvas is not null) {
            opts.Canvas.Halo = new QrPngCanvasHaloOptions {
                Color = new Rgba32(0, 176, 230, 120),
                RadiusPx = 44,
                ProtectQrArea = true,
            };
        }
        return opts;
    }

    /// <summary>
    /// Explicit scan-safe variant of <see cref="LiquidGlass"/>.
    /// </summary>
    [Obsolete("Use QrArt.Theme(...) + QrEasyOptions.Art instead.", false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static QrEasyOptions LiquidGlassSafe() => LiquidGlass();

    /// <summary>
    /// Bolder variant of <see cref="LiquidGlass"/> with punchier gradients.
    /// </summary>
    [Obsolete("Use QrArt.Theme(...) + QrEasyOptions.Art instead.", false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
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
    [Obsolete("Use QrArt.Theme(...) + QrEasyOptions.Art instead.", false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static QrEasyOptions ConnectedSquircleGlowSafe() => ConnectedSquircleGlow();

    /// <summary>
    /// Bolder variant of <see cref="ConnectedSquircleGlow"/> with denser rings.
    /// </summary>
    [Obsolete("Use QrArt.Theme(...) + QrEasyOptions.Art instead.", false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
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
    [Obsolete("Use QrArt.Theme(...) + QrEasyOptions.Art instead.", false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static QrEasyOptions CutCornerTechSafe() => CutCornerTech();

    /// <summary>
    /// Bolder variant of <see cref="CutCornerTech"/> with tighter modules.
    /// </summary>
    [Obsolete("Use QrArt.Theme(...) + QrEasyOptions.Art instead.", false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
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
    [Obsolete("Use QrArt.Theme(...) + QrEasyOptions.Art instead.", false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static QrEasyOptions InsetRingsSafe() => InsetRings();

    /// <summary>
    /// Bolder variant of <see cref="InsetRings"/> with stronger ring contrast.
    /// </summary>
    [Obsolete("Use QrArt.Theme(...) + QrEasyOptions.Art instead.", false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
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

    /// <summary>
    /// Explicit scan-safe variant of <see cref="StripeEyes"/>.
    /// </summary>
    [Obsolete("Use QrArt.Theme(...) + QrEasyOptions.Art instead.", false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static QrEasyOptions StripeEyesSafe() => StripeEyes();

    /// <summary>
    /// Bolder variant of <see cref="StripeEyes"/> that also textures modules.
    /// </summary>
    [Obsolete("Use QrArt.Theme(...) + QrEasyOptions.Art instead.", false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static QrEasyOptions StripeEyesBold() {
        var opts = StripeEyesSafe();
        opts.ModuleScale = 0.94;
        if (opts.ModuleScaleMap is not null) {
            opts.ModuleScaleMap.MinScale = 0.90;
            opts.ModuleScaleMap.RingSize = 1;
        }
        if (opts.ForegroundPattern is not null) {
            opts.ForegroundPattern.ApplyToModules = true;
            opts.ForegroundPattern.SizePx = 6;
            opts.ForegroundPattern.ThicknessPx = 2;
        }
        if (opts.ForegroundPalette is not null) {
            opts.ForegroundPalette.RingSize = 1;
        }
        return opts;
    }

    /// <summary>
    /// Explicit scan-safe variant of <see cref="PaintSplash"/>.
    /// </summary>
    [Obsolete("Use QrArt.Theme(...) + QrEasyOptions.Art instead.", false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static QrEasyOptions PaintSplashSafe() => PaintSplash();

    /// <summary>
    /// Bolder variant of <see cref="PaintSplash"/> with denser splashes.
    /// </summary>
    [Obsolete("Use QrArt.Theme(...) + QrEasyOptions.Art instead.", false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static QrEasyOptions PaintSplashBold() {
        var opts = PaintSplashSafe();
        opts.ModuleScale = 0.95;
        if (opts.ModuleScaleMap is not null) {
            opts.ModuleScaleMap.MinScale = 0.92;
            opts.ModuleScaleMap.RingSize = 1;
        }
        if (opts.Canvas?.Splash is not null) {
            opts.Canvas.Splash.Color = new Rgba32(40, 150, 210, 120);
            if (opts.Canvas.Splash.Colors is not null) {
                opts.Canvas.Splash.Colors = new[] {
                    new Rgba32(40, 150, 210, 112),
                    new Rgba32(214, 72, 150, 112),
                    new Rgba32(110, 190, 70, 112),
                    new Rgba32(230, 96, 54, 112),
                };
            }
            opts.Canvas.Splash.Count = 16;
            opts.Canvas.Splash.DripChance = 0.55;
            opts.Canvas.Splash.DripLengthPx = 36;
            opts.Canvas.Splash.MaxRadiusPx = Math.Max(opts.Canvas.Splash.MaxRadiusPx, 54);
        }
        if (opts.Eyes is not null) {
            opts.Eyes.OuterColors = new[] {
                new Rgba32(20, 44, 120),
                new Rgba32(168, 52, 132),
                new Rgba32(44, 118, 64),
            };
            opts.Eyes.InnerColors = new[] {
                new Rgba32(26, 164, 182),
                new Rgba32(214, 72, 150),
                new Rgba32(110, 190, 70),
            };
        }
        return opts;
    }

    /// <summary>
    /// Explicit scan-safe variant of <see cref="PaintSplashPastel"/>.
    /// </summary>
    [Obsolete("Use QrArt.Theme(...) + QrEasyOptions.Art instead.", false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static QrEasyOptions PaintSplashPastelSafe() => PaintSplashPastel();

    /// <summary>
    /// Bolder variant of <see cref="PaintSplashPastel"/> with denser splashes.
    /// </summary>
    [Obsolete("Use QrArt.Theme(...) + QrEasyOptions.Art instead.", false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static QrEasyOptions PaintSplashPastelBold() {
        var opts = PaintSplashPastelSafe();
        opts.ModuleScale = 0.95;
        if (opts.ModuleScaleMap is not null) {
            opts.ModuleScaleMap.MinScale = 0.92;
            opts.ModuleScaleMap.RingSize = 1;
        }
        if (opts.Canvas?.Splash is not null) {
            opts.Canvas.Splash.Color = new Rgba32(96, 132, 210, 112);
            if (opts.Canvas.Splash.Colors is not null) {
                opts.Canvas.Splash.Colors = new[] {
                    new Rgba32(96, 132, 210, 108),
                    new Rgba32(190, 110, 200, 108),
                    new Rgba32(110, 186, 140, 108),
                    new Rgba32(214, 152, 104, 108),
                };
            }
            opts.Canvas.Splash.Count = 18;
            opts.Canvas.Splash.DripChance = 0.6;
            opts.Canvas.Splash.DripLengthPx = 38;
            opts.Canvas.Splash.MaxRadiusPx = Math.Max(opts.Canvas.Splash.MaxRadiusPx, 56);
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
