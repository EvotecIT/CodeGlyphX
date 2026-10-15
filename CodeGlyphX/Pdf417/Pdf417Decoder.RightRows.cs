using System;
using System.Collections.Generic;

namespace CodeGlyphX.Pdf417;

public static partial class Pdf417Decoder {
    // Repeated scanlines vote for each row's surviving suffix. Metadata votes
    // count distinct logical rows, so a tall row cannot dominate the header.
    private sealed class PixelRightRowSet {
        private readonly Dictionary<int, PixelRowVote> _votes = new();
        private readonly Dictionary<int, int> _positions = new();
        private int _last = -1, _direction;
        private bool _ordered = true;

        internal void Observe(int row, int[] words, int y) {
            if (!_votes.TryGetValue(row, out var vote)) _votes[row] = vote = new PixelRowVote();
            vote.Observe(words, maximumCandidates: 8);
            _positions[row] = y;
            if (_last >= 0 && row != _last) {
                var direction = Math.Sign(row - _last);
                if (_direction != 0 && _direction != direction) _ordered = false;
                _direction = direction;
            }
            _last = row;
        }

        internal bool TryAssemble(out Dictionary<int, int[]> rows, out int columns, out int count, out int ec) {
            rows = new Dictionary<int, int[]>();
            columns = count = ec = 0;
            if (!_ordered || _votes.Count < 3) return false;
            var columnVotes = new int[30];
            var upperVotes = new int[30];
            var lowerVotes = new int[30];
            foreach (var pair in _votes) {
                var value = pair.Value.BestWords[0] % 30;
                (pair.Key % 3 == 0 ? columnVotes : pair.Key % 3 == 1 ? upperVotes : lowerVotes)[value]++;
            }
            var column = BestRightMetadata(columnVotes);
            var upper = BestRightMetadata(upperVotes);
            var lowerEc = BestRightMetadata(lowerVotes);
            if (column < 0 || upper < 0 || lowerEc < 0) return false;
            columns = column + 1;
            ec = lowerEc / 3;
            count = upper * 3 + lowerEc % 3 + 1;
            if (ec > 8 || count < 3 || count > 90 || count * columns > 928) return false;
            var keys = new List<int>(_votes.Keys);
            keys.Sort();
            int? prior = null;
            var direction = 0;
            var clusters = 0;
            foreach (var row in keys) {
                if (row >= count) continue;
                var observed = _votes[row].BestWords;
                var expected = 30 * (row / 3) + (row % 3 == 0 ? column : row % 3 == 1 ? upper : lowerEc);
                if (observed[0] != expected) continue;
                clusters |= 1 << (row % 3);
                var y = _positions[row];
                if (prior.HasValue) {
                    var step = Math.Sign(y - prior.Value);
                    if (step == 0 || direction != 0 && step != direction) return false;
                    direction = step;
                }
                prior = y;
                var words = new int[columns + 1];
                // No observed left indicator is claimed. Assembly consumes only
                // data positions; the right header has independently validated them.
                for (var i = 0; i < words.Length; i++) words[i] = -1;
                for (var i = 1; i < observed.Length && i <= columns; i++) words[columns - i + 1] = observed[i];
                rows[row] = words;
            }
            return rows.Count >= 3 && clusters == 7;
        }

        private static int BestRightMetadata(int[] votes) {
            var best = -1;
            for (var i = 0; i < votes.Length; i++) if (votes[i] > 0 && (best < 0 || votes[i] > votes[best])) best = i;
            return best;
        }
    }
}
