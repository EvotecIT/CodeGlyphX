using System;
using System.Buffers;
using System.IO;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class PngOutputBufferTests {
    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void QrStream_TranslucentPixelsMatchArrayAfterDirtyPool(int compression) {
        var modules = QR.Encode("PNG-TRANSLUCENT-STREAM").Modules;
        var options = new QrPngRenderOptions {
            ModuleSize = 3, QuietZone = 4, PngCompressionLevel = compression,
            Foreground = new Rgba32(10, 20, 30, 140), Background = new Rgba32(220, 235, 245, 170)
        };
        var expected = ImageReader.DecodeRgba32(QrPngRenderer.Render(modules, options), out var width, out var height);
        var dirty = ArrayPool<byte>.Shared.Rent(height * (width * 4 + 1));
        for (var i = 0; i < dirty.Length; i++) dirty[i] = 83;
        ArrayPool<byte>.Shared.Return(dirty);

        using var stream = new MemoryStream();
        QrPngRenderer.RenderToStream(modules, options, stream);
        Assert.True(stream.CanWrite);
        Assert.Equal(expected, ImageReader.DecodeRgba32(stream.ToArray(), out var actualWidth, out var actualHeight));
        Assert.Equal(width, actualWidth);
        Assert.Equal(height, actualHeight);
    }

    [Theory]
    [InlineData(16384, 2, 0)]
    [InlineData(17, 5, 6)]
    public void RowWriter_PreservesEveryPixelAcrossRowsAndStoredBlockBoundaries(int width, int height, int compression) {
        var rowLength = width * 4 + 1;
        var expected = new byte[width * height * 4];
        using var stream = new MemoryStream();
        PngWriter.WriteRgba8(stream, width, height, (y, row, length) => {
            row[0] = 0;
            for (var x = 1; x < length; x++) {
                var value = (byte)(x * 17 + y * 43);
                row[x] = value;
                expected[y * (rowLength - 1) + x - 1] = value;
            }
        }, compression);
        Assert.Equal(expected, ImageReader.DecodeRgba32(stream.ToArray(), out var actualWidth, out var actualHeight));
        Assert.Equal(width, actualWidth);
        Assert.Equal(height, actualHeight);
    }

    [Theory]
    [InlineData(1, 1, 0, 0)]
    [InlineData(17, 5, 0, 300)]
    [InlineData(128, 128, 0, 0)]
    [InlineData(128, 128, 0, 300)]
    [InlineData(17, 5, 1, 300)]
    [InlineData(17, 5, 9, 0)]
    public void RgbaArrayOutput_PreservesStreamBytesPixelsAndPhysicalMetadata(int width, int height, int compression, int dpi) {
        var stride = width * 4 + 1;
        var rows = new byte[height * stride];
        var expected = new byte[width * height * 4];
        for (var y = 0; y < height; y++) for (var x = 0; x < width; x++) {
            var at = (y * width + x) * 4;
            expected[at] = (byte)(x * 17 + y * 31);
            expected[at + 1] = (byte)(x * 43 + y * 13);
            expected[at + 2] = (byte)(x * 7 + y * 59);
            expected[at + 3] = (byte)((x & 1) == 0 ? 127 : 255);
            for (var channel = 0; channel < 4; channel++) rows[y * stride + 1 + x * 4 + channel] = expected[at + channel];
        }

        using var stream = new MemoryStream();
        PngWriter.WriteRgba8(stream, width, height, rows, rows.Length, compression, dpi);
        var encoded = PngWriter.WriteRgba8(width, height, rows, rows.Length, compression, dpi);

        Assert.True(stream.CanWrite);
        Assert.Equal(stream.ToArray(), encoded);
        if (compression == 0 && dpi == 0) {
            Assert.Equal(encoded, PngWriter.WriteRgba8(width, height, rows));
            Assert.Equal(encoded, PngWriter.WriteRgba8(width, height, rows, rows.Length));
        }
        Assert.Equal(expected, ImageReader.DecodeRgba32(encoded, out var decodedWidth, out var decodedHeight));
        Assert.Equal(width, decodedWidth);
        Assert.Equal(height, decodedHeight);
    }

    [Theory]
    [InlineData(false, 17, 5, 0)]
    [InlineData(true, 17, 5, 0)]
    [InlineData(false, 513, 1024, 0)]
    [InlineData(true, 513, 1024, 0)]
    [InlineData(false, 17, 5, 1)]
    [InlineData(true, 17, 5, 1)]
    [InlineData(false, 17, 5, 9)]
    [InlineData(true, 17, 5, 9)]
    public void BinaryArrayOutput_PreservesStreamBytesPackedRowsAndPalette(bool indexed, int width, int height, int compression) {
        var stride = (width + 7) / 8 + 1;
        var rows = new byte[height * stride];
        var expected = new byte[width * height * 4];
        var palette = new byte[] { 9, 80, 130, 210, 60, 180 };
        for (var y = 0; y < height; y++) for (var x = 0; x < width; x++) {
            var bit = (x + y * 3) % 5 < 2;
            if (bit) rows[y * stride + 1 + x / 8] |= (byte)(1 << (7 - (x & 7)));
            var at = (y * width + x) * 4;
            for (var channel = 0; channel < 3; channel++) expected[at + channel] = indexed
                ? palette[(bit ? 3 : 0) + channel] : bit ? (byte)255 : (byte)0;
            expected[at + 3] = 255;
        }

        using var stream = new MemoryStream();
        byte[] encoded;
        if (indexed) {
            PngWriter.WriteIndexed1(stream, width, height, rows, rows.Length, palette, compression);
            encoded = PngWriter.WriteIndexed1(width, height, rows, rows.Length, palette, compression);
            if (compression == 0) Assert.Equal(encoded, PngWriter.WriteIndexed1(width, height, rows, rows.Length, palette));
        } else {
            PngWriter.WriteGray1(stream, width, height, rows, rows.Length, compression);
            encoded = PngWriter.WriteGray1(width, height, rows, rows.Length, compression);
            if (compression == 0) Assert.Equal(encoded, PngWriter.WriteGray1(width, height, rows, rows.Length));
        }

        Assert.True(stream.CanWrite);
        Assert.Equal(stream.ToArray(), encoded);
        Assert.Equal(expected, ImageReader.DecodeRgba32(encoded, out var decodedWidth, out var decodedHeight));
        Assert.Equal(width, decodedWidth);
        Assert.Equal(height, decodedHeight);
    }
}
