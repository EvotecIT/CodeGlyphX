using System;
using System.Collections.Generic;

namespace CodeGlyphX.Pdf417;

public static partial class Pdf417Decoder {
    // Compact and regular rows, and spatially separate start patterns, retain
    // independent hypotheses until their complete data passes error correction.
    private sealed class PixelRowSet {
        internal readonly int Columns;
        internal readonly bool Compact;
        internal int Upper = -1, Lower = -1, Ec = -1;
        internal bool Ordered = true;
        internal readonly Dictionary<int, int[]> Rows = new();
        internal readonly Dictionary<int, PixelRowVote> Votes = new();
        internal readonly Dictionary<int, (double X, double Y, double Slope, double Width)> Positions = new();
        private int _last = -1, _direction;

        internal PixelRowSet(int columns, bool compact) { Columns = columns; Compact = compact; }

        internal double MeanX {
            get {
                var sum = 0.0;
                foreach (var position in Positions.Values) sum += position.X;
                return sum / Positions.Count;
            }
        }

        internal double MeanWidth {
            get {
                var sum = 0.0;
                foreach (var position in Positions.Values) sum += position.Width;
                return sum / Positions.Count;
            }
        }

        internal void Observe(int row, int left, int right, int[] words, double x, double y, double slope, double width) {
            var cluster = row % 3;
            var upper = cluster == 0 ? left % 30 : cluster == 1 && !Compact ? right % 30 : -1;
            var lower = cluster == 1 ? left % 3 : cluster == 2 && !Compact ? right % 3 : -1;
            if (upper >= 0) Upper = upper;
            if (lower >= 0) Lower = lower;
            if (cluster == 1) Ec = left % 30 / 3;
            if (cluster == 2 && !Compact) Ec = right % 30 / 3;
            if (!Votes.TryGetValue(row, out var vote)) Votes[row] = vote = new PixelRowVote();
            Rows[row] = vote.Observe(words);
            Positions[row] = (x, y, slope, width);
            if (_last >= 0 && row != _last) {
                var direction = Math.Sign(row - _last);
                if (_direction != 0 && _direction != direction) Ordered = false;
                _direction = direction;
            }
            _last = row;
        }
    }
}
