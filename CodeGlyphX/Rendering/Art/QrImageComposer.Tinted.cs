using CodeGlyphX.Rendering;
using System;

namespace CodeGlyphX.Rendering.Art;

public static partial class QrImageComposer {
    /// <summary>
    /// The tinted colour model shared by the canvas, the quiet zone and every module. The whole image is the washed
    /// artwork with its own crisp regions. Around the code, a light field lifts only the art's darker areas to a
    /// luminance floor, so the quiet zone stays light without a card silhouette; the floor rises in rounded fields
    /// around the finders and fades out softly beyond the quiet zone. Dark modules are deep tones of the art.
    /// </summary>
    private sealed class TintModel {
        internal const double BodyFloor = 194, FinderFloor = 214, DeepCeiling = 52;
        private readonly Rgba32 _ink, _paper, _base;
        private readonly double _tint, _darkArt, _strength, _opacity, _module, _symbol, _border, _fade;

        internal TintModel(QrImageArtOptions options, double strength, double opacity, Rgba32 canvasBase,
            int moduleSize, int symbolModules, int border, double fade) {
            _ink = options.FunctionalForeground; _paper = options.FunctionalBackground; _base = canvasBase;
            _tint = options.FunctionalTint; _darkArt = options.DarkModuleArt; _strength = strength; _opacity = opacity;
            _module = moduleSize; _symbol = symbolModules * (double)moduleSize; _border = border; _fade = fade;
        }

        /// <summary>Luminance floor at a pixel, in symbol coordinates (the symbol's top-left module corner is 0,0).</summary>
        internal double Floor(double px, double py) {
            px += 0.5; py += 0.5;
            // A squircle (|u|^4 + |v|^4 = 2) just encloses the quiet-zone square, so the field is a soft cushion, not a box.
            var half = _symbol / 2 + _border;
            var u = Math.Abs(px - _symbol / 2) / half; var v = Math.Abs(py - _symbol / 2) / half;
            var outside = Math.Max(0, (Math.Pow(u * u * u * u + v * v * v * v, 0.25) - Math.Pow(2, 0.25)) * half);
            var t = _fade <= 0 ? (outside > 0 ? 1 : 0) : Smooth(outside / _fade);
            var floor = BodyFloor * (1 - t);
            var near = 3.5 * _module; var far = _symbol - 3.5 * _module;
            var finder = Math.Min(Distance(px, py, near, near), Math.Min(Distance(px, py, far, near), Distance(px, py, near, far))) / _module;
            return Math.Max(floor, FinderFloor * (1 - Smooth((finder - 4.6) / 3.2)));
        }

        internal void Light(double r, double g, double b, double floor, out double lr, out double lg, out double lb) {
            r = _base.R + (r - _base.R) * _opacity; g = _base.G + (g - _base.G) * _opacity; b = _base.B + (b - _base.B) * _opacity;
            var luminance = 0.299 * r + 0.587 * g + 0.114 * b;
            // A smooth maximum lifts only areas darker than the floor, keeping lighter art and every region edge intact.
            var target = Math.Pow(Math.Pow(luminance, 8) + Math.Pow(floor, 8), 1 / 8.0);
            var amount = luminance >= 255 ? 0 : Math.Max(0, (255 - Math.Min(255, target)) / (255 - luminance));
            lr = Blend(_paper.R, 255 - (255 - r) * amount);
            lg = Blend(_paper.G, 255 - (255 - g) * amount);
            lb = Blend(_paper.B, 255 - (255 - b) * amount);
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
            Light(r, g, b, Floor(px, py), out var lr, out var lg, out var lb);
            if (coverage > 0) {
                Deep(r, g, b, out var dr, out var dg, out var db);
                lr += (dr - lr) * coverage; lg += (dg - lg) * coverage; lb += (db - lb) * coverage;
            }
            pixels[index] = ToByte(lr); pixels[index + 1] = ToByte(lg); pixels[index + 2] = ToByte(lb);
        }

        // FunctionalTint below one blends toward the solid functional colours.
        private double Blend(byte solid, double art) => solid + (art - solid) * _tint;
        private static double Distance(double x, double y, double cx, double cy) => Math.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
        private static double Smooth(double t) { t = Math.Max(0, Math.Min(1, t)); return t * t * (3 - 2 * t); }
    }

    private static byte ToByte(double value) => (byte)Math.Round(Math.Max(0, Math.Min(255, value)));
}
