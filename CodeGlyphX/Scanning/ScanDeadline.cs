using System;
using System.Diagnostics;
using System.Threading;

namespace CodeGlyphX;

internal sealed class ScanDeadline : IDisposable {
    private readonly CancellationToken _callerToken;
    private readonly CancellationTokenSource? _source;
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

    internal int TimeoutMilliseconds { get; }
    internal CancellationToken Token => _source?.Token ?? _callerToken;
    internal TimeSpan Elapsed => _stopwatch.Elapsed;

    internal ScanDeadline(CancellationToken callerToken, int timeoutMilliseconds) {
        if (timeoutMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(timeoutMilliseconds));
        _callerToken = callerToken;
        TimeoutMilliseconds = timeoutMilliseconds;
        if (timeoutMilliseconds > 0) {
            _source = callerToken.CanBeCanceled
                ? CancellationTokenSource.CreateLinkedTokenSource(callerToken)
                : new CancellationTokenSource();
            _source.CancelAfter(timeoutMilliseconds);
        }
    }

    internal bool ShouldStop => Token.IsCancellationRequested ||
        (TimeoutMilliseconds > 0 && _stopwatch.ElapsedMilliseconds >= TimeoutMilliseconds);
    internal bool CallerCancelled => _callerToken.IsCancellationRequested;
    internal bool DeadlineExceeded => TimeoutMilliseconds > 0 && !CallerCancelled &&
        (_source?.IsCancellationRequested == true || _stopwatch.ElapsedMilliseconds >= TimeoutMilliseconds);

    internal int RemainingMilliseconds {
        get {
            if (TimeoutMilliseconds <= 0) return 0;
            var remaining = TimeoutMilliseconds - _stopwatch.ElapsedMilliseconds;
            if (remaining <= 0) return 1;
            return remaining > int.MaxValue ? int.MaxValue : (int)remaining;
        }
    }

    public void Dispose() {
        _stopwatch.Stop();
        _source?.Dispose();
    }
}
