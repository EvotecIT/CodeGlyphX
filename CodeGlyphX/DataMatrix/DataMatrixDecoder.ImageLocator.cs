using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading;
using CodeGlyphX.Internal;

#if NET8_0_OR_GREATER
using PixelSpan = System.ReadOnlySpan<byte>;
#else
using PixelSpan = byte[];
#endif

namespace CodeGlyphX.DataMatrix;

public static partial class DataMatrixDecoder {
    private readonly struct ImagePoint {
        public readonly double X;
        public readonly double Y;
        public ImagePoint(double x, double y) { X = x; Y = y; }
    }

    private struct ImageComponent {
        public int Area;
        private ImagePoint _topLeft;
        private ImagePoint _topRight;
        private ImagePoint _bottomRight;
        private ImagePoint _bottomLeft;
        public ImageComponent(int x, int y) {
            Area = 0;
            _topLeft = _topRight = _bottomRight = _bottomLeft = new ImagePoint(x, y);
        }
        public ImagePoint Corner(int index) => index == 0 ? _topLeft : index == 1 ? _topRight : index == 2 ? _bottomRight : _bottomLeft;
        public void Add(int x, int y) {
            Area++;
            if (x + y < _topLeft.X + _topLeft.Y) _topLeft = new ImagePoint(x, y);
            if (x - y > _topRight.X - _topRight.Y) _topRight = new ImagePoint(x, y);
            if (x + y > _bottomRight.X + _bottomRight.Y) _bottomRight = new ImagePoint(x, y);
            if (x - y < _bottomLeft.X - _bottomLeft.Y) _bottomLeft = new ImagePoint(x, y);
        }
    }

    /// <summary>
    /// Finds connected finder-border candidates in a bounded binary image. Only
    /// the eight largest usable components survive; every rented buffer is
    /// returned on success, cancellation, and rejection.
    /// </summary>
    private static List<ImageComponent> FindImageComponents(byte[] mask, int width, int height,
        bool invert, CancellationToken cancellationToken) {
        var count = width * height;
        var components = new List<ImageComponent>(8);
        var queue = ArrayPool<int>.Shared.Rent(count);
        try {
            var minimumArea = Math.Max(100, count / 1000);
            for (var at = 0; at < count; at++) {
                if ((at & 511) == 0 && DecodeBudget.ShouldAbort(cancellationToken)) break;
                if ((mask[at] & 2) != 0 || ((mask[at] & 1) != 0) == invert) continue;
                var component = new ImageComponent(at % width, at / width);
                mask[at] |= 2;
                queue[0] = at;
                var length = 1;
                var touchesEdge = false;
                for (var head = 0; head < length; head++) {
                    if ((head & 511) == 0 && DecodeBudget.ShouldAbort(cancellationToken)) return components;
                    var current = queue[head];
                    var x = current % width;
                    var y = current / width;
                    component.Add(x, y);
                    if (x == 0 || y == 0 || x == width - 1 || y == height - 1) touchesEdge = true;
                    if (x > 0) EnqueueImagePixel(mask, current - 1, invert, queue, ref length);
                    if (x + 1 < width) EnqueueImagePixel(mask, current + 1, invert, queue, ref length);
                    if (y > 0) EnqueueImagePixel(mask, current - width, invert, queue, ref length);
                    if (y + 1 < height) EnqueueImagePixel(mask, current + width, invert, queue, ref length);
                }
                if (touchesEdge || component.Area < minimumArea || component.Area > count * 4L / 5) continue;
                var position = 0;
                while (position < components.Count && components[position].Area >= component.Area) position++;
                if (position >= 8) continue;
                components.Insert(position, component);
                if (components.Count > 8) components.RemoveAt(8);
            }
        } finally {
            ArrayPool<int>.Shared.Return(queue);
        }
        return components;
    }

    private static void EnqueueImagePixel(byte[] mask, int at, bool invert, int[] queue, ref int length) {
        if ((mask[at] & 2) != 0 || ((mask[at] & 1) != 0) == invert) return;
        mask[at] |= 2;
        queue[length++] = at;
    }

    private static bool ImageBit(byte[] mask, int width, int height, double x, double y, bool invert) {
        if (double.IsNaN(x) || double.IsNaN(y) || x < 0 || y < 0 || x > width - 1 || y > height - 1) return false;
        var ix = (int)Math.Round(x);
        var iy = (int)Math.Round(y);
        return ((mask[iy * width + ix] & 1) != 0) != invert;
    }

    private static double ImageDistance(ImagePoint a, ImagePoint b) {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    // Pixel extrema lie on the outside of the border. A one-pixel neighborhood
    // tolerates raster stair steps while the later clock and region checks
    // require a complete Data Matrix frame.
    private static bool HasSolidImageEdge(byte[] mask, int width, int height,
        ImagePoint a, ImagePoint b, bool invert) {
        var hits = 0;
        for (var i = 0; i < 64; i++) {
            var f = i / 63.0;
            var x = a.X + (b.X - a.X) * f;
            var y = a.Y + (b.Y - a.Y) * f;
            if (ImageBit(mask, width, height, x, y, invert) ||
                ImageBit(mask, width, height, x - 1, y, invert) || ImageBit(mask, width, height, x + 1, y, invert) ||
                ImageBit(mask, width, height, x, y - 1, invert) || ImageBit(mask, width, height, x, y + 1, invert)) hits++;
        }
        return hits >= 55;
    }
}
