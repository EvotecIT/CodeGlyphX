namespace CodeGlyphX.Examples;

/// <summary>Small deterministic illustrations used as input, separate from the QR rendering engine.</summary>
internal static class QrArtIllustrations {
    private static readonly (double X, double Y, double Radius)[] CitrusCenters = { (0.2, 0.22, 0.2), (0.73, 0.31, 0.23), (0.42, 0.75, 0.27) };
    internal static byte[] Draw(string scene, int size) {
        var pixels = new byte[size * size * 4];
        for (var y = 0; y < size; y++) for (var x = 0; x < size; x++) {
            var u = x / (double)size;
            var v = y / (double)size;
            var color = scene switch {
                "botanical" => Botanical(u, v),
                "citrus" => Citrus(u, v),
                "landscape" => Landscape(u, v),
                "waves" => Waves(u, v),
                _ => Geometric(u, v),
            };
            var p = (y * size + x) * 4;
            pixels[p] = color.R;
            pixels[p + 1] = color.G;
            pixels[p + 2] = color.B;
            pixels[p + 3] = 255;
        }
        return pixels;
    }

    private static (byte R, byte G, byte B) Botanical(double x, double y) {
        (byte, byte, byte) color = (234, 229, 204);
        for (var branch = 0; branch < 3; branch++) {
            var stem = 0.23 + branch * 0.29 + Math.Sin(y * 5 + branch) * 0.055;
            if (Math.Abs(x - stem) < 0.005 && y > 0.12) color = (53, 85, 64);
            for (var leaf = 0; leaf < 7; leaf++) {
                var cy = 0.16 + leaf * 0.115;
                var side = leaf % 2 == 0 ? 1 : -1;
                var dx = x - stem - side * 0.065;
                var dy = y - cy;
                var a = dx * 0.65 + dy * side * 0.76;
                var b = -dx * side * 0.76 + dy * 0.65;
                if (a * a / 0.017 + b * b / 0.0023 < 1) {
                    color = (leaf + branch) % 3 == 0 ? ((byte)159, (byte)172, (byte)111) : ((byte)58, (byte)107, (byte)79);
                    if (Math.Abs(b) < 0.002) color = (192, 193, 139);
                }
            }
        }
        return color;
    }

    private static (byte R, byte G, byte B) Citrus(double x, double y) {
        (byte, byte, byte) color = (246, 214, 172);
        foreach (var (cx, cy, radius) in CitrusCenters) {
            var dx = x - cx;
            var dy = y - cy;
            var distance = Math.Sqrt(dx * dx + dy * dy);
            if (distance < radius) {
                color = (222, 88, 34);
                if (distance < radius * 0.92) color = (255, 228, 161);
                if (distance < radius * 0.83) {
                    color = (239, 140, 48);
                    var angle = Math.Atan2(dy, dx);
                    if (Math.Abs(Math.Sin(angle * 5)) < 0.09 || distance < radius * 0.1) color = (255, 229, 160);
                }
            }
            if (Math.Pow((dx - radius * 0.6) / 0.11, 2) + Math.Pow((dy + radius) / 0.045, 2) < 1) color = (45, 99, 64);
        }
        return color;
    }

    private static (byte R, byte G, byte B) Landscape(double x, double y) {
        (byte, byte, byte) color = ((byte)(221 + 26 * y), (byte)(174 + 33 * y), (byte)(158 + 12 * y));
        if (Math.Pow(x - 0.7, 2) + Math.Pow(y - 0.26, 2) < 0.018) color = (255, 224, 153);
        if (y > 0.42 + 0.1 * Math.Sin(x * 7)) color = (173, 144, 166);
        if (y > 0.6 + 0.1 * Math.Sin(x * 9 + 2)) color = (91, 117, 143);
        if (y > 0.77 + 0.07 * Math.Sin(x * 11)) color = (43, 79, 98);
        if (y > 0.91 + 0.03 * Math.Sin(x * 13)) color = (27, 55, 68);
        return color;
    }

    private static (byte R, byte G, byte B) Waves(double x, double y) {
        var t = (y + 0.055 * Math.Sin(x * 12 + y * 7)) * 8;
        var band = (int)Math.Floor(t);
        if (t - band < 0.035) return (247, 230, 190);
        return (band % 4) switch { 0 => (17, 74, 100), 1 => (28, 135, 151), 2 => (110, 186, 177), _ => (231, 211, 168) };
    }

    private static (byte R, byte G, byte B) Geometric(double x, double y) {
        (byte, byte, byte) color = (240, 223, 194);
        if (x > 0.52 && y < 0.52) color = (197, 85, 63);
        if (x < 0.52 && y > 0.52) color = (41, 85, 98);
        if (Math.Pow(x - 0.27, 2) + Math.Pow(y - 0.27, 2) < 0.042) color = (214, 158, 75);
        if (Math.Pow(x - 0.75, 2) + Math.Pow(y - 0.76, 2) < 0.047) color = (107, 71, 101);
        if (Math.Abs(x - y) < 0.012) color = (244, 233, 210);
        return color;
    }
}
