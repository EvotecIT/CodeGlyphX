using System.Linq;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class QrArtSafetyPresetsTests {
    [Fact]
    public void Art_Presets_Are_Scan_Safe_By_Default() {
        var payload = "https://example.com/scan-safe";
        var presets = new[] {
            ("NeonGlowSafe", QrArtPresets.NeonGlowSafe()),
            ("LiquidGlassSafe", QrArtPresets.LiquidGlassSafe()),
            ("ConnectedSquircleGlowSafe", QrArtPresets.ConnectedSquircleGlowSafe()),
            ("CutCornerTechSafe", QrArtPresets.CutCornerTechSafe()),
            ("InsetRingsSafe", QrArtPresets.InsetRingsSafe()),
            ("StripeEyesSafe", QrArtPresets.StripeEyesSafe()),
            ("PaintSplashSafe", QrArtPresets.PaintSplashSafe()),
        };

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
}
