using CodeGlyphX.Rendering.Art;

namespace CodeGlyphX.Examples;

/// <summary>Supplied illustrations become QR artwork through the shared composition API.</summary>
internal static class QrImageCompositionExample {
    public static void Run(string outputDir) {
        const string payload = "https://example.com/art"; // NOSONAR - reserved example URI.
        var dir = Path.Combine(outputDir, "qr-image-composition");
        Directory.CreateDirectory(dir);
        var qr = QR.Encode(payload, new QrEasyOptions { ErrorCorrectionLevel = QrErrorCorrectionLevel.H });
        foreach (var scene in new[] { "sunset", "botanical", "waves" }) {
            var pixels = DrawIllustration(scene, 512);
            foreach (var style in new[] { QrImageCompositionStyle.ColorModules, QrImageCompositionStyle.ImageOverlay }) {
                var composition = QrImageComposer.Render(qr, pixels, 512, 512,
                    new QrImageCompositionOptions { Style = style, Strength = 1, ModuleSize = 16 });
                var png = composition.ToPng();
                File.WriteAllBytes(Path.Combine(dir, $"{scene}-{style}.png"), png);
                var report = QrArt.ValidateImage(png, payload, budgetMilliseconds: 3000);
                Console.WriteLine($"{scene}/{style}: " + string.Join(", ", report.Checks.Select(c => $"{c.Name}={c.Passed}")));
                if (!report.AllPassed) throw new InvalidOperationException($"Rendered image failed validation: {scene}/{style}");
            }
        }
    }

    // Deterministic sample artwork, generated locally; replace with your own photo/illustration.
    private static byte[] DrawIllustration(string scene, int size) {
        var pixels = new byte[size * size * 4];
        for (var y = 0; y < size; y++) for (var x = 0; x < size; x++) {
            var u = x / (double)size;
            var v = y / (double)size;
            (byte R, byte G, byte B) color;
            if (scene == "sunset") {
                color = ((byte)(230 + 20 * v), (byte)(100 + 80 * v), (byte)(115 - 40 * v));
                if (Math.Pow(u - 0.62, 2) + Math.Pow(v - 0.35, 2) < 0.035) color = (255, 228, 125);
                if (v > 0.58 + 0.09 * Math.Sin(u * 9)) color = (127, 85, 135);
                if (v > 0.73 + 0.08 * Math.Sin(u * 12 + 2)) color = (48, 83, 117);
                if (v > 0.86 + 0.05 * Math.Sin(u * 8)) color = (25, 59, 83);
            } else if (scene == "botanical") {
                color = (245, 231, 203);
                var stem = 0.5 + 0.05 * Math.Sin(v * 7);
                if (Math.Abs(u - stem) < 0.008) color = (44, 96, 69);
                for (var leaf = 0; leaf < 6; leaf++) {
                    var cy = 0.22 + leaf * 0.12;
                    var direction = leaf % 2 == 0 ? 1 : -1;
                    var cx = 0.5 + direction * 0.14;
                    var dx = u - cx;
                    var dy = v - cy;
                    var a = dx * 0.85 + dy * direction * 0.53;
                    var b = -dx * direction * 0.53 + dy * 0.85;
                    if (a * a / 0.035 + b * b / 0.005 < 1) color = leaf % 2 == 0 ? ((byte)45, (byte)119, (byte)80) : ((byte)104, (byte)151, (byte)91);
                }
            } else {
                var band = (int)((v + 0.07 * Math.Sin(u * 10 + v * 5)) * 9);
                color = (band % 4) switch { 0 => (22, 74, 105), 1 => (28, 139, 160), 2 => (102, 198, 190), _ => (237, 217, 162) };
            }
            var p = (y * size + x) * 4;
            pixels[p] = color.R;
            pixels[p + 1] = color.G;
            pixels[p + 2] = color.B;
            pixels[p + 3] = 255;
        }
        return pixels;
    }
}
