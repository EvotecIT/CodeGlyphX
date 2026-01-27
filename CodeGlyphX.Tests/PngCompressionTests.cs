using System.IO;
using CodeGlyphX;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class PngCompressionTests {
    [Fact]
    public void WriteRgba8_CompressionLevel_ReducesSizeAndDecodes() {
        const int width = 512;
        const int height = 512;
        var stride = width * 4;
        var rowLength = stride + 1;
        var scanlines = new byte[height * rowLength];

        for (var y = 0; y < height; y++) {
            var rowStart = y * rowLength;
            scanlines[rowStart] = 0;
            var pixel = rowStart + 1;
            for (var x = 0; x < width; x++) {
                scanlines[pixel + 0] = 255;
                scanlines[pixel + 1] = 255;
                scanlines[pixel + 2] = 255;
                scanlines[pixel + 3] = 255;
                pixel += 4;
            }
        }

        var uncompressed = PngWriter.WriteRgba8(width, height, scanlines, scanlines.Length);

        using var compressedStream = new MemoryStream();
        PngWriter.WriteRgba8(compressedStream, width, height, scanlines, scanlines.Length, compressionLevel: 6);
        var compressed = compressedStream.ToArray();

        Assert.True(compressed.Length < uncompressed.Length, $"Expected compressed PNG to be smaller. Uncompressed={uncompressed.Length}, Compressed={compressed.Length}");
        Assert.True(ImageReader.TryDecodeRgba32(compressed, out var rgba, out var decodedWidth, out var decodedHeight));
        Assert.Equal(width, decodedWidth);
        Assert.Equal(height, decodedHeight);
        Assert.Equal(255, rgba[0]);
        Assert.Equal(255, rgba[1]);
        Assert.Equal(255, rgba[2]);
        Assert.Equal(255, rgba[3]);
    }

    [Fact]
    public void QrRenderToStream_CompressedRowWriter_Decodes() {
        var qr = QrEasy.Encode("PNG-ROW-WRITER").Modules;
        var opts = new QrPngRenderOptions {
            ModuleSize = 8,
            QuietZone = 4,
            PngCompressionLevel = 6
        };

        using var ms = new MemoryStream();
        QrPngRenderer.RenderToStream(qr, opts, ms);
        var bytes = ms.ToArray();

        Assert.True(ImageReader.TryDecodeRgba32(bytes, out _, out var width, out var height));
        Assert.True(width > 0);
        Assert.True(height > 0);
    }
}
