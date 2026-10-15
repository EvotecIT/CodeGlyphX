using System;
using System.Buffers;

namespace CodeGlyphX;

internal static partial class DirectPartMarkPreprocessor {
    /// <summary>
    /// Owns private preprocessing storage until its iterator is disposed.
    /// Retry images own separate arrays and never borrow these pooled buffers.
    /// Every operation receives only the initialized image-sized prefix.
    /// </summary>
    private sealed class Workspace : IDisposable {
        private readonly int _length;
        private byte[]? _gray;
        private byte[]? _means;
        private byte[]? _dark;
        private byte[]? _light;
        private byte[]? _dilated;
        private byte[]? _eroded;

        internal Workspace(int length) => _length = length;

        internal Span<byte> Gray => Buffer(ref _gray);
        internal Span<byte> Means => Buffer(ref _means);
        internal Span<byte> Dark => Buffer(ref _dark);
        internal Span<byte> Light => Buffer(ref _light);
        internal Span<byte> Dilated => Buffer(ref _dilated);
        internal Span<byte> Eroded => Buffer(ref _eroded);

        private Span<byte> Buffer(ref byte[]? buffer) {
            buffer ??= ArrayPool<byte>.Shared.Rent(_length);
            return buffer.AsSpan(0, _length);
        }

        public void Dispose() {
            Return(ref _gray);
            Return(ref _means);
            Return(ref _dark);
            Return(ref _light);
            Return(ref _dilated);
            Return(ref _eroded);
        }

        private static void Return(ref byte[]? buffer) {
            var rented = buffer;
            buffer = null;
            if (rented is not null) ArrayPool<byte>.Shared.Return(rented);
        }
    }
}
