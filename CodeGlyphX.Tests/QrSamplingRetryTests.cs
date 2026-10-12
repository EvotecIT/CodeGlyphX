using System;
using System.Threading;
using CodeGlyphX.Qr;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class QrSamplingRetryTests {
    [Theory]
    [InlineData(1)]
    [InlineData(40)]
    public void FailedSamplingRetriesPreserveDiagnosticsAndReadChangedModules(int version) {
        using var scratch = new QrPixelDecoder.SamplingScratch(version * 4 + 17);
        var budget = new QrPixelDecoder.DecodeBudget(0, CancellationToken.None);
        var random = new Random(714);
        for (var attempt = 0; attempt < 6; attempt++) {
            for (var y = 0; y < scratch.Matrix.Height; y++) {
                for (var x = 0; x < scratch.Matrix.Width; x++) scratch.Matrix[x, y] = random.Next(2) != 0;
            }
            var snapshot = scratch.Matrix.ToPackedBytes();
            Assert.False(QrDecoder.TryDecodeInternal(scratch.Matrix, out _, out var expected));
            Assert.False(scratch.TryDecode(budget, out _, out var first));
            Assert.False(scratch.TryDecode(budget, out _, out var repeated));
            Assert.Equal(expected, first);
            Assert.Equal(expected, repeated);
            Assert.Equal(snapshot, scratch.Matrix.ToPackedBytes());
        }

        var qr = QrCodeEncoder.EncodeText("sampling retry", QrErrorCorrectionLevel.M,
            minVersion: version, maxVersion: version, forceMask: 5);
        scratch.Matrix.CopyFrom(qr.Modules);
        Assert.True(scratch.TryDecode(budget, out var decoded, out _));
        Assert.Equal("sampling retry", decoded.Text);
        Assert.Equal(version, decoded.Version);

        // A successful decode remains eligible on every retry; acceptance belongs to the caller.
        Assert.True(scratch.TryDecode(budget, out var repeatedSuccess, out _));
        Assert.Equal(decoded.Text, repeatedSuccess.Text);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FailedSamplingHistoryDoesNotOverrideCancellationOrDeadline(bool cancel) {
        using var scratch = new QrPixelDecoder.SamplingScratch(21);
        using var cancellation = new CancellationTokenSource();
        Assert.False(scratch.TryDecode(new QrPixelDecoder.DecodeBudget(0, CancellationToken.None), out _, out _));

        var budget = new QrPixelDecoder.DecodeBudget(cancel ? 0 : 1, cancellation.Token);
        if (cancel) cancellation.Cancel();
        else Assert.True(SpinWait.SpinUntil(() => budget.IsExpired, TimeSpan.FromSeconds(1)));
        Assert.False(scratch.TryDecode(budget, out _, out var diagnostics));
        Assert.Equal(QrDecodeFailure.Cancelled, diagnostics.Failure);
    }
}
