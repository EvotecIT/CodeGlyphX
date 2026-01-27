using System.IO;
using CodeGlyphX;
using CodeGlyphX.Payloads;

namespace CodeGlyphX.Examples;

internal static class QrArtPresetsExample {
    public static void Run(string outputDir) {
        var payload = QrPayload.Url("https://example.com/qr/art-presets");
        var dir = Path.Combine(outputDir, "qr-art-presets");
        Directory.CreateDirectory(dir);

#pragma warning disable CS0618 // QrArtPresets is deprecated in favor of QrArt.Theme + QrEasyOptions.Art.
        Save(dir, payload, "art-neon-glow-safe.png", QrArtPresets.NeonGlowSafe());
        Save(dir, payload, "art-neon-glow-bold.png", QrArtPresets.NeonGlowBold());

        Save(dir, payload, "art-liquid-glass-safe.png", QrArtPresets.LiquidGlassSafe());
        Save(dir, payload, "art-liquid-glass-bold.png", QrArtPresets.LiquidGlassBold());

        Save(dir, payload, "art-connected-squircle-glow-safe.png", QrArtPresets.ConnectedSquircleGlowSafe());
        Save(dir, payload, "art-connected-squircle-glow-bold.png", QrArtPresets.ConnectedSquircleGlowBold());

        Save(dir, payload, "art-cut-corner-tech-safe.png", QrArtPresets.CutCornerTechSafe());
        Save(dir, payload, "art-cut-corner-tech-bold.png", QrArtPresets.CutCornerTechBold());

        Save(dir, payload, "art-inset-rings-safe.png", QrArtPresets.InsetRingsSafe());
        Save(dir, payload, "art-inset-rings-bold.png", QrArtPresets.InsetRingsBold());

        Save(dir, payload, "art-stripe-eyes-safe.png", QrArtPresets.StripeEyesSafe());
        Save(dir, payload, "art-stripe-eyes-bold.png", QrArtPresets.StripeEyesBold());

        Save(dir, payload, "art-paint-splash-safe.png", QrArtPresets.PaintSplashSafe());
        Save(dir, payload, "art-paint-splash-bold.png", QrArtPresets.PaintSplashBold());

        Save(dir, payload, "art-paint-splash-pastel-safe.png", QrArtPresets.PaintSplashPastelSafe());
        Save(dir, payload, "art-paint-splash-pastel-bold.png", QrArtPresets.PaintSplashPastelBold());
#pragma warning restore CS0618

        Save(dir, payload, "art-api-neon-glow.png", new QrEasyOptions {
            Art = QrArt.Theme(QrArtTheme.NeonGlow, QrArtVariant.Safe, intensity: 60)
        });
        Save(dir, payload, "art-api-paint-splash-pastel.png", new QrEasyOptions {
            Art = QrArt.Theme(QrArtTheme.PaintSplash, QrArtVariant.Pastel, intensity: 62)
        });
    }

    private static void Save(string dir, string payload, string fileName, QrEasyOptions options) {
        QR.Save(payload, Path.Combine(dir, fileName), options);
    }
}
