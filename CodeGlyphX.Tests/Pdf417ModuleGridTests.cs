using System;
using System.Threading;
using CodeGlyphX.Internal;
using CodeGlyphX.Pdf417;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class Pdf417ModuleGridTests {
    [Theory]
    [InlineData(1, 65)]
    [InlineData(65, 1)]
    [InlineData(19, 23)]
    [InlineData(33, 31)]
    public void ComposedRegions_MatchMaterializedCropsAndOrientations(int width, int height) {
        var matrix = new BitMatrix(width, height);
        for (var y = 0; y < height; y++) for (var x = 0; x < width; x++) matrix[x, y] = (x * 7 + y * 11) % 5 < 2;
        var original = matrix.ToPackedBytes();
        matrix.Freeze();
        var grid = new Pdf417Decoder.ModuleGrid(matrix);
        for (var rotation = 0; rotation < 4; rotation++) for (var mirror = 0; mirror < 2; mirror++) {
            var expected = Rotate(matrix, rotation);
            var actual = grid.Rotate(rotation);
            if (mirror != 0) { expected = BitMatrixTransforms.MirrorHorizontal(expected); actual = actual.Mirror(); }
            var left = expected.Width > 3 ? 2 : 0;
            var top = expected.Height > 3 ? 1 : 0;
            var cropped = new BitMatrix(expected.Width - left, expected.Height - top);
            for (var y = 0; y < cropped.Height; y++) for (var x = 0; x < cropped.Width; x++) cropped[x, y] = expected[x + left, y + top];
            var region = actual.Crop(left, top, cropped.Width, cropped.Height);
            for (var retry = 0; retry < 4; retry++) {
                AssertCells(Rotate(cropped, retry), region.Rotate(retry));
                AssertCells(BitMatrixTransforms.MirrorHorizontal(Rotate(cropped, retry)), region.Rotate(retry).Mirror());
            }
        }
        Assert.Equal(original, matrix.ToPackedBytes());
        Assert.True(matrix.IsReadOnly);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void RegularAndShearedSamples_MatchScalarPixelOracle(bool bgra, bool invert) {
        const int width = 13, height = 11, stride = width * 4 + 12;
        var pixels = Pixels(width, height, stride, bgra);
        var original = (byte[])pixels.Clone();
        var format = bgra ? PixelFormat.Bgra32 : PixelFormat.Rgba32;
        for (var round = 0; round < 2; round++) for (var shearCase = 0; shearCase < 3; shearCase++) {
            var sheared = shearCase != 0;
            var shear = shearCase == 1 ? 0.27 : -0.19;
            const double left = -2.25, top = 0.5, stepX = 1.5, stepY = 1.25;
            var grid = Pdf417Decoder.ModuleGrid.Regular(pixels, width, height, stride, format, left, top, 19, 7,
                stepX, stepY, round != 0, 127, invert, CancellationToken.None, sheared, shear);
            var expected = new BitMatrix(19, 7);
            for (var y = 0; y < 7; y++) for (var x = 0; x < 19; x++) {
                var rowShift = (y - 3.0) * shear * stepX;
                var px = sheared ? left + rowShift + x * stepX + stepX / 2.0 : left + (x + 0.5) * stepX;
                var py = sheared ? top + y * stepY + stepY / 2.0 : top + (y + 0.5) * stepY;
                var sx = Math.Max(0, Math.Min(width - 1, (int)(round != 0 ? Math.Round(px) : Math.Floor(px))));
                var sy = Math.Max(0, Math.Min(height - 1, (int)(round != 0 ? Math.Round(py) : Math.Floor(py))));
                expected[x, y] = (Luminance(pixels, stride, sx, sy, bgra) < 127) ^ invert;
            }
            AssertCells(expected, grid);
            AssertCells(BitMatrixTransforms.Rotate90(expected), grid.Rotate(1));
            AssertCells(BitMatrixTransforms.MirrorHorizontal(expected), grid.Mirror());
        }
        Assert.Equal(original, pixels);
    }

#if NET8_0_OR_GREATER
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void PerspectiveSamples_MatchNeighborhoodOracleIncludingImageEdges(bool bgra, bool invert) {
        const int width = 13, height = 11, stride = width * 4 + 12;
        var pixels = Pixels(width, height, stride, bgra);
        var format = bgra ? PixelFormat.Bgra32 : PixelFormat.Rgba32;
        var transform = CodeGlyphX.Qr.QrPerspectiveTransform.QuadrilateralToQuadrilateral(
            0, 0, 18, 0, 18, 6, 0, 6, -2, -1, 14, 1, 12, 12, 1, 10);
        var grid = Pdf417Decoder.ModuleGrid.Projected(pixels, width, height, stride, format, 19, 7, 127, invert,
            transform, -0.25, 0.25, CancellationToken.None);
        var expected = new BitMatrix(19, 7);
        for (var y = 0; y < 7; y++) for (var x = 0; x < 19; x++) {
            transform.Transform(x + 0.25, y + 0.75, out var px, out var py);
            var cx = (int)Math.Round(px); var cy = (int)Math.Round(py);
            var total = 0; var count = 0;
            for (var yy = cy - 1; yy <= cy + 1; yy++) for (var xx = cx - 1; xx <= cx + 1; xx++) {
                if (xx < 0 || yy < 0 || xx >= width || yy >= height) continue;
                total += Luminance(pixels, stride, xx, yy, bgra); count++;
            }
            expected[x, y] = (count != 0 && total / count < 127) ^ invert;
        }
        AssertCells(expected, grid);
        AssertCells(BitMatrixTransforms.Rotate270(expected), grid.Rotate(3));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UndefinedPerspectiveSamples_RemainLightWhenInverted(bool invert) {
        var transform = CodeGlyphX.Qr.QrPerspectiveTransform.QuadrilateralToQuadrilateral(
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 18, 0, 18, 6, 0, 6);
        transform.Transform(0.5, 0.5, out var px, out var py);
        Assert.True(double.IsNaN(px) || double.IsNaN(py));
        var grid = Pdf417Decoder.ModuleGrid.Projected(new byte[4], 1, 1, 4, PixelFormat.Rgba32,
            19, 7, 128, invert, transform, 0, 0, CancellationToken.None);
        AssertCells(new BitMatrix(19, 7), grid);
    }
#endif

    [Fact]
    public void Cancellation_StopsReadsThroughAlreadySampledViews() {
        var pixels = new byte[4 * 64];
        for (var x = 0; x < 64; x++) pixels[x * 4 + 3] = 255;
        using var token = new CancellationTokenSource();
        var grid = Pdf417Decoder.ModuleGrid.Regular(pixels, 64, 1, 256, PixelFormat.Rgba32,
            0, 0, 64, 1, 1, 1, false, 128, false, token.Token);
        for (var x = 0; x < 64; x++) Assert.True(grid[x, 0]);
        token.Cancel();
        var mirror = grid.Mirror();
        for (var x = 0; x < 32; x++) _ = mirror[x, 0];
        Assert.True(grid.IsAborted);
        Assert.True(mirror.IsAborted);
        Assert.False(grid[0, 0]);
    }

    private static void AssertCells(BitMatrix expected, Pdf417Decoder.ModuleGrid actual) {
        Assert.Equal(expected.Width, actual.Width); Assert.Equal(expected.Height, actual.Height);
        for (var y = expected.Height - 1; y >= 0; y--) for (var x = expected.Width - 1; x >= 0; x--)
            Assert.Equal(expected[x, y], actual[x, y]);
    }

    private static BitMatrix Rotate(BitMatrix matrix, int turns) => turns switch {
        1 => BitMatrixTransforms.Rotate90(matrix), 2 => BitMatrixTransforms.Rotate180(matrix),
        3 => BitMatrixTransforms.Rotate270(matrix), _ => matrix
    };

    private static byte[] Pixels(int width, int height, int stride, bool bgra) {
        var pixels = new byte[stride * height];
        for (var y = 0; y < height; y++) for (var x = 0; x < width; x++) {
            var p = y * stride + x * 4;
            pixels[p + (bgra ? 2 : 0)] = (byte)(x * 17 + y * 31);
            pixels[p + 1] = (byte)(x * 37 + y * 13);
            pixels[p + (bgra ? 0 : 2)] = (byte)(x * 11 + y * 7);
            pixels[p + 3] = (byte)(x % 4 == 0 ? 255 : x * 29 + y * 11);
        }
        return pixels;
    }

    private static int Luminance(byte[] pixels, int stride, int x, int y, bool bgra) {
        var p = y * stride + x * 4;
        var lum = (pixels[p + (bgra ? 2 : 0)] * 77 + pixels[p + 1] * 150 + pixels[p + (bgra ? 0 : 2)] * 29) / 256;
        var alpha = pixels[p + 3];
        return alpha == 255 ? lum : (lum * alpha + 255 * (255 - alpha) + 127) / 255;
    }
}
