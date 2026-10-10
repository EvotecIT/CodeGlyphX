using System;

namespace CodeGlyphX.Internal;

// Inspect an orientation without copying the whole candidate. Materialize only
// after its framing and metadata qualify it for payload decoding.
internal readonly struct BitMatrixView {
    private readonly BitMatrix? _source;
    private readonly Func<int, int, bool>? _sample;
    private readonly int _width;
    private readonly int _height;
    private readonly int _rotation;
    private readonly bool _mirrored;

    internal BitMatrixView(BitMatrix source, int rotation = 0, bool mirrored = false) {
        _source = source;
        _sample = null;
        _width = source.Width;
        _height = source.Height;
        _rotation = rotation & 3;
        _mirrored = mirrored;
    }

    internal BitMatrixView(int width, int height, Func<int, int, bool> sample) {
        _source = null;
        _sample = sample;
        _width = width;
        _height = height;
        _rotation = 0;
        _mirrored = false;
    }

    private BitMatrixView(BitMatrixView source, int rotation, bool mirrored) {
        _source = source._source;
        _sample = source._sample;
        _width = source._width;
        _height = source._height;
        _rotation = rotation & 3;
        _mirrored = mirrored;
    }

    internal BitMatrixView Orient(int rotation, bool mirrored) => new(this, rotation, mirrored);

    internal int Width => (_rotation & 1) == 0 ? _width : _height;
    internal int Height => (_rotation & 1) == 0 ? _height : _width;

    internal bool Get(int x, int y) {
        if ((uint)x >= (uint)Width) throw new ArgumentOutOfRangeException(nameof(x));
        if ((uint)y >= (uint)Height) throw new ArgumentOutOfRangeException(nameof(y));
        var sx = x;
        var sy = y;
        switch (_rotation) {
            case 1: sx = y; sy = _height - 1 - x; break;
            case 2: sx = _width - 1 - x; sy = _height - 1 - y; break;
            case 3: sx = _width - 1 - y; sy = x; break;
        }
        if (_mirrored) sx = _width - 1 - sx;
        return _source is not null ? _source.Get(sx, sy) : _sample!(sx, sy);
    }

    internal BitMatrix ToMatrix() {
        if (_source is null) {
            var result = new BitMatrix(Width, Height);
            for (var y = 0; y < Height; y++)
                for (var x = 0; x < Width; x++) if (Get(x, y)) result.Set(x, y, true);
            return result;
        }
        var matrix = _mirrored ? BitMatrixTransforms.MirrorHorizontal(_source) : _source;
        return _rotation switch {
            1 => BitMatrixTransforms.Rotate90(matrix),
            2 => BitMatrixTransforms.Rotate180(matrix),
            3 => BitMatrixTransforms.Rotate270(matrix),
            _ => matrix
        };
    }
}
