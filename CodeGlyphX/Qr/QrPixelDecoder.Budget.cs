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
        }

        public bool IsExpired => (_hasCancellation && _cancellationToken.IsCancellationRequested) ||
                                 (Enabled && Stopwatch.GetTimestamp() > Deadline);

        public bool IsCancelled => _hasCancellation && _cancellationToken.IsCancellationRequested;
        public bool CanCancel => _hasCancellation;

        public bool IsNearDeadline(int milliseconds) {
            if (_hasCancellation && _cancellationToken.IsCancellationRequested) return true;
            if (!Enabled) return false;
            var remaining = Deadline - Stopwatch.GetTimestamp();
            if (remaining <= 0) return true;
            return remaining <= (long)(milliseconds * (Stopwatch.Frequency / 1000.0));
        }
    }

}
#endif
