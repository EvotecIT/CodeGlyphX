using System;
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

        internal ArtGeometry(BitMatrix modules, BitMatrix functions, int moduleSize, QrImageArtOptions options,
            QrImageSampler image, int imageX, int imageY) {
            _modules = modules;
            _functions = functions;
            _moduleSize = moduleSize;
            _maskSize = moduleSize * 4;
            _mask = QrPngRenderer.BuildModuleMask(_maskSize, options.Shape, options.Scale, 0);
            _connected = options.Shape == QrPngModuleShape.ConnectedRounded || options.Shape == QrPngModuleShape.ConnectedSquircle;
            _cells = new CellGeometry[modules.Width * modules.Height];
            for (var y = 0; y < modules.Height; y++) {
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
                    _cells[y * modules.Width + x] = new CellGeometry(
                        1 - 0.2 * detail * options.DetailProtection,
                        _connected ? 0 : shiftX, _connected ? 0 : shiftY, Math.Cos(angle), Math.Sin(angle));
                }
            }
        }

        internal void Paint(byte[] pixels, int index, int mx, int my, int x, int y, bool dark,
            double r, double g, double b, double strength) {
            var cell = _cells[my * _modules.Width + mx];
            var coverage = 0.0;
            for (var sy = 0; sy < 2; sy++) for (var sx = 0; sx < 2; sx++) {
                var u = (x + 0.25 + sx * 0.5) / _moduleSize;
                var v = (y + 0.25 + sy * 0.5) / _moduleSize;
                if (Inside(cell, mx, my, u, v, dark)) coverage += 0.25;
            }
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

        private bool Inside(CellGeometry cell, int mx, int my, double u, double v, bool dark) {
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
            if (rx < 0 || ry < 0 || rx >= 1 || ry >= 1) return false;
            return _mask[(int)(ry * _maskSize) * _maskSize + (int)(rx * _maskSize)];
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
            internal CellGeometry(double scale, double shiftX, double shiftY, double cos, double sin) {
                Scale = scale;
                ShiftX = shiftX;
                ShiftY = shiftY;
                Cos = cos;
                Sin = sin;
            }
        }
    }
}
