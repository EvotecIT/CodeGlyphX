using System;
using System.Threading;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering.Art;

public static partial class QrImageComposer {
    /// <summary>Per-cell image analysis and antialiased silhouettes over the shared PNG module masks.</summary>
    private sealed class ArtGeometry {
        private readonly BitMatrix _modules;
        private readonly BitMatrix _functions;
        private readonly CellGeometry[] _cells;
        private readonly bool[] _mask;
        private readonly int _maskSize;
        private readonly int _moduleSize;
        private readonly bool _connected;
        private readonly QrImageArtStyle _style;
        private readonly double _nominalScale;
        private readonly Rgba32 _ink;
        private readonly Rgba32 _paper;

        internal ArtGeometry(BitMatrix modules, BitMatrix functions, int moduleSize, QrImageArtOptions options,
            QrImageSampler image, int imageX, int imageY, CancellationToken cancellationToken) {
            _style = options.Style;
            _nominalScale = options.Scale;
            _ink = options.FunctionalForeground;
            _paper = options.FunctionalBackground;
            _modules = modules;
            _functions = functions;
            _moduleSize = moduleSize;
            _maskSize = moduleSize * 4;
            _mask = QrPngRenderer.BuildModuleMask(_maskSize, options.Shape, options.Scale, 0);
            _connected = options.Style == QrImageArtStyle.ModuleShape && (options.Shape == QrPngModuleShape.ConnectedRounded || options.Shape == QrPngModuleShape.ConnectedSquircle);
            _cells = new CellGeometry[modules.Width * modules.Height];
            for (var y = 0; y < modules.Height; y++) {
                cancellationToken.ThrowIfCancellationRequested();
                for (var x = 0; x < modules.Width; x++) {
                    if (functions[x, y]) continue;
                    var left = imageX + (x + 0.2) * moduleSize;
                    var right = imageX + (x + 0.8) * moduleSize;
                    var top = imageY + (y + 0.2) * moduleSize;
                    var bottom = imageY + (y + 0.8) * moduleSize;
                    var tl = Luminance(image, left, top);
                    var tr = Luminance(image, right, top);
                    var bl = Luminance(image, left, bottom);
                    var br = Luminance(image, right, bottom);
                    var detail = Math.Min(1, (Math.Abs(tl - tr) + Math.Abs(bl - br) + Math.Abs(tl - bl) + Math.Abs(tr - br)) / 255);
                    var shiftX = (Math.Abs(tl - bl) - Math.Abs(tr - br)) / 255 * 0.12 * options.DetailProtection;
                    var shiftY = (Math.Abs(tl - tr) - Math.Abs(bl - br)) / 255 * 0.12 * options.DetailProtection;
                    var organic = options.Shape == QrPngModuleShape.Blob || options.Shape == QrPngModuleShape.Leaf;
                    var angle = organic ? Math.Sin(x * 12.9898 + y * 78.233) * 0.5 : 0;
                    if (_style != QrImageArtStyle.ModuleShape) angle = Math.Atan2(bl + br - tl - tr, tr + br - tl - bl) + Math.PI / 4;
                    _cells[y * modules.Width + x] = new CellGeometry(
                        1 - 0.2 * detail * options.DetailProtection,
                        _connected ? 0 : shiftX, _connected ? 0 : shiftY, Math.Cos(angle), Math.Sin(angle), image.Protection(imageX + (x + 0.5) * moduleSize, imageY + (y + 0.5) * moduleSize, options.Subject), (tl + tr + bl + br) / (4 * 255), detail);
                }
            }
        }

        internal void Paint(byte[] pixels, int index, int mx, int my, int x, int y, bool dark,
            double r, double g, double b, double strength) {
            var cell = _cells[my * _modules.Width + mx];
            ApplyTreatment(ref r, ref g, ref b, cell, mx, my, x, y);
            var coverage = 0.0;
            for (var sy = 0; sy < 2; sy++) for (var sx = 0; sx < 2; sx++) {
                var u = (x + 0.25 + sx * 0.5) / _moduleSize;
                var v = (y + 0.25 + sy * 0.5) / _moduleSize;
                if (Inside(cell, mx, my, u, v, dark)) coverage += 0.25;
            }
            coverage *= 1 - 0.85 * cell.Protection;
            var cx = (x + 0.5) / _moduleSize - 0.5;
            var cy = (y + 0.5) / _moduleSize - 0.5;
            // The decorative silhouette may move, but this circular scan anchor never does.
            var anchor = Math.Max(0, Math.Min(1, (0.22 - Math.Sqrt(cx * cx + cy * cy)) / 0.06));
            var luminance = 0.299 * r + 0.587 * g + 0.114 * b;
            var silhouette = ColorAmount(luminance, dark, 28, 227);
            var center = ColorAmount(luminance, dark, 8, 247);
            var amount = 1 + (silhouette - 1) * coverage;
            amount += (center - amount) * anchor;
            amount *= strength;
            pixels[index] = Adapt(r, dark, amount);
            pixels[index + 1] = Adapt(g, dark, amount);
            pixels[index + 2] = Adapt(b, dark, amount);
        }

        private void ApplyTreatment(ref double r, ref double g, ref double b, CellGeometry cell, int mx, int my, int px, int py) {
            if (_style == QrImageArtStyle.ModuleShape || _style == QrImageArtStyle.Botanical
                || _style == QrImageArtStyle.Ribbons || _style == QrImageArtStyle.Weave) return;
            var luminance = (0.299 * r + 0.587 * g + 0.114 * b) / 255;
            double nr = r, ng = g, nb = b;
            if (_style == QrImageArtStyle.Mosaic) {
                // Restrict the palette to broad tessera-like tones without changing the scan anchors.
                nr = Math.Round(r / 42.5) * 42.5;
                ng = Math.Round(g / 42.5) * 42.5;
                nb = Math.Round(b / 42.5) * 42.5;
            } else {
                var u = mx + px / (double)_moduleSize;
                var v = my + py / (double)_moduleSize;
                double tone;
                if (_style == QrImageArtStyle.Engraving) {
                    var stroke = 0.5 + 0.5 * Math.Sin((u + v) * Math.PI * 4);
                    tone = stroke < 1 - luminance ? 0.18 : 0.94;
                } else if (_style == QrImageArtStyle.Contours) {
                    var band = luminance * 9;
                    var edge = Math.Abs(band - Math.Round(band));
                    tone = cell.Detail > 0.08 && edge < 0.12 ? 0.2 : 0.65 + 0.35 * luminance;
                } else {
                    var dx = u - Math.Floor(u) - 0.5;
                    var dy = v - Math.Floor(v) - 0.5;
                    tone = dx * dx + dy * dy < (1 - luminance) * 0.32 ? 0.15 : 0.96;
                }
                nr = _ink.R + (_paper.R - _ink.R) * tone;
                ng = _ink.G + (_paper.G - _ink.G) * tone;
                nb = _ink.B + (_paper.B - _ink.B) * tone;
            }
            // A protected subject retains the supplied image rather than inheriting the print treatment.
            var treatment = 1 - cell.Protection;
            r += (nr - r) * treatment;
            g += (ng - g) * treatment;
            b += (nb - b) * treatment;
        }

        private bool Inside(CellGeometry cell, int mx, int my, double u, double v, bool dark) {
            if (_style == QrImageArtStyle.Ribbons || _style == QrImageArtStyle.Weave)
                return InsideFlow(mx, my, u, v, dark);
            if (_connected) {
                const double halfBridge = 0.22;
                if (Math.Abs(v - 0.5) <= halfBridge &&
                    ((u <= 0.5 && SameData(mx - 1, my, dark)) || (u >= 0.5 && SameData(mx + 1, my, dark)))) return true;
                if (Math.Abs(u - 0.5) <= halfBridge &&
                    ((v <= 0.5 && SameData(mx, my - 1, dark)) || (v >= 0.5 && SameData(mx, my + 1, dark)))) return true;
            }
            var dx = (u - 0.5 - cell.ShiftX) / cell.Scale;
            var dy = (v - 0.5 - cell.ShiftY) / cell.Scale;
            var rx = dx * cell.Cos - dy * cell.Sin + 0.5;
            var ry = dx * cell.Sin + dy * cell.Cos + 0.5;
            if (_style != QrImageArtStyle.ModuleShape) return InsideStyle(dx, dy, cell, mx, my, dark);
            if (rx < 0 || ry < 0 || rx >= 1 || ry >= 1) return false;
            return _mask[(int)(ry * _maskSize) * _maskSize + (int)(rx * _maskSize)];
        }

        private bool InsideStyle(double dx, double dy, CellGeometry cell, int mx, int my, bool dark) {
            var x = (dx * cell.Cos - dy * cell.Sin) / _nominalScale;
            var y = (dx * cell.Sin + dy * cell.Cos) / _nominalScale;
            var radius = Math.Sqrt(x * x + y * y);
            switch (_style) {
                case QrImageArtStyle.Engraving:
                    return Math.Abs(x) < 0.5 && Math.Abs(y) < 0.5 && Math.Abs(Math.Sin((y + 0.5) * Math.PI * 3)) > 0.5;
                case QrImageArtStyle.Halftone:
                    var tone = dark ? 1 - cell.Luminance : cell.Luminance;
                    return radius < 0.3 + 0.2 * tone;
                case QrImageArtStyle.Contours:
                    return radius < 0.55 && Math.Abs(Math.Sin((y + 0.12 * Math.Sin(x * 5 + mx * 0.4 + my * 0.3)) * Math.PI * 3)) > 0.55;
                case QrImageArtStyle.Mosaic:
                    return Math.Abs(x) < 0.48 && Math.Abs(y) < 0.48 && Math.Abs(x) + Math.Abs(y) < 0.72;
                case QrImageArtStyle.Botanical:
                    return (radius < 0.53 && Math.Abs(y) < 0.32 * (1 - Math.Abs(x) / 0.53))
                        || (Math.Abs(y) < 0.035 && Math.Abs(x) < 0.55);
                default:
                    return false;
            }
        }

        private bool InsideFlow(int mx, int my, double u, double v, bool dark) {
            var x = u - 0.5;
            var y = v - 0.5;
            var woven = _style == QrImageArtStyle.Weave;
            var width = woven ? 0.085 : 0.16;
            // Every strand meets its neighbor at the edge midpoint. Curvature vanishes at both ends.
            var horizontal = SameData(mx + (x < 0 ? -1 : 1), my, dark);
            var vertical = SameData(mx, my + (y < 0 ? -1 : 1), dark);
            var bendX = 0.10 * Math.Sin(Math.Abs(x) * Math.PI * 2);
            var bendY = 0.10 * Math.Sin(Math.Abs(y) * Math.PI * 2);
            var across = horizontal && Math.Abs(y - bendX) < width;
            var down = vertical && Math.Abs(x + bendY) < width;
            if (woven && horizontal && vertical && Math.Abs(x) < 0.2 && Math.Abs(y) < 0.2)
                return (mx + my) % 2 == 0 ? across : down;
            return across || down || x * x + y * y < width * width;
        }

        private bool SameData(int x, int y, bool dark) =>
            x >= 0 && y >= 0 && x < _modules.Width && y < _modules.Height && !_functions[x, y] && _modules[x, y] == dark;

        private static double ColorAmount(double luminance, bool dark, double maxDark, double minLight) {
            if (dark) return luminance > maxDark ? maxDark / luminance : 1;
            return luminance < minLight ? (255 - minLight) / (255 - luminance) : 1;
        }

        private static double Luminance(QrImageSampler image, double x, double y) {
            image.Sample(x, y, out var r, out var g, out var b);
            return 0.299 * r + 0.587 * g + 0.114 * b;
        }

        private readonly struct CellGeometry {
            internal readonly double Scale;
            internal readonly double ShiftX;
            internal readonly double ShiftY;
            internal readonly double Cos;
            internal readonly double Sin;
            internal readonly double Protection;
            internal readonly double Luminance;
            internal readonly double Detail;
            internal CellGeometry(double scale, double shiftX, double shiftY, double cos, double sin, double protection, double luminance, double detail) {
                Scale = scale;
                ShiftX = shiftX;
                ShiftY = shiftY;
                Cos = cos;
                Sin = sin;
                Protection = protection;
                Luminance = luminance;
                Detail = detail;
            }
        }
    }
}
