using System;
using System.Threading;
using CodeGlyphX.Qr;

namespace CodeGlyphX.Rendering.Art;

public static partial class QrImageComposer {
    /// <summary>
    /// <see cref="QrImageCompositionStyle.PhotoHalftone"/>: the picture forms the code. The photo is sampled once per
    /// sub-cell, its tones are stretched and held inside a band that leaves every module centre clear contrast, and each
    /// sub-cell is nudged towards its module's colour by an amount set by Strength. The module centre is then painted solid
    /// in a deep or light tone of the local colour (or the duotone ink and paper), and the remaining sub-cells reproduce the
    /// photo through the chosen screen. Finders and alignment patterns are solid rings in the same deep and light tones.
    /// </summary>
    private sealed class HalftonePainter {
        private const byte Data = 0, Finder = 1, Separator = 2, Alignment = 3, OtherFunction = 4;
        // Surround tones are held inside this luminance band, so a local reader threshold always falls between the darkest
        // and lightest neighbourhoods and every solid centre keeps contrast against it.
        private const double ToneLow = 44, ToneHigh = 224;
        // Each sub-cell moves this far towards its module colour: MinBias at Strength 1, MaxBias at Strength 0.
        private const double MinBias = 0, MaxBias = 0.5;
        // Timing, format and version modules carry structure that a single misread loses, so they stay closer to their bit.
        private const double FunctionBias = 0.42, FunctionCenter = 0.5;
        // Photo colours: dots and holes use a deep and a light tone of the local colour; module centres and function
        // patterns use deeper and lighter ones still.
        private const double InkLuma = 52, PaperLuma = 226, DarkCenterLuma = 30, LightCenterLuma = 240;
        private const double ColorBoost = 1.3;
        private const double LocalContrast = 0.35;
        // Width, in modules, of the light band that follows each finder ring through its separator.
        private const double SeparatorBand = 0.6;

        private static readonly byte[] Bayer8 = {
            0, 32, 8, 40, 2, 34, 10, 42, 48, 16, 56, 24, 50, 18, 58, 26, 12, 44, 4, 36, 14, 46, 6, 38, 60, 28, 52, 20, 62, 30, 54, 22,
            3, 35, 11, 43, 1, 33, 9, 41, 51, 19, 59, 27, 49, 17, 57, 25, 15, 47, 7, 39, 13, 45, 5, 37, 63, 31, 55, 23, 61, 29, 53, 21
        };

        private readonly int _moduleSize;
        private readonly int _size;
        private readonly int _cells;
        private readonly int _grid;
        private readonly double _center;
        private readonly bool _round;
        private readonly QrHalftoneScreen _screen;
        private readonly QrImageFinderStyle _finders;
        private readonly QrImageDuotoneOptions? _duotone;
        private readonly byte[] _kinds;
        private readonly int[] _alignmentCenter;
        // Per sub-cell: ink coverage (0..1) for the dot screen, or 0/1 for the binary screens, and the local colour.
        private readonly double[] _coverage;
        private readonly double[] _red, _green, _blue, _luma;

        internal HalftonePainter(QrCode qr, BitMatrix functions, QrImageCompositionOptions options, QrImageSampler image,
            int originX, int originY, CancellationToken cancellationToken) {
            var halftone = options.Halftone ?? new QrImageHalftoneOptions();
            _moduleSize = options.ModuleSize;
            _size = qr.Size;
            _cells = halftone.SubCells;
            _grid = _size * _cells;
            _round = halftone.DotShape == QrHalftoneDotShape.Round;
            // A round centre covers the same area as a square of the requested width.
            _center = halftone.CenterSize * (_round ? 1.128 : 1);
            _screen = halftone.Screen;
            _finders = halftone.Finders;
            _duotone = options.Duotone;
            _kinds = new byte[_size * _size];
            _alignmentCenter = new int[_size * _size];
            ClassifyFunctions(qr, functions);

            var count = _grid * _grid;
            _red = new double[count];
            _green = new double[count];
            _blue = new double[count];
            _luma = new double[count];
            var tones = new double[count];
            var histogram = new int[256];
            var step = _moduleSize / (double)_cells;
            for (var y = 0; y < _grid; y++) {
                cancellationToken.ThrowIfCancellationRequested();
                for (var x = 0; x < _grid; x++) {
                    // Average a 3 by 3 grid of bilinear samples inside the sub-cell, so fine photo detail does not alias.
                    double r = 0, g = 0, b = 0;
                    for (var sy = 0; sy < 3; sy++) for (var sx = 0; sx < 3; sx++) {
                        image.Sample(originX + (x + (sx + 0.5) / 3) * step - 0.5, originY + (y + (sy + 0.5) / 3) * step - 0.5,
                            out var sr, out var sg, out var sb);
                        r += sr; g += sg; b += sb;
                    }
                    var i = y * _grid + x;
                    _red[i] = r / 9; _green[i] = g / 9; _blue[i] = b / 9;
                    _luma[i] = Luma(_red[i], _green[i], _blue[i]);
                    tones[i] = _luma[i];
                    histogram[(int)Math.Round(_luma[i])]++;
                }
            }
            MapTones(tones, histogram, halftone, cancellationToken);

            var bias = MinBias + (MaxBias - MinBias) * (1 - options.Strength);
            _coverage = new double[count];
            for (var y = 0; y < _grid; y++) for (var x = 0; x < _grid; x++) {
                var mx = x / _cells; var my = y / _cells;
                var dark = qr.Modules[mx, my];
                var kind = _kinds[my * _size + mx];
                var amount = kind is OtherFunction or Separator ? Math.Max(bias, FunctionBias) : bias;
                var i = y * _grid + x;
                var tone = tones[i] + ((dark ? 0 : 255) - tones[i]) * amount;
                InkAndPaperLuma(out var ink, out var paper);
                _coverage[i] = Clamp01((paper - tone) / (paper - ink));
            }
            FixCentresAndScreen(qr.Modules, cancellationToken);
        }

        private void ClassifyFunctions(QrCode qr, BitMatrix functions) {
            var alignment = QrTables.GetAlignmentPatternPositions(qr.Version);
            for (var y = 0; y < _size; y++) for (var x = 0; x < _size; x++) {
                if (!functions[x, y]) continue;
                var i = y * _size + x;
                if (InFinderZone(x, y)) {
                    var lx = x < 8 ? x : x - (_size - 7);
                    var ly = y < 8 ? y : y - (_size - 7);
                    _kinds[i] = lx is >= 0 and < 7 && ly is >= 0 and < 7 ? Finder : Separator;
                    continue;
                }
                _kinds[i] = OtherFunction;
                foreach (var ay in alignment) foreach (var ax in alignment) {
                    if (Math.Abs(x - ax) > 2 || Math.Abs(y - ay) > 2 || InFinderZone(ax, ay)) continue;
                    _kinds[i] = Alignment;
                    _alignmentCenter[i] = ay * _size + ax;
                }
            }
        }

        private bool InFinderZone(int x, int y) => (x < 8 && y < 8) || (x >= _size - 8 && y < 8) || (x < 8 && y >= _size - 8);

        // 1. stretch the 2nd..98th percentile to the full range (nearly flat images are left alone);
        // 2. apply the requested contrast and brightness;
        // 3. a mild unsharp mask (radius about 1/16 of the symbol) separates neighbouring regions of similar tone;
        // 4. hold the result inside the surround band.
        private void MapTones(double[] tones, int[] histogram, QrImageHalftoneOptions halftone, CancellationToken cancellationToken) {
            var low = Percentile(histogram, tones.Length, 0.02);
            var high = Percentile(histogram, tones.Length, 0.98);
            var stretch = high - low >= 32 && high - low < 255;
            for (var i = 0; i < tones.Length; i++) {
                var value = stretch ? (tones[i] - low) * 255.0 / (high - low) : tones[i];
                value = 127.5 + (value - 127.5) * halftone.Contrast + halftone.Brightness * 255;
                tones[i] = Clamp255(value);
            }
            cancellationToken.ThrowIfCancellationRequested();
            var side = _grid;
            var radius = Math.Max(2, side / 16);
            var integral = new double[(side + 1) * (side + 1)];
            for (var y = 0; y < side; y++) {
                var row = 0.0;
                for (var x = 0; x < side; x++) {
                    row += tones[y * side + x];
                    integral[(y + 1) * (side + 1) + x + 1] = integral[y * (side + 1) + x + 1] + row;
                }
            }
            var sharpened = new double[tones.Length];
            for (var y = 0; y < side; y++) for (var x = 0; x < side; x++) {
                int x0 = Math.Max(0, x - radius), x1 = Math.Min(side, x + radius + 1);
                int y0 = Math.Max(0, y - radius), y1 = Math.Min(side, y + radius + 1);
                var sum = integral[y1 * (side + 1) + x1] - integral[y0 * (side + 1) + x1] - integral[y1 * (side + 1) + x0] + integral[y0 * (side + 1) + x0];
                var mean = sum / ((x1 - x0) * (y1 - y0));
                var value = tones[y * side + x];
                sharpened[y * side + x] = Clamp255(value + LocalContrast * (value - mean));
            }
            for (var i = 0; i < tones.Length; i++) tones[i] = ToneLow + sharpened[i] * (ToneHigh - ToneLow) / 255;
        }

        private void InkAndPaperLuma(out double ink, out double paper) {
            if (_duotone is null) { ink = InkLuma; paper = PaperLuma; return; }
            ink = Luma(_duotone.Ink.R, _duotone.Ink.G, _duotone.Ink.B);
            paper = Luma(_duotone.Paper.R, _duotone.Paper.G, _duotone.Paper.B);
        }

        // Sub-cells under a module centre, and every finder, separator and alignment sub-cell, are fixed to their module's
        // colour on every screen, so the solid centre is at least as large as those cells. The binary screens then threshold
        // the rest; error diffusion passes on half of the difference a fixed cell makes, so the picture around it compensates
        // a little without undoing the module's scan margin.
        private void FixCentresAndScreen(BitMatrix modules, CancellationToken cancellationToken) {
            var fixedCell = new bool[_coverage.Length];
            var fixedValue = new bool[_coverage.Length];
            for (var y = 0; y < _grid; y++) for (var x = 0; x < _grid; x++) {
                var mx = x / _cells; var my = y / _cells;
                var kind = _kinds[my * _size + mx];
                var center = kind == OtherFunction ? Math.Max(_center, FunctionCenter) : _center;
                var u = (x % _cells + 0.5) / _cells - 0.5;
                var v = (y % _cells + 0.5) / _cells - 0.5;
                var inside = kind is Finder or Alignment
                    || (_round ? Math.Sqrt(u * u + v * v) : Math.Max(Math.Abs(u), Math.Abs(v))) < center / 2;
                if (!inside) continue;
                fixedCell[y * _grid + x] = true;
                fixedValue[y * _grid + x] = modules[mx, my];
            }
            if (_screen == QrHalftoneScreen.Dots) {
                for (var i = 0; i < _coverage.Length; i++) if (fixedCell[i]) _coverage[i] = fixedValue[i] ? 1 : 0;
                return;
            }
            if (_screen == QrHalftoneScreen.Ordered) {
                for (var y = 0; y < _grid; y++) for (var x = 0; x < _grid; x++) {
                    var i = y * _grid + x;
                    var inked = fixedCell[i] ? fixedValue[i] : _coverage[i] > (Bayer8[(y & 7) * 8 + (x & 7)] + 0.5) / 64;
                    _coverage[i] = inked ? 1 : 0;
                }
                return;
            }
            var current = new double[_grid + 2];
            var next = new double[_grid + 2];
            for (var y = 0; y < _grid; y++) {
                cancellationToken.ThrowIfCancellationRequested();
                var reverse = (y & 1) == 1;
                for (var step = 0; step < _grid; step++) {
                    var x = reverse ? _grid - 1 - step : step;
                    var i = y * _grid + x;
                    var value = _coverage[i] + current[x + 1];
                    bool inked;
                    double error;
                    if (fixedCell[i]) {
                        inked = fixedValue[i];
                        error = 0.5 * (value - (inked ? 1 : 0));
                    } else {
                        inked = value >= 0.5;
                        error = value - (inked ? 1 : 0);
                    }
                    _coverage[i] = inked ? 1 : 0;
                    var ahead = reverse ? x : x + 2;
                    var behind = reverse ? x + 2 : x;
                    current[ahead] += error * (7.0 / 16);
                    next[behind] += error * (3.0 / 16);
                    next[x + 1] += error * (5.0 / 16);
                    next[ahead] += error * (1.0 / 16);
                }
                var swap = current; current = next; next = swap;
                Array.Clear(next, 0, next.Length);
            }
        }

        /// <summary>Paints one symbol pixel. px/py are relative to the first module.</summary>
        internal void Paint(byte[] pixels, int index, int px, int py, bool dark) {
            var mx = px / _moduleSize;
            var my = py / _moduleSize;
            var kind = _kinds[my * _size + mx];
            var gx = (int)((long)px * _cells / _moduleSize);
            var gy = (int)((long)py * _cells / _moduleSize);
            var cell = gy * _grid + gx;
            // Module coordinates of the pixel centre.
            var u = (px + 0.5) / _moduleSize;
            var v = (py + 0.5) / _moduleSize;
            if (kind is Finder or Separator) {
                var cx = mx < 8 ? 3.5 : _size - 3.5;
                var cy = my < 8 ? 3.5 : _size - 3.5;
                // The separator keeps a light band that follows the ring; beyond it the picture continues.
                if (kind == Finder || Shape(u - cx, v - cy, 3.5 + SeparatorBand, 1.2 + SeparatorBand) > 0) {
                    PaintRings(pixels, index, u - cx, v - cy, cell, 3.5, 2.5, 1.5);
                    return;
                }
            }
            if (kind == Alignment) {
                var center = _alignmentCenter[my * _size + mx];
                PaintRings(pixels, index, u - (center % _size + 0.5), v - (center / _size + 0.5), cell, 2.5, 1.5, 0.5);
                return;
            }

            // Surround: the screened photo.
            var pixel = (double)_cells / _moduleSize;
            var fu = (px + 0.5) * _cells / _moduleSize - gx - 0.5;
            var fv = (py + 0.5) * _cells / _moduleSize - gy - 0.5;
            var a = _coverage[cell];
            double inkWeight;
            if (_screen == QrHalftoneScreen.Dots) {
                if (a <= 0.5) inkWeight = DotCoverage(fu, fv, a, pixel);
                else inkWeight = 1 - DotCoverage(fu, fv, 1 - a, pixel);
            } else {
                // Binary cells: square cells tile; round cells are discs slightly larger than the cell, clipped to it.
                inkWeight = a < 0.5 ? 0 : _round ? Clamp01(0.5 + (0.6 - Math.Sqrt(fu * fu + fv * fv)) / pixel) : 1;
            }
            Ink(cell, out var ir, out var ig, out var ib);
            Paper(cell, out var pr, out var pg, out var pb);
            var r = pr + (ir - pr) * inkWeight;
            var g = pg + (ig - pg) * inkWeight;
            var b = pb + (ib - pb) * inkWeight;

            // Centre: the module colour at full contrast.
            var centerSize = kind == OtherFunction ? Math.Max(_center, FunctionCenter * (_round ? 1.128 : 1)) : _center;
            var dx = u - mx - 0.5;
            var dy = v - my - 0.5;
            var distance = _round ? Math.Sqrt(dx * dx + dy * dy) : Math.Max(Math.Abs(dx), Math.Abs(dy));
            var centerWeight = Clamp01(0.5 + (centerSize / 2 - distance) * _moduleSize);
            if (centerWeight > 0) {
                var moduleCell = (my * _cells + _cells / 2) * _grid + mx * _cells + _cells / 2;
                double cr, cg, cb;
                if (dark) Deep(moduleCell, out cr, out cg, out cb);
                else Light(moduleCell, out cr, out cg, out cb);
                r += (cr - r) * centerWeight;
                g += (cg - g) * centerWeight;
                b += (cb - b) * centerWeight;
            }
            Write(pixels, index, r, g, b);
        }

        // Ink coverage of a dot (round or square) of the given area in a unit cell, antialiased over one pixel.
        private double DotCoverage(double fu, double fv, double area, double pixel) {
            if (area <= 0) return 0;
            var distance = _round ? Math.Sqrt(fu * fu + fv * fv) : Math.Max(Math.Abs(fu), Math.Abs(fv));
            var radius = _round ? Math.Sqrt(area / Math.PI) : Math.Sqrt(area) / 2;
            var coverage = Clamp01(0.5 + (radius - distance) / pixel);
            // A dot smaller than a pixel would vanish or alias; its average tone is kept instead.
            var dotPixels = radius * 2 / pixel;
            return dotPixels >= 1 ? coverage : area + (coverage - area) * dotPixels;
        }

        // Finder and alignment rings: dark outer ring, light gap, dark ball, with the configured finder geometry. Everything
        // else in the finder zone, including the separator, is light.
        private void PaintRings(byte[] pixels, int index, double x, double y, int cell, double outer, double gap, double ball) {
            // Corner radii keep every corner module centre on its own side of the ring, so module polarity is unchanged.
            var ring = Shape(x, y, outer, outer > 3 ? 1.2 : 1) * (1 - Shape(x, y, gap, 1));
            var darkWeight = Math.Min(1, ring + Shape(x, y, ball, ball >= 1 ? 1 : 0.5));
            Deep(cell, out var dr, out var dg, out var db);
            Light(cell, out var lr, out var lg, out var lb);
            Write(pixels, index, lr + (dr - lr) * darkWeight, lg + (dg - lg) * darkWeight, lb + (db - lb) * darkWeight);
        }

        private double Shape(double x, double y, double half, double radius) {
            x = Math.Abs(x); y = Math.Abs(y);
            double distance;
            switch (_finders) {
                case QrImageFinderStyle.Circular:
                    distance = Math.Sqrt(x * x + y * y) - half;
                    break;
                case QrImageFinderStyle.Chamfered:
                    distance = Math.Max(Math.Max(x, y) - half, (x + y - (2 * half - radius)) / Math.Sqrt(2));
                    break;
                case QrImageFinderStyle.Square:
                    distance = Math.Max(x, y) - half;
                    break;
                case QrImageFinderStyle.Rounded: {
                    var qx = x - (half - radius); var qy = y - (half - radius);
                    var ox = Math.Max(qx, 0); var oy = Math.Max(qy, 0);
                    distance = Math.Sqrt(ox * ox + oy * oy) + Math.Min(Math.Max(qx, qy), 0) - radius;
                    break;
                }
                default: {
                    var su = x / half; var sv = y / half;
                    distance = (Math.Pow(su * su * su * su + sv * sv * sv * sv, 0.25) - 1) * half;
                    break;
                }
            }
            return Clamp01(0.5 - distance * _moduleSize);
        }

        private void Ink(int cell, out double r, out double g, out double b) {
            if (_duotone is not null) { r = _duotone.Ink.R; g = _duotone.Ink.G; b = _duotone.Ink.B; return; }
            ApplyChroma(_red[cell], _green[cell], _blue[cell], _luma[cell], InkLuma, ColorBoost, true, out r, out g, out b);
        }

        private void Paper(int cell, out double r, out double g, out double b) {
            if (_duotone is not null) { r = _duotone.Paper.R; g = _duotone.Paper.G; b = _duotone.Paper.B; return; }
            ApplyChroma(_red[cell], _green[cell], _blue[cell], _luma[cell], PaperLuma, ColorBoost, false, out r, out g, out b);
        }

        private void Deep(int cell, out double r, out double g, out double b) {
            if (_duotone is not null) { r = _duotone.Ink.R; g = _duotone.Ink.G; b = _duotone.Ink.B; return; }
            ApplyChroma(_red[cell], _green[cell], _blue[cell], _luma[cell], DarkCenterLuma, ColorBoost, true, out r, out g, out b);
        }

        private void Light(int cell, out double r, out double g, out double b) {
            if (_duotone is not null) { r = _duotone.Paper.R; g = _duotone.Paper.G; b = _duotone.Paper.B; return; }
            ApplyChroma(_red[cell], _green[cell], _blue[cell], _luma[cell], LightCenterLuma, 1, false, out r, out g, out b);
        }

        private static double Clamp01(double value) => value < 0 ? 0 : value > 1 ? 1 : value;

        private static void Write(byte[] pixels, int index, double r, double g, double b) {
            pixels[index] = (byte)Math.Round(r < 0 ? 0 : r > 255 ? 255 : r);
            pixels[index + 1] = (byte)Math.Round(g < 0 ? 0 : g > 255 ? 255 : g);
            pixels[index + 2] = (byte)Math.Round(b < 0 ? 0 : b > 255 ? 255 : b);
        }
    }
}
