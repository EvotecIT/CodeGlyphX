using System;
using System.Reflection;
using CodeGlyphX.Internal;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class RetailDigitFitTests {
    private delegate bool Normalize(ReadOnlySpan<int> runs, int start, int parity, out int[] pattern, out double fit);

    // Odd-family EAN/UPC run widths, in digit order. Even-family widths reverse
    // each sequence. This oracle does not consume the production encoding table.
    private static readonly int[][] Odd = {
        new[] { 3, 2, 1, 1 }, new[] { 2, 2, 2, 1 }, new[] { 2, 1, 2, 2 },
        new[] { 1, 4, 1, 1 }, new[] { 1, 1, 3, 2 }, new[] { 1, 2, 3, 1 },
        new[] { 1, 1, 1, 4 }, new[] { 1, 3, 1, 2 }, new[] { 1, 2, 1, 3 }, new[] { 3, 1, 1, 2 }
    };

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    public void DigitFitsPreserveResidualLimitsParityTiesAndRejectedWidths(int parity) {
        var normalize = (Normalize)typeof(BarcodeScanline).GetMethod("TryNormalizeRetailDigit", BindingFlags.Static | BindingFlags.NonPublic)!
            .CreateDelegate(typeof(Normalize));
        var runs = new int[6];
        runs[0] = runs[5] = 100;
        // Cover small blurred widths, equal-error ties and invalid patterns.
        for (var a = 1; a <= 12; a++) for (var b = 1; b <= 12; b++)
            for (var c = 1; c <= 12; c++) for (var d = 1; d <= 12; d++) {
                runs[1] = a; runs[2] = b; runs[3] = c; runs[4] = d;
                Check();
            }
        // Larger pitches change the scale of the constrained tie tolerance.
        uint state = 0x75B9D21C;
        for (var i = 0; i < 20000; i++) {
            for (var run = 1; run <= 4; run++) {
                state = state * 1664525 + 1013904223;
                runs[run] = 1 + (int)(state % 4096);
            }
            Check();
        }
        void Check() {
            var pitch = (runs[1] + runs[2] + runs[3] + runs[4]) / 7.0;
            var tolerance = parity < 0 ? 0 : pitch * pitch * 1e-12;
            var expectedFit = double.PositiveInfinity;
            var expected = Array.Empty<int>();
            for (var index = Math.Max(0, parity); index < 20; index += parity < 0 ? 1 : 2) {
                var pattern = Odd[index / 2];
                var error = 0.0;
                var valid = true;
                for (var at = 0; at < 4; at++) {
                    var modules = pattern[(index & 1) == 0 ? at : 3 - at];
                    var residual = Math.Abs(runs[1 + at] - modules * pitch);
                    if (residual > pitch * 0.7 + 0.5) { valid = false; break; }
                    error += residual * residual;
                }
                if (!valid || error + tolerance >= expectedFit) continue;
                expectedFit = error;
                expected = (int[])pattern.Clone();
                if ((index & 1) != 0) Array.Reverse(expected);
            }
            var accepted = normalize(runs, 1, parity, out var actual, out var actualFit);
            Assert.Equal(expected.Length != 0 && expectedFit / (pitch * pitch) < 1.0, accepted);
            Assert.Equal(expected, actual);
            Assert.Equal(expectedFit, actualFit);
        }
    }
}
