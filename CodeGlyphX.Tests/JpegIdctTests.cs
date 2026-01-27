using System;
using System.Reflection;
using CodeGlyphX.Rendering.Jpeg;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class JpegIdctTests {
    [Fact]
    public void InverseDct_DcOnly_ProducesConstant() {
        var coeffs = new int[64];
        var pixels = new int[64];
        coeffs[0] = -1024;

        var method = typeof(JpegReader).GetMethod("InverseDct", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        method!.Invoke(null, new object[] { coeffs, pixels });

        for (var i = 0; i < pixels.Length; i++) {
            Assert.InRange(pixels[i], 0, 1);
        }
    }

    [Fact]
    public void InverseDct_IntegerPath_TracksReferenceWithinTolerance() {
        var method = typeof(JpegReader).GetMethod("InverseDct", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var rng = new Random(12345);
        var coeffs = new int[64];
        var actual = new int[64];

        for (var iter = 0; iter < 200; iter++) {
            for (var i = 0; i < coeffs.Length; i++) {
                var u = i / 8;
                var v = i % 8;
                var weight = 1 + u + v;
                coeffs[i] = rng.Next(-512, 513) / weight;
            }

            method!.Invoke(null, new object[] { coeffs, actual });
            var expected = ReferenceInverseDct(coeffs);

            for (var i = 0; i < actual.Length; i++) {
                var diff = Math.Abs(actual[i] - expected[i]);
                var midRange = expected[i] > 8 && expected[i] < 247;
                var tolerance = midRange ? 4 : 24;
                Assert.True(diff <= tolerance, $"IDCT drift too large at iter={iter}, idx={i}: actual={actual[i]}, expected={expected[i]}, diff={diff}, tol={tolerance}");
            }
        }
    }

    private static int[] ReferenceInverseDct(int[] input) {
        var cos = BuildCosTable();
        Span<double> temp = stackalloc double[64];
        var output = new int[64];

        for (var y = 0; y < 8; y++) {
            var row = y * 8;
            for (var x = 0; x < 8; x++) {
                double sum = 0;
                for (var u = 0; u < 8; u++) {
                    var cu = u == 0 ? 0.7071067811865476 : 1.0;
                    sum += cu * input[row + u] * cos[u, x];
                }
                temp[row + x] = sum * 0.5;
            }
        }

        for (var x = 0; x < 8; x++) {
            for (var y = 0; y < 8; y++) {
                double sum = 0;
                for (var v = 0; v < 8; v++) {
                    var cv = v == 0 ? 0.7071067811865476 : 1.0;
                    sum += cv * temp[v * 8 + x] * cos[v, y];
                }
                output[y * 8 + x] = ClampToByte(sum * 0.5 + 128.0);
            }
        }

        return output;
    }

    private static double[,] BuildCosTable() {
        var table = new double[8, 8];
        for (var u = 0; u < 8; u++) {
            for (var x = 0; x < 8; x++) {
                table[u, x] = Math.Cos(((2 * x + 1) * u * Math.PI) / 16.0);
            }
        }
        return table;
    }

    private static int ClampToByte(double value) {
        if (value <= 0) return 0;
        if (value >= 255) return 255;
        return (int)(value + 0.5);
    }
}
