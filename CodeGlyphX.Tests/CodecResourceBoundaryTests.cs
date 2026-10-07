using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Gif;
using CodeGlyphX.Rendering.Jpeg;
using CodeGlyphX.Rendering.Png;
using CodeGlyphX.Rendering.Tiff;
using CodeGlyphX.Rendering.Webp;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("GlobalState")]
public sealed class CodecResourceBoundaryTests {
    [Fact]
    public void PdfFlateRejectsExpansionBeyondDeclaredRaster() {
        var pdf = BuildPdf(Deflate(new byte[65536]), "/FlateDecode");
        var violations = CaptureViolations(() =>
            Assert.False(ImageReader.TryDecodeRgba32(pdf, ImageDecodeOptions.Strict(maxPixels: 1), out _, out _, out _)));

        Assert.Contains(violations, v => v.Kind == ImageDecodeLimitKind.MaxDecodedBytes && v.Limit == 1 && v.Actual == 2);
    }

    [Theory]
    [InlineData("/ASCII85Decode", "z~>")]
    [InlineData("/RunLengthDecode", "")]
    public void PdfOtherFiltersRejectExpansionBeyondDeclaredRaster(string filter, string text) {
        var data = filter == "/RunLengthDecode" ? new byte[] { 129, 23, 128 } : Encoding.ASCII.GetBytes(text);
        var violations = CaptureViolations(() =>
            Assert.False(ImageReader.TryDecodeRgba32(BuildPdf(data, filter), out _, out _, out _)));

        Assert.Contains(violations, v => v.Kind == ImageDecodeLimitKind.MaxDecodedBytes && v.Limit == 1 && v.Actual > 1);
    }

    [Fact]
    public void PdfIntermediateFiltersHonorConfiguredDecodedLimit() {
        var pdf = BuildPdf(Encoding.ASCII.GetBytes("zzzzzzzz~>"), "[/ASCII85Decode /FlateDecode]");
        var violations = CaptureViolations(() =>
            Assert.False(ImageReader.TryDecodeRgba32(pdf, new ImageDecodeOptions { MaxDecodedBytes = 16 }, out _, out _, out _)));

        Assert.Contains(violations, v => v.Kind == ImageDecodeLimitKind.MaxDecodedBytes && v.Limit == 16);
    }

    [Fact]
    public void TiffPaddedTileHonorsDecodedLimitWithoutRejectingValidPadding() {
        var tiff = BuildTiffTile(32, 32773, PackBits(32 * 32, 199));
        Assert.True(ImageReader.TryDecodeRgba32(tiff, out var rgba, out var width, out var height));
        Assert.Equal(1, width);
        Assert.Equal(1, height);
        Assert.Equal(new byte[] { 199, 199, 199, 255 }, rgba);

        var violations = CaptureViolations(() =>
            Assert.False(ImageReader.TryDecodeRgba32(tiff, ImageDecodeOptions.Strict(maxPixels: 1), out _, out _, out _)));
        Assert.Contains(violations, v => v.Kind == ImageDecodeLimitKind.MaxDecodedBytes && v.Actual == 32);

        // An explicit byte budget permits a legal tile larger than its visible edge.
        Assert.True(ImageReader.TryDecodeRgba32(tiff,
            ImageDecodeOptions.Strict(maxPixels: 1).WithMaxDecodedBytes(1024), out var permitted, out _, out _));
        Assert.Equal(rgba, permitted);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void MalformedDeflateReturnsFalseFromTryDecode(bool pdf) {
        var corrupt = new byte[] { 255, 255, 255, 255 };
        var image = pdf ? BuildPdf(corrupt, "/FlateDecode") : BuildTiffTile(1, 32946, corrupt);
        Assert.False(ImageReader.TryDecodeRgba32(image, out _, out _, out _));
    }

    [Fact]
    public async Task PngStreamAndSharedAsyncReaderStopAtByteLimit() {
        var previous = ImageReader.MaxImageBytes;
        try {
            ImageReader.MaxImageBytes = 64;
            using var pngStream = new CountingStream(new byte[65536]);
            Assert.Throws<FormatException>(() => PngReader.DecodeRgba32(pngStream, out _, out _));
            Assert.Equal(65, pngStream.Consumed);

            using var asyncStream = new CountingStream(new byte[65536]);
            await Assert.ThrowsAsync<FormatException>(() => RenderIO.ReadBinaryAsync(asyncStream, 64));
            Assert.Equal(65, asyncStream.Consumed);
        } finally {
            ImageReader.MaxImageBytes = previous;
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AnimationBudgetCapsRetainedFrames(bool webp) {
        var first = new BitMatrix(2, 2);
        first[0, 0] = true;
        var second = new BitMatrix(2, 2);
        second[1, 1] = true;
        var render = new MatrixPngRenderOptions { ModuleSize = 1, QuietZone = 0 };
        var data = webp
            ? MatrixWebpRenderer.RenderAnimation(new[] { first, second }, render, durationMs: 50)
            : MatrixGifRenderer.RenderAnimation(new[] { first, second }, render, durationMs: 50);

        var previous = ImageReader.MaxDecodedBytes;
        var violations = CaptureViolations(() => {
            try {
                ImageReader.MaxDecodedBytes = 16;
                if (webp) {
                    Assert.False(WebpReader.TryDecodeAnimationFrames(data, out _, out _, out _, out _));
                    Assert.False(WebpReader.TryDecodeAnimationCanvasFrames(data, out _, out _, out _, out _));
                } else {
                    Assert.False(GifReader.TryDecodeAnimationFrames(data, out _, out _, out _, out _));
                    Assert.False(GifReader.TryDecodeAnimationCanvasFrames(data, out _, out _, out _, out _));
                }
            } finally {
                ImageReader.MaxDecodedBytes = previous;
            }
        });
        Assert.Contains(violations, v => v.Kind == ImageDecodeLimitKind.MaxDecodedBytes && v.Actual == 32);

        // The generic composite API may fall back to its existing first-frame path.
        Assert.True(ImageReader.TryDecodeRgba32Composite(data,
            new ImageDecodeOptions { MaxDecodedBytes = 16 }, out var composite, out _, out _));
        Assert.Equal(16, composite.Length);

        Assert.True(ImageReader.TryDecodeAnimationFrames(data,
            new ImageDecodeOptions { MaxDecodedBytes = 32 }, out var frames, out var width, out var height, out _));
        Assert.Equal(2, frames.Length);
        Assert.Equal(2, width);
        Assert.Equal(2, height);
    }

    [Fact]
    public void DecodedLimitSupportsInheritanceAndExplicitDisable() {
        var png = QR.Render("DECODED-LIMIT", OutputFormat.Png).Data;
        var previous = ImageReader.MaxDecodedBytes;
        try {
            ImageReader.MaxDecodedBytes = 4;
            Assert.False(ImageReader.TryDecodeRgba32(png, new ImageDecodeOptions(), out _, out _, out _));
            Assert.True(ImageReader.TryDecodeRgba32(png,
                new ImageDecodeOptions { MaxDecodedBytes = 0 }, out _, out _, out _));
        } finally {
            ImageReader.MaxDecodedBytes = previous;
        }
    }

    [Fact]
    public void GifRejectsOversizedIndexBufferBeforeExpandingPixels() {
        var frame = new BitMatrix(8, 8);
        var gif = MatrixGifRenderer.RenderAnimation(new[] { frame },
            new MatrixPngRenderOptions { ModuleSize = 1, QuietZone = 0 }, durationMs: 50);
        var violations = CaptureViolations(() =>
            Assert.False(ImageReader.TryDecodeAnimationFrames(gif,
                new ImageDecodeOptions { MaxDecodedBytes = 63 }, out _, out _, out _, out _)));
        Assert.Equal(64, Assert.Single(violations).Actual);
    }

    [Fact]
    public void ScannerAndMicroQrImageOptionsPreserveDecodedLimit() {
        var matrix = MicroQrCodeEncoder.EncodeAlphanumeric("LIMIT", minVersion: 4, maxVersion: 4);
        var png = MatrixPngRenderer.Render(matrix.Modules,
            new MatrixPngRenderOptions { ModuleSize = 8, QuietZone = 2 });
        var options = new ImageDecodeOptions { MaxDecodedBytes = 4 };
        Assert.Equal(ScanStatus.InvalidImage,
            SymbolScanner.Scan(png, new ScanOptions { Formats = new[] { SymbolFormat.MicroQrCode }, Image = options }).Status);
        Assert.False(MicroQrDecoder.TryDecodeImage(png, out _, options));
    }

    [Fact]
    public void WebpStillHonorsDecodedBytesEvenWhenPixelLimitIsDisabled() {
        var webp = MatrixWebpRenderer.Render(new BitMatrix(2, 2),
            new MatrixPngRenderOptions { ModuleSize = 1, QuietZone = 0 });
        Assert.True(ImageReader.TryDecodeRgba32(webp, out _, out _, out _));
        Assert.False(ImageReader.TryDecodeRgba32(webp,
            new ImageDecodeOptions { MaxPixels = 0, MaxDecodedBytes = 4 }, out _, out _, out _));
    }

    [Fact]
    public void WebpLossyPaddedPlanesHonorDecodedByteLimit() {
        var webp = WebpWriter.WriteRgba32Lossy(2, 2, new byte[16], 8, quality: 50);
        var violations = CaptureViolations(() =>
            Assert.False(ImageReader.TryDecodeRgba32(webp,
                new ImageDecodeOptions { MaxPixels = 0, MaxDecodedBytes = 16 }, out _, out _, out _)));
        Assert.Contains(violations, v => v.Kind == ImageDecodeLimitKind.MaxDecodedBytes && v.Actual == 16 * 16);
        Assert.True(ImageReader.TryDecodeRgba32(webp,
            new ImageDecodeOptions { MaxDecodedBytes = 256 }, out _, out _, out _));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void JpegPaddedIntegerBuffersHonorActualByteSize(bool progressive) {
        var jpeg = MatrixJpegRenderer.Render(new BitMatrix(25, 25),
            new MatrixPngRenderOptions { ModuleSize = 1, QuietZone = 0 },
            new JpegEncodeOptions { Progressive = progressive });
        Assert.True(ImageReader.TryDecodeRgba32(jpeg, out _, out _, out _));
        var violations = CaptureViolations(() =>
            Assert.False(ImageReader.TryDecodeRgba32(jpeg,
                new ImageDecodeOptions { MaxDecodedBytes = 25 * 25 * 4 }, out _, out _, out _)));
        Assert.Contains(violations, v => v.Kind == ImageDecodeLimitKind.MaxDecodedBytes && v.Actual == 32 * 32 * sizeof(int));
    }

    [Theory]
    [InlineData(1, 128)]
    [InlineData(8, 1024)]
    [InlineData(16, 2048)]
    public void PdfRasterBuffersHonorDecodedLimitAfterSampleExpansion(int bits, int rawLength) {
        var pdf = BuildPdf(new byte[rawLength], null, width: 32, height: 32, bits: bits);
        Assert.True(ImageReader.TryDecodeRgba32(pdf, out _, out _, out _));
        Assert.False(ImageReader.TryDecodeRgba32(pdf,
            new ImageDecodeOptions { MaxDecodedBytes = rawLength }, out _, out _, out _));
    }

    private static List<ImageDecodeLimitViolation> CaptureViolations(Action action) {
        var violations = new List<ImageDecodeLimitViolation>();
        void Handler(ImageDecodeLimitViolation v) => violations.Add(v);
        ImageReader.LimitViolation += Handler;
        try { action(); } finally { ImageReader.LimitViolation -= Handler; }
        return violations;
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SharedRasterLzwPreservesBitOrdersAndRejectsUndefinedCodes(bool msb) {
        Assert.Equal(new byte[] { 193, 199 }, RasterLzwDecoder.Decode(PackLzwCodes(msb, 256, 193, 199, 257), 2, 1, msb));
        Assert.Throws<FormatException>(() => RasterLzwDecoder.Decode(PackLzwCodes(msb, 256, 193, 511, 257), 2, 1, msb));
    }

    [Fact]
    public void TiffLzwSingleStripPreservesPixelsAcrossDictionaryGrowthAndReset() {
        const int side = 64;
        var pixels = new byte[side * side * 4];
        uint state = 37;
        for (var i = 0; i < pixels.Length; i++) {
            state = unchecked(state * 1664525 + 1013904223);
            pixels[i] = i % 4 == 3 ? (byte)255 : (byte)(state >> 24);
        }
        using var stream = new MemoryStream();
        TiffWriter.WriteRgba32(stream, side, side, pixels, side * 4, rowsPerStrip: side, compression: TiffCompression.Lzw, usePredictor: false);
        Assert.True(ImageReader.TryDecodeRgba32(stream.ToArray(), out var decoded, out var width, out var height));
        Assert.Equal(side, width); Assert.Equal(side, height);
        Assert.Equal(pixels, decoded);
    }

    private static byte[] PackLzwCodes(bool msb, params int[] codes) {
        var bytes = new byte[(codes.Length * 9 + 7) / 8];
        for (var c = 0; c < codes.Length; c++) {
            for (var bit = 0; bit < 9; bit++) {
                var position = c * 9 + bit;
                var value = (codes[c] >> (msb ? 8 - bit : bit)) & 1;
                bytes[position / 8] |= (byte)(value << (msb ? 7 - position % 8 : position % 8));
            }
        }
        return bytes;
    }

    private static byte[] BuildPdf(byte[] data, string? filter, int width = 1, int height = 1, int bits = 8) {
        using var stream = new MemoryStream();
        var filterEntry = filter is null ? string.Empty : "/Filter " + filter;
        var prefix = Encoding.ASCII.GetBytes($"%PDF-1.4\n1 0 obj\n<< /Type /XObject /Subtype /Image /Width {width} /Height {height} /BitsPerComponent {bits} /ColorSpace /DeviceGray {filterEntry} /Length {data.Length} >>\nstream\n");
        stream.Write(prefix, 0, prefix.Length);
        stream.Write(data, 0, data.Length);
        var suffix = Encoding.ASCII.GetBytes("\nendstream\nendobj\n%%EOF\n");
        stream.Write(suffix, 0, suffix.Length);
        return stream.ToArray();
    }

    private static byte[] Deflate(byte[] bytes) {
        using var stream = new MemoryStream();
        stream.WriteByte(0x78);
        stream.WriteByte(0x9C);
        using (var deflate = new DeflateStream(stream, CompressionLevel.Optimal, leaveOpen: true))
            deflate.Write(bytes, 0, bytes.Length);
        uint a = 1, b = 0;
        foreach (var value in bytes) { a = (a + value) % 65521; b = (b + a) % 65521; }
        var checksum = (b << 16) | a;
        for (var shift = 24; shift >= 0; shift -= 8) stream.WriteByte((byte)(checksum >> shift));
        return stream.ToArray();
    }

    private static byte[] PackBits(int count, byte value) {
        using var stream = new MemoryStream();
        while (count > 0) {
            var run = Math.Min(128, count);
            stream.WriteByte((byte)(257 - run));
            stream.WriteByte(value);
            count -= run;
        }
        return stream.ToArray();
    }

    private static byte[] BuildTiffTile(int side, ushort compression, byte[] data) {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        const int count = 10;
        writer.Write((ushort)0x4949); writer.Write((ushort)42); writer.Write(8u);
        writer.Write((ushort)count);
        Tag(256, 4, 1); Tag(257, 4, 1); Tag(258, 3, 8); Tag(259, 3, compression);
        Tag(262, 3, 1); Tag(277, 3, 1); Tag(322, 4, (uint)side); Tag(323, 4, (uint)side);
        Tag(324, 4, 8 + 2 + count * 12 + 4); Tag(325, 4, (uint)data.Length);
        writer.Write(0u); writer.Write(data);
        return stream.ToArray();
        void Tag(ushort tag, ushort type, uint value) {
            writer.Write(tag); writer.Write(type); writer.Write(1u); writer.Write(value);
        }
    }

    private sealed class CountingStream : Stream {
        private readonly byte[] _data;
        internal CountingStream(byte[] data) => _data = data;
        internal int Consumed { get; private set; }
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => Consumed; set => throw new NotSupportedException(); }
        public override int Read(byte[] buffer, int offset, int count) {
            count = Math.Min(count, _data.Length - Consumed);
            Array.Copy(_data, Consumed, buffer, offset, count);
            Consumed += count;
            return count;
        }
        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
