#if NET8_0_OR_GREATER
using System;
using System.Buffers;

namespace CodeGlyphX.Qr;

internal static partial class QrPixelDecoder {
    /// <summary>Owns a sampled grid and a small history of completed failures for one finder transform.</summary>
    internal sealed class SamplingScratch : IDisposable {
        private const int Capacity = 4;
        private uint[]? _failedWords;
        private QrDecodeDiagnostics[]? _failures;
        private int _count;
        private int _next;

        public BitMatrix Matrix { get; }

        public SamplingScratch(int dimension) {
            Matrix = new BitMatrix(dimension, dimension);
        }

        /// <summary>Reuses only byte-identical failed grids, never successful payloads or interrupted searches.</summary>
        public bool TryDecode(DecodeBudget budget, out QrDecoded result, out QrDecodeDiagnostics diagnostics) {
            result = null!;
            if (budget.IsExpired) {
                diagnostics = new QrDecodeDiagnostics(QrDecodeFailure.Cancelled);
                return false;
            }

            var words = Matrix.Words.AsSpan();
            if (_failedWords is not null) {
                for (var i = 0; i < _count; i++) {
                    if (words.SequenceEqual(_failedWords.AsSpan(i * words.Length, words.Length))) {
                        diagnostics = _failures![i];
                        return false;
                    }
                }
            }

            Func<bool>? shouldStop = budget.Enabled || budget.CanCancel ? () => budget.IsExpired : null;
            if (QrDecoder.TryDecodeInternal(Matrix, shouldStop, out result, out diagnostics)) return true;

            // Some broad format fallbacks report their format score after an interrupted
            // candidate. Check the shared deadline as well as the final diagnostic.
            if (diagnostics.Failure == QrDecodeFailure.Cancelled || budget.IsExpired) return false;

            _failedWords ??= ArrayPool<uint>.Shared.Rent(words.Length * Capacity);
            _failures ??= new QrDecodeDiagnostics[Capacity];
            words.CopyTo(_failedWords.AsSpan(_next * words.Length, words.Length));
            _failures[_next] = diagnostics;
            _next = (_next + 1) % Capacity;
            if (_count < Capacity) _count++;
            return false;
        }

        public void Dispose() {
            var words = _failedWords;
            _failedWords = null;
            _count = 0;
            if (words is not null) ArrayPool<uint>.Shared.Return(words, clearArray: false);
        }
    }
}
#endif
