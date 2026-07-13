using System.Linq;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class QrArtHeuristicTests {
    [Fact]
    public void Conservative_Art_Themes_Pass_Static_Heuristics() {
        const string payload = "https://example.com/scan-heuristics";
        var arts = new[] {
            ("NeonGlow", QrArt.Theme(QrArtTheme.NeonGlow, QrArtVariant.Conservative, intensity: 55)),
            ("LiquidGlass", QrArt.Theme(QrArtTheme.LiquidGlass, QrArtVariant.Conservative, intensity: 55)),
            ("ConnectedSquircleGlow", QrArt.Theme(QrArtTheme.ConnectedSquircleGlow, QrArtVariant.Conservative, intensity: 55)),
            ("CutCornerTech", QrArt.Theme(QrArtTheme.CutCornerTech, QrArtVariant.Conservative, intensity: 55)),
            ("InsetRings", QrArt.Theme(QrArtTheme.InsetRings, QrArtVariant.Conservative, intensity: 55)),
            ("StripeEyes", QrArt.Theme(QrArtTheme.StripeEyes, QrArtVariant.Conservative, intensity: 60)),
            ("PaintSplash", QrArt.Theme(QrArtTheme.PaintSplash, QrArtVariant.Conservative, intensity: 55)),
            ("PaintSplashPastel", QrArt.Theme(QrArtTheme.PaintSplash, QrArtVariant.Pastel, intensity: 58)),
        };

        foreach (var (name, art) in arts) {
            var report = QrEasy.EvaluateScanHeuristics(payload, new QrEasyOptions { Art = art });
            var warnings = string.Join(", ", report.Warnings.Select(w => w.Kind));

            Assert.True(report.PassesHeuristics, $"{name} should pass the static checks (score={report.Score}, warnings={warnings}).");
            Assert.True(report.Score >= 80, $"{name} should score at least 80 (score={report.Score}, warnings={warnings}).");
            Assert.DoesNotContain(report.Warnings, warning =>
                warning.Kind == QrArtWarningKind.LowContrast ||
                warning.Kind == QrArtWarningKind.LowContrastGradient ||
                warning.Kind == QrArtWarningKind.LowContrastPalette ||
                warning.Kind == QrArtWarningKind.ModuleScaleTooSmall ||
                warning.Kind == QrArtWarningKind.QuietZoneTooSmall);
        }
    }
}
