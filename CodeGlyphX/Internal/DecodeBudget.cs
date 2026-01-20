using System;
using System.Diagnostics;
using System.Threading;

namespace CodeGlyphX.Internal;

internal readonly struct DecodeBudgetState {
    public readonly long DeadlineTicks;
    public readonly bool Enabled;

    public DecodeBudgetState(long deadlineTicks, bool enabled) {
        DeadlineTicks = deadlineTicks;
        Enabled = enabled;
    }
}

internal static class DecodeBudget {
    private static readonly AsyncLocal<DecodeBudgetState> CurrentState = new();

    public static bool IsExpired {
        get {
            var state = CurrentState.Value;
            if (!state.Enabled) return false;
            return Stopwatch.GetTimestamp() >= state.DeadlineTicks;
        }
    }

    public static bool ShouldAbort(CancellationToken token) {
        return token.IsCancellationRequested || IsExpired;
    }

    public static IDisposable? Begin(int maxMilliseconds) {
        if (maxMilliseconds <= 0) return null;
        var prev = CurrentState.Value;
        var ticksPerMs = Stopwatch.Frequency / 1000.0;
        var deadline = Stopwatch.GetTimestamp() + (long)(maxMilliseconds * ticksPerMs);
        CurrentState.Value = new DecodeBudgetState(deadline, enabled: true);
        return new Scope(prev);
    }

    private sealed class Scope : IDisposable {
        private readonly DecodeBudgetState _previous;
        private bool _disposed;

        public Scope(DecodeBudgetState previous) {
            _previous = previous;
        }

        public void Dispose() {
            if (_disposed) return;
            CurrentState.Value = _previous;
            _disposed = true;
        }
    }
}
