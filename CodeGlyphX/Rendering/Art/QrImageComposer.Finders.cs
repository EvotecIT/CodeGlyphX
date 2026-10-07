using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering.Art;

public static partial class QrImageComposer {
    private sealed class FinderGeometry {
        private readonly bool[] _outer, _hole, _dot;
        private readonly int _moduleSize, _size;
        private readonly Rgba32 _ink, _paper;

        internal FinderGeometry(int moduleSize, QrImageArtOptions options) {
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

        private bool Inside(bool[] mask, int modules, int inset, int x, int y) {
            x -= inset * _moduleSize; y -= inset * _moduleSize;
            var size = modules * _moduleSize;
            return x >= 0 && y >= 0 && x < size && y < size && mask[y * size + x];
        }
    }
}
