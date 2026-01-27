using System.IO;
using CodeGlyphX;
using CodeGlyphX.Payloads;

namespace CodeGlyphX.Examples;

internal static class QrArtPresetsExample {
    public static void Run(string outputDir) {
        var payload = QrPayload.Url("https://example.com/qr/art-presets");
        var dir = Path.Combine(outputDir, "qr-art-presets");
        Directory.CreateDirectory(dir);

        Save(dir, payload, "art-neon-glow.png", QrArtPresets.NeonGlow());
        Save(dir, payload, "art-liquid-glass.png", QrArtPresets.LiquidGlass());
        Save(dir, payload, "art-connected-squircle-glow.png", QrArtPresets.ConnectedSquircleGlow());
        Save(dir, payload, "art-cut-corner-tech.png", QrArtPresets.CutCornerTech());
        Save(dir, payload, "art-inset-rings.png", QrArtPresets.InsetRings());
    }

    private static void Save(string dir, string payload, string fileName, QrEasyOptions options) {
        QR.Save(payload, Path.Combine(dir, fileName), options);
    }
}
