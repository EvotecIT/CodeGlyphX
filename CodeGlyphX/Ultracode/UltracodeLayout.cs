using System;
using System.Collections.Generic;

namespace CodeGlyphX.Ultracode;

internal sealed class UltracodeLayout {
    private static readonly int[] Factors = { 0, 1, 2, 4, 6, 8 };
    private static readonly int[] MinimumColumns = { 5, 13, 22, 29 };
    private static readonly Dictionary<int, int> TileValues = CreateTileValues();
    internal int Rows { get; }
    internal int DataColumns { get; }
    internal int Columns => DataColumns + DataColumns / 15;
    internal int Width => Columns + 6;
    internal int Height => Rows * 6 + 1;
    internal int TileCount => Rows * DataColumns;
    internal int Dcc => Columns - MinimumColumns[Rows - 2];

    internal UltracodeLayout(int messageCount, int parityCount) {
        var total = messageCount + parityCount + 3;
        var size = total - 6;
        Rows = size <= 37 ? 2 : size <= 84 ? 3 : size <= 161 ? 4 : 5;
        DataColumns = (total + Rows - 1) / Rows;
    }

    private UltracodeLayout((int Rows, int Columns) geometry) { Rows = geometry.Rows; DataColumns = geometry.Columns; }

    internal static UltracodeLayout? FromDimensions(int width, int height) {
        if (height < 13 || height > 31 || (height - 1) % 6 != 0 || width < 11 || width > 66) return null;
        var rows = (height - 1) / 6;
        for (var columns = 1; columns <= 57; columns++) {
            if (columns + columns / 15 != width - 6) continue;
            var layout = new UltracodeLayout((rows, columns));
            return layout.Dcc is >= 0 and < 32 ? layout : null;
        }
        return null;
    }

    internal static int ParityCount(int messageCount, UltracodeErrorCorrectionLevel level) =>
        Factors[(int)level] * ((messageCount + 24) / 25) + 5;

    internal UltracodeGrid Place(int[] words) {
        if (words.Length != TileCount) throw new ArgumentException("The tile count does not match the layout.", nameof(words));
        var cells = Frame();
        for (var i = 0; i < words.Length; i++) {
            TilePosition(i, out var x, out var y);
            WriteTile(cells, x, y, UltracodeTables.Tiles[words[i]]);
        }
        WriteTile(cells, 2, (Height - 11) / 2, UltracodeTables.UpperDcc[32 + Dcc]);
        WriteTile(cells, 2, (Height - 11) / 2 + 6, UltracodeTables.LowerDcc[32 + Dcc]);
        return cells;
    }

    internal void TilePosition(int index, out int x, out int y) {
        var column = index / Rows;
        x = 5 + column + (column + 1) / 15;
        y = index % Rows * 6 + 1;
    }

    internal int[] ReadTiles(UltracodeGrid cells) {
        var words = new int[TileCount];
        for (var i = 0; i < words.Length; i++) {
            TilePosition(i, out var x, out var y);
            words[i] = TileValues.TryGetValue(ReadTile(cells, x, y), out var value) ? value : -1;
        }
        return words;
    }

    internal bool HasFrame(UltracodeGrid cells) {
        if (cells.Width != Width || cells.Height != Height) return false;
        var frame = Frame();
        for (var y = 0; y < Height; y++) for (var x = 0; x < Width; x++) {
            if (x == 2 && y > 0 && y < Height - 1 || x >= 5 && x < Width - 1 && y % 6 != 0 && x != 19 && x != 35 && x != 51) continue;
            if (cells[x, y] != frame[x, y]) return false;
        }
        var upper = ReadTile(cells, 2, (Height - 11) / 2);
        var lower = ReadTile(cells, 2, (Height - 11) / 2 + 6);
        return upper == UltracodeTables.UpperDcc[Dcc] && lower == UltracodeTables.LowerDcc[Dcc]
            || upper == UltracodeTables.UpperDcc[32 + Dcc] && lower == UltracodeTables.LowerDcc[32 + Dcc];
    }

    private UltracodeGrid Frame() {
        var cells = new UltracodeGrid(Width, Height);
        for (var y = 0; y < Height; y++) for (var x = 0; x < Width; x++) {
            var black = y == 0 || y == Height - 1 || x == 0 || x == 3 || x == Width - 1
                || y % 2 == 0 && (x == 1 || x == 19 || x == 35 || x == 51)
                || y % 6 == 0 && x >= 5 && x % 2 == 1;
            cells[x, y] = black ? UltracodeColor.Black : UltracodeColor.White;
        }
        return cells;
    }

    private static void WriteTile(UltracodeGrid cells, int x, int y, int pattern) {
        for (var row = 0; row < 5; row++) cells[x, y + row] = FromPattern(pattern >> ((4 - row) * 3) & 7);
    }

    private static int ReadTile(UltracodeGrid cells, int x, int y) {
        var result = 0;
        for (var row = 0; row < 5; row++) {
            var value = cells[x, y + row] switch {
                UltracodeColor.Cyan => 1, UltracodeColor.Magenta => 3,
                UltracodeColor.Yellow => 5, UltracodeColor.Green => 6, _ => -1
            };
            if (value < 0) return -1;
            result = result * 8 + value;
        }
        return result;
    }

    private static UltracodeColor FromPattern(int value) => value switch {
        1 => UltracodeColor.Cyan, 3 => UltracodeColor.Magenta,
        5 => UltracodeColor.Yellow, 6 => UltracodeColor.Green,
        _ => throw new InvalidOperationException("Invalid Ultracode tile colour.")
    };

    private static Dictionary<int, int> CreateTileValues() {
        var values = new Dictionary<int, int>();
        for (var i = 0; i < UltracodeTables.Tiles.Length; i++) values.Add(UltracodeTables.Tiles[i], i);
        return values;
    }
}
