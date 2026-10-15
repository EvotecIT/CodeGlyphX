namespace CodeGlyphX.Pdf417;

public static partial class Pdf417Decoder {
    // A stack iterator keeps the pixel span alive only for this synchronous
    // framing search and preserves the rolling-window order of offset hits.
    private ref struct OffsetMatches {
        private readonly ModuleGrid _modules;
        private readonly int _row, _pattern, _length, _maxErrors, _mask;
        private int _offset, _bits;
        public OffsetMatch Current { get; private set; }

        internal OffsetMatches(ModuleGrid modules, int row, int pattern, int length, int maxErrors) {
            _modules = modules; _row = row; _pattern = pattern;
            _length = length; _maxErrors = maxErrors;
            _mask = length >= 32 ? -1 : (1 << length) - 1;
            _offset = -1; _bits = 0;
            Current = default;
        }

        public OffsetMatches GetEnumerator() => this;

        public bool MoveNext() {
            if (_length <= 0 || _modules.Width < _length || _modules.IsAborted) return false;
            if (_offset < 0) {
                for (var i = 0; i < _length; i++) _bits = (_bits << 1) | (_modules[i, _row] ? 1 : 0);
            }
            while (++_offset <= _modules.Width - _length) {
                if (_offset != 0) _bits = ((_bits << 1) | (_modules[_offset + _length - 1, _row] ? 1 : 0)) & _mask;
                if (_modules.IsAborted) return false;
                var errors = PopCount(_bits ^ _pattern);
                if (errors > _maxErrors) continue;
                Current = new OffsetMatch(_offset, errors);
                return true;
            }
            return false;
        }
    }
}
