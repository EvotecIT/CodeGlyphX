using CodeGlyphX.Rendering;
using System;

namespace CodeGlyphX.Rendering.Art;

public static partial class QrImageComposer {
    /// <summary>
    /// The tinted colour model shared by the canvas, the quiet zone and every module. The art exists in two keys from
    /// the same geometry: the washed normal key, and a high key where every colour becomes a light tint of its own
    /// hue (high OKLab lightness, reduced but held chroma) with its relative values kept. The high key covers the code
    /// and its quiet zone inside a rounded mask and eases into the normal key in OKLCH, so the art keeps its structure
    /// and the transition never turns grey. Dark modules are deep tones of the art.
    /// </summary>
    private sealed class TintModel {
        // High-key OKLab lightness runs from HighKeyFloor (the art's darkest colour) to one.
        internal const double HighKeyFloor = 0.8, DeepCeiling = 52;
        private readonly Rgba32 _ink, _paper, _base;
        private readonly double _tint, _darkArt, _saturation, _strength, _opacity, _module, _symbol, _border, _fade;

        internal TintModel(QrImageArtOptions options, double strength, double opacity, Rgba32 canvasBase,
            int moduleSize, int symbolModules, int border, double fade) {
            _ink = options.FunctionalForeground; _paper = options.FunctionalBackground; _base = canvasBase;
            _tint = options.FunctionalTint; _darkArt = options.DarkModuleArt; _saturation = options.LightModuleSaturation; _strength = strength; _opacity = opacity;
            _module = moduleSize; _symbol = symbolModules * (double)moduleSize; _border = border; _fade = fade;
        }

        /// <summary>
        /// High-key weight at a pixel, in symbol coordinates (the symbol's top-left module corner is 0,0). The mask is a
        /// rounded rectangle with a four-module corner radius, just large enough to hold the whole quiet zone at full
        /// weight, followed by an eased (smoothstep) transition over FadeModules.
        /// </summary>
        internal double Field(double px, double py) {
            px += 0.5; py += 0.5;
            var radius = 4 * _module;
            // Expanding by (1 - 1/sqrt 2) of the radius keeps the quiet zone's corners inside the rounded shape.
            var half = _symbol / 2 + _border + radius * (1 - Math.Sqrt(0.5));
            var qx = Math.Abs(px - _symbol / 2) - (half - radius); var qy = Math.Abs(py - _symbol / 2) - (half - radius);
            var outside = Math.Sqrt(Math.Max(qx, 0) * Math.Max(qx, 0) + Math.Max(qy, 0) * Math.Max(qy, 0)) + Math.Min(Math.Max(qx, qy), 0) - radius;
            if (outside <= 0) return 1;
            return _fade <= 0 ? 0 : 1 - Smooth(outside / _fade);
        }

        internal void Light(double r, double g, double b, double field, out double lr, out double lg, out double lb) {
            r = _base.R + (r - _base.R) * _opacity; g = _base.G + (g - _base.G) * _opacity; b = _base.B + (b - _base.B) * _opacity;
            if (field > 0) {
                // Both keys live in OKLCH with the hue held: lightness is raised along an expanding curve, so neighbouring
                // light colours stay distinct, and chroma is reduced but kept. The transition interpolates lightness and
                // chroma only, so it never passes through grey.
                ToOkLch(r, g, b, out var l, out var chroma, out var hue);
                var highL = HighKeyFloor + (1 - HighKeyFloor) * Math.Pow(Math.Max(0, Math.Min(1, l)), 1.5);
                var highC = chroma * 0.55 * _saturation;
                FromOkLch(l + (highL - l) * field, chroma + (highC - chroma) * field, hue, out r, out g, out b);
            }
            lr = Blend(_paper.R, r); lg = Blend(_paper.G, g); lb = Blend(_paper.B, b);
        }

        private static void ToOkLch(double r, double g, double b, out double l, out double c, out double h) {
            double Linear(double v) { v /= 255; return v <= 0.04045 ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4); }
            var lr = Linear(r); var lg = Linear(g); var lb = Linear(b);
            var lc = Math.Pow(0.4122214708 * lr + 0.5363325363 * lg + 0.0514459929 * lb, 1 / 3.0);
            var mc = Math.Pow(0.2119034982 * lr + 0.6806995451 * lg + 0.1073969566 * lb, 1 / 3.0);
            var sc = Math.Pow(0.0883024619 * lr + 0.2817188376 * lg + 0.6299787005 * lb, 1 / 3.0);
            l = 0.2104542553 * lc + 0.7936177850 * mc - 0.0040720468 * sc;
            var a = 1.9779984951 * lc - 2.4285922050 * mc + 0.4505937099 * sc;
            var bb = 0.0259040371 * lc + 0.7827717662 * mc - 0.8086757660 * sc;
            c = Math.Sqrt(a * a + bb * bb); h = Math.Atan2(bb, a);
        }

        // Converts back to sRGB, reducing chroma (never lightness or hue) until the colour fits the gamut.
        private static void FromOkLch(double l, double c, double h, out double r, out double g, out double b) {
            for (var step = 0; ; step++) {
                var a = c * Math.Cos(h); var bb = c * Math.Sin(h);
                var lc = l + 0.3963377774 * a + 0.2158037573 * bb; var mc = l - 0.1055613458 * a - 0.0638541728 * bb; var sc = l - 0.0894841775 * a - 1.2914855480 * bb;
                lc = lc * lc * lc; mc = mc * mc * mc; sc = sc * sc * sc;
                var lr = 4.0767416621 * lc - 3.3077115913 * mc + 0.2309699292 * sc;
                var lg = -1.2684380046 * lc + 2.6097574011 * mc - 0.3413193965 * sc;
                var lb = -0.0041960863 * lc - 0.7034186147 * mc + 1.7076147010 * sc;
                if (step >= 12 || (lr >= -1e-4 && lr <= 1.0001 && lg >= -1e-4 && lg <= 1.0001 && lb >= -1e-4 && lb <= 1.0001)) {
                    r = Gamma(lr); g = Gamma(lg); b = Gamma(lb);
                    return;
                }
                c *= 0.85;
            }
            static double Gamma(double v) { v = Math.Max(0, Math.Min(1, v)); return 255 * (v <= 0.0031308 ? 12.92 * v : 1.055 * Math.Pow(v, 1 / 2.4) - 0.055); }
        }
        internal void Deep(double r, double g, double b, out double dr, out double dg, out double db) {
            var luminance = 0.299 * r + 0.587 * g + 0.114 * b;
            var amount = (luminance > DeepCeiling ? DeepCeiling / luminance : 1) * _strength;
            dr = Blend(_ink.R, _ink.R + (r * amount - _ink.R) * _darkArt);
            dg = Blend(_ink.G, _ink.G + (g * amount - _ink.G) * _darkArt);
            db = Blend(_ink.B, _ink.B + (b * amount - _ink.B) * _darkArt);
        }

        /// <summary>Writes a light-to-deep mix at a symbol-relative pixel; coverage is the dark share of the pixel.</summary>
        internal void Write(byte[] pixels, int index, double px, double py, double r, double g, double b, double coverage) {
            Light(r, g, b, Field(px, py), out var lr, out var lg, out var lb);
            if (coverage > 0) {
                Deep(r, g, b, out var dr, out var dg, out var db);
                lr += (dr - lr) * coverage; lg += (dg - lg) * coverage; lb += (db - lb) * coverage;
            }
            pixels[index] = ToByte(lr); pixels[index + 1] = ToByte(lg); pixels[index + 2] = ToByte(lb);
        }

        // FunctionalTint below one blends toward the solid functional colours.
        private double Blend(byte solid, double art) => solid + (art - solid) * _tint;
        private static double Smooth(double t) { t = Math.Max(0, Math.Min(1, t)); return t * t * (3 - 2 * t); }
    }

    private static byte ToByte(double value) => (byte)Math.Round(Math.Max(0, Math.Min(255, value)));
}
