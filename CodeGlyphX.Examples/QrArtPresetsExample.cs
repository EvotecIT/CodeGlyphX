using System.IO;
using CodeGlyphX;
using CodeGlyphX.Payloads;

namespace CodeGlyphX.Examples;

internal static class QrArtPresetsExample {
    public static void Run(string outputDir) {
        var payload = QrPayload.Url("https://example.com/qr/art-presets");
        var dir = Path.Combine(outputDir, "qr-art-presets");
        Directory.CreateDirectory(dir);

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
    }

    private static void Save(string dir, string payload, string fileName, QrEasyOptions options) {
        QR.Save(payload, Path.Combine(dir, fileName), options);
    }
}
