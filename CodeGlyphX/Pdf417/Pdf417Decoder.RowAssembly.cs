using System;
using System.Collections.Generic;

namespace CodeGlyphX.Pdf417;

public static partial class Pdf417Decoder {
    // Perspective gives individual rows different slopes. Combine only rows
    // whose two indicators agree with one header, retaining physical order.
    private sealed class PixelRowAssembly {
        private readonly int _columns, _upper, _lower, _ec;
        private readonly bool _compact;
        private readonly Dictionary<int, PixelRowVote> _votes = new();
        private readonly Dictionary<int, (double Sum, int Count)> _positions = new();
        private readonly double _anchorX, _width;

        internal PixelRowAssembly(int columns, int upper, int lower, int ec, bool compact, double anchorX, double width) {
            _columns = columns; _upper = upper; _lower = lower; _ec = ec; _compact = compact;
            _anchorX = anchorX; _width = width;
        }

        internal bool Accepts(PixelRowSet rows) => Math.Abs(rows.MeanX - _anchorX) <= Math.Min(rows.MeanWidth, _width) / 2;

        internal void Observe(PixelRowSet rows) {
            foreach (var pair in rows.Rows) {
                var row = pair.Key;
                if (row >= _upper * 3 + _lower + 1) continue;
                var group = 30 * (row / 3);
                var left = group + (row % 3 == 0 ? _upper : row % 3 == 1 ? _ec * 3 + _lower : _columns - 1);
                var right = group + (row % 3 == 0 ? _columns - 1 : row % 3 == 1 ? _upper : _ec * 3 + _lower);
                var words = pair.Value;
                if (words.Length != _columns + (_compact ? 1 : 2) || words[0] != left ||
                    !_compact && words[words.Length - 1] != right) continue;
                if (!_votes.TryGetValue(row, out var vote)) _votes[row] = vote = new PixelRowVote();
                var weight = rows.Votes[row].BestCount;
                vote.Observe(words, weight);
                _positions.TryGetValue(row, out var position);
                var sample = rows.Positions[row];
                // Compare all slopes at one physical anchor on this symbol,
                // independently of its position within the surrounding page.
                var y = sample.Y + (_anchorX - sample.X) * sample.Slope;
                _positions[row] = (position.Sum + y * weight, position.Count + weight);
            }
        }

        internal bool TryAssemble(out Dictionary<int, int[]> rows) {
            rows = new Dictionary<int, int[]>();
            var keys = new List<int>(_votes.Keys);
            keys.Sort();
            double? prior = null;
            var direction = 0;
            foreach (var row in keys) {
                var position = _positions[row];
                var y = position.Sum / (double)position.Count;
                if (prior.HasValue) {
                    var step = Math.Sign(y - prior.Value);
                    if (step == 0 || direction != 0 && step != direction) return false;
                    direction = step;
                }
                prior = y;
                rows[row] = _votes[row].BestWords;
            }
            return true;
        }
    }
}
