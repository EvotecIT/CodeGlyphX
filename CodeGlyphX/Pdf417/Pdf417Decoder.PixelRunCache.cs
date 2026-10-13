using System;
using System.Collections.Generic;
using System.Threading;
using CodeGlyphX.Internal;

namespace CodeGlyphX.Pdf417;

public static partial class Pdf417Decoder {
    // Sampling the same thresholded line again changes only run order or color.
    // Keep a bounded, per-image cache across orientations and the print-gain retry.
    // Segmented value slots avoid a dictionary entry and wrapper for every line.
    private sealed class PixelRunCache {
        private const int MaximumCacheBytes = 8 * 1024 * 1024;
        // Let tilted lines share the byte allowance instead of exhausting the
        // entry limit while most of that allowance is still unused.
        private const int MaximumEntries = 32768;
        private const int EntryOverheadBytes = 160;
        private const int SegmentShift = 7;
        private const int SegmentLength = 1 << SegmentShift;
        // Include the slot array and dictionary bookkeeping before allocating it.
        private const int SegmentOverheadBytes = SegmentLength * 16 + 160;
        private readonly bool[] _pixels;
        private readonly int _width;
        private readonly int _height;
        private readonly Dictionary<(int Axis, int Segment, double Slope), CachedRuns[]> _segments = new();
        private bool _hasRecent;
        private int _recentAxis, _recentIndex;
        private double _recentSlope;
        private CachedRuns[]? _recentSegment;
        private int _bytes;
        private int _entries;
        internal readonly PixelCodewordCache Codewords = new();

        private readonly struct CachedRuns {
            internal readonly Array? Widths;
            internal readonly bool First;
            internal CachedRuns(Array widths, bool first) { Widths = widths; First = first; }
        }

        internal PixelRunCache(bool[] pixels, int width, int height) {
            _pixels = pixels; _width = width; _height = height;
        }

        internal bool TryRead(int axis, bool reverse, bool invert, int line, double slope,
            List<int> runs, CancellationToken token, out bool first) {
            first = false;
            var budget = DecodeBudget.Capture();
            if (budget.ShouldAbort(token)) return false;
            var segmentIndex = line >> SegmentShift;
            var slot = line & (SegmentLength - 1);
            CachedRuns[]? segment;
            // Callers traverse adjacent lines. Reuse both found and absent
            // segments until the axis, slope or segment index changes.
            if (_hasRecent && _recentAxis == axis && _recentIndex == segmentIndex && _recentSlope.Equals(slope)) {
                segment = _recentSegment;
            } else {
                _segments.TryGetValue((axis, segmentIndex, slope), out segment);
                _hasRecent = true;
                _recentAxis = axis; _recentIndex = segmentIndex; _recentSlope = slope;
                _recentSegment = segment;
            }
            var cached = segment is null ? default : segment[slot];
            if (cached.Widths is not null) {
                runs.Clear();
                first = cached.First;
                if (reverse && (cached.Widths.Length & 1) == 0) first = !first;
                if (invert) first = !first;
                if (cached.Widths is ushort[] compact) {
                    for (var i = 0; i < compact.Length; i++) {
                        if (i != 0 && (i & 255) == 0 && budget.ShouldAbort(token)) return false;
                        runs.Add(compact[reverse ? compact.Length - 1 - i : i]);
                    }
                } else {
                    var widths = (int[])cached.Widths;
                    for (var i = 0; i < widths.Length; i++) {
                        if (i != 0 && (i & 255) == 0 && budget.ShouldAbort(token)) return false;
                        runs.Add(widths[reverse ? widths.Length - 1 - i : i]);
                    }
                }
                return true;
            }
            if (!TryReadPixelRuns(_pixels, _width, _height, axis, reverse, invert, line, slope, runs, token, out first)) return false;
            // Every run is at most the scanline length, including tilted lines
            // padded with light pixels. Keep full widths for larger images.
            var useCompact = (axis == 0 ? _width : _height) <= ushort.MaxValue;
            var bytes = (long)runs.Count * (useCompact ? sizeof(ushort) : sizeof(int)) + EntryOverheadBytes;
            if (segment is null) bytes += SegmentOverheadBytes;
            if (_entries >= MaximumEntries || bytes > MaximumCacheBytes - _bytes) return true;
            Array stored;
            if (useCompact) {
                var widths = new ushort[runs.Count];
                for (var i = 0; i < widths.Length; i++) {
                    if ((i & 255) == 0 && budget.ShouldAbort(token)) return false;
                    widths[i] = (ushort)runs[reverse ? widths.Length - 1 - i : i];
                }
                stored = widths;
            } else {
                var widths = new int[runs.Count];
                for (var i = 0; i < widths.Length; i++) {
                    if ((i & 255) == 0 && budget.ShouldAbort(token)) return false;
                    widths[i] = runs[reverse ? widths.Length - 1 - i : i];
                }
                stored = widths;
            }
            // Canonical entries run forward without inversion, including the
            // out-of-bounds light pixels on tilted lines.
            var canonicalFirst = invert ? !first : first;
            if (reverse && (stored.Length & 1) == 0) canonicalFirst = !canonicalFirst;
            if (segment is null) {
                segment = new CachedRuns[SegmentLength];
                _segments.Add((axis, segmentIndex, slope), segment);
                _recentSegment = segment;
            }
            segment[slot] = new CachedRuns(stored, canonicalFirst);
            _entries++;
            _bytes += (int)bytes;
            return true;
        }
    }
}
