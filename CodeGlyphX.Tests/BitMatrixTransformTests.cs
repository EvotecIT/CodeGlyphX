using CodeGlyphX.Internal;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class BitMatrixTransformTests {
    [Theory]
    [InlineData(1, 1)]
    [InlineData(1, 65)]
    [InlineData(65, 1)]
    [InlineData(19, 23)]
    [InlineData(33, 31)]
    [InlineData(151, 151)]
    public void Transforms_PreserveEveryCellAndFrozenSource(int width, int height) {
        var source = new BitMatrix(width, height);
        for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++) source[x, y] = (x * 7 + y * 11) % 5 < 2;
        var original = source.ToPackedBytes();
        source.Freeze();

        var clockwise = BitMatrixTransforms.Rotate90(source);
        var halfTurn = BitMatrixTransforms.Rotate180(source);
        var anticlockwise = BitMatrixTransforms.Rotate270(source);
        var mirrored = BitMatrixTransforms.MirrorHorizontal(source);

        Assert.Equal(height, clockwise.Width);
        Assert.Equal(width, clockwise.Height);
        Assert.Equal(height, anticlockwise.Width);
        Assert.Equal(width, anticlockwise.Height);
        Assert.Equal(width, halfTurn.Width);
        Assert.Equal(height, halfTurn.Height);
        Assert.Equal(width, mirrored.Width);
        Assert.Equal(height, mirrored.Height);
        for (var y = 0; y < height; y++) {
            for (var x = 0; x < width; x++) {
                Assert.Equal(source[x, y], clockwise[height - 1 - y, x]);
                Assert.Equal(source[x, y], halfTurn[width - 1 - x, height - 1 - y]);
                Assert.Equal(source[x, y], anticlockwise[y, width - 1 - x]);
                Assert.Equal(source[x, y], mirrored[width - 1 - x, y]);
            }
        }
        Assert.Equal(original, source.ToPackedBytes());
        Assert.True(source.IsReadOnly);
        Assert.False(clockwise.IsReadOnly);

        for (var mirror = 0; mirror < 2; mirror++) {
            var input = mirror == 0 ? source : mirrored;
            var expected = new[] { input, BitMatrixTransforms.Rotate90(input),
                BitMatrixTransforms.Rotate180(input), BitMatrixTransforms.Rotate270(input) };
            for (var rotation = 0; rotation < 4; rotation++) {
                var view = new BitMatrixView(source, rotation, mirror != 0);
                Assert.Equal(expected[rotation].Width, view.Width);
                Assert.Equal(expected[rotation].Height, view.Height);
                for (var y = 0; y < view.Height; y++)
                    for (var x = 0; x < view.Width; x++) Assert.Equal(expected[rotation][x, y], view.Get(x, y));
                Assert.Equal(expected[rotation].ToPackedBytes(), view.ToMatrix().ToPackedBytes());
                var sampled = new BitMatrixView(width, height, source.Get).Orient(rotation, mirror != 0);
                Assert.Equal(expected[rotation].ToPackedBytes(), sampled.ToMatrix().ToPackedBytes());
            }
        }
    }
}
