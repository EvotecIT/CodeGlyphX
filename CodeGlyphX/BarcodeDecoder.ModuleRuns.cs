using System;
using System.Buffers;

namespace CodeGlyphX;

public static partial class BarcodeDecoder {
    /// <summary>
    /// Lazily extracts one read-only run sequence for a single immutable module input.
    /// The synchronous decoding scope returns its pooled storage after every format
    /// attempt finishes. Nested or transformed inputs require their own workspace.
    /// </summary>
    private ref struct ModuleRunWorkspace {
        private readonly bool[] _modules;
        private int[]? _buffer;
        private int _count;

        public ModuleRunWorkspace(bool[] modules) {
            _modules = modules;
            _buffer = null;
            _count = 0;
        }

        public bool[] Modules => _modules;

        public ReadOnlySpan<int> Runs {
            get {
                if (_buffer is null) {
                    var count = 1;
                    for (var i = 1; i < _modules.Length; i++) {
                        if (_modules[i] != _modules[i - 1]) count++;
                    }
                    _buffer = ArrayPool<int>.Shared.Rent(count);
                    _count = count;
                    var run = 0;
                    var start = 0;
                    for (var i = 1; i < _modules.Length; i++) {
                        if (_modules[i] != _modules[i - 1]) {
                            _buffer[run++] = i - start;
                            start = i;
                        }
                    }
                    _buffer[run] = _modules.Length - start;
                }
                return _buffer.AsSpan(0, _count);
            }
        }

        public void Dispose() {
            var buffer = _buffer;
            _buffer = null;
            _count = 0;
            if (buffer is not null) ArrayPool<int>.Shared.Return(buffer);
        }
    }
}
