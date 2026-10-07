using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;
using CodeGlyphX.Rendering.Tiff;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class StreamContractTests {
    private const int PrefixLength = 5;
    private const string Payload = "STREAM-CONTRACT";

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public async Task SharedReadersConsumeOnlyRemainingBytes(int streamKind) {
        var expected = new byte[] { 10, 20, 30 };
        foreach (var limit in new[] { 0, expected.Length }) {
            using var syncStream = CreateStream(expected, streamKind);
            Assert.Equal(expected, RenderIO.ReadBinary(syncStream, limit));
            Assert.Empty(RenderIO.ReadBinary(syncStream, limit));
            AssertConsumed(syncStream);

            using var asyncStream = CreateStream(expected, streamKind);
            Assert.Equal(expected, await RenderIO.ReadBinaryAsync(asyncStream, limit));
            Assert.Empty(await RenderIO.ReadBinaryAsync(asyncStream, limit));
            AssertConsumed(asyncStream);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public async Task LimitedReadersRejectKnownOversizeBeforeReadingAndBoundUnknownLengths(int streamKind) {
        var input = new byte[16];
        using var syncStream = CreateStream(input, streamKind);
        var expectedRemaining = syncStream.CanSeek ? input.Length : input.Length - 4;
        Assert.Throws<FormatException>(() => RenderIO.ReadBinary(syncStream, 3));
        Assert.Equal(expectedRemaining, RenderIO.ReadBinary(syncStream).Length);

        using var asyncStream = CreateStream(input, streamKind);
        await Assert.ThrowsAsync<FormatException>(() => RenderIO.ReadBinaryAsync(asyncStream, 3));
        Assert.Equal(expectedRemaining, (await RenderIO.ReadBinaryAsync(asyncStream)).Length);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void QrDecodeAdaptersReadFromCurrentPosition(int streamKind) {
        var png = CreateQrPng();
        using var stream = CreateStream(png, streamKind);
        var result = QrImageDecoder.DecodeImageResult(stream, new ImageDecodeOptions { MaxBytes = png.Length });
        Assert.True(result.IsSuccess, result.Message);
        Assert.Equal(Payload, result.Value!.Text);
        AssertConsumed(stream);
        Assert.False(QrImageDecoder.DecodeImageResult(stream).IsSuccess);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public async Task UnifiedScannerReadsFromCurrentPosition(int streamKind) {
        var png = CreateQrPng();
        var options = new ScanOptions {
            Formats = new[] { SymbolFormat.QrCode },
            TimeoutMilliseconds = 5000,
            Image = new ImageDecodeOptions { MaxBytes = png.Length }
        };
        using (var stream = CreateStream(png, streamKind)) {
            var result = SymbolScanner.Scan(stream, options);
            Assert.True(result.IsSuccess, result.Failure);
            Assert.Equal(Payload, Assert.Single(result.Symbols).Text);
            AssertConsumed(stream);
            Assert.False(SymbolScanner.Scan(stream, options).IsSuccess);
        }
        using (var stream = CreateStream(png, streamKind)) {
            var result = await SymbolScanner.ScanAsync(stream, options);
            Assert.True(result.IsSuccess, result.Failure);
            Assert.Equal(Payload, Assert.Single(result.Symbols).Text);
            AssertConsumed(stream);
        }
    }

    [Fact]
    public void BoundedFileReadersEnforceSharedReadContract() {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".bin");
        var expected = new byte[] { 10, 20, 30 };
        try {
            File.WriteAllBytes(path, expected);
            Assert.Equal(expected, RenderIO.ReadBinary(path, expected.Length));
            Assert.True(RenderIO.TryReadBinary(path, expected.Length, out var actual));
            Assert.Equal(expected, actual);
            Assert.Throws<FormatException>(() => RenderIO.ReadBinary(path, expected.Length - 1));
            Assert.False(RenderIO.TryReadBinary(path, expected.Length - 1, out var rejected));
            Assert.Empty(rejected);
        } finally {
            File.Delete(path);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void SpecialistResultsReadFromCurrentPosition(int streamKind) {
        using (var stream = CreateStream(Barcode.Render(SymbolFormat.Code128, Payload, OutputFormat.Png).ToArray(), streamKind)) {
            var result = Barcode.DecodeImageResult(stream, SymbolFormat.Code128);
            Assert.True(result.IsSuccess, result.Message);
            Assert.Equal(Payload, result.Value!.Text);
            AssertConsumed(stream);
        }
        using (var stream = CreateStream(DataMatrixCode.Render(Payload, OutputFormat.Png).ToArray(), streamKind)) {
            var result = DataMatrixCode.DecodeImageResult(stream);
            Assert.True(result.IsSuccess, result.Message);
            Assert.Equal(Payload, result.Value);
            AssertConsumed(stream);
        }
        using (var stream = CreateStream(Pdf417Code.Render(Payload, OutputFormat.Png).ToArray(), streamKind)) {
            var result = Pdf417Code.DecodeImageResult(stream);
            Assert.True(result.IsSuccess, result.Message);
            Assert.Equal(Payload, result.Value);
            AssertConsumed(stream);
        }
        using (var stream = CreateStream(AztecCode.Render(Payload, OutputFormat.Png).ToArray(), streamKind)) {
            var result = AztecCode.DecodeImageResult(stream);
            Assert.True(result.IsSuccess, result.Message);
            Assert.Equal(Payload, result.Value);
            AssertConsumed(stream);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void ImageMetadataAndMultipleFramesConsumeRemainingBytes(int streamKind) {
        using (var stream = CreateStream(CreateQrPng(), streamKind)) {
            Assert.True(ImageReader.TryReadInfo(stream, out var info));
            Assert.Equal(ImageFormat.Png, info.Format);
            AssertConsumed(stream);
        }

        var gif = Convert.FromBase64String("R0lGODlhAQABAPAAAAAAAAAAACH5BAEAAAAALAAAAAABAAEAAAICRAEAOw==");
        using (var stream = CreateStream(gif, streamKind)) {
            Assert.True(ImageReader.TryReadAnimationInfo(stream, out var info));
            Assert.Equal(1, info.FrameCount);
            AssertConsumed(stream);
        }
        using (var stream = CreateStream(gif, streamKind)) {
            Assert.True(ImageReader.TryDecodeGifAnimationFrames(stream, out var frames, out _, out _, out _));
            Assert.Single(frames);
            AssertConsumed(stream);
        }
        using (var stream = CreateStream(gif, streamKind)) {
            Assert.True(ImageReader.TryDecodeGifAnimationCanvasFrames(stream, out var frames, out _, out _, out _));
            Assert.Single(frames);
            AssertConsumed(stream);
        }

        var tiff = TiffWriter.WriteRgba32Pages(
            new TiffRgba32Page(new byte[] { 255, 0, 0, 255 }, 1, 1, 4),
            new TiffRgba32Page(new byte[] { 0, 255, 0, 255 }, 1, 1, 4));
        using (var stream = CreateStream(tiff, streamKind)) {
            Assert.True(ImageReader.TryReadInfo(stream, 1, out var info));
            Assert.Equal(1, info.Width);
            AssertConsumed(stream);
        }
        using (var stream = CreateStream(tiff, streamKind)) {
            Assert.True(ImageReader.TryReadPageCount(stream, out var count));
            Assert.Equal(2, count);
            AssertConsumed(stream);
        }
        using (var stream = CreateStream(tiff, streamKind)) {
            Assert.True(ImageReader.TryDecodeTiffPagesRgba32(stream, out var pages));
            Assert.Equal(2, pages.Length);
            AssertConsumed(stream);
        }
        using (var stream = CreateStream(tiff, streamKind)) {
            Assert.Equal(2, ImageReader.DecodeTiffPagesRgba32(stream).Length);
            AssertConsumed(stream);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public async Task CancelledScannerReturnsStructuredCancellationWithoutReading(int streamKind) {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var options = new ScanOptions { CancellationToken = cancellation.Token };
        Func<Stream, Task<ScanResult>>[] operations = {
            stream => SymbolScanner.ScanAsync(stream, cancellationToken: cancellation.Token),
            stream => SymbolScanner.ScanAsync(stream, options)
        };
        var png = CreateQrPng();
        foreach (var operation in operations) {
            using var stream = CreateStream(png, streamKind);
            var result = await operation(stream);
            Assert.Equal(ScanStatus.Cancelled, result.Status);
            Assert.Equal(ScanCompletionReason.Cancelled, result.CompletionReason);
            Assert.Equal(png, RenderIO.ReadBinary(stream));
        }
        using var syncStream = CreateStream(png, streamKind);
        Assert.Equal(ScanStatus.Cancelled, SymbolScanner.Scan(syncStream, options).Status);
        Assert.Equal(png, RenderIO.ReadBinary(syncStream));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public async Task ExpertAsyncReadersReturnCancelledTasksWithoutReading(int streamKind) {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var png = CreateQrPng();
        using var stream = CreateStream(png, streamKind);
        var task = RenderIO.ReadBinaryAsync(stream, png.Length, cancellation.Token);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        Assert.True(task.IsCanceled);
        Assert.Equal(png, RenderIO.ReadBinary(stream));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    public async Task SharedAsyncReadersObserveCancellationRaisedDuringRead(int limit) {
        using var cancellation = new CancellationTokenSource();
        using var stream = new CancellingStream(cancellation);
        var task = RenderIO.TryReadBinaryAsync(stream, limit, cancellation.Token);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        Assert.True(task.IsCanceled);
        Assert.Equal(1, stream.ReadCount);
    }

    [Fact]
    public async Task ScannerPreservesCancellationRaisedDuringRead() {
        using var cancellation = new CancellationTokenSource();
        using var stream = new CancellingStream(cancellation);
        var result = await SymbolScanner.ScanAsync(stream, cancellationToken: cancellation.Token);
        Assert.Equal(ScanStatus.Cancelled, result.Status);
        Assert.Equal(ScanCompletionReason.Cancelled, result.CompletionReason);
        Assert.Equal(1, stream.ReadCount);
    }

    [Fact]
    public async Task ExpertFileAsyncReadersPreserveTaskCancellation() {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        // Cancellation is checked before opening even a nonexistent file.
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".png");
        Func<Task>[] operations = {
            () => RenderIO.ReadBinaryAsync(path, cancellation.Token),
            () => RenderIO.ReadBinaryAsync(path, maxBytes: 1024, cancellation.Token),
            () => RenderIO.TryReadBinaryAsync(path, maxBytes: 0, cancellation.Token),
            () => RenderIO.TryReadBinaryAsync(path, maxBytes: 1024, cancellation.Token)
        };
        foreach (var operation in operations) {
            var task = operation();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
            Assert.True(task.IsCanceled);
        }
    }

    private static byte[] CreateQrPng() => QrPngRenderer.Render(
        QrCodeEncoder.EncodeText(Payload).Modules,
        new QrPngRenderOptions { ModuleSize = 14, QuietZone = 6 });

    private static Stream CreateStream(byte[] payload, int kind) {
        var bytes = new byte[PrefixLength + payload.Length];
        for (var i = 0; i < PrefixLength; i++) bytes[i] = 255;
        Array.Copy(payload, 0, bytes, PrefixLength, payload.Length);
        MemoryStream memory;
        if (kind == 2) {
            var backing = new byte[7 + bytes.Length + 9];
            Array.Copy(bytes, 0, backing, 7, bytes.Length);
            memory = new MemoryStream(backing, 7, bytes.Length, false, true);
        } else {
            memory = kind == 0
                ? new MemoryStream(bytes, 0, bytes.Length, false, true)
                : new MemoryStream(bytes, false);
        }
        memory.Position = PrefixLength;
        if (kind == 3) return new BufferedStream(memory);
        if (kind == 4) return new NonSeekableStream(memory);
        return memory;
    }

    private static void AssertConsumed(Stream stream) {
        if (stream.CanSeek) Assert.Equal(stream.Length, stream.Position);
        Assert.Equal(-1, stream.ReadByte());
    }

    private sealed class NonSeekableStream : Stream {
        private readonly Stream _inner;
        internal NonSeekableStream(Stream inner) => _inner = inner;
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => _inner.ReadAsync(buffer, offset, count, cancellationToken);
        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        protected override void Dispose(bool disposing) { if (disposing) _inner.Dispose(); base.Dispose(disposing); }
    }

    private sealed class CancellingStream : Stream {
        private readonly CancellationTokenSource _cancellation;
        internal CancellingStream(CancellationTokenSource cancellation) => _cancellation = cancellation;
        internal int ReadCount { get; private set; }
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override int Read(byte[] buffer, int offset, int count) {
            ReadCount++;
            buffer[offset] = 42;
            _cancellation.Cancel();
            return 1;
        }
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => Task.FromResult(Read(buffer, offset, count));
        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
