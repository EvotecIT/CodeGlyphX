using System;
using System.Collections.Generic;
using System.Threading;
using CodeGlyphX.Internal;

namespace CodeGlyphX.Pdf417;

public static partial class Pdf417Decoder {
    // Sampling the same thresholded line again changes only run order or color.
    // Keep a bounded, per-image cache across orientations and the print-gain retry.
    private sealed class PixelRunCache {
        private const int MaximumCacheBytes = 8 * 1024 * 1024;
        private const int MaximumEntries = 8192;
        private const int EntryOverheadBytes = 160;
        private readonly bool[] _pixels;
        private readonly int _width;
        private readonly int _height;
        private readonly Dictionary<(int Axis, int Line, double Slope), CachedRuns> _lines = new();
        private int _bytes;

        private sealed class CachedRuns {
            internal readonly int[] Widths;
            internal readonly bool First;
            internal CachedRuns(int[] widths, bool first) { Widths = widths; First = first; }
        }

        internal PixelRunCache(bool[] pixels, int width, int height) {
            _pixels = pixels; _width = width; _height = height;
        }

        internal bool TryRead(int axis, bool reverse, bool invert, int line, double slope,
            List<int> runs, CancellationToken token, out bool first) {
            first = false;
            if (DecodeBudget.ShouldAbort(token)) return false;
            var key = (axis, line, slope);
            if (_lines.TryGetValue(key, out var cached)) {
                runs.Clear();
                first = cached.First;
                if (reverse && (cached.Widths.Length & 1) == 0) first = !first;
                if (invert) first = !first;
                for (var i = 0; i < cached.Widths.Length; i++) {
                    if ((i & 255) == 0 && DecodeBudget.ShouldAbort(token)) return false;
                    runs.Add(cached.Widths[reverse ? cached.Widths.Length - 1 - i : i]);
                }
                return true;
            }
            if (!TryReadPixelRuns(_pixels, _width, _height, axis, reverse, invert, line, slope, runs, token, out first)) return false;
            var bytes = (long)runs.Count * sizeof(int) + EntryOverheadBytes;
            if (_lines.Count >= MaximumEntries || bytes > MaximumCacheBytes - _bytes) return true;
            var widths = new int[runs.Count];
            for (var i = 0; i < widths.Length; i++) {
                if ((i & 255) == 0 && DecodeBudget.ShouldAbort(token)) return false;
                widths[i] = runs[reverse ? widths.Length - 1 - i : i];
            }
            // Canonical entries run forward without inversion, including the
            // out-of-bounds light pixels on tilted lines.
            var canonicalFirst = invert ? !first : first;
            if (reverse && (widths.Length & 1) == 0) canonicalFirst = !canonicalFirst;
            _lines.Add(key, new CachedRuns(widths, canonicalFirst));
            _bytes += (int)bytes;
            return true;
        }
    }
}
