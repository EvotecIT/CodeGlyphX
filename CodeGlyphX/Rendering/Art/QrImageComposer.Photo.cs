using System;
using CodeGlyphX.Qr;

namespace CodeGlyphX.Rendering.Art;

public static partial class QrImageComposer {
    /// <summary>
    /// Square-free photo treatments used when no module silhouettes are requested. Every module colour is derived from
    /// the image: its luminance is remapped into a dark or light band and its chroma is retained where the band allows.
    /// Within a locally uniform image region, dark and light modules therefore keep a constant luminance gap of
    /// 255 minus the band width, independently of the image tone.
    /// </summary>
    private sealed class PhotoPainter {
        private const byte Data = 0, FinderZone = 1, Alignment = 2, OtherFunction = 3;
        // Function patterns keep a narrower band (darker darks, lighter lights) than data modules.
        private const double FunctionBandLimit = 44;
        // Strongest halo, used where the image luminance opposes the module, and the distance where every halo ends.
        // Module corners lie beyond HaloEnd, so neighbouring modules always meet on the untreated image.
        private const double ConflictHalo = 0.8;
        private const double HaloEnd = 0.6;

        private readonly QrImageCompositionStyle _style;
        private readonly int _moduleSize;
        private readonly int _size;
        private readonly byte[] _kinds;
        private readonly int[] _alignmentCenter;
        private readonly double _band;
        private readonly double _functionBand;
        private readonly double _radius;
        private readonly double _feather;
        private readonly double _halo;

        internal PhotoPainter(QrCode qr, BitMatrix functions, QrImageCompositionOptions options) {
            _style = options.Style;
            _moduleSize = options.ModuleSize;
            _size = qr.Size;
            var strength = options.Strength;
            _band = _style == QrImageCompositionStyle.ColorModules ? 30 + 70 * strength : 20 + 60 * strength;
            _functionBand = Math.Min(_band, FunctionBandLimit);
            // A circle with radius 0.564w covers the same area as a square of width w.
            _radius = Math.Min(0.5, 0.564 * options.CenterSize);
            var pixel = 1.0 / _moduleSize;
            _feather = _style == QrImageCompositionStyle.ImageOverlay ? Math.Max(1.5 * pixel, 0.12) : pixel;
            // The halo tints the image around each dot and reaches zero before the module edge, so neighbouring
            // modules meet on the untreated image and no grid lines appear.
            _halo = _style == QrImageCompositionStyle.ImageOverlay ? 0.6 - 0.3 * strength : 0.12 + 0.28 * (1 - strength);
            _kinds = new byte[_size * _size];
            _alignmentCenter = new int[_size * _size];
            var alignment = QrTables.GetAlignmentPatternPositions(qr.Version);
            for (var y = 0; y < _size; y++) for (var x = 0; x < _size; x++) {
                if (!functions[x, y]) continue;
                var i = y * _size + x;
                if ((x < 8 && y < 8) || (x >= _size - 8 && y < 8) || (x < 8 && y >= _size - 8)) {
                    _kinds[i] = FinderZone;
                    continue;
                }
                _kinds[i] = OtherFunction;
                foreach (var ay in alignment) foreach (var ax in alignment) {
                    if (Math.Abs(x - ax) > 2 || Math.Abs(y - ay) > 2) continue;
                    if ((ax < 8 && ay < 8) || (ax >= _size - 8 && ay < 8) || (ax < 8 && ay >= _size - 8)) continue;
                    _kinds[i] = Alignment;
                    _alignmentCenter[i] = ay * _size + ax;
                }
            }
        }

        /// <summary>Paints one symbol pixel. px/py are relative to the first module; r/g/b is the image sample.</summary>
        internal void Paint(byte[] pixels, int index, int px, int py, bool dark, double r, double g, double b) {
            var mx = px / _moduleSize;
            var my = py / _moduleSize;
            var kind = _kinds[my * _size + mx];
            if (_style == QrImageCompositionStyle.ColorModules) {
                Tint(r, g, b, dark, kind == Data ? _band : _functionBand, out var tr, out var tg, out var tb);
                Write(pixels, index, tr, tg, tb);
                return;
            }
            if (_style == QrImageCompositionStyle.ImageOverlay && kind != Data) {
                WriteGray(pixels, index, dark ? (byte)0 : (byte)255);
                return;
            }
            // Module coordinates of the pixel centre.
            var u = (px + 0.5) / _moduleSize;
            var v = (py + 0.5) / _moduleSize;
            if (kind == FinderZone) {
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
            var dx = u - mx - 0.5;
            var dy = v - my - 0.5;
            var distance = Math.Sqrt(dx * dx + dy * dy);
            var dot = Clamp01(0.5 + (_radius - distance) / _feather);
            // Where the image fights the module (a light module over a dark area or the reverse), the halo grows so the
            // module's average tone still follows its bit after downscaling or blur. Elsewhere it stays faint.
            var conflict = (0.299 * r + 0.587 * g + 0.114 * b) / 255;
            if (!dark) conflict = 1 - conflict;
            var halo = (_halo + (ConflictHalo - _halo) * conflict * conflict) * (1 - SmoothStep(_radius, HaloEnd, distance));
            var weight = dot + (1 - dot) * halo;
            Tint(r, g, b, dark, kind == Data ? _band : _functionBand, out var dr, out var dg, out var db);
            Write(pixels, index, r + (dr - r) * weight, g + (dg - g) * weight, b + (db - b) * weight);
        }

        // Concentric rounded squares: dark ring, light gap and dark ball, all tinted from the image. Finders also get a
        // light, image-tinted separator that fades into the image instead of a hard white block.
        private void PaintRings(byte[] pixels, int index, double x, double y, double r, double g, double b,
            double half, double outerRadius, double holeRadius, double ballHalf, double ballRadius, bool separator) {
            var outer = Coverage(RoundBox(x, y, half, outerRadius));
            var hole = Coverage(RoundBox(x, y, half - 1, holeRadius));
            var ball = Coverage(RoundBox(x, y, ballHalf, ballRadius));
            var darkWeight = outer * (1 - hole) + ball;
            var lightWeight = Math.Max(0, hole - ball);
            if (separator) {
                var outside = RoundBox(x, y, half, outerRadius);
                lightWeight += (1 - outer) * (1 - SmoothStep(0.5, 1.0, outside));
            }
            Tint(r, g, b, true, _functionBand, out var dr, out var dg, out var db);
            Tint(r, g, b, false, _functionBand, out var lr, out var lg, out var lb);
            var image = Math.Max(0, 1 - darkWeight - lightWeight);
            Write(pixels, index, r * image + dr * darkWeight + lr * lightWeight,
                g * image + dg * darkWeight + lg * lightWeight, b * image + db * darkWeight + lb * lightWeight);
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

    /// <summary>
    /// Maps the image luminance into the dark band [0, band] or the light band [255 - band, 255] and keeps as much of
    /// the image chroma as the band and the RGB gamut allow. Rec. 601 luminance is preserved exactly by the chroma offset.
    /// </summary>
    internal static void Tint(double r, double g, double b, bool dark, double band, out double tr, out double tg, out double tb) {
        var luminance = 0.299 * r + 0.587 * g + 0.114 * b;
        var target = dark ? luminance * band / 255 : 255 - band + luminance * band / 255;
        double cr = r - luminance, cg = g - luminance, cb = b - luminance;
        // Chroma is lifted slightly so darkened and lightened modules do not look washed out; gamut limits still apply.
        var k = ChromaBoost;
        k = LimitChroma(k, cr, target);
        k = LimitChroma(k, cg, target);
        k = LimitChroma(k, cb, target);
        tr = target + k * cr;
        tg = target + k * cg;
        tb = target + k * cb;
    }

    private const double ChromaBoost = 1.15;

    private static double LimitChroma(double k, double chroma, double target) =>
        chroma > 0 ? Math.Min(k, (255 - target) / chroma) : chroma < 0 ? Math.Min(k, target / -chroma) : k;
}
