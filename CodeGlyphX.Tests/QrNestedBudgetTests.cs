using System;
using System.Threading;
using CodeGlyphX.Qr;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class QrNestedBudgetTests {
    [Theory]
    [InlineData(0)]
    [InlineData(5000)]
    public void TileAttempts_CannotRestartAnExpiredSharedDeadline(int attemptBudget) {
        var shared = new QrPixelDecoder.DecodeBudget(1, CancellationToken.None);
        Assert.True(SpinWait.SpinUntil(() => shared.IsExpired, TimeSpan.FromSeconds(1)));

        // A fallback attempt must inherit the same deadline, including when its own
        // options would otherwise disable the limit entirely.
        var attempt = new QrPixelDecoder.DecodeBudget(attemptBudget, CancellationToken.None, shared);
        Assert.True(attempt.IsExpired);
    }

    [Fact]
    public void ExpiredCoarseAllowance_DoesNotExpireTheReservedFineAllowance() {
        var fine = new QrPixelDecoder.DecodeBudget(60000, CancellationToken.None);
        var coarse = new QrPixelDecoder.DecodeBudget(1, CancellationToken.None, fine);
        Assert.True(SpinWait.SpinUntil(() => coarse.IsExpired, TimeSpan.FromSeconds(1)));
        Assert.False(fine.IsExpired);
        var fineAttempt = new QrPixelDecoder.DecodeBudget(1000, CancellationToken.None, fine);
        Assert.False(fineAttempt.IsExpired);
    }
}
