using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering.Art;

public static partial class QrImageComposer {
    private sealed class FinderGeometry {
        private readonly bool[] _outer, _hole, _dot;
        private readonly int _moduleSize, _size;
        private readonly Rgba32 _ink, _paper;
        private readonly QrImageFinderStyle _style;

        internal FinderGeometry(int moduleSize, QrImageArtOptions options) {
            _style = options.Finders;
            _moduleSize = moduleSize;
            _size = 7 * moduleSize;
            _ink = options.FunctionalForeground;
            _paper = options.FunctionalBackground;
            var shape = options.Finders switch {
                QrImageFinderStyle.Rounded => QrModuleShape.Rounded,
                QrImageFinderStyle.Circular => QrModuleShape.Circle,
                QrImageFinderStyle.Chamfered => QrModuleShape.Square,
                _ => QrModuleShape.Squircle
            };
            _outer = QrPngRenderer.BuildModuleMask(_size, shape, 1, moduleSize * 2);
            _hole = QrPngRenderer.BuildModuleMask(5 * moduleSize, shape, 1, moduleSize);
            _dot = QrPngRenderer.BuildModuleMask(3 * moduleSize, shape, 1, moduleSize);
            if (options.Finders == QrImageFinderStyle.Chamfered) {
                ClipCorners(_outer, _size, moduleSize);
                ClipCorners(_hole, 5 * moduleSize, moduleSize);
                ClipCorners(_dot, 3 * moduleSize, moduleSize / 2);
            }
        }

        private static void ClipCorners(bool[] mask, int size, int cut) {
            for (var y = 0; y < size; y++) for (var x = 0; x < size; x++) {
                if (System.Math.Min(x, size - 1 - x) + System.Math.Min(y, size - 1 - y) < cut) mask[y * size + x] = false;
            }
        }

        internal bool TryPaint(byte[] pixels, int index, int x, int y, int qrSize) {
            var far = (qrSize - 7) * _moduleSize;
            if (x >= far && y < _size) x -= far;
            else if (y >= far && x < _size) y -= far;
            else if (x >= _size || y >= _size) return false;
            var dark = _outer[y * _size + x];
            if (Inside(_hole, 5, 1, x, y)) dark = false;
            if (Inside(_dot, 3, 2, x, y)) dark = true;
            WriteColor(pixels, index, dark ? _ink : _paper);
            return true;
        }

        internal bool Contains(int x, int y, int qrSize) {
            var far = (qrSize - 7) * _moduleSize;
            return (x < _size && y < _size) || (x >= far && y < _size) || (y >= far && x < _size);
        }

        // Tinted finders use analytic signed distances, giving smooth ring and ball edges at any module size.
        internal void PaintTinted(byte[] pixels, int index, int x, int y, int qrSize, double r, double g, double b, double strength, double tint, double lightFade) {
            var far = (qrSize - 7) * _moduleSize;
            if (x >= far) x -= far;
            if (y >= far) y -= far;
            var m = (double)_moduleSize;
            var dx = x + 0.5 - 3.5 * m; var dy = y + 0.5 - 3.5 * m;
            var ring = Coverage(dx, dy, 3.5 * m, 2 * m, m) * (1 - Coverage(dx, dy, 2.5 * m, m, m));
            var dark = System.Math.Min(1, ring + Coverage(dx, dy, 1.5 * m, m, m / 2));
            QrImageComposer.PaintTinted(pixels, index, r, g, b, dark, strength, tint, _ink, _paper, lightFade);
        }

        private double Coverage(double x, double y, double half, double radius, double chamfer) {
            x = System.Math.Abs(x); y = System.Math.Abs(y);
            double distance;
            switch (_style) {
                case QrImageFinderStyle.Circular:
                    distance = System.Math.Sqrt(x * x + y * y) - half;
                    break;
                case QrImageFinderStyle.Chamfered:
                    distance = System.Math.Max(System.Math.Max(x, y) - half, (x + y - (2 * half - chamfer)) / System.Math.Sqrt(2));
                    break;
                case QrImageFinderStyle.Rounded:
                    var qx = x - (half - radius); var qy = y - (half - radius);
                    var ox = System.Math.Max(qx, 0); var oy = System.Math.Max(qy, 0);
                    distance = System.Math.Sqrt(ox * ox + oy * oy) + System.Math.Min(System.Math.Max(qx, qy), 0) - radius;
                    break;
                default:
                    // A superellipse level set, scaled to approximate pixel distance near its edge.
                    var u = x / half; var v = y / half;
                    distance = (System.Math.Pow(u * u * u * u + v * v * v * v, 0.25) - 1) * half;
                    break;
            }
            return System.Math.Max(0, System.Math.Min(1, 0.5 - distance));
        }

        private bool Inside(bool[] mask, int modules, int inset, int x, int y) {
            x -= inset * _moduleSize; y -= inset * _moduleSize;
            var size = modules * _moduleSize;
            return x >= 0 && y >= 0 && x < size && y < size && mask[y * size + x];
        }
    }
}
