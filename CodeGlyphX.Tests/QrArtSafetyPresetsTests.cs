using System.Linq;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class QrArtSafetyPresetsTests {
    [Fact]
    public void Art_Presets_Are_Scan_Safe_By_Default() {
        var payload = "https://example.com/scan-safe";
#pragma warning disable CS0618 // QrArtPresets is deprecated in favor of QrArt.Theme + QrEasyOptions.Art.
        var presets = new[] {
            ("NeonGlowSafe", QrArtPresets.NeonGlowSafe()),
            ("LiquidGlassSafe", QrArtPresets.LiquidGlassSafe()),
            ("ConnectedSquircleGlowSafe", QrArtPresets.ConnectedSquircleGlowSafe()),
            ("CutCornerTechSafe", QrArtPresets.CutCornerTechSafe()),
            ("InsetRingsSafe", QrArtPresets.InsetRingsSafe()),
            ("StripeEyesSafe", QrArtPresets.StripeEyesSafe()),
            ("PaintSplashSafe", QrArtPresets.PaintSplashSafe()),
            ("PaintSplashPastelSafe", QrArtPresets.PaintSplashPastelSafe()),
        };
#pragma warning restore CS0618

        foreach (var (name, preset) in presets) {
            var report = QrEasy.EvaluateSafety(payload, preset);
            var warnings = string.Join(", ", report.Warnings.Select(w => w.Kind));

            Assert.True(report.IsSafe, $"{name} should be scan safe (score={report.Score}, warnings={warnings}).");
            Assert.True(report.Score >= 80, $"{name} should score at least 80 (score={report.Score}, warnings={warnings}).");

            var hasCriticalWarnings = report.Warnings.Any(w =>
                w.Kind == QrArtWarningKind.LowContrast ||
                w.Kind == QrArtWarningKind.LowContrastGradient ||
                w.Kind == QrArtWarningKind.LowContrastPalette ||
                w.Kind == QrArtWarningKind.ModuleScaleTooSmall ||
                w.Kind == QrArtWarningKind.QuietZoneTooSmall);

            Assert.False(hasCriticalWarnings, $"{name} reported critical warnings: {warnings}.");
        }
    }

    [Fact]
    public void Art_Api_Safe_Mode_Is_Scan_Safe() {
        var payload = "https://example.com/art-api";
        var arts = new[] {
            ("NeonGlow", QrArt.Theme(QrArtTheme.NeonGlow, QrArtVariant.Safe, intensity: 55)),
            ("StripeEyes", QrArt.Theme(QrArtTheme.StripeEyes, QrArtVariant.Safe, intensity: 60)),
            ("PaintSplashPastel", QrArt.Theme(QrArtTheme.PaintSplash, QrArtVariant.Pastel, intensity: 58)),
        };

        foreach (var (name, art) in arts) {
            var opts = new QrEasyOptions { Art = art };
            var report = QrEasy.EvaluateSafety(payload, opts);
            var warnings = string.Join(", ", report.Warnings.Select(w => w.Kind));
            Assert.True(report.IsSafe, $"{name} art should be scan safe (score={report.Score}, warnings={warnings}).");
            Assert.True(report.Score >= 80, $"{name} art should score at least 80 (score={report.Score}, warnings={warnings}).");
        }
    }
}
