using System;

namespace CodeGlyphX.Internal;

// A crop and orientation expressed in the original matrix's flat coordinates.
// Composing regions never copies cells or changes the underlying storage.
internal readonly struct BitMatrixRegion {
    private readonly int _x;
    private readonly int _y;
    private readonly int _xx;
    private readonly int _xy;
    private readonly int _yx;
    private readonly int _yy;
    internal int Width { get; }
    internal int Height { get; }

    internal BitMatrixRegion(int width, int height) : this(width, height, 0, 0, 1, 0, 0, 1) { }

    private BitMatrixRegion(int width, int height, int x, int y, int xx, int xy, int yx, int yy) {
        Width = width;
        Height = height;
        _x = x; _y = y;
        _xx = xx; _xy = xy; _yx = yx; _yy = yy;
    }

    internal void GetCoordinates(int x, int y, out int sourceX, out int sourceY) {
        if ((uint)x >= (uint)Width) throw new ArgumentOutOfRangeException(nameof(x));
        if ((uint)y >= (uint)Height) throw new ArgumentOutOfRangeException(nameof(y));
        sourceX = _x + x * _xx + y * _yx;
        sourceY = _y + x * _xy + y * _yy;
    }

    internal BitMatrixRegion Rotate(int quarterTurns) => (quarterTurns & 3) switch {
        1 => new(Height, Width, _x + (Height - 1) * _yx, _y + (Height - 1) * _yy, -_yx, -_yy, _xx, _xy),
        2 => new(Width, Height, _x + (Width - 1) * _xx + (Height - 1) * _yx,
            _y + (Width - 1) * _xy + (Height - 1) * _yy, -_xx, -_xy, -_yx, -_yy),
        3 => new(Height, Width, _x + (Width - 1) * _xx, _y + (Width - 1) * _xy, _yx, _yy, -_xx, -_xy),
        _ => this
    };

    internal BitMatrixRegion Mirror() => new(Width, Height, _x + (Width - 1) * _xx,
        _y + (Width - 1) * _xy, -_xx, -_xy, _yx, _yy);

    internal BitMatrixRegion Crop(int left, int top, int width, int height) {
        if (left < 0 || width <= 0 || left > Width - width) throw new ArgumentOutOfRangeException(nameof(left));
        if (top < 0 || height <= 0 || top > Height - height) throw new ArgumentOutOfRangeException(nameof(top));
        return new(width, height, _x + left * _xx + top * _yx, _y + left * _xy + top * _yy, _xx, _xy, _yx, _yy);
    }

    internal bool IsSame(BitMatrixRegion other) => Width == other.Width && Height == other.Height &&
        _x == other._x && _y == other._y && _xx == other._xx && _xy == other._xy && _yx == other._yx && _yy == other._yy;
}
