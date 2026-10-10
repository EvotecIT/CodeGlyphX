using System.Collections.Generic;

namespace CodeGlyphX.Pdf417;

public static partial class Pdf417Decoder {
    private sealed class PixelRowHints {
        private readonly HashSet<int> _rows = new();
        private int _upper = -1, _lower = -1, _columns = -1, _ec = -1;
        internal int Columns => _columns;

        internal bool Observe(int row, int cluster, int indicator) {
            var value = indicator % 30;
            var consistent = cluster == 0 ? _upper < 0 || _upper == value
                : cluster == 1 ? (_lower < 0 || _lower == value % 3) && (_ec < 0 || _ec == value / 3)
                : _columns < 0 || _columns == value + 1;
            if (!consistent) { _rows.Clear(); _upper = _lower = _columns = _ec = -1; }
            if (cluster == 0) _upper = value;
            if (cluster == 1) { _lower = value % 3; _ec = value / 3; }
            if (cluster == 2) _columns = value + 1;
            _rows.Add(row);
            var count = _upper * 3 + _lower + 1;
            return _rows.Count >= 3 && _upper >= 0 && _lower >= 0 && _columns > 0 && _ec >= 0 && _ec <= 8 &&
                count >= 3 && count <= 90 && count * _columns <= 928;
        }
    }
}
