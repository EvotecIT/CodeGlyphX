// Recognition patterns and placement adapted from the Zint backend, Copyright (c) 2009-2024 Robin Stuart.
// Licensed under BSD-3-Clause; see THIRD-PARTY-NOTICES.md.
using System;
using CodeGlyphX.Internal;

namespace CodeGlyphX.CodeOne;

/// <summary>One reversible coordinate map shared by placement and extraction.</summary>
internal sealed partial class CodeOneLayout {
    private static readonly int[][] NumericBitOrder = { new[] { 0, 1, 2, 5, 6 }, new[] { 3, 4, 7, 8, 9 } };
    internal static readonly CodeOneLayout[] All = CreateLayouts();
    internal CodeOneVersion Version { get; }
    internal int Width => _finder.Width;
    internal int Height => _finder.Height;
    internal int DataCount { get; }
    internal int EccCount { get; }
    internal int Blocks { get; }
    internal int BitsPerWord { get; }
    private readonly int _gridWidth;
    private readonly BitMatrix _finder;
    private readonly int[] _positions;
    private readonly bool[] _dataCells;

    private CodeOneLayout(int version, int width, int height, int data, int ecc, int blocks, int gridWidth, int wordBits = 8) {
        Version = (CodeOneVersion)version; DataCount = data; EccCount = ecc; Blocks = blocks;
        _gridWidth = gridWidth; BitsPerWord = wordBits;
        _finder = new BitMatrix(width, height);
        _positions = new int[(data + ecc) * wordBits];
        for (var i = 0; i < _positions.Length; i++) _positions[i] = -1;
        _dataCells = new bool[width * height];
        if (version <= 8) BuildLargeGeometry();
        else BuildSmallGeometry();
        foreach (var position in _positions) if (position < 0) throw new InvalidOperationException("Incomplete Code One placement map.");
        _finder.Freeze();
    }

    private static CodeOneLayout[] CreateLayouts() => new[] {
        new CodeOneLayout(1, 18, 16, 10, 10, 1, 16),
        new CodeOneLayout(2, 22, 22, 19, 16, 1, 20),
        new CodeOneLayout(3, 32, 28, 44, 26, 1, 28),
        new CodeOneLayout(4, 42, 40, 91, 44, 1, 36),
        new CodeOneLayout(5, 54, 52, 182, 70, 1, 48),
        new CodeOneLayout(6, 76, 70, 370, 140, 2, 68),
        new CodeOneLayout(7, 98, 104, 732, 280, 4, 88),
        new CodeOneLayout(8, 134, 148, 1480, 560, 8, 120),
        new CodeOneLayout(9, 11, 8, 4, 4, 1, 10, 5),
        new CodeOneLayout(10, 21, 8, 8, 8, 1, 20, 5),
        new CodeOneLayout(11, 31, 8, 12, 12, 1, 30, 5),
        new CodeOneLayout(12, 17, 16, 10, 10, 1, 16),
        new CodeOneLayout(13, 33, 16, 24, 16, 1, 32),
        new CodeOneLayout(14, 49, 16, 38, 22, 1, 48)
    };

    internal BitMatrix Place(int[] words) {
        if (words.Length * BitsPerWord != _positions.Length) throw new ArgumentException("Incorrect Code One codeword count.", nameof(words));
        var grid = _finder.Clone();
        for (var i = 0; i < _positions.Length; i++) {
            var position = _positions[i];
            grid[position % Width, position / Width] = (words[i / BitsPerWord] & (1 << (BitsPerWord - 1 - i % BitsPerWord))) != 0;
        }
        return grid;
    }

    internal bool TryExtract(BitMatrixView grid, out int[] words) {
        words = Array.Empty<int>();
        if (grid.Width != Width || grid.Height != Height) return false;
        for (var y = 0; y < Height; y++)
            for (var x = 0; x < Width; x++)
                if (!_dataCells[y * Width + x] && grid.Get(x, y) != _finder[x, y]) return false;
        words = new int[_positions.Length / BitsPerWord];
        for (var i = 0; i < _positions.Length; i++) {
            var position = _positions[i];
            words[i / BitsPerWord] = (words[i / BitsPerWord] << 1) | (grid.Get(position % Width, position / Width) ? 1 : 0);
        }
        return true;
    }

    private void MapBlock(int sourceRow, int sourceColumn, int rows, int columns, int rowOffset, int columnOffset) {
        for (var y = sourceRow; y < sourceRow + rows; y++) {
            for (var x = sourceColumn; x < sourceColumn + columns; x++) {
                var bit = BitsPerWord == 8 ? ((y / 2 * (_gridWidth / 4) + x / 4) * 8) + y % 2 * 4 + x % 4
                    : (y / 2 * (_gridWidth / 5) + x / 5) * 10 + NumericBitOrder[y % 2][x % 5];
                var position = (y + rowOffset) * Width + x + columnOffset;
                if (_positions[bit] != -1 || _dataCells[position]) throw new InvalidOperationException("Duplicate Code One placement cell.");
                _positions[bit] = position;
                _dataCells[position] = true;
            }
        }
    }

    private void Horizontal(int row, bool full) {
        for (var x = full ? 0 : 1; x < Width - (full ? 0 : 1); x++) _finder[x, row] = true;
    }
    private void CentralFinder(int start, int count, int fullRows) {
        for (var i = 0; i < count; i++) {
            Horizontal(start + i * 2, i < fullRows);
            if (i >= fullRows && i != count - 1) {
                _finder[1, start + i * 2 + 1] = true;
                _finder[Width - 2, start + i * 2 + 1] = true;
            }
        }
    }
    private void Vertical(int column, int height, int top) {
        for (var i = 0; i < height; i++) _finder[column, top == 1 ? i : Height - i - 1] = true;
    }
    private void Spigot(int row) {
        for (var x = Width - 1; x > 0; x--) if (_finder[x - 1, row]) _finder[x, row] = true;
    }

    private void BuildSmallGeometry() {
        var numeric = BitsPerWord == 5;
        var start = numeric ? 5 : 11;
        for (var y = start; y < Height; y += 2) {
            Horizontal(y, true);
            if (y > start) { _finder[1, y] = false; _finder[Width - 2, y] = false; }
            if (y + 1 < Height) { _finder[0, y + 1] = true; _finder[Width - 1, y + 1] = true; }
        }
        var middle = Width / 2;
        _finder[middle, 0] = true;
        if (!numeric || Version != CodeOneVersion.S10) _finder[middle, start - 1] = true;
        if (Version is CodeOneVersion.S30 or CodeOneVersion.T32 or CodeOneVersion.T48) _finder[middle, start + 1] = true;
        if (Version == CodeOneVersion.T48) _finder[middle, start + 3] = true;
        MapBlock(0, 0, start - 1, middle, 0, 0);
        MapBlock(0, middle, start - 1, middle, 0, 1);
    }
}
