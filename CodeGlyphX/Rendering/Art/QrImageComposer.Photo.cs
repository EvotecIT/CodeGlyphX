using System;

namespace CodeGlyphX.Rendering.Art;

public static partial class QrImageComposer {
    /// <summary>
    /// Square-free photo treatments used when no module silhouettes are requested. Every module colour is derived from
    /// the image. ColorModules remaps the image luminance into a dark or light band, so dark and light modules keep a
    /// constant luminance gap within any locally uniform area. The dot treatments only move the image as far as needed:
    /// a light module over an already light area keeps the image, and a dark module over a dark area does too.
    /// All three share one function-zone treatment: the image continues under separators, timing and format zones,
    /// and finders and alignment patterns are rounded, image-tinted rings.
    /// </summary>
    private sealed class PhotoPainter {
        // Finder and alignment rings use a narrower dark band (deeper darks) than data modules.
        private const double FunctionBandLimit = 44;
        // Strongest halo, used where the image luminance opposes the module, and the distance where every halo ends.
        // Module corners lie beyond HaloEnd, so neighbouring modules always meet on the untreated image.
        private const double ConflictHalo = 0.8;
        private const double HaloEnd = 0.6;
        // ColorModules keeps light modules below this luminance so they read as lit image colour, not pastel.
        private const double MaxLightModule = 238;
        // Light finder gaps and separator bands keep the local image colour, lifted only to this scan minimum.
        private const double RingLightFloor = 175;
        // Width, in modules, of the light band around finders; the rest of the separator shows the image.
        private const double SeparatorWidth = 0.6;
        // Light dots ease back to the image between these image luminances.
        private const double LightAreaStart = 165, LightAreaFull = 225;
        // ImageOverlay centre marks: side relative to CenterSize, and the allowed range in modules.
        // Centre marks are nearly black or white, still tinted by the image, because they are small.
        private const double MarkBand = 24;
        // Strongest nudge of an opposing image around a centre mark.
        private const double MarkNudge = 0.3;
        private const double MarkScale = 0.75, MinMark = 0.3, MaxMark = 0.45;

        private readonly QrImageCompositionStyle _style;
        private readonly bool _solidFunctions;
        private readonly int _moduleSize;
        private readonly int _size;
        private readonly byte[] _kinds;
        private readonly int[] _alignmentCenter;
        private readonly double _band;
        private readonly double _functionBand;
        private readonly double _radius;
        private readonly double _feather;
        private readonly double _halo;
        private readonly double _mark;

        internal PhotoPainter(QrCode qr, BitMatrix functions, QrImageCompositionOptions options) {
            _style = options.Style;
            _solidFunctions = options.SolidFunctionPatterns;
            _moduleSize = options.ModuleSize;
            _size = qr.Size;
            var strength = options.Strength;
            _band = _style == QrImageCompositionStyle.ColorModules ? 40 + 80 * strength : 20 + 60 * strength;
            _functionBand = Math.Min(_band, FunctionBandLimit);
            // A circle with radius 0.564w covers the same area as a square of width w.
            _radius = Math.Min(0.5, 0.564 * options.CenterSize);
            var pixel = 1.0 / _moduleSize;
            _feather = _style == QrImageCompositionStyle.ImageOverlay ? Math.Max(1.5 * pixel, 0.12) : pixel;
            // The halo tints the image around each dot and reaches zero before the module edge, so neighbouring
            // modules meet on the untreated image and no grid lines appear.
            _halo = 0;
            _mark = Math.Max(MinMark, Math.Min(MaxMark, MarkScale * options.CenterSize));
            ClassifyPhotoFunctions(qr, functions, out _kinds, out _alignmentCenter);
        }

        /// <summary>Paints one symbol pixel. px/py are relative to the first module; r/g/b is the image sample.</summary>
        internal void Paint(byte[] pixels, int index, int px, int py, bool dark, double r, double g, double b) {
            var mx = px / _moduleSize;
            var my = py / _moduleSize;
            var kind = _kinds[my * _size + mx];
            if (_solidFunctions && kind != Data) {
                WriteGray(pixels, index, dark ? (byte)0 : (byte)255);
                return;
            }
            // Module coordinates of the pixel centre.
            var u = (px + 0.5) / _moduleSize;
            var v = (py + 0.5) / _moduleSize;
            if (kind == Finder || (kind == Separator && _style != QrImageCompositionStyle.ColorModules)) {
                var cx = mx < 8 ? 3.5 : _size - 3.5;
                var cy = my < 8 ? 3.5 : _size - 3.5;
                PaintRings(pixels, index, u - cx, v - cy, r, g, b, 3.5, 1.75, 0.75, 1.5, 0.85, true);
                return;
            }
            if (kind == Alignment) {
                var center = _alignmentCenter[my * _size + mx];
                PaintRings(pixels, index, u - (center % _size + 0.5), v - (center / _size + 0.5), r, g, b, 2.5, 1.2, 0.45, 0.5, 0.5, false);
                return;
            }
            // Data, separators, timing, format and version modules all share the data treatment.
            if (_style == QrImageCompositionStyle.ColorModules) {
                ModuleTint(r, g, b, dark, out var tr, out var tg, out var tb);
                Write(pixels, index, tr, tg, tb);
                return;
            }
            var dx = u - mx - 0.5;
            var dy = v - my - 0.5;
            if (_style == QrImageCompositionStyle.ImageOverlay) {
                // Photo first: the image stays unaltered across the module and only a small square centre mark carries
                // the bit, in the deep function band so it survives downscaling.
                var mark = Clamp01(0.5 + (_mark / 2 - Math.Abs(dx)) * _moduleSize) * Clamp01(0.5 + (_mark / 2 - Math.Abs(dy)) * _moduleSize);
                DotTint(r, g, b, dark, MarkBand, out var mr, out var mg, out var mb);
                // Only where the image opposes the bit is it nudged towards the mark, fading out before the module corners.
                var opposed = Luma(r, g, b) / 255;
                if (!dark) opposed = 1 - opposed;
                var nudge = MarkNudge * opposed * opposed * (1 - SmoothStep(_mark / 2, HaloEnd, Math.Sqrt(dx * dx + dy * dy)));
                var markWeight = mark + (1 - mark) * nudge;
                Write(pixels, index, r + (mr - r) * markWeight, g + (mg - g) * markWeight, b + (mb - b) * markWeight);
                return;
            }
            var distance = Math.Sqrt(dx * dx + dy * dy);
            var dot = Clamp01(0.5 + (_radius - distance) / _feather);
            // Where the image fights the module (a light module over a dark area or the reverse), the halo grows so the
            // module's average tone still follows its bit after downscaling or blur. Elsewhere it stays faint.
            var conflict = (0.299 * r + 0.587 * g + 0.114 * b) / 255;
            if (!dark) conflict = 1 - conflict;
            var halo = (_halo + (ConflictHalo - _halo) * conflict * conflict) * (1 - SmoothStep(_radius, HaloEnd, distance));
            var weight = dot + (1 - dot) * halo;
            // Format, version and timing modules use the deeper function band: one misread format bit loses the symbol.
            DotTint(r, g, b, dark, kind == OtherFunction ? _functionBand : _band, out var dr, out var dg, out var db);
            Write(pixels, index, r + (dr - r) * weight, g + (dg - g) * weight, b + (db - b) * weight);
        }

        // Concentric rounded squares: dark ring, light gap and dark ball, all tinted from the local image. The rings stay
        // inside the pattern's own bounds; outside the rounded corners the image (or, for ColorModules, a light module
        // tint) shows through, so there is no feathered glow and no white block around them.
        private void PaintRings(byte[] pixels, int index, double x, double y, double r, double g, double b,
            double half, double outerRadius, double holeRadius, double ballHalf, double ballRadius, bool separator) {
            var outer = Coverage(RoundBox(x, y, half, outerRadius));
            var hole = Coverage(RoundBox(x, y, half - 1, holeRadius));
            var ball = Coverage(RoundBox(x, y, ballHalf, ballRadius));
            var darkWeight = outer * (1 - hole) + ball;
            var lightWeight = Math.Max(0, hole - ball);
            // Dot treatments give finders a crisp, image-tinted light band (SeparatorWidth) that follows the ring. It ends
            // at the separator's own edge, so it reads as part of the finder rather than a glow.
            if (separator && _style != QrImageCompositionStyle.ColorModules)
                lightWeight += (1 - outer) * Coverage(RoundBox(x, y, half + SeparatorWidth, outerRadius + SeparatorWidth));
            DarkFunctionTint(r, g, b, out var dr, out var dg, out var db);
            double lr, lg, lb;
            if (_style == QrImageCompositionStyle.ColorModules) ModuleTint(r, g, b, false, out lr, out lg, out lb);
            // The light gap keeps the local image colour: lifted only as far as the ring contrast needs, not to a pale wash.
            else RingLightTint(r, g, b, out lr, out lg, out lb);
            var rest = Math.Max(0, 1 - darkWeight - lightWeight);
            double br = r, bg = g, bb = b;
            if (_style == QrImageCompositionStyle.ColorModules) { br = lr; bg = lg; bb = lb; }
            Write(pixels, index, br * rest + dr * darkWeight + lr * lightWeight,
                bg * rest + dg * darkWeight + lg * lightWeight, bb * rest + db * darkWeight + lb * lightWeight);
        }

        private void DarkFunctionTint(double r, double g, double b, out double tr, out double tg, out double tb) {
            var luminance = Luma(r, g, b);
            ApplyChroma(r, g, b, luminance, luminance * _functionBand / 255, 1.3, true, out tr, out tg, out tb);
        }

        // ColorModules: the image's tonal range is remapped into each band, darks keep their hue and saturation, and
        // lights are held below pastel.
        private void ModuleTint(double r, double g, double b, bool dark, out double tr, out double tg, out double tb) {
            var luminance = Luma(r, g, b);
            var target = dark ? luminance * _band / 255 : Math.Min(MaxLightModule, 255 - _band + luminance * _band / 255);
            // Darkening a pale colour keeps its hue only if its chroma grows as its lightness falls (a pale sun becomes
            // deep amber rather than olive-brown); the gamut still limits it. Light modules get a little extra chroma so
            // grey-green and grey-blue areas do not wash out.
            var boost = dark ? 1.35 * Math.Sqrt(Math.Max(1, luminance / Math.Max(1, target))) : 1.45;
            ApplyChroma(r, g, b, luminance, target, boost, dark, out tr, out tg, out tb);
        }

        // Dot treatments: the image's tonal range is remapped into each band, except that light dots over an already light
        // area ease back towards the image itself, so bright skies and highlights do not read as a bead mesh.
        private static void DotTint(double r, double g, double b, bool dark, double band, out double tr, out double tg, out double tb) {
            var luminance = Luma(r, g, b);
            double target;
            if (dark) target = luminance * band / 255;
            else {
                var remapped = 255 - band + luminance * band / 255;
                var settled = Math.Max(luminance, 255 - band);
                target = remapped + (settled - remapped) * SmoothStep(LightAreaStart, LightAreaFull, luminance);
            }
            ApplyChroma(r, g, b, luminance, target, ChromaBoost, dark, out tr, out tg, out tb);
        }

        // Light finder and alignment gaps and finder separator bands: the local image colour, lifted only as far as the
        // scan minimum. Areas already lighter than that keep the image as it is.
        private static void RingLightTint(double r, double g, double b, out double tr, out double tg, out double tb) {
            var luminance = Luma(r, g, b);
            ApplyChroma(r, g, b, luminance, Math.Max(luminance, RingLightFloor), ChromaBoost, false, out tr, out tg, out tb);
        }

        // Signed distance, in modules, to a rounded square centred on the origin.
        private static double RoundBox(double x, double y, double half, double radius) {
            var qx = Math.Abs(x) - half + radius;
            var qy = Math.Abs(y) - half + radius;
            var outside = Math.Sqrt(Math.Max(qx, 0) * Math.Max(qx, 0) + Math.Max(qy, 0) * Math.Max(qy, 0));
            return outside + Math.Min(Math.Max(qx, qy), 0) - radius;
        }

        private double Coverage(double signedDistance) => Clamp01(0.5 - signedDistance * _moduleSize);

        private static double SmoothStep(double edge0, double edge1, double x) {
            var t = Clamp01((x - edge0) / (edge1 - edge0));
            return t * t * (3 - 2 * t);
        }

        private static double Clamp01(double value) => value < 0 ? 0 : value > 1 ? 1 : value;

        private static void Write(byte[] pixels, int index, double r, double g, double b) {
            pixels[index] = ToByte(r);
            pixels[index + 1] = ToByte(g);
            pixels[index + 2] = ToByte(b);
        }

        private static byte ToByte(double value) => (byte)Math.Round(value < 0 ? 0 : value > 255 ? 255 : value);
    }

    private const double ChromaBoost = 1.15;

    private static double Luma(double r, double g, double b) => 0.299 * r + 0.587 * g + 0.114 * b;

    /// <summary>
    /// Moves a colour to the target Rec. 601 luminance exactly, keeping its chroma offset (scaled by boost) as far as the
    /// RGB gamut allows. Darkened yellows are turned slightly towards orange so they read as warm shadow, not olive.
    /// </summary>
    private static void ApplyChroma(double r, double g, double b, double luminance, double target, double boost, bool warmShadows,
        out double tr, out double tg, out double tb) {
        double cr = r - luminance, cb = b - luminance;
        if (warmShadows && luminance > 1 && target < luminance) {
            // Hue angle in the Cb/Cr plane: about 160 degrees for yellow, 140 for orange, 113 for red.
            var angle = Math.Atan2(cr, cb) * 180 / Math.PI;
            if (angle < 0) angle += 360;
            if (angle > 135 && angle < 210) {
                var darkening = 1 - target / luminance;
                var rotate = -Math.Min(angle - 135, 22 * darkening) * Math.PI / 180;
                var cos = Math.Cos(rotate);
                var sin = Math.Sin(rotate);
                var nb = cb * cos - cr * sin;
                var nr = cb * sin + cr * cos;
                cb = nb;
                cr = nr;
            }
        }
        var cg = -(0.299 * cr + 0.114 * cb) / 0.587;
        var k = boost;
        k = LimitChroma(k, cr, target);
        k = LimitChroma(k, cg, target);
        k = LimitChroma(k, cb, target);
        tr = target + k * cr;
        tg = target + k * cg;
        tb = target + k * cb;
    }

    private static double LimitChroma(double k, double chroma, double target) =>
        chroma > 0 ? Math.Min(k, (255 - target) / chroma) : chroma < 0 ? Math.Min(k, target / -chroma) : k;
}
