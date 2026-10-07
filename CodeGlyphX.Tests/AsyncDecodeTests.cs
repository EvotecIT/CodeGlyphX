using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CodeGlyphX.Rendering;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class AsyncDecodeTests {
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
