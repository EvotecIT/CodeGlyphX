using System;
using System.IO;
using System.Threading;
using CodeGlyphX;
using CodeGlyphX.DataMatrix;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class ImageDecodeOptionsTests {
    [Fact]
    public void CodeGlyph_Downscales_NonQr_ImageOptions() {
        var matrix = DataMatrixEncoder.Encode("DOWNSCALE");
        var pixels = MatrixPngRenderer.RenderPixels(
            matrix,
            new MatrixPngRenderOptions { ModuleSize = 20, QuietZone = 2 },
            out var width,
            out var height,
            out var stride);

        var options = new CodeGlyphDecodeOptions {
            Image = new ImageDecodeOptions { MaxDimension = 200, RecognitionBudgetMilliseconds = 200 },
            Qr = new QrPixelDecodeOptions { Profile = QrDecodeProfile.Fast, BudgetMilliseconds = 200 }
        };

        Assert.True(CodeGlyph.TryDecode(pixels, width, height, stride, PixelFormat.Rgba32, out var decoded, options));
        Assert.Equal("DOWNSCALE", decoded.Text);
    }

    [Fact]
    public void Barcode_Downscales_WithImageOptions() {
        var barcode = BarcodeEncoder.Encode(BarcodeType.Code128, "TEST-123");
        var png = BarcodePngRenderer.Render(barcode, new BarcodePngRenderOptions {
            ModuleSize = 12,
            QuietZone = 10,
            HeightModules = 40
        });

        var options = new ImageDecodeOptions { MaxDimension = 600 };

        Assert.True(Barcode.TryDecodePng(png, BarcodeType.Code128, options, out var decoded));
        Assert.Equal("TEST-123", decoded.Text);
    }

    [Fact]
    public void Barcode_MaxDimension_Is_A_Hard_Recognition_Bound() {
        var barcode = BarcodeEncoder.Encode(BarcodeType.Code128, "BOUND-123");
        var png = BarcodePngRenderer.Render(barcode, new BarcodePngRenderOptions {
            ModuleSize = 12,
            QuietZone = 10,
            HeightModules = 40
        });

        Assert.True(Barcode.TryDecodePng(png, BarcodeType.Code128, out _));
        Assert.False(Barcode.TryDecodePng(
            png,
            BarcodeType.Code128,
            new ImageDecodeOptions { MaxDimension = 1 },
            out _));
    }

    [Fact]
    public void Barcode_RecognitionBudget_Starts_After_Stream_Decode() {
        var barcode = BarcodeEncoder.Encode(BarcodeType.Code128, "POST-RASTER-BUDGET");
        var png = BarcodePngRenderer.Render(barcode, new BarcodePngRenderOptions {
            ModuleSize = 6,
            QuietZone = 10,
            HeightModules = 40
        });
        using var stream = new DelayedReadStream(png, delayMilliseconds: 1200);

        var success = Barcode.TryDecodeImage(
            stream,
            BarcodeType.Code128,
            new ImageDecodeOptions { RecognitionBudgetMilliseconds = 1000 },
            out var decoded);

        Assert.True(success);
        Assert.Equal("POST-RASTER-BUDGET", decoded.Text);
    }

    [Fact]
    public void Qr_Downscales_WithImageOptions() {
        var png = QrCode.Render("HELLO-WORLD", OutputFormat.Png, new QrEasyOptions { ModuleSize = 18, QuietZone = 4 }).Data;
        var imageOptions = new ImageDecodeOptions { MaxDimension = 200 };

        Assert.True(QR.TryDecodePng(png, imageOptions, options: null, out var decoded));
        Assert.Equal("HELLO-WORLD", decoded.Text);
    }

    [Fact]
    public void Aztec_OptionAware_Image_Transports_RoundTrip_WithDiagnostics() {
        const string payload = "AZTEC-IMAGE-OPTIONS";
        var png = AztecCode.Render(payload, OutputFormat.Png).Data;
        var options = new ImageDecodeOptions { RecognitionBudgetMilliseconds = TestBudget.Adjust(2000) };

        Assert.True(AztecCode.TryDecodeImage(png, options, CancellationToken.None, out var byteText));
        Assert.Equal(payload, byteText);

        Assert.True(AztecCode.TryDecodeImage((ReadOnlySpan<byte>)png, options, CancellationToken.None, out var spanText, out var diagnostics));
        Assert.Equal(payload, spanText);
        Assert.Null(diagnostics.Failure);

        Assert.True(AztecCode.TryDecodePng((ReadOnlySpan<byte>)png, options, CancellationToken.None, out var spanPngText));
        Assert.Equal(payload, spanPngText);

        Assert.True(AztecCode.TryDecodePng(png, options, CancellationToken.None, out var diagnosedPngText, out var pngDiagnostics));
        Assert.Equal(payload, diagnosedPngText);
        Assert.Null(pngDiagnostics.Failure);

        using var stream = new MemoryStream(png, writable: false);
        Assert.True(AztecCode.TryDecodeImage(stream, options, CancellationToken.None, out var streamText));
        Assert.Equal(payload, streamText);
    }

    [Fact]
    public void DataMatrix_OptionAware_Image_Transports_RoundTrip_WithDiagnostics() {
        const string payload = "DATA-MATRIX-IMAGE-OPTIONS";
        var png = DataMatrixCode.Render(payload, OutputFormat.Png).Data;
        var options = new ImageDecodeOptions { RecognitionBudgetMilliseconds = TestBudget.Adjust(2000) };

        Assert.True(DataMatrixCode.TryDecodePng(png, options, CancellationToken.None, out var byteText));
        Assert.Equal(payload, byteText);

        Assert.True(DataMatrixCode.TryDecodeImage((ReadOnlySpan<byte>)png, options, CancellationToken.None, out var spanText, out var diagnostics));
        Assert.Equal(payload, spanText);
        Assert.Null(diagnostics.Failure);

        Assert.True(DataMatrixCode.TryDecodePng((ReadOnlySpan<byte>)png, options, CancellationToken.None, out var spanPngText));
        Assert.Equal(payload, spanPngText);

        Assert.True(DataMatrixCode.TryDecodePng(png, options, CancellationToken.None, out var diagnosedPngText, out var pngDiagnostics));
        Assert.Equal(payload, diagnosedPngText);
        Assert.Null(pngDiagnostics.Failure);

        using var stream = new MemoryStream(png, writable: false);
        Assert.True(DataMatrixCode.TryDecodeImage(stream, options, CancellationToken.None, out var streamText));
        Assert.Equal(payload, streamText);
    }

    [Fact]
    public void Pdf417_OptionAware_Image_Transports_RoundTrip_WithDiagnostics() {
        const string payload = "PDF417-IMAGE-OPTIONS";
        var png = Pdf417Code.Render(payload, OutputFormat.Png).Data;
        var options = new ImageDecodeOptions { RecognitionBudgetMilliseconds = TestBudget.Adjust(2000) };

        Assert.True(Pdf417Code.TryDecodePng(png, options, CancellationToken.None, out string byteText));
        Assert.Equal(payload, byteText);

        Assert.True(Pdf417Code.TryDecodeImage((ReadOnlySpan<byte>)png, options, CancellationToken.None, out var spanText, out var diagnostics));
        Assert.Equal(payload, spanText);
        Assert.Null(diagnostics.Failure);

        Assert.True(Pdf417Code.TryDecodePng((ReadOnlySpan<byte>)png, options, CancellationToken.None, out string spanPngText));
        Assert.Equal(payload, spanPngText);

        Assert.True(Pdf417Code.TryDecodePng(png, options, CancellationToken.None, out var diagnosedPngText, out var pngDiagnostics));
        Assert.Equal(payload, diagnosedPngText);
        Assert.Null(pngDiagnostics.Failure);

        using var stream = new MemoryStream(png, writable: false);
        Assert.True(Pdf417Code.TryDecodeImage(stream, options, CancellationToken.None, out var streamText));
        Assert.Equal(payload, streamText);
    }

    [Fact]
    public void ImageDecodeOptions_Guarded_Sets_DefaultCaps() {
        var options = ImageDecodeOptions.Guarded();

        Assert.Equal(64 * 1024 * 1024, options.MaxBytes);
        Assert.Equal(20_000_000, options.MaxPixels);
        Assert.Equal(120, options.MaxAnimationFrames);
        Assert.Equal(60_000, options.MaxAnimationDurationMs);
        Assert.Equal(20_000_000, options.MaxAnimationFramePixels);
    }

    [Fact]
    public void ImageDecodeOptions_Strict_Sets_Tighter_Caps() {
        var options = ImageDecodeOptions.Strict();

        Assert.Equal(8 * 1024 * 1024, options.MaxBytes);
        Assert.Equal(8_000_000, options.MaxPixels);
        Assert.Equal(60, options.MaxAnimationFrames);
        Assert.Equal(15_000, options.MaxAnimationDurationMs);
        Assert.Equal(8_000_000, options.MaxAnimationFramePixels);
    }

    [Fact]
    public void ImageDecodeOptions_Guarded_ZeroAnimationFramePixels_DisablesThatLimit() {
        var options = ImageDecodeOptions.Guarded(maxAnimationFramePixels: 0);

        Assert.Equal(0, options.MaxAnimationFramePixels);
    }

    private sealed class DelayedReadStream : Stream {
        private readonly Stream _inner;
        private readonly int _delayMilliseconds;
        private bool _delayed;

        public DelayedReadStream(byte[] data, int delayMilliseconds) {
            _inner = new MemoryStream(data, writable: false);
            _delayMilliseconds = delayMilliseconds;
        }

        public override bool CanRead => _inner.CanRead;
        public override bool CanSeek => _inner.CanSeek;
        public override bool CanWrite => false;
        public override long Length => _inner.Length;
        public override long Position {
            get => _inner.Position;
            set => _inner.Position = value;
        }

        public override void Flush() { }

        public override int Read(byte[] buffer, int offset, int count) {
            if (!_delayed) {
                _delayed = true;
                Thread.Sleep(_delayMilliseconds);
            }
            return _inner.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing) {
            if (disposing) _inner.Dispose();
            base.Dispose(disposing);
        }
    }
}
