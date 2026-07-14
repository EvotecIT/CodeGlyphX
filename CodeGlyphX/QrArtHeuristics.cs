using CodeGlyphX.Rendering.Png;
using System;
using System.Collections.Generic;

namespace CodeGlyphX;

/// <summary>
/// Warning kinds for static QR art heuristic evaluation.
/// </summary>
public enum QrArtWarningKind {
    /// <summary>
    /// Quiet zone is below the recommended size (4 modules).
    /// </summary>
    QuietZoneTooSmall,
    /// <summary>
    /// Module size is too small for reliable scanning.
    /// </summary>
    ModuleSizeTooSmall,
    /// <summary>
    /// Contrast between foreground and background is too low.
    /// </summary>
    LowContrast,
    /// <summary>
    /// Gradient includes colors with low contrast against the background.
    /// </summary>
    LowContrastGradient,
    /// <summary>
    /// Palette includes colors with low contrast against the background.
    /// </summary>
    LowContrastPalette,
    /// <summary>
    /// Module scale is too small (excessive rounding or shrink).
    /// </summary>
    ModuleScaleTooSmall,
    /// <summary>
    /// Functional patterns are not protected while decorative styling is enabled.
    /// </summary>
    FunctionalPatternsUnprotected,
    /// <summary>
    /// Background pattern may be drawn into the quiet zone.
    /// </summary>
    QuietZonePatterned,
    /// <summary>
    /// Logo covers too much of the QR area.
    /// </summary>
    LogoTooLarge,
    /// <summary>
    /// Logo is used with low error correction.
    /// </summary>
    LogoNeedsHighEcc
}

/// <summary>
/// QR art warning with a short message.
/// </summary>
public readonly struct QrArtWarning {
    /// <summary>
    /// Warning kind.
    /// </summary>
    public QrArtWarningKind Kind { get; }

    /// <summary>
    /// Warning message.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Creates a warning entry.
    /// </summary>
    /// <param name="kind">Warning category.</param>
    /// <param name="message">Human-readable warning message.</param>
    public QrArtWarning(QrArtWarningKind kind, string message) {
        Kind = kind;
        Message = message ?? throw new ArgumentNullException(nameof(message));
    }
}

/// <summary>
/// Static heuristic score and warnings for QR art/custom styling.
/// This report does not decode the rendered output and cannot guarantee scanner interoperability.
/// </summary>
public sealed class QrArtHeuristicReport {
    /// <summary>
    /// Heuristic score (0..100).
    /// </summary>
    public int Score { get; }

    /// <summary>
    /// Warnings detected for the render options.
    /// </summary>
    public QrArtWarning[] Warnings { get; }

    /// <summary>
    /// Indicates whether the configuration passes the library's static checks.
    /// Passing does not guarantee that a rendered QR code will scan.
    /// </summary>
    public bool PassesHeuristics => Score >= 70;

    internal QrArtHeuristicReport(int score, QrArtWarning[] warnings) {
        Score = Math.Min(Math.Max(score, 0), 100);
        Warnings = warnings ?? Array.Empty<QrArtWarning>();
    }
}

/// <summary>
/// Evaluates QR art/render options using static heuristics.
/// It does not render or decode the result; validate final artifacts with target scanners.
/// </summary>
public static class QrArtHeuristics {
    /// <summary>
    /// Evaluates a QR render configuration.
    /// </summary>
    public static QrArtHeuristicReport Evaluate(QrCode qr, QrPngRenderOptions options) {
        if (qr is null) throw new ArgumentNullException(nameof(qr));
        if (options is null) throw new ArgumentNullException(nameof(options));

        var evaluation = new QrArtEvaluation();
        EvaluateLayout(options, evaluation);
        EvaluateContrast(options, evaluation);
        EvaluateLogo(qr, options, evaluation);
        return evaluation.ToReport();
    }

    private static void EvaluateLayout(QrPngRenderOptions options, QrArtEvaluation evaluation) {
        if (options.QuietZone < 4) {
            evaluation.Add(QrArtWarningKind.QuietZoneTooSmall, "Quiet zone below 4 modules can reduce scan reliability.", 20);
        }

        if (options.ModuleSize < 3) {
            evaluation.Add(QrArtWarningKind.ModuleSizeTooSmall, "Module size below 3px can be hard for cameras to resolve.", 10);
        }

        if (GetMinimumScale(options) < 0.8) {
            evaluation.Add(QrArtWarningKind.ModuleScaleTooSmall, "Module scale below 0.8 can weaken timing/finder clarity.", 10);
        }

        if (!options.ProtectFunctionalPatterns && HasDecorativeModules(options)) {
            evaluation.Add(QrArtWarningKind.FunctionalPatternsUnprotected, "Decorative styling without functional pattern protection can reduce scan reliability.", 15);
        }

        if (options.BackgroundPattern is not null && options.QuietZone > 0 && !options.ProtectQuietZone) {
            evaluation.Add(QrArtWarningKind.QuietZonePatterned, "Background patterns should avoid the quiet zone to preserve scan detection.", 20);
        }
    }

    private static double GetMinimumScale(QrPngRenderOptions options) {
        if (options.ModuleScaleMap is null) return options.ModuleScale;
        return options.ModuleScale * Math.Min(options.ModuleScaleMap.MinScale, options.ModuleScaleMap.MaxScale);
    }

    private static bool HasDecorativeModules(QrPngRenderOptions options) {
        return options.ModuleShape != QrPngModuleShape.Square
            || options.ModuleScale < 1.0
            || options.ModuleScaleMap is not null
            || options.ModuleShapeMap is not null
            || options.ModuleJitter is not null
            || options.ForegroundGradient is not null
            || options.ForegroundPalette is not null
            || options.ForegroundPattern is not null
            || options.ForegroundPaletteZones is not null
            || options.Eyes is not null;
    }

    private static void EvaluateContrast(QrPngRenderOptions options, QrArtEvaluation evaluation) {
        var bg = options.BackgroundGradient is null
            ? new[] { options.Background }
            : new[] { options.BackgroundGradient.StartColor, options.BackgroundGradient.EndColor };
        var pattern = options.ForegroundPattern;
        var patternActive = pattern is not null && pattern.ThicknessPx > 0 && (pattern.BlendMode == QrPngForegroundPatternBlendMode.Mask || pattern.Color.A != 0);

        if (options.ForegroundPalette is not null || options.ForegroundPaletteZones is not null) {
            EvaluatePaletteContrast(options, evaluation, bg, pattern, patternActive);
            return;
        }

        if (options.ForegroundGradient is null) {
            EvaluateSolidContrast(options, evaluation, bg, pattern, patternActive);
            return;
        }

        EvaluateGradientContrast(options, evaluation, bg, pattern, patternActive);
    }

    private static void EvaluatePaletteContrast(QrPngRenderOptions options, QrArtEvaluation evaluation, Rgba32[] backgrounds, QrPngForegroundPatternOptions? pattern, bool patternActive) {
        var palettes = CollectPalettes(options.Foreground, options.ForegroundPalette, options.ForegroundPaletteZones);
        var colors = patternActive ? AddPatternVariants(palettes, pattern!) : palettes;
        if (MinContrast(colors, backgrounds) < 4.5) {
            evaluation.Add(QrArtWarningKind.LowContrastPalette, "Palette includes low-contrast colors against the background.", 30);
        }
    }

    private static void EvaluateSolidContrast(QrPngRenderOptions options, QrArtEvaluation evaluation, Rgba32[] backgrounds, QrPngForegroundPatternOptions? pattern, bool patternActive) {
        var colors = patternActive
            ? new[] { options.Foreground, Rgba32Compositor.ComposeOver(pattern!.Color, options.Foreground) }
            : new[] { options.Foreground };
        var contrast = MinContrast(colors, backgrounds);
        if (contrast < 4.5) {
            evaluation.Add(QrArtWarningKind.LowContrast, $"Foreground/background contrast {contrast:0.00} is low.", 30);
        }
    }

    private static void EvaluateGradientContrast(QrPngRenderOptions options, QrArtEvaluation evaluation, Rgba32[] backgrounds, QrPngForegroundPatternOptions? pattern, bool patternActive) {
        var gradient = options.ForegroundGradient!;
        var gradientColors = new[] { gradient.StartColor, gradient.EndColor };
        var colors = patternActive ? AddPatternVariants(gradientColors, pattern!) : gradientColors;
        if (MinContrast(colors, backgrounds) < 4.5) {
            evaluation.Add(QrArtWarningKind.LowContrastGradient, "Gradient includes low-contrast colors against the background.", 30);
        }
    }

    private static void EvaluateLogo(QrCode qr, QrPngRenderOptions options, QrArtEvaluation evaluation) {
        if (options.Logo is null) return;

        var area = GetEffectiveLogoArea(qr, options);
        if (area > 0.25) {
            evaluation.Add(QrArtWarningKind.LogoTooLarge, "Logo covers too much of the QR area (>25%).", 35);
        } else if (area > 0.15) {
            evaluation.Add(QrArtWarningKind.LogoTooLarge, "Logo covers a large portion of the QR area (>15%).", 20);
        }

        if (qr.ErrorCorrectionLevel != QrErrorCorrectionLevel.H) {
            evaluation.Add(QrArtWarningKind.LogoNeedsHighEcc, "Logo overlays work best with error correction level H.", 15);
        }

        if (options.Logo.DrawBackground && qr.Version < 8) {
            evaluation.Add(QrArtWarningKind.LogoTooLarge, "Logo background plates are risky on small versions; consider a higher version or disable the background.", 15);
        }
    }

    private static double GetEffectiveLogoArea(QrCode qr, QrPngRenderOptions options) {
        var logo = options.Logo!;
        var area = logo.Scale * logo.Scale;
        if (!logo.DrawBackground || logo.PaddingPx <= 0 || options.ModuleSize <= 0) return area;

        var qrSizePx = qr.Size * options.ModuleSize;
        if (qrSizePx <= 0) return area;

        var paddedPx = qrSizePx * logo.Scale + logo.PaddingPx * 2;
        var effectiveScale = paddedPx / qrSizePx;
        return Math.Max(area, effectiveScale * effectiveScale);
    }

    private static double ContrastRatio(Rgba32 a, Rgba32 b) {
        var l1 = RelativeLuminance(a);
        var l2 = RelativeLuminance(b);
        if (l1 < l2) (l1, l2) = (l2, l1);
        return (l1 + 0.05) / (l2 + 0.05);
    }

    private static double MinContrast(Rgba32[] foregrounds, Rgba32[] backgrounds) {
        var min = double.MaxValue;
        for (var i = 0; i < foregrounds.Length; i++) {
            for (var j = 0; j < backgrounds.Length; j++) {
                var visibleForeground = Rgba32Compositor.ComposeOver(foregrounds[i], backgrounds[j]);
                var contrast = ContrastRatio(visibleForeground, backgrounds[j]);
                if (contrast < min) min = contrast;
            }
        }
        return min;
    }

    private static Rgba32[] AddPatternVariants(Rgba32[] colors, QrPngForegroundPatternOptions pattern) {
        var list = new List<Rgba32>(colors.Length * 2);
        for (var i = 0; i < colors.Length; i++) {
            var color = colors[i];
            list.Add(color);
            list.Add(Rgba32Compositor.ComposeOver(pattern.Color, color));
        }
        return list.ToArray();
    }

    private static Rgba32[] CollectPalettes(Rgba32 baseForeground, QrPngPaletteOptions? basePalette, QrPngPaletteZoneOptions? zones) {
        var count = 1;
        if (basePalette is not null) count += basePalette.Colors.Length;
        if (zones?.CenterPalette is not null && zones.CenterSize > 0) count += zones.CenterPalette.Colors.Length;
        if (zones?.CornerPalette is not null && zones.CornerSize > 0) count += zones.CornerPalette.Colors.Length;

        var colors = new Rgba32[count];
        colors[0] = baseForeground;
        var offset = 1;
        if (basePalette is not null) {
            Array.Copy(basePalette.Colors, 0, colors, offset, basePalette.Colors.Length);
            offset += basePalette.Colors.Length;
        }
        if (zones?.CenterPalette is not null && zones.CenterSize > 0) {
            Array.Copy(zones.CenterPalette.Colors, 0, colors, offset, zones.CenterPalette.Colors.Length);
            offset += zones.CenterPalette.Colors.Length;
        }
        if (zones?.CornerPalette is not null && zones.CornerSize > 0) {
            Array.Copy(zones.CornerPalette.Colors, 0, colors, offset, zones.CornerPalette.Colors.Length);
        }
        return colors;
    }

    private static double RelativeLuminance(Rgba32 c) {
        static double ToLinear(byte v) {
            var f = v / 255.0;
            return f <= 0.03928 ? f / 12.92 : Math.Pow((f + 0.055) / 1.055, 2.4);
        }

        var r = ToLinear(c.R);
        var g = ToLinear(c.G);
        var b = ToLinear(c.B);
        return 0.2126 * r + 0.7152 * g + 0.0722 * b;
    }

    private sealed class QrArtEvaluation {
        private readonly List<QrArtWarning> warnings = new(6);
        private int score = 100;

        public void Add(QrArtWarningKind kind, string message, int penalty) {
            warnings.Add(new QrArtWarning(kind, message));
            score -= penalty;
        }

        public QrArtHeuristicReport ToReport() => new(score, warnings.ToArray());
    }
}
