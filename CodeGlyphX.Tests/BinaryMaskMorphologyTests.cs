using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CodeGlyphX.Internal;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class BinaryMaskMorphologyTests {
    [Theory]
    [InlineData(1, 1)]
    [InlineData(1, 9)]
    [InlineData(9, 1)]
    [InlineData(3, 3)]
    [InlineData(17, 9)]
    [InlineData(257, 3)]
    public void SquareFilter_PreservesEachBoundaryRuleAndOnlyWritesItsDestination(int width, int height) {
        foreach (var pattern in new[] { 0, 1, 2 }) {
            var source = new byte[width * height];
            for (var i = 0; i < source.Length; i++) source[i] = pattern == 0 ? (byte)0 : pattern == 1 ? (byte)255 : i % 5 == 0 ? (byte)0 : (byte)(i % 255 + 1);
            var original = (byte[])source.Clone();
            foreach (var radius in new[] { 0, 1, 2, 3 })
            foreach (var erode in new[] { false, true })
            foreach (var zeroOutside in new[] { false, true }) {
                var dirty = ArrayPool<int>.Shared.Rent(width);
                for (var i = 0; i < dirty.Length; i++) dirty[i] = int.MaxValue;
                ArrayPool<int>.Shared.Return(dirty);
                var storage = Enumerable.Repeat((byte)213, source.Length + 16).ToArray();
                Assert.True(BinaryMaskMorphology.FilterSquare(source, storage.AsSpan(8, source.Length),
                    width, height, radius, erode, zeroOutside, CancellationToken.None, default));
                Assert.Equal(Expected(source, width, height, radius, erode, zeroOutside), storage.AsSpan(8, source.Length).ToArray());
                Assert.Equal(original, source);
                Assert.All(storage.Take(8).Concat(storage.Skip(8 + source.Length)), value => Assert.Equal((byte)213, value));
            }
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SquareFilter_RejectsCancelledOrExpiredWorkBeforeChangingTheDestination(bool expired) {
        using var cancellation = new CancellationTokenSource();
        using var scope = expired ? DecodeBudget.Begin(1) : null;
        if (expired) Assert.True(SpinWait.SpinUntil(() => DecodeBudget.IsExpired, 1000));
        else cancellation.Cancel();
        var source = Enumerable.Repeat((byte)1, 32 * 17).ToArray();
        var output = Enumerable.Repeat((byte)213, source.Length).ToArray();
        Assert.False(BinaryMaskMorphology.FilterSquare(source, output, 32, 17, 3,
            erode: true, zeroOutside: true, cancellation.Token, DecodeBudget.Capture()));
        Assert.All(output, value => Assert.Equal((byte)213, value));
    }

    private static byte[] Expected(byte[] source, int width, int height, int radius, bool erode, bool zeroOutside) {
        var output = new byte[source.Length];
        for (var y = 0; y < height; y++) for (var x = 0; x < width; x++) {
            // The mathematical neighborhood definition is independent of the
            // production moving column counts and pooled working row.
            var neighbors = new List<bool>();
            foreach (var dy in Enumerable.Range(-radius, radius * 2 + 1))
            foreach (var dx in Enumerable.Range(-radius, radius * 2 + 1)) {
                var sx = x + dx; var sy = y + dy;
                if (sx >= 0 && sx < width && sy >= 0 && sy < height) neighbors.Add(source[sy * width + sx] != 0);
                else if (zeroOutside) neighbors.Add(false);
            }
            output[y * width + x] = (erode ? neighbors.All(value => value) : neighbors.Any(value => value)) ? (byte)1 : (byte)0;
        }
        return output;
    }
}
