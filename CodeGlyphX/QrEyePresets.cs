using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX;

/// <summary>
/// Eye-only styling presets for QR codes.
/// </summary>
public static class QrEyePresets {
    /// <summary>
    /// Neon sparkle eyes for dark canvases.
    /// </summary>
    public static QrPngEyeOptions NeonSparkle() => new() {
        UseFrame = true,
        FrameStyle = QrPngEyeFrameStyle.Target,
        OuterShape = QrPngModuleShape.Rounded,
        InnerShape = QrPngModuleShape.Circle,
        OuterColor = new Rgba32(0, 240, 220),
        InnerColor = new Rgba32(10, 12, 22),
        SparkleCount = 36,
        SparkleRadiusPx = 3,
        SparkleProtectQrArea = true,
        SparkleColor = new Rgba32(255, 255, 255, 200),
    };

    /// <summary>
    /// Sunburst rays around eyes with safe spacing.
    /// </summary>
    public static QrPngEyeOptions Sunburst() => new() {
        UseFrame = true,
        FrameStyle = QrPngEyeFrameStyle.Badge,
        OuterShape = QrPngModuleShape.Rounded,
        InnerShape = QrPngModuleShape.Rounded,
        OuterColor = new Rgba32(36, 24, 80),
        InnerColor = new Rgba32(252, 248, 242),
        AccentRayCount = 18,
        AccentRayLengthPx = 20,
        AccentRayThicknessPx = 3,
        AccentRaySpreadPx = 28,
        AccentRayProtectQrArea = true,
        AccentRayColor = new Rgba32(255, 172, 72, 190),
    };

    /// <summary>
    /// Minimal ring eyes with subtle accents.
    /// </summary>
    public static QrPngEyeOptions MinimalRing() => new() {
        UseFrame = true,
        FrameStyle = QrPngEyeFrameStyle.InsetRing,
        OuterShape = QrPngModuleShape.Rounded,
        InnerShape = QrPngModuleShape.Rounded,
        OuterColor = new Rgba32(24, 32, 64),
        InnerColor = new Rgba32(250, 250, 250),
        InnerScale = 0.9,
        AccentRingCount = 6,
        AccentRingThicknessPx = 2,
        AccentRingProtectQrArea = true,
        AccentRingColor = new Rgba32(90, 120, 210, 180),
    };

    /// <summary>
    /// Retro target eyes with stripes.
    /// </summary>
    public static QrPngEyeOptions RetroTarget() => new() {
        UseFrame = true,
        FrameStyle = QrPngEyeFrameStyle.Target,
        OuterShape = QrPngModuleShape.Rounded,
        InnerShape = QrPngModuleShape.Rounded,
        OuterColor = new Rgba32(18, 36, 66),
        InnerColor = new Rgba32(255, 255, 255),
        AccentStripeCount = 16,
        AccentStripeLengthPx = 24,
        AccentStripeThicknessPx = 3,
        AccentStripeSpreadPx = 28,
        AccentStripeProtectQrArea = true,
        AccentStripeColor = new Rgba32(255, 111, 97, 180),
    };
}

