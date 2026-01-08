namespace CodeGlyphX.Pdf417;

internal sealed class Pdf417BarcodeRow {
    private readonly sbyte[] _row;
    private int _current;

    internal Pdf417BarcodeRow(int width) {
        _row = new sbyte[width];
        _current = 0;
    }

    internal sbyte this[int x] {
        get => _row[x];
        set => _row[x] = value;
    }

    internal void AddBar(bool black, int width) {
        for (var i = 0; i < width; i++) {
            _row[_current++] = (sbyte)(black ? 1 : 0);
        }
    }

    internal sbyte[] GetScaledRow(int scale) {
        var output = new sbyte[_row.Length * scale];
        for (var i = 0; i < output.Length; i++) {
            output[i] = _row[i / scale];
        }
        return output;
    }
}
