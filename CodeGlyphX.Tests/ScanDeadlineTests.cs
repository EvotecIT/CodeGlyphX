using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CodeGlyphX.Internal;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class ScanDeadlineTests {
    [Theory]
    [InlineData(0)]
    [InlineData(10000)]
    public void NestedRecognitionBudgetCannotExtendAnExpiredEnclosingBudget(int nestedMilliseconds) {
        using var enclosing = DecodeBudget.Begin(1);
        Assert.True(SpinWait.SpinUntil(() => DecodeBudget.IsExpired, TimeSpan.FromSeconds(1)));

        using (DecodeBudget.Begin(nestedMilliseconds)) {
            Assert.True(DecodeBudget.ShouldAbort(CancellationToken.None));
        }
        Assert.True(DecodeBudget.IsExpired);
    }

    [Fact]
    public async Task ScannerDeadlineFlowsAcrossAwaitAndRestoresThePreviousDecoderScope() {
        Assert.False(DecodeBudget.IsExpired);
        using (var deadline = new ScanDeadline(CancellationToken.None, 1)) {
            Assert.True(SpinWait.SpinUntil(() => DecodeBudget.IsExpired, TimeSpan.FromSeconds(1)));
            await Task.Yield();
            Assert.True(DecodeBudget.ShouldAbort(CancellationToken.None));
            Assert.True(deadline.ShouldStop);
        }
        Assert.False(DecodeBudget.IsExpired);
    }

    [Fact]
    public void ExpiredLocalAttemptDoesNotExpireTheRemainingScan() {
        using var deadline = new ScanDeadline(CancellationToken.None, 60000);
        using (var attempt = deadline.CreateAttempt(1, recognitionBudgetMilliseconds: 1)) {
            Assert.True(SpinWait.SpinUntil(() => DecodeBudget.IsExpired, TimeSpan.FromSeconds(1)));
            Assert.True(attempt.ShouldStop);
        }
        Assert.False(DecodeBudget.IsExpired);
        Assert.False(deadline.ShouldStop);
        Assert.False(deadline.DeadlineExceeded);
    }

    [Fact]
    public void SynchronousTransportObservesAnExpiredClockWithoutACancelledToken() {
        using var deadline = DecodeBudget.Begin(1);
        Assert.True(SpinWait.SpinUntil(() => DecodeBudget.IsExpired, TimeSpan.FromSeconds(1)));
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });

        Assert.Throws<OperationCanceledException>(() => RenderIO.ReadBinary(stream, 0, CancellationToken.None));

        Assert.Equal(0, stream.Position);
        Assert.True(stream.CanRead);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(64)]
    public async Task ImmediatelyCompletedAsyncReadsStopWhenTheAmbientClockExpires(int maxBytes) {
        using var stream = new ExpiringReadStream();
        using var deadline = DecodeBudget.Begin(100);

        await Assert.ThrowsAsync<OperationCanceledException>(() => RenderIO.ReadBinaryAsync(stream, maxBytes, CancellationToken.None));

        Assert.Equal(1, stream.ReadCalls);
        Assert.Equal(1, stream.Position);
        Assert.True(stream.CanRead);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PixelDecodersObserveAmbientExpiryWithoutACancelledToken(bool microQr) {
        byte[] pixels;
        int width;
        int height;
        if (microQr) {
            var modules = MicroQrCodeEncoder.EncodeNumeric("1").Modules;
            pixels = MatrixPngRenderer.RenderPixels(modules, new MatrixPngRenderOptions { ModuleSize = 7, QuietZone = 2 },
                out width, out height, out _);
        } else {
            pixels = QR.RenderPixels("AMBIENT-CLOCK", out width, out height, out _);
        }
        bool Decode() => microQr
            ? MicroQrDecoder.TryDecode(pixels, width, height, width * 4, PixelFormat.Rgba32, out _)
            : QrImageDecoder.TryDecode(pixels, width, height, width * 4, PixelFormat.Rgba32, out _);
        Assert.True(Decode());
        using var deadline = DecodeBudget.Begin(1);
        Assert.True(SpinWait.SpinUntil(() => DecodeBudget.IsExpired, TimeSpan.FromSeconds(1)));

        Assert.False(Decode());
    }

    [Fact]
    public async Task AsyncScannerInheritsTheEnclosingClockAndDoesNotLeakItsScope() {
        var png = QR.Render("ASYNC-SCOPE", OutputFormat.Png).ToArray();
        var options = new ScanOptions { Formats = new[] { SymbolFormat.QrCode }, TimeoutMilliseconds = 10000, MaxSymbols = 1 };
        using (var enclosing = new ScanDeadline(CancellationToken.None, 1)) {
            Assert.True(SpinWait.SpinUntil(() => DecodeBudget.IsExpired, TimeSpan.FromSeconds(1)));
            using var stream = new MemoryStream(png);

            var stopped = await SymbolScanner.ScanAsync(stream, options);

            Assert.Equal(ScanCompletionReason.DeadlineExceeded, stopped.CompletionReason);
            Assert.Equal(0, stream.Position);
            Assert.True(DecodeBudget.IsExpired);
        }
        Assert.False(DecodeBudget.IsExpired);
        using var freshStream = new MemoryStream(png);
        var fresh = await SymbolScanner.ScanAsync(freshStream, options);
        Assert.Equal("ASYNC-SCOPE", Assert.Single(fresh.Symbols).Text);
        Assert.False(DecodeBudget.IsExpired);
    }

    private sealed class ExpiringReadStream : Stream {
        private bool _disposed;
        public int ReadCalls { get; private set; }
        public override bool CanRead => !_disposed;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => ReadCalls; set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
            ReadCalls++;
            if (ReadCalls == 1) Assert.True(SpinWait.SpinUntil(() => DecodeBudget.IsExpired, TimeSpan.FromSeconds(1)));
            if (ReadCalls > 16) return Task.FromResult(0);
            buffer[offset] = 42;
            return Task.FromResult(1);
        }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        protected override void Dispose(bool disposing) { _disposed = true; base.Dispose(disposing); }
    }
}
