using System;
using System.Diagnostics;
using System.Threading;
using CodeGlyphX.Internal;

namespace CodeGlyphX;

internal sealed class ScanDeadline : IDisposable {
    private readonly CancellationToken _callerToken;
    private readonly CancellationTokenSource? _source;
    private readonly IDisposable? _decoderScope;
    private readonly DecodeBudgetState _recognitionDeadline;
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
        // WASM timer callbacks cannot run while recognition blocks its thread. Native decoder
        // loops also observe this monotonic scope, with nested attempts preserving the earliest limit.
        _decoderScope = DecodeBudget.Begin(timeoutMilliseconds);
        // Capture the enclosing limit after clamping. Shorter descendant scopes must not
        // make this instance expire while its own effective deadline still has time.
        _recognitionDeadline = DecodeBudget.Capture();
    }

    internal bool ShouldStop => Token.IsCancellationRequested || _recognitionDeadline.IsExpired ||
        (TimeoutMilliseconds > 0 && _stopwatch.ElapsedMilliseconds >= TimeoutMilliseconds);
    internal bool CallerCancelled => _callerToken.IsCancellationRequested;
    internal bool DeadlineExceeded => !CallerCancelled && (_recognitionDeadline.IsExpired ||
        TimeoutMilliseconds > 0 && (_source?.IsCancellationRequested == true || _stopwatch.ElapsedMilliseconds >= TimeoutMilliseconds));

    internal int RemainingMilliseconds {
        get {
            if (TimeoutMilliseconds <= 0) return 0;
            var remaining = TimeoutMilliseconds - _stopwatch.ElapsedMilliseconds;
            if (remaining <= 0) return 1;
            return remaining > int.MaxValue ? int.MaxValue : (int)remaining;
        }
    }

    // A local attempt may yield to later candidates without cancelling the complete scan.
    // Linking to this token keeps every attempt within the original caller/deadline boundary.
    internal ScanDeadline CreateAttempt(int remainingAttempts, int recognitionBudgetMilliseconds = 0) {
        var milliseconds = TimeoutMilliseconds > 0
            ? Math.Max(1, RemainingMilliseconds / Math.Max(1, remainingAttempts))
            : 0;
        if (recognitionBudgetMilliseconds > 0 && (milliseconds == 0 || recognitionBudgetMilliseconds < milliseconds))
            milliseconds = recognitionBudgetMilliseconds;
        return new ScanDeadline(Token, milliseconds);
    }

    public void Dispose() {
        _decoderScope?.Dispose();
        _stopwatch.Stop();
        _source?.Dispose();
    }
}
