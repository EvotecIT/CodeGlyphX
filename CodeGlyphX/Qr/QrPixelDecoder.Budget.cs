#if NET8_0_OR_GREATER
using System;
using System.Diagnostics;
using System.Threading;

namespace CodeGlyphX.Qr;

internal static partial class QrPixelDecoder {
    /// <summary>Monotonic decode deadline shared by a pass and its nested attempts.</summary>
    internal readonly struct DecodeBudget {
        public bool Enabled { get; }
        public long Deadline { get; }
        public int BudgetMilliseconds { get; }
        private readonly CancellationToken _cancellationToken;
        private readonly bool _hasCancellation;
        private readonly global::CodeGlyphX.Internal.DecodeBudgetState _enclosingDeadline;
        private readonly long _effectiveDeadline;
        private readonly bool _hasEffectiveDeadline;

        /// <summary>Creates a local allowance that can shorten, but never extend, a parent deadline.</summary>
        public DecodeBudget(int budgetMilliseconds, CancellationToken cancellationToken, DecodeBudget? parent = null) {
            BudgetMilliseconds = budgetMilliseconds;
            _cancellationToken = cancellationToken;
            _hasCancellation = cancellationToken.CanBeCanceled;
            if (budgetMilliseconds > 0) {
                Enabled = true;
                Deadline = Stopwatch.GetTimestamp() + (long)(budgetMilliseconds * (Stopwatch.Frequency / 1000.0));
            } else {
                Enabled = false;
                Deadline = 0;
            }
            if (parent is { Enabled: true } shared && (!Enabled || shared.Deadline < Deadline)) {
                Enabled = true;
                Deadline = shared.Deadline;
                BudgetMilliseconds = budgetMilliseconds > 0 ? Math.Min(budgetMilliseconds, shared.BudgetMilliseconds) : shared.BudgetMilliseconds;
            }
            // The enclosing scope is fixed for this pass. Capture it once rather than
            // reading AsyncLocal for every module, keeping the earliest monotonic limit.
            _enclosingDeadline = global::CodeGlyphX.Internal.DecodeBudget.Capture();
            _hasEffectiveDeadline = Enabled || _enclosingDeadline.Enabled;
            _effectiveDeadline = Enabled ? Deadline : _enclosingDeadline.DeadlineTicks;
            if (_enclosingDeadline.Enabled && (!Enabled || _enclosingDeadline.DeadlineTicks < _effectiveDeadline))
                _effectiveDeadline = _enclosingDeadline.DeadlineTicks;
            if (parent is { } bounded && bounded._hasEffectiveDeadline &&
                (!_hasEffectiveDeadline || bounded._effectiveDeadline < _effectiveDeadline)) {
                _hasEffectiveDeadline = true;
                _effectiveDeadline = bounded._effectiveDeadline;
            }
        }

        public bool IsExpired => (_hasCancellation && _cancellationToken.IsCancellationRequested) ||
                                 (_hasEffectiveDeadline && Stopwatch.GetTimestamp() >= _effectiveDeadline);

        public bool IsCancelled => _hasCancellation && _cancellationToken.IsCancellationRequested;
        /// <summary>Whether a pass must poll caller cancellation or any captured deadline.</summary>
        public bool CanAbort => _hasCancellation || _hasEffectiveDeadline;

        public bool IsNearDeadline(int milliseconds) {
            if ((_hasCancellation && _cancellationToken.IsCancellationRequested) ||
                _enclosingDeadline.IsExpired) return true;
            if (!Enabled) return false;
            var remaining = Deadline - Stopwatch.GetTimestamp();
            if (remaining <= 0) return true;
            // A reserve for optional recovery must not consume an entire short family allowance.
            // Keep the requested reserve for ordinary budgets and scale it down for fast calls.
            var reserve = Math.Min(milliseconds, BudgetMilliseconds / 4);
            return remaining <= (long)(reserve * (Stopwatch.Frequency / 1000.0));
        }
    }

}
#endif
