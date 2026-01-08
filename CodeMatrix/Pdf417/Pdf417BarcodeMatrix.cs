namespace CodeMatrix.Pdf417;

internal sealed class Pdf417BarcodeMatrix {
    internal const int ColumnWidth = 17;

    private readonly Pdf417BarcodeRow[] _matrix;
    private int _currentRow;
    private readonly int _height;
    private readonly int _width;

    internal Pdf417BarcodeMatrix(int height, int width, bool compact) {
        _matrix = new Pdf417BarcodeRow[height];
        for (var i = 0; i < _matrix.Length; i++) {
            _matrix[i] = new Pdf417BarcodeRow(width * ColumnWidth + (2 * ColumnWidth) + (compact ? 0 : 2) * ColumnWidth + 1);
        }
        _width = width * ColumnWidth;
        _height = height;
        _currentRow = -1;
    }

    internal void StartRow() {
        _currentRow++;
    }

    internal Pdf417BarcodeRow GetCurrentRow() => _matrix[_currentRow];

    internal sbyte[][] GetMatrix() => GetScaledMatrix(1, 1);

    internal sbyte[][] GetScaledMatrix(int xScale, int yScale) {
        var matrixOut = new sbyte[_height * yScale][];
        for (var idx = 0; idx < matrixOut.Length; idx++) {
            matrixOut[idx] = new sbyte[_width * xScale];
        }
        var yMax = _height * yScale;
        for (var ii = 0; ii < yMax; ii++) {
            matrixOut[yMax - ii - 1] = _matrix[ii / yScale].GetScaledRow(xScale);
        }
        return matrixOut;
    }
}
