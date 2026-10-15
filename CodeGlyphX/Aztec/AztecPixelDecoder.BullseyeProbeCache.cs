using System;
using System.Collections.Generic;

namespace CodeGlyphX.Aztec;

internal static partial class AztecPixelDecoder {
    /// <summary>
    /// Keeps central-ring decisions on the small offset grid shared by candidate
    /// dimensions. Each instance belongs to one image, threshold and polarity.
    /// </summary>
    private sealed class BullseyeProbeCache {
        private const int MaximumWindows = 256;
        private readonly Dictionary<int, ProbeWindow> _windows = new();

        internal bool TryRead(int pitch, int x, int y, out bool passed) {
            passed = false;
            return _windows.TryGetValue(pitch, out var window) && window.TryRead(x, y, out passed);
        }

        internal void Store(int pitch, int x, int y, bool passed) {
            if (!_windows.TryGetValue(pitch, out var window)) {
                if (_windows.Count >= MaximumWindows) return;
                // The first offset starts at the lower corner of the grid.
                // Other coordinates remain uncached rather than moving it.
                var half = Math.Max(1, pitch / 2);
                var step = Math.Max(1, (half + 3) / 4);
                var side = half * 2 / step + 1;
                _windows.Add(pitch, window = new ProbeWindow(x, y, step, side));
            }
            window.Store(x, y, passed);
        }

        private sealed class ProbeWindow {
            private readonly int _minX, _minY, _step, _side, _last;
            // Zero means unobserved; both rejected and accepted probes are kept.
            private readonly byte[] _states;

            internal ProbeWindow(int minX, int minY, int step, int side) {
                _minX = minX; _minY = minY; _step = step; _side = side;
                _last = (side - 1) * step;
                _states = new byte[side * side];
            }

            private int GetIndex(int x, int y) {
                var dx = (long)x - _minX;
                var dy = (long)y - _minY;
                if (dx < 0 || dy < 0 || dx > _last || dy > _last) return -1;
                var column = (int)dx / _step;
                var row = (int)dy / _step;
                if (column * _step != dx || row * _step != dy) return -1;
                return row * _side + column;
            }

            internal bool TryRead(int x, int y, out bool passed) {
                var index = GetIndex(x, y);
                var state = index < 0 ? 0 : _states[index];
                passed = state == 2;
                return state != 0;
            }

            internal void Store(int x, int y, bool passed) {
                var index = GetIndex(x, y);
                if (index >= 0) _states[index] = passed ? (byte)2 : (byte)1;
            }
        }
    }
}
