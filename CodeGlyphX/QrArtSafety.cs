using System;
using System.Collections.Generic;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX;

/// <summary>
/// Warning kinds for QR art safety evaluation.
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
/// Safety score and warnings for QR art/custom styling.
/// </summary>
public sealed class QrArtSafetyReport {
    /// <summary>
    /// Safety score (0..100).
    /// </summary>
    public int Score { get; }

    /// <summary>
    /// Warnings detected for the render options.
    /// </summary>
    public QrArtWarning[] Warnings { get; }

    /// <summary>
    /// Indicates whether the configuration is likely safe for scanning.
    /// </summary>
    public bool IsSafe => Score >= 70;

    internal QrArtSafetyReport(int score, QrArtWarning[] warnings) {
        Score = Math.Min(Math.Max(score, 0), 100);
        Warnings = warnings ?? Array.Empty<QrArtWarning>();
    }
}

/// <summary>
/// Evaluates QR art/render options for scan safety.
/// </summary>
public static class QrArtSafety {
    /// <summary>
    /// Evaluates a QR render configuration.
    /// </summary>
    public static QrArtSafetyReport Evaluate(QrCode qr, QrPngRenderOptions options) {
        if (qr is null) throw new ArgumentNullException(nameof(qr));
        if (options is null) throw new ArgumentNullException(nameof(options));

        var score = 100;
        var warnings = new List<QrArtWarning>(6);

        void Add(QrArtWarningKind kind, string message, int penalty) {
            warnings.Add(new QrArtWarning(kind, message));
            score -= penalty;
        }

        if (options.QuietZone < 4) {
            Add(QrArtWarningKind.QuietZoneTooSmall, "Quiet zone below 4 modules can reduce scan reliability.", 20);
        }

        if (options.ModuleSize < 3) {
            Add(QrArtWarningKind.ModuleSizeTooSmall, "Module size below 3px can be hard for cameras to resolve.", 10);
        }

        var minScale = options.ModuleScale;
        if (options.ModuleScaleMap is not null) {
            var mapMin = Math.Min(options.ModuleScaleMap.MinScale, options.ModuleScaleMap.MaxScale);
            minScale *= mapMin;
        }
        if (minScale < 0.8) {
            Add(QrArtWarningKind.ModuleScaleTooSmall, "Module scale below 0.8 can weaken timing/finder clarity.", 10);
        }

        var hasDecorativeModules = options.ModuleShape != QrPngModuleShape.Square
            || options.ModuleScale < 1.0
            || options.ModuleScaleMap is not null
            || options.ForegroundGradient is not null
            || options.ForegroundPalette is not null
            || options.ForegroundPaletteZones is not null
            || options.Eyes is not null;

        if (!options.ProtectFunctionalPatterns && hasDecorativeModules) {
            Add(QrArtWarningKind.FunctionalPatternsUnprotected, "Decorative styling without functional pattern protection can reduce scan reliability.", 15);
        }

        if (options.BackgroundPattern is not null && options.QuietZone > 0 && !options.ProtectQuietZone) {
            Add(QrArtWarningKind.QuietZonePatterned, "Background patterns should avoid the quiet zone to preserve scan detection.", 20);
        }

        var bg = options.BackgroundGradient is null
            ? new[] { options.Background }
            : new[] { options.BackgroundGradient.StartColor, options.BackgroundGradient.EndColor };

        if (options.ForegroundPalette is not null || options.ForegroundPaletteZones is not null) {
            var palettes = CollectPalettes(options.ForegroundPalette, options.ForegroundPaletteZones);
            var min = MinContrast(palettes, bg);
            if (min < 4.5) {
                Add(QrArtWarningKind.LowContrastPalette, "Palette includes low-contrast colors against the background.", 30);
            }
        } else if (options.ForegroundGradient is null) {
            var contrast = MinContrast(new[] { options.Foreground }, bg);
            if (contrast < 4.5) {
                Add(QrArtWarningKind.LowContrast, $"Foreground/background contrast {contrast:0.00} is low.", 30);
            }
        } else {
            var min = MinContrast(new[] { options.ForegroundGradient.StartColor, options.ForegroundGradient.EndColor }, bg);
            if (min < 4.5) {
                Add(QrArtWarningKind.LowContrastGradient, "Gradient includes low-contrast colors against the background.", 30);
            }
        }

        if (options.Logo is not null) {
            var area = options.Logo.Scale * options.Logo.Scale;
            if (options.Logo.DrawBackground && options.Logo.PaddingPx > 0 && options.ModuleSize > 0) {
                var qrSizePx = qr.Size * options.ModuleSize;
                if (qrSizePx > 0) {
                    var logoPx = qrSizePx * options.Logo.Scale;
                    var paddedPx = logoPx + options.Logo.PaddingPx * 2;
                    var effectiveScale = paddedPx / qrSizePx;
                    area = Math.Max(area, effectiveScale * effectiveScale);
                }
            }
            if (area > 0.25) {
                Add(QrArtWarningKind.LogoTooLarge, "Logo covers too much of the QR area (>25%).", 35);
            } else if (area > 0.15) {
                Add(QrArtWarningKind.LogoTooLarge, "Logo covers a large portion of the QR area (>15%).", 20);
            }

            if (qr.ErrorCorrectionLevel != QrErrorCorrectionLevel.H) {
                Add(QrArtWarningKind.LogoNeedsHighEcc, "Logo overlays work best with error correction level H.", 15);
            }

            if (options.Logo.DrawBackground && qr.Version < 8) {
                Add(QrArtWarningKind.LogoTooLarge, "Logo background plates are risky on small versions; consider a higher version or disable the background.", 15);
            }
        }

        return new QrArtSafetyReport(score, warnings.ToArray());
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
                var contrast = ContrastRatio(foregrounds[i], backgrounds[j]);
                if (contrast < min) min = contrast;
            }
        }
        return min;
    }

    private static Rgba32[] CollectPalettes(QrPngPaletteOptions? basePalette, QrPngPaletteZoneOptions? zones) {
        var count = 0;
        if (basePalette is not null) count += basePalette.Colors.Length;
        if (zones?.CenterPalette is not null) count += zones.CenterPalette.Colors.Length;
        if (zones?.CornerPalette is not null) count += zones.CornerPalette.Colors.Length;
        if (count == 0) return Array.Empty<Rgba32>();

        var colors = new Rgba32[count];
        var offset = 0;
        if (basePalette is not null) {
            Array.Copy(basePalette.Colors, 0, colors, offset, basePalette.Colors.Length);
            offset += basePalette.Colors.Length;
        }
        if (zones?.CenterPalette is not null) {
            Array.Copy(zones.CenterPalette.Colors, 0, colors, offset, zones.CenterPalette.Colors.Length);
            offset += zones.CenterPalette.Colors.Length;
        }
        if (zones?.CornerPalette is not null) {
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
}
