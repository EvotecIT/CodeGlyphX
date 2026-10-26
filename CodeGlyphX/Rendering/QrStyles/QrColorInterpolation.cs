using System;

namespace CodeGlyphX.Rendering;

/// <summary>The colour space used to interpolate gradient stops.</summary>
public enum QrGradientInterpolation {
    /// <summary>Perceptual lightness and chroma, with the shortest hue arc.</summary>
    Oklch,
    /// <summary>Cartesian perceptual lightness and opponent colour axes.</summary>
    Oklab,
    /// <summary>Legacy interpolation in encoded sRGB channels.</summary>
    Srgb
}

/// <summary>Gradient colour math independent of output size and rendering format.</summary>
internal static class QrColorInterpolation {
    internal static Rgba32 Interpolate(Rgba32 start, Rgba32 end, double t, QrGradientInterpolation space) {
        if (t <= 0) return start;
        if (t >= 1) return end;
        var alpha = Byte(start.A + (end.A - start.A) * t);
        if (space == QrGradientInterpolation.Srgb)
            return new Rgba32(Byte(start.R + (end.R - start.R) * t), Byte(start.G + (end.G - start.G) * t), Byte(start.B + (end.B - start.B) * t), alpha);
        var a = Lab(start); var b = Lab(end);
        var lightness = a.L + (b.L - a.L) * t;
        double aa, bb;
        if (space == QrGradientInterpolation.Oklch) {
            var ac = Math.Sqrt(a.A * a.A + a.B * a.B); var bc = Math.Sqrt(b.A * b.A + b.B * b.B);
            var ah = Math.Atan2(a.B, a.A); var bh = Math.Atan2(b.B, b.A);
            if (ac < .0001) ah = bh;
            if (bc < .0001) bh = ah;
            var delta = bh - ah;
            if (delta > Math.PI) delta -= 2 * Math.PI;
            if (delta < -Math.PI) delta += 2 * Math.PI;
            var hue = ah + delta * t; var chroma = ac + (bc - ac) * t;
            aa = chroma * Math.Cos(hue); bb = chroma * Math.Sin(hue);
        } else {
            aa = a.A + (b.A - a.A) * t; bb = a.B + (b.B - a.B) * t;
        }
        // Reduce chroma along a constant hue/lightness ray rather than clipping channels independently.
        var rgb = Rgb(lightness, aa, bb);
        if (!InGamut(rgb)) {
            var lo = 0.0; var hi = 1.0;
            for (var i = 0; i < 14; i++) {
                var mid = (lo + hi) / 2;
                if (InGamut(Rgb(lightness, aa * mid, bb * mid))) lo = mid; else hi = mid;
            }
            rgb = Rgb(lightness, aa * lo, bb * lo);
        }
        return new Rgba32(Encode(rgb.R), Encode(rgb.G), Encode(rgb.B), alpha);
    }

    private static (double L, double A, double B) Lab(Rgba32 c) {
        var r = Linear(c.R); var g = Linear(c.G); var b = Linear(c.B);
        var l = Math.Pow(.4122214708 * r + .5363325363 * g + .0514459929 * b, 1.0 / 3);
        var m = Math.Pow(.2119034982 * r + .6806995451 * g + .1073969566 * b, 1.0 / 3);
        var s = Math.Pow(.0883024619 * r + .2817188376 * g + .6299787005 * b, 1.0 / 3);
        return (.2104542553 * l + .7936177850 * m - .0040720468 * s,
            1.9779984951 * l - 2.4285922050 * m + .4505937099 * s,
            .0259040371 * l + .7827717662 * m - .8086757660 * s);
    }

    private static (double R, double G, double B) Rgb(double l, double a, double b) {
        var ll = l + .3963377774 * a + .2158037573 * b;
        var mm = l - .1055613458 * a - .0638541728 * b;
        var ss = l - .0894841775 * a - 1.2914855480 * b;
        ll *= ll * ll; mm *= mm * mm; ss *= ss * ss;
        return (4.0767416621 * ll - 3.3077115913 * mm + .2309699292 * ss,
            -1.2684380046 * ll + 2.6097574011 * mm - .3413193965 * ss,
            -.0041960863 * ll - .7034186147 * mm + 1.7076147010 * ss);
    }

    private static bool InGamut((double R, double G, double B) c) => c.R >= 0 && c.R <= 1 && c.G >= 0 && c.G <= 1 && c.B >= 0 && c.B <= 1;
    private static double Linear(byte c) { var v = c / 255.0; return v <= .04045 ? v / 12.92 : Math.Pow((v + .055) / 1.055, 2.4); }
    private static byte Encode(double v) => Byte(255 * (v <= .0031308 ? 12.92 * v : 1.055 * Math.Pow(v, 1 / 2.4) - .055));
    private static byte Byte(double v) => (byte)Math.Max(0, Math.Min(255, Math.Round(v)));
}
