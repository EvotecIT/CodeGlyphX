using CodeGlyphX.Rendering;
using System;

namespace CodeGlyphX.Rendering.Art;

public static partial class QrImageComposer {
    /// <summary>
    /// The tinted colour model shared by the canvas, the quiet zone and every module. The art exists in two keys from
    /// the same geometry: the washed normal key, and a high key where every colour becomes a light tint of its own
    /// hue (high lightness, reduced but non-zero chroma) with its relative values kept. The high key covers the code's
    /// footprint (symbol and quiet zone) and blends into the normal key with a distance-graded step beyond it, so the
    /// art keeps its structure inside the code. Dark modules are deep tones of the art.
    /// </summary>
    private sealed class TintModel {
        // High-key luminance runs from HighKeyFloor (the art's darkest colour) to 255.
        internal const double HighKeyFloor = 192, DeepCeiling = 52;
        private readonly Rgba32 _ink, _paper, _base;
        private readonly double _tint, _darkArt, _saturation, _strength, _opacity, _symbol, _border, _fade;

        internal TintModel(QrImageArtOptions options, double strength, double opacity, Rgba32 canvasBase,
            int moduleSize, int symbolModules, int border, double fade) {
            _ink = options.FunctionalForeground; _paper = options.FunctionalBackground; _base = canvasBase;
            _tint = options.FunctionalTint; _darkArt = options.DarkModuleArt; _saturation = options.LightModuleSaturation; _strength = strength; _opacity = opacity;
            _symbol = symbolModules * (double)moduleSize; _border = border; _fade = fade;
        }

        /// <summary>
        /// High-key weight at a pixel, in symbol coordinates (the symbol's top-left module corner is 0,0): one across
        /// the symbol and its quiet zone, then a smooth distance-graded step over FadeModules beyond the quiet zone.
        /// </summary>
        internal double Field(double px, double py) {
            px += 0.5; py += 0.5;
            var ox = Math.Max(Math.Max(-_border - px, px - _symbol - _border), 0);
            var oy = Math.Max(Math.Max(-_border - py, py - _symbol - _border), 0);
            var outside = Math.Sqrt(ox * ox + oy * oy);
            if (outside <= 0) return 1;
            return _fade <= 0 ? 0 : 1 - Smooth(outside / _fade);
        }

        internal void Light(double r, double g, double b, double field, out double lr, out double lg, out double lb) {
            r = _base.R + (r - _base.R) * _opacity; g = _base.G + (g - _base.G) * _opacity; b = _base.B + (b - _base.B) * _opacity;
            var luminance = 0.299 * r + 0.587 * g + 0.114 * b;
            if (field > 0) {
                // The high key: optional desaturation, then lightness compressed into HighKeyFloor..255 toward white,
                // which keeps every region's hue and its order of values.
                // Lightness is remapped with an expanding curve so neighbouring light colours stay distinct, and hue
                // is kept as a reduced but clearly visible chroma around the new lightness.
                var target = HighKeyFloor + (255 - HighKeyFloor) * Math.Pow(luminance / 255, 1.8);
                var chroma = Math.Min(1, (255 - target) / Math.Max(1, 255 - luminance) * 1.9) * _saturation;
                var hr = target + (r - luminance) * chroma; var hg = target + (g - luminance) * chroma; var hb = target + (b - luminance) * chroma;
                // Keep the tint inside the displayable range without shifting its lightness.
                var over = Math.Max(Math.Max(hr, hg), hb) - 255;
                if (over > 0) { var k = (255 - target) / (255 - target + over); hr = target + (hr - target) * k; hg = target + (hg - target) * k; hb = target + (hb - target) * k; }
                r += (hr - r) * field; g += (hg - g) * field; b += (hb - b) * field;
            }
            lr = Blend(_paper.R, r); lg = Blend(_paper.G, g); lb = Blend(_paper.B, b);
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
