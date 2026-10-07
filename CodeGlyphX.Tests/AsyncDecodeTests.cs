using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class AsyncDecodeTests {
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Scanner_AsyncTransportsPreserveRecognitionOutcome(bool containsSymbol) {
        var png = containsSymbol ? QR.Render("ASYNC", OutputFormat.Png).ToArray()
            : MatrixPngRenderer.Render(new BitMatrix(1, 1), new MatrixPngRenderOptions { ModuleSize = 1, QuietZone = 0 });
        var options = new ScanOptions {
            Formats = new[] { SymbolFormat.QrCode }, MaxSymbols = 1, TimeoutMilliseconds = 5000,
            Qr = QrPixelDecodeOptions.Fast()
        };
        using var stream = new MemoryStream(png);
        var streamed = await SymbolScanner.ScanAsync(stream, options);
        Assert.Equal(stream.Length, stream.Position);
        Assert.True(stream.CanRead);
        AssertTransportOutcome(streamed, containsSymbol);

        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".png");
        try {
            await File.WriteAllBytesAsync(path, png);
            AssertTransportOutcome(await SymbolScanner.ScanFileAsync(path, options), containsSymbol);
        } finally {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task Scanner_AsyncTransportsRejectPayloadAbovePerCallByteLimit() {
        var png = QR.Render("ASYNC", OutputFormat.Png).ToArray();
        var options = new ScanOptions { Image = new ImageDecodeOptions { MaxBytes = png.Length - 1 } };
        using var stream = new MemoryStream(png);
        Assert.Equal(ScanStatus.InvalidImage, (await SymbolScanner.ScanAsync(stream, options)).Status);
        Assert.Equal(0, stream.Position);
        Assert.True(stream.CanRead);

        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".png");
        try {
            await File.WriteAllBytesAsync(path, png);
            Assert.Equal(ScanStatus.InvalidImage, (await SymbolScanner.ScanFileAsync(path, options)).Status);
        } finally {
            File.Delete(path);
        }
    }

    private static void AssertTransportOutcome(ScanResult result, bool containsSymbol) {
        Assert.Equal(containsSymbol ? ScanStatus.Success : ScanStatus.NoSymbolFound, result.Status);
        Assert.Equal(containsSymbol ? ScanCompletionReason.SymbolLimitReached : ScanCompletionReason.Completed, result.CompletionReason);
        if (containsSymbol) Assert.Equal("ASYNC", Assert.Single(result.Symbols).Text);
        else Assert.Empty(result.Symbols);
    }

    [Fact]
    public async Task Scanner_SyncAndAsyncConsumeTheRemainingStream() {
        var png = QR.Render("ASYNC", OutputFormat.Png).Data.ToArray();
        var prefixed = new byte[png.Length + 3];
        Array.Copy(png, 0, prefixed, 3, png.Length);
        var options = new ScanOptions { Formats = new[] { SymbolFormat.QrCode }, MaxSymbols = 1, TimeoutMilliseconds = 5000 };
        using var sync = new MemoryStream(prefixed);
        sync.Position = 3;
        Assert.Equal("ASYNC", Assert.Single(SymbolScanner.Scan(sync, options).Symbols).Text);
        Assert.Equal(sync.Length, sync.Position);
        using var asyncStream = new MemoryStream(prefixed);
        asyncStream.Position = 3;
        var result = await SymbolScanner.ScanAsync(asyncStream, options);
        Assert.Equal("ASYNC", Assert.Single(result.Symbols).Text);
        Assert.Equal(asyncStream.Length, asyncStream.Position);
        Assert.Equal(ScanStatus.InvalidImage, (await SymbolScanner.ScanAsync(asyncStream, options)).Status);
        Assert.True(asyncStream.CanRead);
    }

    [Fact]
    public async Task Scanner_CancellationPrecedesOpeningAFile() {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "missing.png");
        var token = new CancellationToken(true);
        var sync = SymbolScanner.ScanFile(path, new ScanOptions { CancellationToken = token });
        var asyncResult = await SymbolScanner.ScanFileAsync(path, cancellationToken: token);
        Assert.Equal(ScanCompletionReason.Cancelled, sync.CompletionReason);
        Assert.Equal(ScanCompletionReason.Cancelled, asyncResult.CompletionReason);
        Assert.Empty(asyncResult.Symbols);
    }

    [Fact]
    public async Task Scanner_DeadlineIncludesAsyncTransport() {
        using var stream = new SlowStream();
        var result = await SymbolScanner.ScanAsync(stream, new ScanOptions { TimeoutMilliseconds = 30 });
        Assert.Equal(ScanStatus.DeadlineExceeded, result.Status);
        Assert.Equal(ScanCompletionReason.DeadlineExceeded, result.CompletionReason);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(64)]
    public void Scanner_SynchronousTransportStopsAfterTheReadThatCancels(int maxBytes) {
        using var cancellation = new CancellationTokenSource();
        using var stream = new ChunkedReadStream(cancellation.Cancel);

        var result = SymbolScanner.Scan(stream, new ScanOptions {
            CancellationToken = cancellation.Token, Image = new ImageDecodeOptions { MaxBytes = maxBytes }
        });

        Assert.Equal(1, stream.ReadCalls);
        Assert.Equal(1, stream.BytesRead);
        Assert.True(stream.CanRead);
        Assert.Equal(ScanStatus.Cancelled, result.Status);
        Assert.Equal(ScanCompletionReason.Cancelled, result.CompletionReason);
    }

    [Fact]
    public async Task Scanner_SynchronousTransportObservesDeadlineWhenABlockedReadReturns() {
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        using var stream = new ChunkedReadStream(() => {
            entered.Set();
            if (!release.Wait(TimeSpan.FromSeconds(5))) throw new TimeoutException("The test did not release the read.");
        });
        var scan = Task.Run(() => SymbolScanner.Scan(stream, new ScanOptions { TimeoutMilliseconds = 50 }));
        try {
            Assert.True(entered.Wait(TimeSpan.FromSeconds(5)), "The scanner did not begin reading.");
            // The real deadline must expire while Read is blocked. No elapsed-time performance
            // assertion is made: the contract is to stop before another read after it is released.
            await Task.Delay(250);
        } finally {
            release.Set();
        }
        var result = await scan;

        Assert.Equal(1, stream.ReadCalls);
        Assert.True(stream.CanRead);
        Assert.Equal(ScanStatus.DeadlineExceeded, result.Status);
        Assert.Equal(ScanCompletionReason.DeadlineExceeded, result.CompletionReason);
    }

    private sealed class ChunkedReadStream : Stream {
        private readonly Action _firstRead;
        private bool _disposed;
        public int ReadCalls { get; private set; }
        public int BytesRead { get; private set; }
        public ChunkedReadStream(Action firstRead) => _firstRead = firstRead;
        public override bool CanRead => !_disposed;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => BytesRead; set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) {
            ReadCalls++;
            if (ReadCalls == 1) _firstRead();
            if (count == 0 || BytesRead == 16) return 0;
            buffer[offset] = 42;
            BytesRead++;
            return 1;
        }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        protected override void Dispose(bool disposing) { _disposed = true; base.Dispose(disposing); }
    }

    private sealed class SlowStream : Stream {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
            await Task.Delay(10000, cancellationToken);
            return 0;
        }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
