using System.Collections.Generic;

namespace CodeGlyphX.Pdf417;

public static partial class Pdf417Decoder {
    // Several pixel scanlines sample each logical row. A damaged bottom edge
    // must not replace the repeated, intact codeword sequence above it.
    private sealed class PixelRowVote {
        private readonly List<(int[] Words, int Count)> _candidates = new();
        private int _best;

        internal int BestCount => _candidates[_best].Count;
        internal int[] BestWords => _candidates[_best].Words;

        internal int[] Observe(int[] words, int weight = 1, int maximumCandidates = int.MaxValue) {
            var index = 0;
            for (; index < _candidates.Count; index++) {
                var prior = _candidates[index].Words;
                if (prior.Length != words.Length) continue;
                var equal = true;
                for (var i = 0; i < words.Length; i++)
                    if (prior[i] != words[i]) { equal = false; break; }
                if (equal) break;
            }
            if (index == _candidates.Count && index >= maximumCandidates) return BestWords;
            if (index == _candidates.Count) _candidates.Add((words, weight));
            else _candidates[index] = (_candidates[index].Words, _candidates[index].Count + weight);
            if (_candidates[index].Count > _candidates[_best].Count) _best = index;
            return _candidates[_best].Words;
        }
    }
}
