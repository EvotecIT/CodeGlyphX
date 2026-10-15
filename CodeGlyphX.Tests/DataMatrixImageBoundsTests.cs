using CodeGlyphX.DataMatrix;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class DataMatrixImageBoundsTests {
    [Theory]
    [InlineData(PixelFormat.Rgba32)]
    [InlineData(PixelFormat.Bgra32)]
    public void OffsetPixels_WithPaddedStride_PreserveGs1Metadata(PixelFormat format) {
        var payload = "0109501101020917" + Gs1.GroupSeparator + "10LOT42";
        var matrix = DataMatrixEncoder.Encode(payload, new DataMatrixEncodingOptions {
            Rows = 16, Columns = 48, IsGs1 = true, Mode = DataMatrixEncodingMode.Ascii
        });
        var source = MatrixPngRenderer.RenderPixels(matrix,
            new MatrixPngRenderOptions { ModuleSize = 3, QuietZone = 0 },
            out var symbolWidth, out var symbolHeight, out var sourceStride);
        const int left = 19;
        const int top = 7;
        var width = left + symbolWidth + 11;
        var height = top + symbolHeight + 23;
        var stride = width * 4 + 12;
        var pixels = new byte[stride * height];
        // Transparent colored margins composite to white. The row padding is
        // opaque black so accidentally treating it as pixels changes the bounds.
        for (var y = 0; y < height; y++) {
            for (var x = 0; x < width; x++) pixels[y * stride + x * 4] = 200;
            for (var at = width * 4 + 3; at < stride; at += 4) pixels[y * stride + at] = 255;
            for (var x = 0; x < symbolWidth; x++) {
                if (y < top || y >= top + symbolHeight) continue;
                var from = (y - top) * sourceStride + x * 4;
                var to = y * stride + (left + x) * 4;
                pixels[to] = source[from + (format == PixelFormat.Bgra32 ? 2 : 0)];
                pixels[to + 1] = source[from + 1];
                pixels[to + 2] = source[from + (format == PixelFormat.Bgra32 ? 0 : 2)];
                pixels[to + 3] = source[from + 3];
            }
        }

        Assert.True(DataMatrixDecoder.TryDecodeDetailed(pixels, width, height, stride, format, out var decoded));
        Assert.Equal(payload, decoded.Text);
        Assert.True(decoded.IsGs1);
        Assert.Equal(16, decoded.Rows);
        Assert.Equal(48, decoded.Columns);
    }
}
