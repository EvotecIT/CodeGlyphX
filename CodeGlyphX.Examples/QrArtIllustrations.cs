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
                "portrait" => Portrait(u, v),
                "flower" => Flower(u, v),
                "architecture" => Architecture(u, v),
                "logo" => Logo(u, v),
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

    private static (byte R, byte G, byte B) Portrait(double x, double y) {
        (byte, byte, byte) color = (224, 210, 185);
        if (Math.Pow((x - .5) / .37, 2) + Math.Pow((y - 1) / .42, 2) < 1) color = (39, 91, 103);
        if (Math.Pow((x - .5) / .24, 2) + Math.Pow((y - .42) / .32, 2) < 1) color = (78, 48, 41);
        if (Math.Pow((x - .5) / .19, 2) + Math.Pow((y - .45) / .25, 2) < 1) color = (211, 153, 113);
        if (Math.Pow((x - .42) / .035, 2) + Math.Pow((y - .43) / .014, 2) < 1 || Math.Pow((x - .58) / .035, 2) + Math.Pow((y - .43) / .014, 2) < 1) color = (43, 39, 35);
        if (y > .46 && y < .55 && Math.Abs(x - .51) < .009) color = (148, 92, 67);
        if (Math.Abs(y - (.59 + .08 * Math.Pow((x - .5) / .08, 2))) < .008 && Math.Abs(x - .5) < .08) color = (135, 59, 54);
        return color;
    }

    private static (byte R, byte G, byte B) Flower(double x, double y) {
        (byte, byte, byte) color = (236, 226, 206);
        if (y > .45 && Math.Abs(x - .5 - .03 * Math.Sin(y * 8)) < .01) color = (49, 105, 66);
        if (Math.Pow((x - .61) / .14, 2) + Math.Pow((y - .72) / .06, 2) < 1) color = (91, 137, 73);
        var dx = x - .5; var dy = y - .38;
        var angle = Math.Atan2(dy, dx); var radius = Math.Sqrt(dx * dx + dy * dy);
        if (radius < .22 + .055 * Math.Cos(angle * 9)) color = (196, 71, 79);
        if (radius < .15 + .035 * Math.Cos(angle * 9 + 1)) color = (229, 118, 112);
        if (radius < .075) color = (211, 151, 48);
        return color;
    }

    private static (byte R, byte G, byte B) Architecture(double x, double y) {
        (byte, byte, byte) color = (177, 210, 212);
        if (y > .86) color = (180, 164, 144);
        if (x > .16 && x < .84 && y > .28 && y < .86) {
            color = (221, 189, 142);
            if (Math.Abs((y - .28) % .16) < .014) color = (167, 126, 88);
            if ((x - .16) % .17 > .045 && (x - .16) % .17 < .125 && (y - .28) % .16 > .04 && (y - .28) % .16 < .125) color = (42, 74, 87);
        }
        if (y > .12 + Math.Abs(x - .5) * .48 && y < .28 && Math.Abs(x - .5) < .36) color = (111, 72, 62);
        return color;
    }

    private static (byte R, byte G, byte B) Logo(double x, double y) {
        var dx = x - .5; var dy = y - .5;
        var radius = Math.Sqrt(dx * dx + dy * dy);
        if (radius > .32) return (244, 228, 190);
        if (Math.Abs(dx) + Math.Abs(dy) < .27 && Math.Abs(dx) + Math.Abs(dy) > .15) return (244, 228, 190);
        return (29, 88, 88);
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
