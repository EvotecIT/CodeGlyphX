using CodeGlyphX.Rendering;
using System;

namespace CodeGlyphX.Rendering.Art;

public static partial class QrImageComposer {
    /// <summary>
    /// The tinted colour model shared by the canvas, the quiet zone and every module. The whole image is the washed
    /// artwork with its own crisp regions. Behind and between modules, and across the quiet zone, the art's darker
    /// areas are lifted to a luminance floor, so the code's footprint stays light without a veil or halo; the lift
    /// fades out over a short distance beyond the quiet zone. Dark modules are deep tones of the art.
    /// </summary>
    private sealed class TintModel {
        internal const double BodyFloor = 202, DeepCeiling = 52;
        private readonly Rgba32 _ink, _paper, _base;
        private readonly double _tint, _darkArt, _saturation, _strength, _opacity, _module, _symbol, _border, _fade;

        internal TintModel(QrImageArtOptions options, double strength, double opacity, Rgba32 canvasBase,
            int moduleSize, int symbolModules, int border, double fade) {
            _ink = options.FunctionalForeground; _paper = options.FunctionalBackground; _base = canvasBase;
            _tint = options.FunctionalTint; _darkArt = options.DarkModuleArt; _saturation = options.LightModuleSaturation; _strength = strength; _opacity = opacity;
            _module = moduleSize; _symbol = symbolModules * (double)moduleSize; _border = border; _fade = fade;
        }

        /// <summary>
        /// Weight of the light field at a pixel, in symbol coordinates (the symbol's top-left module corner is 0,0).
        /// The field is built from the code's own footprint on the module grid: the symbol and its whole quiet zone are
        /// lifted, and beyond the quiet zone a fringe of module-sized light tiles thins out over three rings, so the
        /// field ends in the code's own pixel language instead of a straight edge or a veil.
        /// </summary>
        internal double Field(double px, double py) {
            px += 0.5; py += 0.5;
            var outside = Math.Max(Math.Max(-px, px - _symbol), Math.Max(-py, py - _symbol));
            if (outside < _border) return 1;
            var ring = (int)((outside - _border) / _module);
            if (ring >= 3) return 0;
            var cellX = (int)Math.Floor(px / _module); var cellY = (int)Math.Floor(py / _module);
            var keep = ring == 0 ? 0.72 : ring == 1 ? 0.38 : 0.12;
            if (CellHash(cellX, cellY) >= keep) return 0;
            // A crisp rounded tile, one module wide, like a light module that drifted out of the code.
            var u = Mod(px, _module) / _module - 0.5; var v = Mod(py, _module) / _module - 0.5;
            const double corner = 0.22, halfTile = 0.47;
            var qx = Math.Max(Math.Abs(u) - (halfTile - corner), 0); var qy = Math.Max(Math.Abs(v) - (halfTile - corner), 0);
            var edge = (Math.Sqrt(qx * qx + qy * qy) - corner) * _module;
            return Math.Max(0, Math.Min(1, 0.5 - edge));
        }

        private static double CellHash(int x, int y) {
            unchecked {
                var value = (uint)x * 0x9e3779b9u ^ (uint)y * 0x85ebca6bu ^ 0x5bd1e995u;
                value ^= value >> 16; value *= 0x7feb352du; value ^= value >> 15; value *= 0x846ca68bu; value ^= value >> 16;
                return (value & 0xffffff) / 16777216.0;
            }
        }
        private static double Mod(double value, double size) => value - Math.Floor(value / size) * size;
        internal void Light(double r, double g, double b, double field, out double lr, out double lg, out double lb) {
            r = _base.R + (r - _base.R) * _opacity; g = _base.G + (g - _base.G) * _opacity; b = _base.B + (b - _base.B) * _opacity;
            var luminance = 0.299 * r + 0.587 * g + 0.114 * b;
            // Inside the field the art may be desaturated, so modules win over busy colour.
            var keep = 1 - (1 - _saturation) * field;
            r = luminance + (r - luminance) * keep; g = luminance + (g - luminance) * keep; b = luminance + (b - luminance) * keep;
            // A smooth maximum lifts only areas darker than the floor, keeping lighter art and every region edge crisp.
            var floor = BodyFloor * field;
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
