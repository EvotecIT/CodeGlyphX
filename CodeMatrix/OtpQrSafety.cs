using System;
using CodeMatrix.Rendering.Png;

namespace CodeMatrix;

/// <summary>
/// Safety checks for OTP QR rendering parameters.
/// </summary>
public static class OtpQrSafety {
    /// <summary>
    /// Recommended quiet zone size in modules.
    /// </summary>
    public const int RecommendedQuietZone = 4;
    /// <summary>
    /// Recommended module size in pixels.
    /// </summary>
    public const int RecommendedModuleSize = 6;
    /// <summary>
    /// Minimum recommended contrast ratio between foreground and background.
    /// </summary>
    public const double MinimumContrastRatio = 4.0;

    /// <summary>
    /// Computes a contrast ratio between two colors (1.0..21.0).
    /// </summary>
    public static double GetContrastRatio(Rgba32 foreground, Rgba32 background) {
        var bg = background;
        if (bg.A < 255) {
            bg = BlendSrgb(bg, Rgba32.White);
        }

        var fg = foreground.A < 255 ? BlendSrgb(foreground, bg) : foreground;
        var l1 = GetRelativeLuminance(fg);
        var l2 = GetRelativeLuminance(bg);
        if (l1 < l2) (l1, l2) = (l2, l1);
        return (l1 + 0.05) / (l2 + 0.05);
    }

    /// <summary>
    /// Evaluates OTP scan safety for a rendered QR code.
    /// </summary>
    public static OtpQrSafetyReport Evaluate(QrCode qr, QrPngRenderOptions opts, bool requireHighEcc = true) {
        if (qr is null) throw new ArgumentNullException(nameof(qr));
        if (opts is null) throw new ArgumentNullException(nameof(opts));

        var contrast = GetContrastRatio(opts.Foreground, opts.Background);
        var hasGradient = opts.ForegroundGradient is not null;
        if (hasGradient) {
            var grad = opts.ForegroundGradient!;
            var c1 = GetContrastRatio(grad.StartColor, opts.Background);
            var c2 = GetContrastRatio(grad.EndColor, opts.Background);
            contrast = Math.Min(c1, c2);
        }
        var hasContrast = contrast >= MinimumContrastRatio;
        var hasQuietZone = opts.QuietZone >= RecommendedQuietZone;
        var hasModuleSize = opts.ModuleSize >= 4;
        var hasOpaque = opts.Foreground.A == 255 && opts.Background.A == 255;
        var hasEcc = requireHighEcc ? qr.ErrorCorrectionLevel == QrErrorCorrectionLevel.H : qr.ErrorCorrectionLevel >= QrErrorCorrectionLevel.M;

        var issues = new System.Collections.Generic.List<string>(5);
        var score = 100;

        if (!hasContrast) {
            score -= 40;
            issues.Add("Increase foreground/background contrast.");
        } else if (contrast < 7.0) {
            score -= 10;
            issues.Add("Contrast is acceptable but could be higher.");
        }

        if (!hasQuietZone) {
            score -= 20;
            issues.Add($"Use at least {RecommendedQuietZone} modules of quiet zone.");
        }

        if (!hasModuleSize) {
            score -= 20;
            issues.Add("Increase module size to at least 4px.");
        }

        if (!hasOpaque) {
            score -= 10;
            issues.Add("Avoid transparent foreground/background.");
        }

        if (!hasEcc) {
            score -= 20;
            issues.Add("Use error correction level H for OTP.");
        }

        if (hasGradient) {
            score -= 10;
            issues.Add("Avoid gradient foreground for OTP (reliability risk).");
        }

        if (score < 0) score = 0;

        return new OtpQrSafetyReport(
            contrast,
            hasContrast,
            hasQuietZone,
            hasModuleSize,
            hasOpaque,
            hasEcc,
            RecommendedModuleSize,
            RecommendedQuietZone,
            score,
            issues.ToArray());
    }

    private static double GetRelativeLuminance(Rgba32 color) {
        var r = SrgbToLinear(color.R / 255.0);
        var g = SrgbToLinear(color.G / 255.0);
        var b = SrgbToLinear(color.B / 255.0);
        return 0.2126 * r + 0.7152 * g + 0.0722 * b;
    }

    private static double SrgbToLinear(double c) {
        return c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
    }

    private static Rgba32 BlendSrgb(Rgba32 fg, Rgba32 bg) {
        var a = fg.A / 255.0;
        var r = (byte)Math.Round(((fg.R / 255.0) * a + (bg.R / 255.0) * (1 - a)) * 255.0);
        var g = (byte)Math.Round(((fg.G / 255.0) * a + (bg.G / 255.0) * (1 - a)) * 255.0);
        var b = (byte)Math.Round(((fg.B / 255.0) * a + (bg.B / 255.0) * (1 - a)) * 255.0);
        return new Rgba32(r, g, b, 255);
    }
}
