using System.IO;
using CodeGlyphX;
using CodeGlyphX.Payloads;

namespace CodeGlyphX.Examples;

internal static class QrArtThemesExample {
    public static void Run(string outputDir) {
        var payload = QrPayload.Url("https://example.com/qr/art-themes");
        var dir = Path.Combine(outputDir, "qr-art-themes");
        Directory.CreateDirectory(dir);

        Save(dir, payload, "art-neon-glow-conservative.png", QrArtTheme.NeonGlow, QrArtVariant.Conservative, 60);
        Save(dir, payload, "art-liquid-glass-conservative.png", QrArtTheme.LiquidGlass, QrArtVariant.Conservative, 60);
        Save(dir, payload, "art-connected-squircle-glow-conservative.png", QrArtTheme.ConnectedSquircleGlow, QrArtVariant.Conservative, 60);
        Save(dir, payload, "art-cut-corner-tech-conservative.png", QrArtTheme.CutCornerTech, QrArtVariant.Conservative, 60);
        Save(dir, payload, "art-inset-rings-conservative.png", QrArtTheme.InsetRings, QrArtVariant.Conservative, 60);
        Save(dir, payload, "art-stripe-eyes-conservative.png", QrArtTheme.StripeEyes, QrArtVariant.Conservative, 60);
        Save(dir, payload, "art-paint-splash-conservative.png", QrArtTheme.PaintSplash, QrArtVariant.Conservative, 60);
        Save(dir, payload, "art-paint-splash-pastel.png", QrArtTheme.PaintSplash, QrArtVariant.Pastel, 62);
    }

    private static void Save(
        string dir,
        string payload,
        string fileName,
        QrArtTheme theme,
        QrArtVariant variant,
        int intensity) {
        QR.Save(payload, Path.Combine(dir, fileName), new QrEasyOptions {
            Art = QrArt.Theme(theme, variant, intensity)
        });
    }
}
