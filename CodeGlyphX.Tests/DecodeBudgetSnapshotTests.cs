using System;
using System.Threading;
using CodeGlyphX.Internal;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class DecodeBudgetSnapshotTests {
    [Fact]
    public void CapturedRecognitionDeadlineContinuesToObserveClockExpiry() {
        using var scope = DecodeBudget.Begin(20);
        var captured = DecodeBudget.Capture();

        Assert.True(SpinWait.SpinUntil(() => captured.ShouldAbort(CancellationToken.None), TimeSpan.FromSeconds(1)));
        Assert.True(DecodeBudget.IsExpired);
    }

    [Fact]
    public void CapturedRecognitionDeadlineObservesLaterCallerCancellation() {
        using var cancellation = new CancellationTokenSource();
        var captured = DecodeBudget.Capture();
        Assert.False(captured.ShouldAbort(cancellation.Token));

        cancellation.Cancel();

        Assert.True(captured.ShouldAbort(cancellation.Token));
        Assert.False(DecodeBudget.IsExpired);
    }
}
