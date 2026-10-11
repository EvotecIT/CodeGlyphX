using CodeGlyphX.DataMatrix;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class DataMatrixOrientationTests {
    [Theory]
    [InlineData(26, 26, false)]
    [InlineData(16, 48, false)]
    [InlineData(8, 64, true)]
    public void PixelOrientations_PreservePayloadAndGs1SymbolMetadata(int rows, int columns, bool dmre) {
        var payload = "0109501101020917" + Gs1.GroupSeparator + "10LOT42";
        var source = DataMatrixEncoder.Encode(payload, new DataMatrixEncodingOptions {
            Rows = rows, Columns = columns, IsGs1 = true, Mode = DataMatrixEncodingMode.Ascii
        });
        var original = source.ToPackedBytes();
        source.Freeze();

        for (var mirror = 0; mirror < 2; mirror++) {
            for (var rotation = 0; rotation < 4; rotation++) {
                // Construct the input independently of the decoder's packed transforms.
                var swapped = (rotation & 1) != 0;
                var oriented = new BitMatrix(swapped ? rows : columns, swapped ? columns : rows);
                for (var y = 0; y < rows; y++) {
                    for (var x = 0; x < columns; x++) {
                        var mx = mirror == 0 ? x : columns - 1 - x;
                        var tx = rotation == 0 ? mx : rotation == 1 ? rows - 1 - y
                            : rotation == 2 ? columns - 1 - mx : y;
                        var ty = rotation == 0 ? y : rotation == 1 ? mx
                            : rotation == 2 ? rows - 1 - y : columns - 1 - mx;
                        oriented[tx, ty] = source[x, y];
                    }
                }
                var pixels = MatrixPngRenderer.RenderPixels(oriented,
                    new MatrixPngRenderOptions { ModuleSize = 4, QuietZone = 2 },
                    out var width, out var height, out var stride);

                Assert.True(DataMatrixDecoder.TryDecode(pixels, width, height, stride, PixelFormat.Rgba32, out var text));
                Assert.Equal(payload, text);
                Assert.True(DataMatrixDecoder.TryDecode(pixels, width, height, stride, PixelFormat.Rgba32,
                    out text, out var diagnostics), diagnostics.Failure);
                Assert.Equal(payload, text);
                Assert.True(diagnostics.Success);
                Assert.Null(diagnostics.Failure);
                Assert.True(DataMatrixDecoder.TryDecodeDetailed(pixels, width, height, stride, PixelFormat.Rgba32, out var decoded));
                Assert.Equal(payload, decoded.Text);
                Assert.True(decoded.IsGs1);
                Assert.Equal(dmre, decoded.IsDmre);
                Assert.Equal(rows, decoded.Rows);
                Assert.Equal(columns, decoded.Columns);
            }
        }
        Assert.Equal(original, source.ToPackedBytes());
    }
}
